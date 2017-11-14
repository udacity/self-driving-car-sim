// Builds a Mesh containing a single triangle with uvs.
// Create arrays of vertices, uvs and triangles, and copy them into the mesh.

using UnityEngine;

public class simple_mesh : MonoBehaviour
{
	// Use this for initialization
	void Start()
	{
		gameObject.AddComponent<MeshFilter>();
		gameObject.AddComponent<MeshRenderer>();
		Mesh mesh = GetComponent<MeshFilter>().mesh;

		mesh.Clear();

		// make changes to the Mesh by creating arrays which contain the new values
		mesh.vertices = new Vector3[] {new Vector3(0, 0, 0), new Vector3(0, 10, 0), new Vector3(10, 10, 0)};
		mesh.uv = new Vector2[] {new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1)};
		mesh.triangles =  new int[] {0, 1, 2};
	}
}
