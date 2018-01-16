using UnityEngine;
using System.Collections;

public class SteerCamTarget : MonoBehaviour {
	/*public float speed=0.1f;
	public float run_speed=0.5f;
	public LayerMask ground_layerMask=0;
	public GameObject target;
	public float xmin=10;
	public float xmax=10;
	public float zmin=190;
	public float zmax=190;
	public float mindist=0.5f;
	public float maxdist=6f;
	public float ground_offset=0.55f;
	private float act_speed=0;
	private Vector3 dir=Vector3.zero;
	
	// Use this for initialization
	void Start () {
		Vector3 pos=target.transform.position;
		Vector3 normal=Vector3.up;
		get_ground_pos(ref pos, ref normal, Vector3.up, ground_layerMask);
		target.transform.position=pos;
	}
	
	void OnGUI () {
		// Display label with two fractional digits
		GUI.Box(new Rect(5,5,360,60),"");
		GUILayout.Space(10);
		GUILayout.Label("    " + FPSmeter.fps.ToString("f2")+"   WSAD/ARROW - move (SHIFT - run)");
		GUILayout.Space(-8);
		GUILayout.Label("    Mouse - camera angle  , /. distance from followed target");
		GUILayout.Space(-8);
		GUILayout.Label("    ESC - quit for standalone player");
		GUILayout.Space(10);
		GUILayout.BeginArea(new Rect (5,70,100,100));
		if (GUILayout.Button("lock cursor", GUILayout.Width(100))) {
			Screen.lockCursor = true;
		}
		GUILayout.EndArea();
	}
	
	// Update is called once per frame
	void Update () {
		Vector3 pos=target.transform.position;
		Vector3 lpos=target.transform.position;
		Vector3 normal=Vector3.up;
		Camera camera=Camera.main;
		
		get_ground_pos(ref pos, ref normal, Vector3.up, ground_layerMask);
		Quaternion target_rot=target.transform.rotation;
		target_rot.eulerAngles=new Vector3(0, camera.transform.rotation.eulerAngles.y, 0);
		target.transform.rotation=target_rot;
		//Debug.Log(target.transform.rotation.eulerAngles);
		pos.x=lpos.x;
		pos.y=lpos.y;
		pos.z=lpos.z;
		get_ground_pos(ref pos, ref normal, normal, ground_layerMask);
		//pos=Vector3.Project(lpos, normal);
		Vector3 flat_forward=camera.transform.forward;
		flat_forward.y=0;
		MouseOrbitCS mo=gameObject.GetComponent<MouseOrbitCS>();
		mo.set_normal_angle(90-Vector3.Angle(normal, flat_forward));
			
		bool run=Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
		bool move=true;
		if (Input.GetKey(KeyCode.Escape)) {
			Application.Quit();
		}
		if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S)) {
			Vector3 vec=camera.transform.forward;
			vec.y=0; vec.Normalize();
			dir=Vector3.Cross(vec, normal);
			dir=Vector3.Cross(dir, normal);
		} else if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) {
			Vector3 vec=camera.transform.right;
			vec.y=0; vec.Normalize();
			dir=Vector3.Cross(vec, normal);
			dir=Vector3.Cross(dir, normal);
		} else if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) {
			Vector3 vec=-camera.transform.right;
			vec.y=0; vec.Normalize();
			dir=Vector3.Cross(vec, normal);
			dir=Vector3.Cross(dir, normal);
		} else if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W)) {
			Vector3 vec=camera.transform.forward;
			vec.y=0; vec.Normalize();
			dir=Vector3.Cross(-vec, normal);
			dir=Vector3.Cross(dir, normal);
		} else {
			pos=lpos;
			move=false;
		}
		if (Input.GetKey(KeyCode.Comma)) {
			mo.distance+=6f*Time.deltaTime;
			if (mo.distance>maxdist) mo.distance=maxdist;
		}
		if (Input.GetKey(KeyCode.Period)) {
			mo.distance-=6f*Time.deltaTime;
			if (mo.distance<mindist) mo.distance=mindist;
		}
		
		if (move) {
			if (run) {
				act_speed+=(run_speed-act_speed)*0.1f;
			} else {
				act_speed+=(speed-act_speed)*0.1f;
			}
		} else {
			act_speed+=(0-act_speed)*0.1f;
		}
		pos+=dir*act_speed*Time.deltaTime;
		if (pos.x<xmin) pos.x=xmin; else if (pos.x>xmax) pos.x=xmax;
		if (pos.z<zmin) pos.z=zmin; else if (pos.z>zmax) pos.z=zmax;
		
		//get_ground_pos(ref pos, ref normal, ground_layerMask);
		target.transform.position=pos;
	}
		
	void get_ground_pos(ref Vector3 pos, ref Vector3 normal, Vector3 dir, LayerMask layerMask) {
		float planeLevel = 0;
        var groundPlane = new Plane(Vector3.up, new Vector3(0, planeLevel, 0));
		
        var ray = new Ray(pos+dir*20,-dir);
        RaycastHit rayHit;
        float dist;

        if (Physics.Raycast(ray, out rayHit, Mathf.Infinity, layerMask.value)) {
            pos = rayHit.point;
			normal = rayHit.normal;
		} else if (groundPlane.Raycast(ray, out dist)) {
            pos = ray.origin + ray.direction.normalized * dist;
			normal=Vector3.up;
		}
		pos+=normal*ground_offset;
	}*/
}
