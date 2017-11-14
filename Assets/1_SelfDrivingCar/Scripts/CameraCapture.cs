using System.Collections;
using System.Collections.Generic;

using System;

using UnityEngine;

public class CameraCapture : MonoBehaviour {

	public byte[] information;
	bool ready = false;
	private System.Object lck;
	Camera cam;
	// Use this for initialization
	void Start () {
		//Debug.Log("Cam start");
		lck = new System.Object ();
		cam = GetComponent<Camera> ();
	}

	// Update is called once per frame
	void Update () {
		//Debug.Log("Cam update");
	}

	public void OnPostRender() {
		long milliseconds = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
		RenderTexture tt = cam.activeTexture;
		RenderTexture.active = tt;
		Texture2D texture2D = new Texture2D(tt.width, tt.height, TextureFormat.RGB24, false);
		texture2D.ReadPixels(new Rect(0, 0, tt.width, tt.height), 0, 0);
		texture2D.Apply();
		lock(lck){
			information = texture2D.EncodeToJPG();
			ready = true;
		}
		long m2 = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

		UnityEngine.Object.DestroyImmediate(texture2D); // Required to prevent leaking the texture
		//Debug.Log("Image Read \n" + (m2 - milliseconds).ToString());
	}

	public byte[] GetFrame() {
		if (ready) {
			byte[] ret;
			lock (lck) {
				ret = (byte[])information.Clone();
				ready = false;
			}
			return ret;

		}
		return null;

	}
}
