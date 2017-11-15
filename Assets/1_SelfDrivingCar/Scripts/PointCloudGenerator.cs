using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO;
using UnityEngine.UI;
using UnityEditor;

namespace PointCloudExporter
{
	//[ExecuteInEditMode]
	public class PointCloudGenerator : MonoBehaviour
	{
		[Header("Point Cloud")]
		private string map_data;
		private bool heatmap;
		private float heatmap_min = 0;
		private float heatmap_max = 4;

		private string[] arrayString;
		private string m_saveLocation = "testlot_normals_combined";
		public Text current_file;
		private bool file_change = false;
		public Toggle Heatmap;
		public InputField Min_z;
		public InputField Max_z;
		public GameObject car;
		public InputField car_x;
		public InputField car_y;
		public InputField car_z;
		public InputField car_angle;
		public InputField radius;
		private bool gen_init = false;

		[Header("Renderer")]
		public float size = .1f;
		public Texture sprite;
		public Shader shader;

		[Header("Displace")]
		[Range(0,1)] public float should = 0.5f;
		public float time = 1f;
		public float speed = 1f;
		public float noiseScale = 1f;
		public float noiseSpeed = 1f;
		public float targetSpeed = 1f;
		[Range(0,1)] public float noisy = 0.1f;
		public Transform targetDisplace;

		[Header("Baking")]
		public int details = 32;
		public int circleRadius = 32;
		public Shader shaderBaked;

		private MeshInfos points;
		private const int verticesMax = 16248;
		private Material material;
		private Material materialBaked;
		private Mesh[] meshArray;
		private Transform[] transformArray;
		private float displaceFiredAt = -1000f;
		private Texture2D colorMapTexture;

		void Start ()
		{
			heatmap = Heatmap.isOn;

			current_file.text = m_saveLocation;
			//arrayString = map_data.text.Split ('\n');
			//Generate();
		}

		public void SetFile()
		{
			SimpleFileBrowser.ShowSaveDialog (OpenFolder, null, false, null, "Select Output File", "Select"); 
		}

		public void SetGenerate()
		{
			heatmap = Heatmap.isOn;

			arrayString = map_data.Split ('\n');
			Generate();

			gen_init = true;

		}

		private void OpenFolder(string location)
		{

			m_saveLocation = location;

			StreamReader reader = new StreamReader(m_saveLocation); 
			string text = reader.ReadToEnd ();


			string[] path_parse_name = m_saveLocation.Split ('\\');
			if (path_parse_name.Length == 1) {
				path_parse_name = m_saveLocation.Split ('/');
			}
			string file_name = path_parse_name [path_parse_name.Length - 1];
			string[] file_parse_name = file_name.Split ('.');
			m_saveLocation = file_parse_name [0];
			current_file.text = m_saveLocation;


			map_data = text;

		}

		public void setHeatmap()
		{
			try{
				heatmap_min = float.Parse(Min_z.text.ToString());
				heatmap_max = float.Parse(Max_z.text.ToString());
			}
			catch (System.FormatException e) {
			}
		}
		public void setRadius()
		{
			try{
				size = float.Parse(radius.text.ToString());
			}
			catch (System.FormatException e) {
			}
		}

		public void setCar()
		{
			try{
				float carx = float.Parse(car_x.text.ToString());
				float cary = float.Parse(car_y.text.ToString());
				float carz = float.Parse(car_z.text.ToString());
				float cart = float.Parse(car_angle.text.ToString());

				car.transform.position = new Vector3(carx,carz,cary);
				car.transform.eulerAngles = new Vector3(0, cart, 0);
			}
			catch (System.FormatException e) {
			}

		}

		public void getCar()
		{
			car_x.text = car.transform.position.x.ToString("N2");
			car_y.text = car.transform.position.z.ToString("N2");
			car_z.text = car.transform.position.y.ToString("N2");
			car_angle.text = car.transform.eulerAngles.z.ToString("N2");
		}


		void Update ()
		{
			if (gen_init) {
				material.SetFloat ("_Size", size);
				material.SetTexture ("_MainTex", sprite);

				if (displaceFiredAt + time > Time.time) {
					Displace (Time.deltaTime);
				}
			}
		}


		public void Generate ()
		{
			material = new Material(shader);
			Generate(material, MeshTopology.Points);
		}

		public void Export ()
		{
			MeshInfos triangles = GetTriangles(points, size);
			materialBaked = new Material(shaderBaked);
			//Generate(triangles, materialBaked, MeshTopology.Triangles);
			materialBaked.SetTexture("_MainTex", GetBakedColors(triangles));
		}

