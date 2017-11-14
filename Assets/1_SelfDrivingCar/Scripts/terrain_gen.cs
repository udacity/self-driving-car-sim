using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class terrain_gen : MonoBehaviour {

	public Terrain terrain;

	// Use this for initialization
	void Start () {
		var xRes = terrain.terrainData.heightmapWidth;
		var yRes = terrain.terrainData.heightmapHeight;
		var heights = terrain.terrainData.GetHeights(0, 0, xRes, yRes);


		for (int y = 0; y < yRes; y++) {
			for (int x = 0; x < xRes; x++) {
				//heights[x,y] = (Random.Range(0f, 1f) + heights[x,y]) * .005f;
				heights[x,y] = ( Mathf.Max(y/10,x/10)/((float)xRes/10) + heights[x,y]);



			}
		}

		terrain.terrainData.SetHeights(0, 0, heights);
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
