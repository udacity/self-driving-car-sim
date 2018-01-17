using UnityEngine;
using System.Collections;

public class AnimateColor : MonoBehaviour {
	public float speedX=0.1f;
	public float speedZ=0.1f;
	
	Vector4 cv;
	
	// Use this for initialization
	void Start () {
		if (GetComponent<Renderer>() && GetComponent<Renderer>().material) {
			cv=GetComponent<Renderer>().material.GetVector("_coloring_noise_tiling");
		}
	}
	
	// Update is called once per frame
	void Update () {
		if (GetComponent<Renderer>() && GetComponent<Renderer>().material) {
			cv.z+=speedX*Time.deltaTime;
			cv.w+=speedZ*Time.deltaTime;
			GetComponent<Renderer>().material.SetVector("_coloring_noise_tiling", cv);
		}
	}
}
