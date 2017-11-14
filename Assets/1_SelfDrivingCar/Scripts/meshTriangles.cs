// Builds a Mesh containing a single triangle with uvs.
// Create arrays of vertices, uvs and triangles, and copy them into the mesh.

using System;
using System.Collections;
using System.IO;
using Random = UnityEngine.Random;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;



public class meshTriangles : MonoBehaviour
{
	// Use this for initialization

	public GameObject point;

	public float triangle_area(Vector3 A, Vector3 B, Vector3 C)
	{
		Vector3 V = Vector3.Cross(A-B, A-C);
		return V.magnitude * 0.5f;
	}

	void Start()
	{
		//gameObject.AddComponent<MeshFilter>();
		//gameObject.AddComponent<MeshRenderer>();
		Mesh mesh = GetComponent<MeshFilter>().mesh;

		mesh.Clear();

		//create 64 random points

		Vector3[] vertex_collection = new Vector3[128];

		int mindex = 0;
		for (int i = 0; i < 8; i++) 
		{
			for (int j = 0; j < 16; j++) {
				float x = Random.Range (-.01f, .01f);
				float y = Random.Range (0f, .2f);
				float z = Random.Range (-.01f, .01f);

				vertex_collection [mindex] = new Vector3 (i+x, y, j+z-15);

				GameObject get_point = (GameObject)Instantiate (point);
				get_point.transform.position = vertex_collection [mindex];
				mindex++;
			}

		}


		//create 4 known points
		/*
		Vector3[] vertex_collection = new Vector3[4];

		vertex_collection [0] = new Vector3 (0, 0, -10);
		vertex_collection [1] = new Vector3 (0, 0, -9);
		vertex_collection [2] = new Vector3 (1, 0, -10);
		vertex_collection [3] = new Vector3 (1, 0, -9);

		for (int i = 0; i < 4; i++) {

			float x = vertex_collection [i].x;
			float y = vertex_collection [i].y;
			float z = vertex_collection [i].z;

			GameObject get_point = (GameObject)Instantiate (point);
			get_point.transform.position = vertex_collection [i];

		}
		*/
		Debug.Log ("created points");

		mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 100f);

		mesh.vertices = vertex_collection;

		Dictionary<string, int[]> triangles = new Dictionary<string, int[]>();


		Debug.Log ("mesh vertices " + mesh.vertices.Length);

		//create triangles

		for (int i = 0; i < vertex_collection.Length; i++) 
		{
			List< float[]> dist_rank = new List<float[]>();
			for (int j = 0; j < mesh.vertices.Length; j++) 
			{
				if (j != i)
				{
					float distance = Vector3.Distance (mesh.vertices [i], mesh.vertices [j]);
					if (distance < 1.5f) {
						if (dist_rank.Count > 0) {
							int index = 0;

							while (dist_rank [index] [1] < distance && index+1 < dist_rank.Count) {
								index++;
							}
							float[] pair = { j, distance };
							if (index == dist_rank.Count - 1) {
								dist_rank.Add (pair);
							} else {
								dist_rank.Insert (index, pair);
							}
						} 
						else 
						{
							float[] pair = { (float)j, distance };
							dist_rank.Add (pair);
						}
					}

				}
			}

			Dictionary<string, int[]> possible_triangles = new Dictionary<string, int[]>();

			//create all possible triangles
			for(int tri_i = 0; tri_i < dist_rank.Count; tri_i++){
				for (int tri_j = 0; tri_j < dist_rank.Count; tri_j++) {
					for (int tri_k = 0; tri_k < dist_rank.Count; tri_k++) {

						//if none of the points are the same
						if ((tri_i != tri_j) && (tri_j != tri_k) && (tri_i != tri_k)) {

							//create possible triangle
							int[] possible_triangle = new int[3];
							possible_triangle [0] = (int)dist_rank [tri_i] [0];
							possible_triangle [1] = (int)dist_rank [tri_j] [0];
							possible_triangle [2] = (int)dist_rank [tri_k] [0];

							Array.Sort (possible_triangle); 

							string key = "";
							key += possible_triangle [0].ToString () + "," + possible_triangle [1].ToString () + "," + possible_triangle [2].ToString ();

							if (!possible_triangles.ContainsKey (key)) {
								possible_triangles.Add (key, possible_triangle);

							}
						}
					}
				}

			}

			//pick three good triangles from possible_triangles
			int picked_triangles = 0;
			foreach(string key in possible_triangles.Keys)
			{
				int[] triangle = possible_triangles [key];

				Vector3 A = mesh.vertices [triangle [0]];
				Vector3 B = mesh.vertices [triangle [1]];
				Vector3 C = mesh.vertices [triangle [2]];

				float area = triangle_area (A, B, C);

				if( (area > .3) && !triangles.ContainsKey (key)){
					triangles.Add (key, triangle);
					picked_triangles++;
				}
			
				if (picked_triangles >= 5) {
					break;
				}
			}
		}

