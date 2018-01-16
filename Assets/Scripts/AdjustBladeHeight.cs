using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AdjustBladeHeight : MonoBehaviour {

	private float mowingDepth=0.0f;
	public Transform MowerModel;
//	public float MowerModelPosHeight=0.3f;
	private float defaultModel_yPos;
	UISystem uiSystem;

//	private Light dlight;
//	private bool L_downflag=false;

	void Awake() {

		uiSystem = FindObjectOfType<UISystem> ();
//		Debug.LogError ("START HERE. . Use UI Slider for adjusting Height");




		if (MowerModel) {
			defaultModel_yPos=MowerModel.localPosition.y;
		}		


//		dlight=GameObject.Find("Directional light").GetComponent<Light>();
	}
	void Start(){
		AdjustBlades ();
	}


	public void Something(){
		Debug.Log (uiSystem.gameObject.name);
		mowingDepth = uiSystem.heightAdjustSlider.value;
		AdjustBlades();
	}


	void OnGUI() {
//		GUILayout.BeginArea(new Rect (400,5,200,40));
//		GUILayout.Box("MowingDepth");
//		mowingDepth=GUILayout.HorizontalSlider(mowingDepth,0,1);
//		AdjustBlades();
//		GUILayout.EndArea();

//		GUI.Box(new Rect (5,96,180,22),"");
//		GUILayout.BeginArea(new Rect (5,98,170,20));
//		GUILayout.Space(-2);
//		GUILayout.Label("   L - toggle realtime shadow");
//		GUILayout.EndArea();
	}

	void AdjustBlades() {
		if (GameManager.instance.replayState != ReplayState.PlayBack) {
			if (GetComponent<Renderer> () && GetComponent<Renderer> ().material) {
				GetComponent<Renderer> ().material.SetVector ("mow_depth", new Vector4 (mowingDepth, 1, 1, 1));
			}
			Vector3 pos = transform.localPosition;
			transform.localPosition = new Vector3 (pos.x, mowingDepth * 10, pos.z);
			if (MowerModel) {
				MowerModel.localPosition = new Vector3 (0, defaultModel_yPos - mowingDepth/**MowerModelPosHeight*/, 0);
			}
		}
	}

//	void Update() {
//		if (Input.GetKey(KeyCode.L)) {
//			if (!L_downflag) {
//				L_downflag=true;
//				if (dlight.shadows==LightShadows.None) {
//					dlight.shadows=LightShadows.Soft;
//				} else {
//					dlight.shadows=LightShadows.None;
//				}
//			}
//		} else {
//			L_downflag=false;
//		}
//	}


	void Update(){
//		Debug.Log (uiSystem.heightAdjustSlider.value);
	}
}