		Vector3[] LoadVertices(int start, int amount, string[] pcd_map)
		{
			Vector3[] collect_vertices = new Vector3[amount];

			int vertex_index = 0;
			for (int i = start; i < start+amount; i++) 
			{
				string line = pcd_map[i];

				string[] entries = line.Split(' ');

				float this_x = float.Parse(entries [0]);
				float this_y = float.Parse(entries [1]);
				float this_z = float.Parse(entries [2]);

				Vector3 get_vertex = new Vector3 (this_y, this_x, this_z);

				collect_vertices [vertex_index] = get_vertex;
				vertex_index++;
			}

			return collect_vertices;

		}

		Vector3[] LoadNormals(int start, int amount, Vector3 set_norm, string[] pcd_map)
		{
			Vector3[] collect_normals = new Vector3[amount];

			int vertex_index = 0; 
			for (int i = start; i < start+amount; i++) 
			{
				string line = pcd_map[i];
				string[] entries = line.Split(' ');

				if (entries.Length> 6) {

					float norm_x = float.Parse (entries [4]);
					float norm_y = float.Parse (entries [5]);
					float norm_z = float.Parse (entries [6]);

					Vector3 norm1 = new Vector3 (norm_x, norm_y, norm_z);
					collect_normals [vertex_index] = norm1;

				} 
				else {
					collect_normals [vertex_index] = set_norm;
				}

				vertex_index++;
			}

			return collect_normals;

		}
		Color[] LoadColors(int start, int amount, string[] pcd_map)
		{
			Color[] collect_colors = new Color[amount];

			int vertex_index = 0;
			for (int i = start; i < start+amount; i++) 
			{

				string line = pcd_map[i];
				string[] entries = line.Split(' ');

				float this_z = float.Parse(entries [2]);

				float min_z = heatmap_min;
				float max_z = heatmap_max;

				float r_id = 0f;
				float g_id = 0f;
				float b_id = 0f;

				if (this_z <= min_z) {
					r_id = 0.0f;
					g_id = 0.0f;
					b_id = 1.0f;
				} else if (this_z > max_z) {
					r_id = 1.0f;
					g_id = 0.0f;
					b_id = 0.0f;
				} 
				// Create Short Rainbow for heatmap
				else
				{
					float color_id = (this_z - min_z) / (max_z - min_z);
					float a = (1-color_id)/0.25f;
					int X = (int) Mathf.Floor(a);
					float Y = (a-X);
					switch(X)
					{
					case 0:
						r_id = 1.0f;
						g_id = Y;
						b_id = 0.0f;
						break;
					case 1: 
						r_id = 1.0f - Y;
						g_id = 1.0f;
						b_id = 0.0f; 
						break;
					case 2:
						r_id = 0.0f;
						g_id = 1.0f;
						b_id = Y; 
						break;
					case 3:
						r_id = 0.0f;
						g_id = 1.0f - Y;
						b_id = 1.0f; 
						break;
					case 4:
						r_id = 0.0f;
						g_id = 0.0f;
						b_id = 1.0f; 
						break;
					}

				}

				Color get_color = new Color (r_id, g_id, b_id, 1.0f);



				collect_colors [vertex_index] = get_color;
				vertex_index++;
			}

			return collect_colors;

		}

		public Mesh get_triangle_mesh(Vector3[] subVertices, Vector3[] subNormals, float radius)
		{
			Mesh mesh = new Mesh ();
			mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 100f);

			int mesh_size = subVertices.Length * 3;

			Vector3[] subVertex = new Vector3[mesh_size];
			int [] tri = new int[mesh_size];
			Vector3[] subNormal = new Vector3[mesh_size];
			Vector2[] uvs = new Vector2[mesh_size];

			for (int i = 0; i < subVertices.Length; i++) {

				Vector3 center = subVertices [i];
				Vector3 normal = subNormals [i];
				Vector3 tangent = Vector3.Normalize (Vector3.Cross (Vector3.up, normal));
				Vector3 up = Vector3.Normalize (Vector3.Cross (tangent, normal));

				subVertex [3 * i] = center + tangent * -radius / 1.5f;
				subVertex [3 * i + 1] = center + up * radius;
				subVertex [3 * i + 2] = center + tangent * radius / 1.5f;

				tri [3 * i] = 3 * i;
				tri [3 * i + 1] = 3 * i + 1;
				tri [3 * i + 2] = 3 * i + 2;

				subNormal [3 * i] = normal;
				subNormal [3 * i + 1] = normal;
				subNormal [3 * i + 2] = normal;

				uvs [3 * i] = new Vector2(subVertex [3 * i].x, subVertex [3 * i].z);
				uvs [3 * i + 1] = new Vector2(subVertex [3 * i + 1].x, subVertex [3 * i + 1].z);
				uvs [3 * i + 2] = new Vector2(subVertex [3 * i + 2].x, subVertex [3 * i + 2].z);
			}

