////////////////////////////////////////////////////////////////////////////////////////////
//
// Use it with VolumeGrass class.
// Script for dynamically adjusting mesh LOD for different viewieng distances.
// Using of the script is optional - forget it if you do not intend to use different LODs
// (when only LOD0 is built in VolumeGrass).
//
// (C) Tomasz Stobierski 2010
//
////////////////////////////////////////////////////////////////////////////////////////////
using UnityEngine;
using System.Collections;

public class VolumeGrassLODAdjuster : MonoBehaviour {
	MeshFilter filter;
	MeshFilter filter_sidewalls;
	//
	// Mesh LOD distances defined in VolumeGrass class
	//
	private int[] LOD_distances;
	private Mesh[] LODs;
	private int cur_lod=-1;
	public bool useSimpleMaterial=false;
	//
	// Put distance trashold where object will use different (simplier) material
	// Leave it 0.0f if you don't need to change material in regards to viewing distance
	//
	public float MaterialDistanceTreshold=500.0f;
	//
	// 2 versions of materials used for different distances from object
	//
	public Material OrigMaterial,SimpleMaterial;
	VolumeGrass grassScript;
	
	// Use this for initialization
	void Start () {
		if (gameObject.GetComponent(typeof(MeshRenderer))==null) {
	        gameObject.AddComponent(typeof(MeshRenderer));
		}
		if (gameObject.GetComponent(typeof(MeshFilter))==null) {
        	filter = gameObject.AddComponent(typeof(MeshFilter)) as MeshFilter;
		} else {
			filter = gameObject.GetComponent(typeof(MeshFilter)) as MeshFilter;
		}
		Transform tr=transform.Find("sidewalls");
		if (tr!=null) {
			tr.gameObject.layer=GrassRenderingReservedLayer.layer_num;
			filter_sidewalls=tr.gameObject.GetComponent(typeof(MeshFilter)) as MeshFilter;
		}
		grassScript=gameObject.GetComponent(typeof(VolumeGrass)) as VolumeGrass;
		if (grassScript) {
			LOD_distances=grassScript.LOD_distances;
			LODs=grassScript.LODs;
			if (GetComponent<Renderer>()) {
				OrigMaterial=GetComponent<Renderer>().material;
			}
		}
	}

	// Update is called once per frame
	void Update () {
		if (grassScript && Camera.main) {
			float dist=Vector3.Distance(Camera.main.transform.position, transform.position);
			//
			// Use different mesh for different viewing distances
			//
			if ((dist<LOD_distances[0]) && LODs[0]) {
		        if (cur_lod != 0) {
					cur_lod = 0;
					grassScript.setupLODAndShader(cur_lod, false);
				}
			} else if ((dist<LOD_distances[1]) && LODs[1]) {
		        if (cur_lod != 1) {
					cur_lod = 1;
					grassScript.setupLODAndShader(cur_lod, false);
				}
			} else if ((dist<LOD_distances[2]) && LODs[2]) {
		        if (cur_lod != 2) {
					cur_lod = 2;
					grassScript.setupLODAndShader(cur_lod, false);
				}
			} else if ((dist<LOD_distances[3]) && LODs[3]) {
		        if (cur_lod != 3) {
					cur_lod = 3;
					grassScript.setupLODAndShader(cur_lod, false);
				}
			} else {
				if (filter.mesh != null) filter.mesh = null;
				if (filter_sidewalls.mesh != null) filter_sidewalls.mesh = null;
			}
			//
			// Use different material for different viewing distances
			//
			if (useSimpleMaterial && OrigMaterial && SimpleMaterial) {
				if (dist>MaterialDistanceTreshold) {
					if (GetComponent<Renderer>().material!=SimpleMaterial) {
						GetComponent<Renderer>().material=SimpleMaterial;
					}
				} else {
					if (GetComponent<Renderer>().material!=OrigMaterial) {
						GetComponent<Renderer>().material=OrigMaterial;
					}
				}
			}
		}
	}

}

