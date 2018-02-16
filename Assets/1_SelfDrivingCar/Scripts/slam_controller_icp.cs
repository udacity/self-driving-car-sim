using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class slam_controller_icp : MonoBehaviour {
	[SerializeField] private List<GameObject> sensors;
	[SerializeField] private List<GameObject> landmarks;
	private int update_status;
	private int map_status;
	private float x_drive;
	private float t_drive;
	private List<float> scan_measure;

	private float delta_x;
	private float delta_t;

	public Slider sigma_x_slider;
	public Slider sigma_t_slider;
	public Slider sigma_lx_slider;
	public Slider sigma_ly_slider;

	public Slider drive_speed_slider;
	public Slider turn_speed_slider;

	public Text value_x;
	public Text value_y;
	public Text value_lx;
	public Text value_ly;

	[SerializeField] private List<GameObject> sense_landmarks;
	public GameObject sense_car;
	public Camera sense_camera;

	private float sigma_pos_x;
	private float sigma_pos_t;
	private float sigma_landmark_x;
	private float sigma_landmark_y;

	private float sigma_drive_x;
	private float sigma_drive_t;

	private float angle_range = 2*Mathf.PI;

	public GameObject key_marker;
	private List<GameObject> key_markers;

	public GameObject measure_marker;
	private List<GameObject> measure_markers;

	public GameObject reference_marker;
	private List<GameObject> reference_markers;

	public GameObject deadreckoning_marker;

	public GameObject map;
	private int map_refresh = 0;
	private SpriteRenderer maps_graphics;
	private float grid_min_x = -4;
	private float grid_min_y = -4;

	public Text map_error;
	private int map_error_value;

	bool server_connection = false;

	public GameObject landmark;

	private string filePath;

	public Texture2D ground_truth;

	private bool map_diff = false;

	private void Awake() {
	//filePath = Path.Combine(Application.streamingAssetsPath, "my_map.png");
	filePath = "D:/udacity/slam/isam_dense_lab/src/my_map.png";

	}

	private IEnumerator TextureMap() {
		if (filePath.Contains ("://")) {
			WWW www = new WWW (filePath);
			yield return www;
			Texture2D SpriteTexture = www.texture;
			maps_graphics.sprite = Sprite.Create (SpriteTexture, new Rect (0, 0, SpriteTexture.width, SpriteTexture.height), new Vector2 (0, 0), 100.0f);
		} else {
			Texture2D SpriteTexture = LoadTexture (filePath);
			Texture2D CompareTex = MapScore (SpriteTexture);
			maps_graphics.sprite = Sprite.Create (CompareTex, new Rect (0, 0, CompareTex.width, CompareTex.height), new Vector2 (0, 0), 100.0f);
			

		}
	}

	// Use this for initialization
	void Start () {
		update_status = 0;
		map_status = 0;
		x_drive = 0;
		t_drive = 0;
  
		delta_x = 0;
		delta_t = 0;

		sigma_pos_x = 0.01f;
		sigma_pos_t = 0.01f;
		sigma_landmark_x = 0.5f;
		sigma_landmark_y = 0.5f;

		sigma_drive_x = 0.01f;
		sigma_drive_t = 1.0f;

		reference_markers = new List<GameObject>();
		measure_markers = new List<GameObject>();
		key_markers = new List<GameObject>();

		//scan_measure = SenseDistance ();
		//GraphReference (scan_measure);

		maps_graphics = map.GetComponent<SpriteRenderer> ();

		map_error_value = 694400;

	}

	public Texture2D LoadTexture(string FilePath) {

		// Load a PNG or JPG file from disk to a Texture2D
		// Returns null if load fails

		Texture2D Tex2D;
		byte[] FileData;

		if (File.Exists(FilePath)){
			FileData = File.ReadAllBytes(FilePath);
			Tex2D = new Texture2D(2, 2);           // Create new "empty" texture
			if (Tex2D.LoadImage(FileData))           // Load the imagedata into the texture (size is set automatically)
				return Tex2D;                 // If data = readable -> return texture
		}  
		return null;                     // Return null if load failed
	}

	public Texture2D MapScore(Texture2D sourceTex)
	{
		
		int width = Mathf.FloorToInt(sourceTex.width);
		int height = Mathf.FloorToInt(sourceTex.height);

		int gt_width = Mathf.FloorToInt( ground_truth.width);
		int gt_height = Mathf.FloorToInt( ground_truth.height);

		int max_width = gt_width;
		int max_height = gt_height;

		if (width > gt_width)
			max_width = width;
		if (height > gt_height)
			max_height = height;

		Texture2D mod_sourceTex = expandTexture (sourceTex, width, height, max_width, max_height);
		Texture2D mod_ground_truth = expandTexture (ground_truth, gt_width, gt_height, max_width, max_height);


		Color[] pix = mod_sourceTex.GetPixels(0, 0, max_width, max_height);
		Color[] gtpix = mod_ground_truth.GetPixels(0, 0, max_width, max_height);

		float error = 0;

		for(int i = 0; i < gtpix.Length; i++)
		{
			
			float error_part = Mathf.Abs (pix [i][0] - gtpix [i][0]);
			error += error_part;

			if (map_diff) 
			{
				pix [i] [0] = error_part;
				pix [i] [1] = error_part;
				pix [i] [2] = error_part;
			}

		}

		if (map_diff) {
			mod_sourceTex.SetPixels (pix);
			mod_sourceTex.Apply ();
		}

		setGridCenter (-Mathf.Floor(max_width / 80)-1, -Mathf.Floor(max_height / 40)+4);

		map_error.text = "Map Error: "+error.ToString("N0");
		map_error_value = (int) error;

		return mod_sourceTex;

	}

	public void toggel_map_diff()
	{
		map_diff = !map_diff;
	}

	public int getMapError()
	{
		return map_error_value;
	}

	public void zoom_out_Sense()
	{
		//Camera sense_camera = sense_car.GetComponent<Camera>();

		float focus = sense_camera.orthographicSize;

		if (focus < 16) {
			focus += 1;
			sense_camera.orthographicSize = focus;
		}

	}
	public void zoom_in_Sense()
	{
		//Camera sense_camera = sense_car.GetComponent<Camera>();

		float focus = sense_camera.orthographicSize;

		if (focus > 5) {
			focus -= 1;
			sense_camera.orthographicSize = focus;
		}

	}

	// expand texture to new width and height by filling with 0.5 for new elements
	Texture2D expandTexture(Texture2D sourceTex, int width, int height,int new_width, int new_height)
	{
		if (new_width > width || new_height > height) {

			Texture2D expand = new Texture2D (new_width, new_height);

			Color[] expand_values = expand.GetPixels (0, 0, new_width, new_height);

			for (int i = 0; i < expand_values.Length; i++) {
				expand_values [i] [0] = 0.5f;
				expand_values [i] [1] = 0.5f;
				expand_values [i] [2] = 0.5f;
			}

			expand.SetPixels (expand_values);
			expand.Apply ();

			Color[] pix = sourceTex.GetPixels (0, 0, width, height);

			expand.SetPixels (new_width - width, new_height - height, width, height, pix);
			expand.Apply ();

			return expand;
		}

		return sourceTex;

	}
		
		

	void UpdateSliderValues()
	{
		value_x.text = (sigma_x_slider.value*sigma_drive_x).ToString ("N4");
		value_y.text = (sigma_t_slider.value*sigma_drive_t).ToString ("N4");
		value_lx.text = (sigma_lx_slider.value*sigma_landmark_x).ToString ("N4");
		value_ly.text = (sigma_ly_slider.value*sigma_landmark_y).ToString ("N4");

	}

	public void setGridCenter(float min_x, float min_y)
	{
		grid_min_x = min_x;
		grid_min_y = min_y;

		//Debug.Log("grid_min_x "+ min_x);
		//Debug.Log("grid_min_y "+ min_y);
	}

	public void setConnection()
	{
		server_connection = true;
	}
	
	// Update is called once per frame
	void FixedUpdate () {

		x_drive = 0;
		t_drive = 0;

		//control
		if (update_status == 0 || !server_connection) {
			if (Input.GetKey (KeyCode.UpArrow)) {
				x_drive = .01f*drive_speed_slider.value;
			} else if (Input.GetKey (KeyCode.DownArrow)) {
				x_drive = -.01f*drive_speed_slider.value;;
			}
			if (Input.GetKey (KeyCode.RightArrow)) {
				t_drive = -1f*turn_speed_slider.value;
			} else if (Input.GetKey (KeyCode.LeftArrow)) {
				t_drive = 1f*turn_speed_slider.value;
			}
		}
			
		if ((x_drive != 0 || t_drive != 0) ) 
		{
			if (Mathf.Abs(delta_x) >= 0.00) {
				update_status = 1;
			}

			float x_error = normrand (x_drive, sigma_x_slider.value*sigma_drive_x);
			float t_error = normrand (t_drive, sigma_t_slider.value*sigma_drive_t);

			transform.position += transform.right * x_error;
			transform.Rotate (Vector3.forward * t_error);

			delta_x += x_drive;
			delta_t += t_drive;
				
			deadreckoning_marker.transform.position += deadreckoning_marker.transform.right * x_drive;
			deadreckoning_marker.transform.Rotate (Vector3.forward * t_drive);

		}


		// Car Sense
		ResetSensors ();
		scan_measure = SenseDistance ();

		/*
		if (Input.GetKey (KeyCode.S)) {
			//GraphMeasure (scan_measure);
			OutputScan ();
		}
		*/

		UpdateSliderValues();

			
	}
	public void GenerateMap()
	{
		StartCoroutine(TextureMap());
		map.transform.position = new Vector3 (grid_min_x, grid_min_y-20, 0);
		map_status = 1;
	}
	public void resetStatus()
	{
		
		if (update_status == 1) {
			delta_x = 0;
			delta_t = 0;
		}
		update_status = 0;
	}
	public void resetMapStatus()
	{
		map_status = 0;
	}

	public void plot_keyframes(List<float> my_key_x, List<float> my_key_y, List<float> my_key_t, List<float> closeframes, int ref_index)
	{
		for (int i = 0; i < my_key_x.Count; i++)
		{
			if (key_markers.Count < i+1) {
				//create new key marker
				GameObject get_key_marker = (GameObject)Instantiate (key_marker);
				get_key_marker.name = "key_marker_" + key_markers.Count;
				key_markers.Add (get_key_marker);
			}
				
			key_markers[i].transform.position = new Vector2 (my_key_x[i], my_key_y[i]-20);
			key_markers[i].transform.GetChild(0).transform.rotation = Quaternion.AngleAxis (my_key_t[i] * Mathf.Rad2Deg, Vector3.forward);

			//link all keyframes with blue paths
			if (i + 1 < my_key_x.Count) 
			{
				Vector2 raycastRender = new Vector2 (my_key_x[i+1]-my_key_x[i], my_key_y[i+1]-my_key_y[i]);

				LineRenderer lineRenderer = key_markers[i].transform.GetChild(1).GetComponentInParent<LineRenderer> ();
				lineRenderer.SetWidth ((float).03, (float).03);
				lineRenderer.SetPosition (1, raycastRender*20);

			}

			int getcloseframe = (int)closeframes [i];

			if (getcloseframe != -1) {
				Vector2 raycastRenderClose = new Vector2 (my_key_x [getcloseframe] - my_key_x [i], my_key_y [getcloseframe] - my_key_y [i]);

				LineRenderer lineRendererClose = key_markers [i].transform.GetChild (3).GetComponentInParent<LineRenderer> ();
				lineRendererClose.SetWidth ((float).03, (float).03);
				lineRendererClose.SetPosition (1, raycastRenderClose * 20);

			} 
			else {
				LineRenderer lineRendererClose = key_markers [i].transform.GetChild (3).GetComponentInParent<LineRenderer> ();
				lineRendererClose.SetPosition (1, Vector2.zero);
			}


			LineRenderer lineRenderer_ref = (LineRenderer) key_markers [i].transform.GetChild (2).GetComponentInParent<LineRenderer> ();
			lineRenderer_ref.SetPosition (1, Vector2.zero);


		}
			
		Vector2 raycastRender2 = new Vector2 (sense_car.transform.position.x-my_key_x[ref_index], (sense_car.transform.position.y+20)-my_key_y[ref_index]);

		LineRenderer lineRenderer2 = key_markers[ref_index].transform.GetChild(2).GetComponentInParent<LineRenderer> ();
		lineRenderer2.SetWidth ((float).03, (float).03);
		lineRenderer2.SetPosition (1, raycastRender2*20);
	}

	public void plot_landmark(float x, float y)
	{
		//float car_x = transform.position.x;
		//float car_y = transform.position.y;
		//landmark.transform.position = new Vector3 (car_x, car_y, 0)+x*transform.right+y*transform.up;
		landmark.transform.position = new Vector3 (x,y,0);
	}

	public void plot_car(float car_x, float car_y, float car_t)
	{
		sense_car.transform.position = new Vector3 (car_x, car_y-20, 0);
		sense_car.transform.rotation = Quaternion.AngleAxis (car_t * Mathf.Rad2Deg, Vector3.forward);
	}

	public float normrand(float mean, float stdDev)
	{
		float u1 = 1.0f-Random.Range (0.0f, 1.0f); //uniform(0,1] random doubles
		float u2 = 1.0f-Random.Range (0.0f, 1.0f);
		float randStdNormal = Mathf.Sqrt((float)(-2.0 * Mathf.Log((float)u1,(float)2.718))) * Mathf.Sin((float)(2.0 * Mathf.PI * u2)); //random normal(0,1)
		float randNormal = (float)(mean + stdDev * randStdNormal); //random normal(mean,stdDev^2)
		return randNormal;
	}


	List<float> SenseDistance()
	{

		List<float> ray_lengths = new List<float> ();

		int Raysize = sensors.Count;

		for (int i = 0; i < Raysize; i++) 
		{

			float angle = -angle_range / 2 + i * (angle_range) / (Raysize-1);

			float base_angle = transform.eulerAngles.z * Mathf.Deg2Rad;

			Vector2 raycastDir = new Vector2(Mathf.Cos (base_angle + angle), Mathf.Sin (base_angle + angle));
			Vector2 origin = new Vector2 (transform.position.x, transform.position.y);
			RaycastHit2D hit = Physics2D.Raycast (origin, raycastDir, 10);

			Vector2 raycastRender =  new Vector2(Mathf.Cos (angle), Mathf.Sin (angle));// - transform.position;

			LineRenderer lineRenderer = sensors [i].GetComponentInParent<LineRenderer> ();
			lineRenderer.SetWidth ((float).01, (float).01);

			if (hit.collider) { 
				Vector3 vector_point = transform.TransformPoint (raycastRender.normalized * hit.distance * 20);
				lineRenderer.SetPosition (1, raycastRender.normalized * hit.distance * 20);

				float vector_x = normrand (vector_point.x, sigma_lx_slider.value*sigma_landmark_x);
				float vector_y = normrand (vector_point.y, sigma_ly_slider.value*sigma_landmark_y);

				ray_lengths.Add(Vector2.Distance(new Vector2(vector_x,vector_y),origin)/10f);
			} 
			else {
				
				Vector3 vector_point = transform.TransformPoint (raycastRender.normalized * 10 * 20);
				lineRenderer.SetPosition (1, raycastRender.normalized * 10 * 20);

				float vector_x = normrand (vector_point.x, sigma_lx_slider.value*sigma_landmark_x);
				float vector_y = normrand (vector_point.y, sigma_ly_slider.value*sigma_landmark_y);

				ray_lengths.Add(Vector2.Distance(new Vector2(vector_x,vector_y),origin)/10f);
			}


		}

		return ray_lengths;
			
	}
	void ResetSensors()
	{
		foreach (GameObject sensor in sensors)
		{
			LineRenderer lineRenderer = sensor.GetComponentInParent<LineRenderer> ();
			lineRenderer.SetPosition (1, new Vector3 (0,0,0));
			lineRenderer.SetWidth ((float).03, (float).03);
		}
	}

	void OutputScan()
	{
		string output = "{"+scan_measure[0]+"f";
		for(int i = 1; i < scan_measure.Count; i++)
		{
			output += "," + scan_measure [i].ToString ("N3")+"f";
		}
		output += "}";

		Debug.Log (output);

	}
		

	public void GraphReference(List<float> ray_lengths)
	{
		int Raysize = ray_lengths.Count;
		for (int i = 0; i < Raysize; i++)
		{
			if (reference_markers.Count < i+1) {
				//create new marker
				GameObject get_reference_marker = (GameObject)Instantiate (reference_marker);
				get_reference_marker.name = "reference_marker_" + reference_markers.Count;
				reference_markers.Add (get_reference_marker);
			}
				
			float angle = -angle_range / 2 + i * (angle_range) / (Raysize - 1);

			reference_markers[i].transform.position = new Vector2 (ray_lengths [i] * Mathf.Cos (angle), ray_lengths [i] * Mathf.Sin (angle)-20);

		}
	}

	public void GraphMeasure(List<float> ray_x, List<float> ray_y)
	{
		int Raysize = ray_x.Count;
		for (int i = 0; i < Raysize; i++)
		{
			if (measure_markers.Count < i+1) {
				//create new marker
				GameObject get_measure_marker = (GameObject)Instantiate (measure_marker);
				get_measure_marker.name = "measure_marker_" + measure_markers.Count;
				measure_markers.Add (get_measure_marker);
			}

			measure_markers[i].transform.position = new Vector2 (ray_x[i], ray_y[i]-20); //-20

		}
	}

	public void GraphRef(List<float> ray_x, List<float> ray_y)
	{
		int Raysize = ray_x.Count;
		for (int i = 0; i < Raysize; i++)
		{
			if (reference_markers.Count < i+1) {
				//create new marker
				GameObject get_reference_marker = (GameObject)Instantiate (reference_marker);
				get_reference_marker.name = "reference_marker_" + reference_markers.Count;
				reference_markers.Add (get_reference_marker);
			}

			reference_markers[i].transform.position = new Vector2 (ray_x[i], ray_y[i]-20);

		}
	}


	public string Sense_Obs()
	{
		string obs_sense = "";
		for (int i = 0; i < scan_measure.Count; i++) 
		{
			obs_sense += (scan_measure[i]).ToString ("N4") + " ";
		}
		return obs_sense;
	}

	public float get_x()
	{
		//return normrand (transform.position.x, .3f);
		return transform.position.x;
	}
	public float get_y()
	{
		//return normrand (transform.position.y, .3f);
		return transform.position.y;
	}
	public float get_t()
	{
		//return normrand (transform.eulerAngles.z, 10f);
		return transform.eulerAngles.z*Mathf.Deg2Rad;
	}

	public int getStatus()
	{
		return update_status;
	}
	public int getMapStatus()
	{
		return map_status;
	}
	public float getOdometry_x()
	{
		//return normrand (x_drive, sigma_pos_x*sigma_x_slider.value);
		return x_drive;
	}
	public float getDelta_x()
	{
		return delta_x;
	}
	public float getOdometry_t()
	{
		//return normrand (t_drive*Mathf.Deg2Rad, sigma_pos_t*sigma_t_slider.value);
		return t_drive*Mathf.Deg2Rad;
	}
	public float getDelta_t()
	{
		return delta_t*Mathf.Deg2Rad;
	}

}
