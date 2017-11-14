using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class slam_controller_icp : MonoBehaviour {
	[SerializeField] private List<GameObject> sensors;
	[SerializeField] private List<GameObject> landmarks;
	private int update_status;
	private float x_drive;
	private float t_drive;
	private List<float> scan_measure;
	private int moves;

	public Slider sigma_x_slider;
	public Slider sigma_t_slider;
	public Slider sigma_lx_slider;
	public Slider sigma_ly_slider;

	public Text value_x;
	public Text value_y;
	public Text value_lx;
	public Text value_ly;

	[SerializeField] private List<GameObject> sense_landmarks;
	public GameObject sense_car;

	private float sigma_pos_x;
	private float sigma_pos_t;
	private float sigma_landmark_x;
	private float sigma_landmark_y;

	private float angle_range = 2*Mathf.PI;

	public GameObject measure_marker;
	private List<GameObject> measure_markers;

	public GameObject reference_marker;
	private List<GameObject> reference_markers;

	// Use this for initialization
	void Start () {
		update_status = 0;
		x_drive = 0;
		t_drive = 0;
		moves = 0;

		sigma_pos_x = 0.01f;
		sigma_pos_t = 0.01f;
		sigma_landmark_x = 1.0f;
		sigma_landmark_y = 1.0f;

		reference_markers = new List<GameObject>();
		measure_markers = new List<GameObject>();

		scan_measure = SenseDistance ();
		GraphReference (scan_measure);


	}

	public bool inboundary (float x_drive)
	{
		Vector3 test_position = transform.position + transform.right * x_drive;

		return ((test_position.x > -4) && (test_position.x < 5) && (test_position.y > -5) && (test_position.y < 5));

	}

	void UpdateSliderValues()
	{
		value_x.text = (sigma_x_slider.value*sigma_pos_x).ToString ("N4");
		value_y.text = (sigma_t_slider.value*sigma_pos_t).ToString ("N4");
		value_lx.text = (sigma_lx_slider.value*sigma_landmark_x).ToString ("N4");
		value_ly.text = (sigma_ly_slider.value*sigma_landmark_y).ToString ("N4");

	}
	
	// Update is called once per frame
	void FixedUpdate () {

		x_drive = 0;
		t_drive = 0;

		//control
		if (Input.GetKey(KeyCode.UpArrow)) {
			x_drive = .025f;
		}
		else if (Input.GetKey(KeyCode.DownArrow)) {
			x_drive = -.025f;
		}
		if (Input.GetKey(KeyCode.RightArrow)) {
			t_drive = -3f;
		}
		else if (Input.GetKey(KeyCode.LeftArrow)) {
			t_drive = 3f;
		}
			
		if (inboundary(x_drive) &&  (x_drive != 0 || t_drive != 0) ) 
		{
			update_status = 1;
			//Debug.Log ("moves: " + moves);
			moves++;

			transform.position += transform.right * x_drive;
			transform.Rotate (Vector3.forward * t_drive);
			//Debug.Log("Pose2d odometry( "+x_drive+", 0, "+t_drive*Mathf.Deg2Rad+")");
		}


		// Car Sense
		ResetSensors ();
		scan_measure = SenseDistance ();

		if (Input.GetKey (KeyCode.Space)) {
			//GraphMeasure (scan_measure);
			OutputScan ();
		}

		UpdateSliderValues();

		/*
		if ((x_drive != 0 || t_drive != 0) && (landmark_measure.Count > 0)) {
			foreach (Vector3 obs in landmark_measure) {
				Debug.Log ("landmark "+obs[0]+" Point2d measure( " + obs [1] + ", " + obs [2] + ")");
			}
		}
		*/
			
	}
	public void resetStatus()
	{
		update_status = 0;
	}

	public void plot_landmarks(List<float> my_landmark_i, List<float> my_landmark_x, List<float> my_landmark_y)
	{
		for (int i = 0; i < my_landmark_i.Count; i++)
		{
			sense_landmarks[(int)my_landmark_i[i]].transform.position = new Vector3 (my_landmark_x[i]+10, my_landmark_y [i], 0);
		}
	}

	public void plot_car(float car_x, float car_y, float car_t)
	{
		sense_car.transform.position = new Vector3 (car_x+10, car_y, 0);
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

			float angle = -angle_range / 2 + i * (angle_range) / (Raysize - 1);

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

				ray_lengths.Add(Vector2.Distance(new Vector2(vector_point.x,vector_point.y),origin)/10f);
			} 
			else {
				
				Vector3 vector_point = transform.TransformPoint (raycastRender.normalized * 10 * 20);
				lineRenderer.SetPosition (1, raycastRender.normalized * 10 * 20);

				ray_lengths.Add(Vector2.Distance(new Vector2(vector_point.x,vector_point.y),origin)/10f);
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
		

	void GraphReference(List<float> ray_lengths)
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

			reference_markers[i].transform.position = new Vector2 (ray_lengths [i] * Mathf.Cos (angle)+10, ray_lengths [i] * Mathf.Sin (angle));

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

			measure_markers[i].transform.position = new Vector2 (ray_x[i]+10, ray_y[i]);

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
		return transform.eulerAngles.z;
	}

	public int getStatus()
	{
		return update_status;

	}
	public float getOdometry_x()
	{
		//return normrand (x_drive, sigma_pos_x*sigma_x_slider.value);
		return x_drive;
	}
	public float getOdometry_t()
	{
		//return normrand (t_drive*Mathf.Deg2Rad, sigma_pos_t*sigma_t_slider.value);
		return t_drive;
	}
}