			mesh.vertices = subVertex;
			mesh.normals = subNormal;
			mesh.triangles = tri;
			mesh.uv = uvs;

			return mesh;

		}

		public void Generate (Material materialToApply, MeshTopology topology)
		{

			for (int c = transform.childCount - 1; c >= 0; --c) {
				Transform child = transform.GetChild(c);
				GameObject.DestroyImmediate(child.gameObject);
			}

			int vertexCount = arrayString.Length;//meshInfos.vertexCount;
			Debug.Log ("vertexCount " + vertexCount);
			int meshCount = (int)Mathf.Ceil(vertexCount / (float)verticesMax);

			meshArray = new Mesh[meshCount];
			transformArray = new Transform[meshCount];

			int index = 0;
			int meshIndex = 0;
			int vertexIndex = 0;

			int resolution = GetNearestPowerOfTwo(Mathf.Sqrt(vertexCount));

			while (meshIndex < meshCount) {

				int count = verticesMax;
				if (vertexCount <= verticesMax) {
					count = vertexCount;
				} else if (vertexCount > verticesMax && meshCount == meshIndex + 1) {
					count = vertexCount % verticesMax;
				}

				Vector3[] subVertices = LoadVertices (meshIndex * verticesMax, count, arrayString);//meshInfos.vertices.Skip(meshIndex * verticesMax).Take(count).ToArray();
				Vector3 norm = new Vector3 (0f, 0f, 1f);
				Vector3[] subNormals =   LoadNormals(meshIndex * verticesMax, count, norm, arrayString);//meshInfos.normals.Skip(meshIndex * verticesMax).Take(count).ToArray();
				Color[] subColors = LoadColors(meshIndex * verticesMax, count, arrayString);//meshInfos.colors.Skip(3 * verticesMax).Take(count).ToArray();//meshInfos.colors.Skip(meshIndex * verticesMax).Take(count).ToArray();



				int[] subIndices = new int[count];
				for (int i = 0; i < count; ++i) {
					subIndices[i] = i;
				}

				Mesh mesh = new Mesh();
				mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 100f);
				mesh.vertices = subVertices;
				mesh.normals = subNormals;
				mesh.colors = subColors;
				mesh.SetIndices(subIndices, topology, 0);

				if (heatmap) {

					Vector2[] uvs2 = new Vector2[mesh.vertices.Length];
					for (int i = 0; i < uvs2.Length; ++i) {
						float x = vertexIndex % resolution;
						float y = Mathf.Floor (vertexIndex / (float)resolution); 
						uvs2 [i] = new Vector2 (x, y) / (float)resolution;
						++vertexIndex;
					}
					mesh.uv2 = uvs2;


					Vector3 norm2 = new Vector3 (0f, 1f, 0f);
					Vector3[] subNormals2 = LoadNormals (meshIndex * verticesMax, count, norm2, arrayString);

					Mesh mesh2 = new Mesh ();
					mesh2.bounds = new Bounds (Vector3.zero, Vector3.one * 100f);
					mesh2.vertices = subVertices;
					mesh2.normals = subNormals2;
					mesh2.colors = subColors;
					mesh2.SetIndices (subIndices, topology, 0);
					mesh2.uv2 = uvs2;

					Vector3 norm3 = new Vector3 (1f, 0f, 0f);
					Vector3[] subNormals3 = LoadNormals (meshIndex * verticesMax, count, norm3, arrayString);

					Mesh mesh3 = new Mesh ();
					mesh3.bounds = new Bounds (Vector3.zero, Vector3.one * 100f);
					mesh3.vertices = subVertices;
					mesh3.normals = subNormals3;
					mesh3.colors = subColors;
					mesh3.SetIndices (subIndices, topology, 0);
					mesh3.uv2 = uvs2;

					GameObject go1 = CreateGameObjectWithMesh(mesh2, materialToApply, gameObject.name + "_1" + meshIndex, transform);
					GameObject go2 = CreateGameObjectWithMesh(mesh3, materialToApply, gameObject.name + "_2" + meshIndex, transform);
				}

				Mesh collision_mesh = get_triangle_mesh(subVertices,subNormals,size);

				GameObject go = CreateGameObjectWithMesh(mesh, materialToApply, gameObject.name + "_0" + meshIndex, transform, collision_mesh);

				meshArray[meshIndex] = mesh;
				transformArray[meshIndex] = go.transform;

				index += count;
				++meshIndex;
			}
		}

		public void Displace ()
		{
			displaceFiredAt = Time.time;
		}

		public void Displace (float dt)
		{
			int meshInfosIndex = 0;
			for (int meshIndex = 0; meshIndex < meshArray.Length; ++meshIndex)
			{
				Mesh mesh = meshArray[meshIndex];
				Vector3[] vertices = mesh.vertices;
				Vector3[] normals = mesh.normals;
				Vector3 offsetNoise = new Vector3();
				Vector3 offsetTarget = new Vector3();
				Matrix4x4 matrixWorld = transform.localToWorldMatrix;
				Matrix4x4 matrixLocal = transform.worldToLocalMatrix;
				for (int vertexIndex = 0; vertexIndex < vertices.Length; ++vertexIndex)
				{
					Vector3 position = matrixWorld.MultiplyVector(vertices[vertexIndex]) + transform.position;
					Vector3 normal = normals[vertexIndex];

					offsetNoise.x = Mathf.PerlinNoise(position.x * noiseScale, position.y * noiseScale);
					offsetNoise.y = Mathf.PerlinNoise(position.y * noiseScale, position.z * noiseScale);
					offsetNoise.z = Mathf.PerlinNoise(position.z * noiseScale, position.x * noiseScale);
					offsetNoise = (offsetNoise * 2f - Vector3.one) * noiseSpeed;

					offsetTarget = Vector3.Normalize(position - targetDisplace.position) * targetSpeed;

					float noisyFactor = Mathf.Lerp(1f, Random.Range(0f,1f), noisy);
					float shouldMove = Mathf.InverseLerp(1f-should, 1f, Mathf.PerlinNoise(normal.x*8f, normal.y*8f));

					position += (offsetNoise + offsetTarget) * dt * speed * noisyFactor * shouldMove;
					vertices[vertexIndex] = matrixLocal.MultiplyVector(position - transform.position);
					++meshInfosIndex;
				}
				mesh.vertices = vertices;
			}
		}

		public void Reset ()
		{
			int meshInfosIndex = 0;
			for (int meshIndex = 0; meshIndex < meshArray.Length; ++meshIndex) {
				Mesh mesh = meshArray[meshIndex];
				Vector3[] vertices = mesh.vertices;
				for (int vertexIndex = 0; vertexIndex < vertices.Length; ++vertexIndex) {
					vertices[vertexIndex] = points.vertices[meshInfosIndex];
					++meshInfosIndex;
				}
				mesh.vertices = vertices;
			}
		}

		// http://stackoverflow.com/questions/466204/rounding-up-to-nearest-power-of-2
		public int GetNearestPowerOfTwo (float x)
		{
			return (int)Mathf.Pow(2f, Mathf.Ceil(Mathf.Log(x) / Mathf.Log(2f)));
		}

		public GameObject CreateGameObjectWithMesh (Mesh mesh, Material materialToApply, string name = "GeneratedMesh", Transform parent = null, Mesh collision_mesh = null)
		{
			GameObject meshGameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
			GameObject.DestroyImmediate(meshGameObject.GetComponent<Collider>());

			meshGameObject.name = name;
			meshGameObject.transform.parent = parent;
			meshGameObject.transform.localPosition = Vector3.zero;
			meshGameObject.transform.localRotation = Quaternion.identity;
			meshGameObject.transform.localScale = Vector3.one;

			if (collision_mesh != null) {
				MeshCollider meshc = meshGameObject.AddComponent (typeof(MeshCollider)) as MeshCollider;
				meshc.sharedMesh = collision_mesh;
				meshGameObject.GetComponent<MeshFilter> ().mesh = collision_mesh;
			} else {
				meshGameObject.GetComponent<MeshFilter> ().mesh = mesh;
				meshGameObject.GetComponent<Renderer> ().sharedMaterial = materialToApply;

			}

			return meshGameObject;
		}

		public MeshInfos GetTriangles (MeshInfos points, float radius)
		{
			MeshInfos triangles = new MeshInfos();
			triangles.vertexCount = points.vertexCount * 3;
			triangles.vertices = new Vector3[triangles.vertexCount];
			triangles.normals = new Vector3[triangles.vertexCount];
			triangles.colors = new Color[triangles.vertexCount];
			int index = 0;
			int meshVertexIndex = 0;
			int meshIndex = 0;
			Vector3[] vertices = meshArray[meshIndex].vertices;
			for (int v = 0; v < triangles.vertexCount; v += 3) {
				Vector3 center = vertices[meshVertexIndex];
				Vector3 normal = points.normals[index];
				Vector3 tangent = Vector3.Normalize(Vector3.Cross(Vector3.up, normal));
				Vector3 up = Vector3.Normalize(Vector3.Cross(tangent, normal));

				triangles.vertices[v] = center + tangent * -radius / 1.5f;
				triangles.vertices[v+1] = center + up * radius;
				triangles.vertices[v+2] = center + tangent * radius / 1.5f;

				triangles.normals[v] = normal;
				triangles.normals[v+1] = normal;
				triangles.normals[v+2] = normal;

				Color color = points.colors[index];
				triangles.colors[v] = color;
				triangles.colors[v+1] = color;
				triangles.colors[v+2] = color;

				++meshVertexIndex;

				if (meshVertexIndex >= meshArray[meshIndex].vertices.Length) {
					meshVertexIndex = 0;
					++meshIndex;
					if (meshIndex < meshArray.Length) {
						vertices = meshArray[meshIndex].vertices;
					}
				}

				++index;
			}
			return triangles;
		}

		public Texture2D GetBakedColors (MeshInfos triangles)
		{
			List<Color> colorList = new List<Color>();
			int[] colorIndexMap = new int[triangles.vertexCount / 3];
			int globalIndex = 0;
			for (int meshIndex = 0; meshIndex < meshArray.Length; ++meshIndex) {
				Mesh mesh = meshArray[meshIndex];
				Color[] colors = mesh.colors;
				for (int i = 0; i < colors.Length; i += 3) {
					Color color = colors[i];
					Color colorSimple = new Color(Mathf.Floor(color.r * details) / details, Mathf.Floor(color.g * details) / (float)details, Mathf.Floor(color.b * details) / (float)details);

					int colorIndex = colorList.IndexOf(colorSimple);
					if (colorIndex == -1) { 
						colorIndex = colorList.Count;
						colorList.Add(colorSimple);
					}

					colorIndexMap[globalIndex] = colorIndex;
					++globalIndex;
				}
			}

			int colorCount = colorList.Count;
			int columnCount = GetNearestPowerOfTwo(Mathf.Sqrt(colorCount));
			int rowCount = columnCount;//1 + (int)Mathf.Floor(colorCount / (float)columnCount);
			int width = circleRadius * columnCount;
			int height = circleRadius * rowCount;

			colorMapTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
			Color[] colorMapArray = new Color[width * height];
			Vector2 pos;
			Vector2 target = new Vector2(0.5f, 0.3f);
			for (int i = 0; i < colorList.Count; ++i) {
				int x = i % columnCount;
				int y = (int)Mathf.Floor(i / columnCount);
				for (int c = 0; c < circleRadius*circleRadius; ++c) {
					int ix = c % circleRadius;
					int iy = (int)Mathf.Floor(c / circleRadius);
					pos.x = ix / (float)circleRadius;
					pos.y = iy / (float)circleRadius;
					float dist = Mathf.Clamp01(Vector2.Distance(target, pos));
					int colorIndex = x * circleRadius + y * width * circleRadius + ix + iy * width;
					float circle = 1f - Mathf.InverseLerp(0.2f, 0.35f, dist);
					colorMapArray[colorIndex] = Color.Lerp(Color.clear, colorList[i], circle);
				}
			}
			colorMapTexture.SetPixels(colorMapArray);
			colorMapTexture.Apply(false);

			Vector2 halfSize = new Vector2(0.5f * circleRadius / (float)width, 0.5f * circleRadius / (float)height);
			Vector2 right = Vector2.right * halfSize.x;
			Vector2 up = Vector2.up * halfSize.y;
			globalIndex = 0;
			for (int meshIndex = 0; meshIndex < meshArray.Length; ++meshIndex) {
				Mesh mesh = meshArray[meshIndex];
				Vector2[] uvs = new Vector2[mesh.vertices.Length];
				for (int i = 0; i < uvs.Length; i += 3) {

					int colorIndex = colorIndexMap[globalIndex];
					float x = ((colorIndex % columnCount) * circleRadius) / (float)width;
					float y = (Mathf.Floor(colorIndex / columnCount) * circleRadius) / (float)height;
					Vector2 center = new Vector2(x + halfSize.x, y + halfSize.y);

					uvs[i] = center + right - up;
					uvs[i+1] = center + up;
					uvs[i+2] = center - right - up;

					++globalIndex;
				}
				mesh.uv = uvs;
			}

			return colorMapTexture;
		}

		public Texture2D GetBakedMap ()
		{
			return colorMapTexture;
		}
	}
}
