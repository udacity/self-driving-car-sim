using UnityEngine;
using System.Collections;

public class SetupCamForMowing : MonoBehaviour {
	
	private bool clearFlagsInited=false;
	public VolumeGrass lawnObject;
	
	private bool res_changed=true;
	private float res_w=0,res_h=0;
		
	void OnPostRender() {
		if (!clearFlagsInited) {
			clearFlagsInited=true;
			if (lawnObject && GetComponent<Camera>()) {
				GameObject go=lawnObject.gameObject;
				if (go.GetComponent<Renderer>() && go.GetComponent<Renderer>().material) {
					Vector4 tiling=go.GetComponent<Renderer>().material.GetVector("_auxtex_tiling");
					tiling.x*=GetComponent<Camera>().rect.width;
					tiling.y*=GetComponent<Camera>().rect.height;
					go.GetComponent<Renderer>().material.SetVector("_auxtex_tiling", tiling);
				}
			}
		}
		if (res_changed) {
			if (GetComponent<Camera>()!=null) {
				GetComponent<Camera>().clearFlags=CameraClearFlags.Nothing;
			}
			res_changed=false;
		}
	}
	
	void Update() {
		if ((Screen.width!=res_w) || (Screen.height!=res_h)) {
			res_changed=true;
			res_w=Screen.width;
			res_h=Screen.height;			
			if (GetComponent<Camera>()!=null) {
				GetComponent<Camera>().clearFlags=CameraClearFlags.SolidColor;
			}
		}		
	}
}