		List<int> tri = new List<int>();
		int count_keys = 0;
		foreach (string key in triangles.Keys) 
		{
			int[] triangle = triangles [key];
			tri .Add(triangle[0]);
			tri .Add(triangle[1]);
			tri .Add(triangle[2]);

			count_keys++;

		}
		Debug.Log ("count keys " + count_keys);
		int []mesh_triangles = tri.ToArray ();

		for (int i = 0; i < mesh_triangles.Length; i++) 
		{
			Debug.Log ("tri array " + mesh_triangles[i]);
		}

		// make changes to the Mesh by creating arrays which contain the new values
		//mesh.vertices = new Vector3[] {new Vector3(0, 0, 0), new Vector3(0, 5, 0), new Vector3(0, 5, 5),new Vector3(0, 0, 0), new Vector3(0, 0, 5), new Vector3(0, 5, 0), 
		//							   new Vector3(0, 0, 0), new Vector3(0, 5, 5), new Vector3(0, 5, 0),new Vector3(0, 0, 0), new Vector3(0, 5, 0), new Vector3(0, 0, 5)};

		//mesh.vertices = new Vector3[] {new Vector3(0, 0, 0), new Vector3(0, 0, 1), new Vector3(3, 0, 0),new Vector3(3, 0, 1)};

		//mesh.triangles = new int[] {0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11};
		//mesh.triangles = new int[] {0, 1, 2, 0, 2, 1, 0, 2, 3, 0, 3, 2,4,5,7,4,7,5,5,6,7,5,7,6};
		//int[] mesh_triangles = new int[] {0, 1, 2, 0, 2, 3, 1, 3, 2};
		//MeshCollider meshc = gameObject.AddComponent(typeof(MeshCollider)) as MeshCollider;
		//meshc.sharedMesh = mesh;

		//calculate normals


		Vector3[] subNormal = new Vector3[mesh.vertices.Length];

		for (int i = 0; i < subNormal.Length; i++) 
		{
			subNormal [i] = new Vector3(0f,1f,0f);
		}

		for (int i = 0; i < mesh_triangles.Length; i+=3) 
		{
			Vector3 P1 = mesh.vertices [mesh_triangles [i]];
			Vector3 P2 = mesh.vertices [mesh_triangles [i + 1]];
			Vector3 P3 = mesh.vertices [mesh_triangles [i + 2]];

			for (int k = 0; k < 3; k++) 
			{

				Vector3 normal1 = Vector3.Cross (P2 - P1, P3 - P1);
				Vector3 normal2 = Vector3.Cross (P3 - P1, P2 - P1);

				Vector3 direction_to_sun = point.transform.position - mesh.vertices [mesh_triangles [i + k]];
				float angle1 = Vector3.Angle (direction_to_sun, normal1);
				float angle2 = Vector3.Angle (direction_to_sun, normal2);

				if (angle1 > angle2) {
					int temp = mesh_triangles [i + 1];
					mesh_triangles [i + 1] = mesh_triangles [i + 2];
					mesh_triangles [i + 2] = temp;
				}
			}
		}

		mesh.triangles = mesh_triangles;

		Vector2[] uvs = new Vector2[mesh.vertices.Length];

		for (int i = 0; i < uvs.Length; i++)
		{
			uvs[i] = new Vector2(mesh.vertices[i].x, mesh.vertices[i].z);
		}
		mesh.uv = uvs;

		/*
		for (int i = 0; i < mesh.triangles.Length; i++) 
		{
			Debug.Log ("mesh tri check " + mesh.triangles[i]);
		}
		*/

		mesh.normals = subNormal;

		MeshCollider meshc = gameObject.AddComponent(typeof(MeshCollider)) as MeshCollider;
		meshc.sharedMesh = mesh;

	}
}
