using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class map_creator : MonoBehaviour {

	public GameObject point;
	public Terrain terrain;
	//private List<GameObject> points = new List<GameObject> ();
	private int x_dim = 500;
	private int y_dim = 500;

	public TextAsset map_data;

	// Use this for initialization
	void Start () {

		float min_x = 1000;
		float max_x = -1000;
		float min_y = 1000;
		float max_y = -1000;
		float min_z = 1000;
		float max_z = -1000;


		string[] arrayString = map_data.text.Split ('\n');

		for(int i = 0; i < arrayString.Length; i++) 
		{
			string line = arrayString [i];

			string[] entries = line.Split(' ');

			float this_x = float.Parse(entries [0]);
			float this_y = float.Parse(entries [1]);
			float this_z = float.Parse(entries [2]);

			if (this_x > max_x) 
			{
				max_x = this_x;
			}
			if (this_x < min_x) 
			{
				min_x = this_x;
			}
			if (this_y > max_y) 
			{
				max_y = this_y;
			}
			if (this_y < min_y) 
			{
				min_y = this_y;
			}
			if (this_z > max_z) 
			{
				max_z = this_z;
			}
			if (this_z < min_z) 
			{
				min_z = this_z;
			}



		}

		min_z = -2.5f;

		terrain.transform.position = new Vector3 (min_x, min_z, min_y);
		terrain.terrainData.size = new Vector3 (max_x-min_x, max_z-min_z, max_y-min_y);

		string[] arrayString2 = map_data.text.Split ('\n');

		var xRes = terrain.terrainData.heightmapWidth;
		var yRes = terrain.terrainData.heightmapHeight;
		var heights = terrain.terrainData.GetHeights(0, 0, xRes, yRes);

		for (int i = 0; i < arrayString2.Length; i++) 
		{
			string line = arrayString2 [i];

			string[] entries = line.Split(' ');

			float this_x = float.Parse(entries [0]);
			float this_y = float.Parse(entries [1]);
			float this_z = float.Parse(entries [2]);

			if (this_y == max_y) {
				this_y = max_y - 1;
			}
			if (this_x == max_x) {
				this_x = max_x - 1;
			}


			if (this_z > min_z ) 
			{
				heights [xRes-(int)((this_x - min_x) / (max_x - min_x) * xRes),(int)((this_y - min_y) / (max_y - min_y) * yRes)] = ((float)(this_z-min_z) / (max_z-min_z));
			}

			//GameObject get_point = (GameObject)Instantiate (point);
			//get_point.transform.position = new Vector3 (float.Parse(entries[0]), float.Parse(entries[2]), float.Parse(entries[1]));
		}
			
		terrain.terrainData.SetHeights(0, 0, heights);



		Debug.Log ("min x: " + min_x);
		Debug.Log ("max x: " + max_x);
		Debug.Log ("min y: " + min_y);
		Debug.Log ("max y: " + max_y);
		Debug.Log ("min z: " + min_z);
		Debug.Log ("max z: " + max_z);

			
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
