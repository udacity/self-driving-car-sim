using UnityEngine;
using System.Collections;

public class SetupShaderLOD : MonoBehaviour {
	
	public int ShaderLOD=700;
	
	// switch to subshader dedicated for this demo scene
	void Start() {
		if (GetComponent<Renderer>() && GetComponent<Renderer>().material) {
			GetComponent<Renderer>().material.shader.maximumLOD=ShaderLOD;
		}
	}
}
