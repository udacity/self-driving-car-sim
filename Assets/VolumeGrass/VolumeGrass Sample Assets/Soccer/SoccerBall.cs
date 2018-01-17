using UnityEngine;
using System.Collections;

public class SoccerBall : MonoBehaviour {
	private LayerMask ballMask;
	public VolumeGrass grass;
	private Light dlight;
	private bool L_downflag=false;
	
	void Awake() {
		ballMask=1<<gameObject.layer;
		grass=(VolumeGrass)GameObject.FindObjectOfType(typeof(VolumeGrass));
		dlight=GameObject.Find("Directional light").GetComponent<Light>();
	}
	
	void OnGUI() {
		GUI.Box(new Rect (5,96,360,42),"");
		GUILayout.BeginArea(new Rect (5,98,350,40));
			GUILayout.Label("   click to kick, hold SPACE to freeze camera orbiting");
			GUILayout.Space(-8);
			GUILayout.Label("   L - toggle realtime shadow");
		GUILayout.EndArea();
	}
	
	void OnMouseDown() {
		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		RaycastHit hit;
		if (Physics.Raycast(ray, out hit, 1000, ballMask)) {
			Vector3 dir=GetComponent<Rigidbody>().position-hit.point;
			dir.Normalize();
			GetComponent<Rigidbody>().AddForceAtPosition(dir*250, hit.point);
			GetComponent<Rigidbody>().AddForce(Vector3.up*10);
		}
	}
	
	void Update() {
		if (Input.GetKey(KeyCode.L)) {
			if (!L_downflag) {
				L_downflag=true;
				if (dlight.shadows==LightShadows.None) {
					dlight.shadows=LightShadows.Soft;
				} else {
					dlight.shadows=LightShadows.None;
				}
			}
		} else {
			L_downflag=false;
		}
		
		Material mat=grass.GetComponent<Renderer>().material;
		Collider col=grass.GetComponent<Collider>();

		Ray ray = new Ray(GetComponent<Rigidbody>().position+Vector3.up, -Vector3.up);
		RaycastHit hit=new RaycastHit();
		if (col.Raycast(ray, out hit, 100f)) {
			float dmp=Mathf.Clamp(1-(GetComponent<Rigidbody>().position.y-0.3042075f)/0.35f,0,1);
			Vector4 pos=new Vector4(hit.textureCoord.x, hit.textureCoord.y, dmp*dmp, 0);
			mat.SetVector("_ballpos", pos);
			GetComponent<Rigidbody>().drag=dmp*1.0f;
			float v=GetComponent<Rigidbody>().velocity.magnitude*5.0f;
			dmp/=(v<1) ? 1 : v;
			GetComponent<Rigidbody>().angularDrag=dmp*1.0f;
		}
	}
	
}