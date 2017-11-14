using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class slam_controller : MonoBehaviour {
	[SerializeField] private List<GameObject> sensors;
	[SerializeField] private List<GameObject> landmarks;
	private int update_status;
	private float x_drive;
	private float t_drive;
	private List<Vector3> landmark_measure;
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
			Debug.Log ("moves: " + moves);
			moves++;

			transform.position += transform.right * x_drive;
			transform.Rotate (Vector3.forward * t_drive);
			//Debug.Log("Pose2d odometry( "+x_drive+", 0, "+t_drive*Mathf.Deg2Rad+")");
		}


		// Car Sense
		ResetSensors ();
		landmark_measure = SenseDistance ();

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


	List<Vector3> SenseDistance()
	{
		List<Vector3> landmark_measure = new List<Vector3> ();
		int obs_index = 0;

		for(int landmark_index = 0; landmark_index < landmarks.Count; landmark_index++) 
		{

			GameObject landmark = landmarks[landmark_index];

			if (Vector3.Distance (landmark.transform.position, transform.position) < 1.4) {
				LineRenderer lineRenderer = sensors[obs_index].GetComponentInParent<LineRenderer> ();
			
				float t_position = -transform.rotation.eulerAngles.z*Mathf.Deg2Rad;

				float x_obs = landmark.transform.position.x-transform.position.x;
				float y_obs = landmark.transform.position.y-transform.position.y;

				float measure_x = x_obs * Mathf.Cos (t_position) - y_obs * Mathf.Sin (t_position);
				float measure_y = x_obs * Mathf.Sin (t_position) + y_obs * Mathf.Cos (t_position);

				lineRenderer.SetPosition (1, new Vector3 (measure_x*20, measure_y*20, 0));
				lineRenderer.SetWidth ((float).03, (float).03);

				landmark_measure.Add(new Vector3(landmark_index,measure_x,measure_y));

				obs_index++;
			}

		}
		return landmark_measure;
			
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

	public string Sense_Obs()
	{
		string obs_sense = "";
		for (int i = 0; i < landmark_measure.Count; i++) 
		{
			obs_sense += (landmark_measure[i][0]).ToString ("N4") + " ";
		}
		return obs_sense;
	}
	public string Sense_Obsx()
	{
		string obs_x_sense = "";
		for (int i = 0; i < landmark_measure.Count; i++) 
		{
			float noise_landmark_x = normrand (landmark_measure [i] [1], sigma_landmark_x*sigma_lx_slider.value);
			obs_x_sense += (noise_landmark_x).ToString ("N4") + " ";
		}
		return obs_x_sense;
	}
	public string Sense_Obsy()
	{
		string obs_y_sense = "";
		for (int i = 0; i < landmark_measure.Count; i++) 
		{
			float noise_landmark_y = normrand (landmark_measure [i] [2], sigma_landmark_y*sigma_ly_slider.value);
			obs_y_sense += (noise_landmark_y).ToString ("N4") + " ";
		}
		return obs_y_sense;
	}

	public int getStatus()
	{
		return update_status;

	}
	public float getOdometry_x()
	{
		return normrand (x_drive, sigma_pos_x*sigma_x_slider.value);
		//return x_drive;
	}
	public float getOdometry_t()
	{
		return normrand (t_drive*Mathf.Deg2Rad, sigma_pos_t*sigma_t_slider.value);
		//return t_drive;
	}
}
