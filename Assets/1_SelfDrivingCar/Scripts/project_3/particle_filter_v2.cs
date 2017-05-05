using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Text;
using System.IO;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class particle_filter_v2 : MonoBehaviour {

	private List<GameObject> map;
	public GameObject landmark;
	public GameObject particle;

	public float sigma_pos_x;
	public float sigma_pos_y;
	public float sigma_pos_theta;

	public float sigma_landmark_x;
	public float sigma_landmark_y;

	[SerializeField] private List<GameObject> sensors;
	[SerializeField] private List<GameObject> Particlesensors;

	private List<float> x_positions;
	private List<float> y_positions;
	private List<float> t_positions;

	private List<float> control_velocity;
	private List<float> control_yawrate;

	private List<float> x_obs;
	private List<float> y_obs;

	private double scale = .1;
	private int time_step;

	private float x_cum;
	private float y_cum;
	private float yaw_cum;

	private float x_tot_err;
	private float y_tot_err;
	private float yaw_tot_err;

	private bool running;

	// UI
	public Text run_button;
	public Text time;
	public Text status;
	public Text average_error;
	private bool status_check;

	//if there is new data to process
	private bool process_data;
	private bool script_running = false;

	public TextAsset map_data;
	public TextAsset gt_data;
	public TextAsset control_data;
	[SerializeField] private List<TextAsset> observation_data;

	// Use this for initialization
	void Start () {

		running = false;
		run_button.text = "Start";

		x_cum = 0;
		y_cum = 0;
		yaw_cum = 0;

		x_tot_err = 0;
		y_tot_err = 0;
		yaw_tot_err = 0;

		average_error.text = "Error: x y yaw ";
		status.text = "";
		status_check = false;

		time_step = 0;
		time.text = "Time Step: "+time_step.ToString ();

		//Clear landmarks if restarting
		if (map != null) 
		{
			foreach (GameObject landmark in map) {
				if (landmark != null) {
					Destroy (landmark);
				}
			}
			map.Clear ();
		}

		map = new List<GameObject> ();

		x_positions = new List<float> ();
		y_positions = new List<float> ();
		t_positions = new List<float> ();

		control_velocity = new List<float> ();
		control_yawrate = new List<float> ();

		x_obs = new List<float> ();
		y_obs = new List<float> ();

		x_obs.Clear ();
		y_obs.Clear ();

		Load (map_data, 0);
		Load (gt_data, 1);
		Load (observation_data[time_step], 2);
		Load (control_data, 3);
			
		transform.position = new Vector3 (x_positions [time_step], y_positions [time_step], 0);
		transform.rotation = Quaternion.AngleAxis (t_positions [time_step] * Mathf.Rad2Deg, Vector3.forward);

		//Sense Noisy Position
		float sense_x = Sense_x();
		float sense_y = Sense_y();
		float sense_theta = Sense_theta();
		Estimate (sense_x, sense_y, sense_theta);

		// Car Sense
		ResetSensors ();
		SenseDistance ();

		ResetParticleSensors ();

		//flag new data is ready to process
		process_data = true;

	}
	
	// Update is called once per frame
	void FixedUpdate () {

		//dont run past time interval and dont run until last data was processed
		if (running && time_step < x_positions.Count-1 && (!process_data||!script_running)) 
		{
				

				time_step++;
				time.text = "Time Step: "+time_step.ToString ();

				transform.position = new Vector3 (x_positions [time_step], y_positions [time_step], 0);
				transform.rotation = Quaternion.AngleAxis (t_positions [time_step] * Mathf.Rad2Deg, Vector3.forward);

				x_obs.Clear ();
				y_obs.Clear ();
				
				Load (observation_data[time_step], 2);
				
				// Car Sense
				ResetSensors ();
				SenseDistance ();

				//flag new data is ready to process
				process_data = true;
				

		}
		if (running && time_step >= x_positions.Count-1) {
			if (!status_check && script_running) 
			{
				status.text = "Success! Your particle filter passed!"; 
				status_check = true;
			}
			ToggleRunning();
		}
		if (Input.GetKey (KeyCode.Escape)) 
		{
			SceneManager.LoadScene ("MenuScene");
		}
	}

	public void ToggleRunning()
	{
		
		running = !running;
		if (running) 
		{
			run_button.text = "Pause";
		} 
		else 
		{
			run_button.text = "Start";
		}
	}

	public void Restart()
	{
		Start ();
	}
	public bool isRunning()
	{
		return running;
	}
	public bool isReadyProcess()
	{
		return process_data;
	}
	public void Processed()
	{
		process_data = false;
	}

	public float normrand(float mean, float stdDev)
	{
		float u1 = 1.0f-Random.Range (0.0f, 1.0f); //uniform(0,1] random doubles
		float u2 = 1.0f-Random.Range (0.0f, 1.0f);
		float randStdNormal = Mathf.Sqrt((float)(-2.0 * Mathf.Log((float)u1,(float)2.718))) * Mathf.Sin((float)(2.0 * Mathf.PI * u2)); //random normal(0,1)
		float randNormal = (float)(mean + stdDev * randStdNormal); //random normal(mean,stdDev^2)
		return randNormal;
	}

	public void Estimate(float estimate_x, float estimate_y, float estimate_theta)
	{
		particle.transform.position = new Vector3 ((float)(estimate_x*scale), (float)(estimate_y*scale), 0);
		particle.transform.rotation = Quaternion.AngleAxis (estimate_theta * Mathf.Rad2Deg, Vector3.forward);

		if (time_step > 0) 
		{
			float x_error = (float)((1 / scale) * Math.Abs (x_positions [time_step] - estimate_x*scale));
			float y_error = (float)((1 / scale) * Math.Abs (y_positions [time_step] - estimate_y*scale));
			float yaw_error = (float)(Math.Abs (t_positions [time_step] - estimate_theta));

			x_tot_err += x_error;
			y_tot_err += y_error;
			yaw_tot_err += yaw_error;

			x_cum = x_tot_err / (time_step + 1);
			y_cum = y_tot_err / (time_step + 1);
			yaw_cum = yaw_tot_err / (time_step + 1);

			SetError (time_step, x_positions.Count, x_cum, y_cum, yaw_cum);
		}

	}
	public float Sense_x()
	{
		return  normrand ((float)(x_positions[time_step]*(1/scale)), sigma_pos_x);

	}
	public float Sense_y()
	{
		return normrand ((float)(y_positions [time_step]*(1/scale)), sigma_pos_y); 
		 
	}
	public float Sense_theta()
	{
		
		return normrand (t_positions [time_step], sigma_pos_theta);  
		
	}
	public float getVelocity()
	{
		
		if(time_step == 0)
		{
			return control_velocity[0];
		}
		else
		{
			return control_velocity[time_step-1];
		}

	}
	public float getYawrate()
	{
		
		if(time_step == 0)
		{
			return control_yawrate[0];
		}
		else
		{
			return control_yawrate[time_step-1];
		}
	}
	public string Sense_Obsx()
	{
		string obs_x_sense = "";
		for (int i = 0; i < x_obs.Count; i++) 
		{
			obs_x_sense += (normrand((float)(x_obs[i]*(1/scale)), sigma_landmark_x)).ToString ("N4") + " ";
		}
		return obs_x_sense;
	}
	public string Sense_Obsy()
	{
		string obs_y_sense = "";
		for (int i = 0; i < y_obs.Count; i++) 
		{
			obs_y_sense += (normrand((float)(y_obs[i]*(1/scale)), sigma_landmark_y)).ToString ("N4") + " ";
		}
		return obs_y_sense;
	}


	void SenseDistance()
	{
		
		if (sensors.Count != 0 && x_obs.Count != 0) {
			
			int observations = x_obs.Count;
			int obs_index = 0;

			foreach (GameObject sensor in sensors) 
			{
				
				if (obs_index < observations) 
				{
					LineRenderer lineRenderer = sensor.GetComponentInParent<LineRenderer> ();


					lineRenderer.SetPosition (1, new Vector3 (20 * x_obs [obs_index], 20 * y_obs [obs_index], 0));
					//lineRenderer.SetPosition (1, new Vector3 (20*x_obs [obs_index]*Mathf.Cos(t_positions [time_step])-20*y_obs [obs_index]*Mathf.Sin(t_positions [time_step]), 20*x_obs [obs_index]*Mathf.Sin(t_positions [time_step])+20*y_obs [obs_index]*Mathf.Cos(t_positions [time_step]), 0));
					lineRenderer.SetWidth ((float).03, (float).03);
					obs_index++;
				}

			}

		}
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
	private void Load(TextAsset data, int function)
	{
		var arrayString = data.text.Split ('\n');
		foreach (var line in arrayString) 
		{

			if (!String.IsNullOrEmpty (line)) 
			{
				if (function == 0) {
					CreateLandmark (line);
				}
				if (function == 1) {
					CreateGT (line);
				}
				if (function == 2) {
					CreateObs (line);
				}
				if (function == 3) {
					CreateControl (line);
				}

			}

		}
	}
	private void CreateLandmark(string line)
	{
		string[] entries = line.Split('\t');
		if (entries.Length > 0)
		{
			GameObject get_landmark = (GameObject)Instantiate (landmark);

			get_landmark.GetComponent<SpriteRenderer>().enabled = true;

			float pos_x = (float)(float.Parse(entries[0])*scale);
			float pos_y = (float)(float.Parse(entries[1])*scale);

			get_landmark.transform.position = new Vector3 (pos_x, pos_y, 0);
			get_landmark.name = "landmark_"+(map.Count+1);

			map.Add (get_landmark);

		}

	}
	private void CreateGT(string line)
	{
		string[] entries = line.Split(' ');
		if (entries.Length > 0)
		{


			float pos_x = (float)(float.Parse(entries[0])*scale);
			float pos_y = (float)(float.Parse(entries[1])*scale);
			float pos_t = (float)(float.Parse(entries[2]));

			x_positions.Add (pos_x);
			y_positions.Add (pos_y);
			t_positions.Add (pos_t);

		}

	}
	private void CreateObs(string line)
	{
		string[] entries = line.Split(' ');
		if (entries.Length > 0)
		{


			float pos_x = (float)(float.Parse(entries[0])*scale);
			float pos_y = (float)(float.Parse(entries[1])*scale);

			x_obs.Add (pos_x);
			y_obs.Add (pos_y);


		}

	}
	private void CreateControl(string line)
	{
		string[] entries = line.Split(' ');
		if (entries.Length > 0)
		{


			float velocity = (float)(float.Parse(entries[0]));
			float yawrate = (float)(float.Parse(entries[1]));

			control_velocity.Add (velocity);
			control_yawrate.Add (yawrate);


		}

	}
	public void SenseParticleDistance(string associations, string sense_x, string sense_y)
	{
		associations = associations.Remove (0, 1);
		associations = associations.Remove (associations.Length - 1, 1);
		sense_x = sense_x.Remove (0, 1);
		sense_x = sense_x.Remove (sense_x.Length - 1, 1);
		sense_y = sense_y.Remove (0, 1);
		sense_y = sense_y.Remove (sense_y.Length - 1, 1);

		if (associations.Length != 0 && sense_x.Length != 0 && sense_y.Length != 0) {

			string[] psa_entries = associations.Split (' ');
			string[] psx_entries = sense_x.Split (' ');
			string[] psy_entries = sense_y.Split (' ');
			if (psa_entries.Length > 0) {

				// Particle Sense
				ResetParticleSensors ();

				float px_position = particle.transform.position.x;
				float py_position = particle.transform.position.y;
				float pos_t = (float)(particle.transform.rotation.eulerAngles.z * Mathf.Deg2Rad);

				int obs_index = 0;

				foreach (GameObject sensor in Particlesensors) {
				

					if (obs_index < psa_entries.Length) {
						//draw particles sensors
						LineRenderer lineRenderer = sensor.GetComponentInParent<LineRenderer> ();


						float x_d = 20 * ((float)(float.Parse (psx_entries [obs_index]) * scale) - px_position);
						float y_d = 20 * ((float)(float.Parse (psy_entries [obs_index]) * scale) - py_position);

						lineRenderer.SetPosition (1, new Vector3 (x_d * Mathf.Cos (-pos_t) - y_d * Mathf.Sin (-pos_t), x_d * Mathf.Sin (-pos_t) + y_d * Mathf.Cos (-pos_t), 0));
						lineRenderer.SetWidth ((float).03, (float).03);

						//draw associated landmark sensors
						int associatiton = int.Parse (psa_entries [obs_index]) - 1;
						LineRenderer landmarkRenderer = map [associatiton].GetComponentInParent<LineRenderer> ();

						x_d = 4 * ((float)(float.Parse (psx_entries [obs_index]) * scale) - map [associatiton].transform.position.x);
						y_d = 4 * ((float)(float.Parse (psy_entries [obs_index]) * scale) - map [associatiton].transform.position.y);

						landmarkRenderer.SetPosition (1, new Vector3 (x_d, y_d, 0));
						landmarkRenderer.SetWidth ((float).03, (float).03);


						obs_index++;
					}
				}
			}
		}
	}
	void ResetParticleSensors()
	{
		for (int i = 0; i < map.Count; i++) 
		{
			LineRenderer lineRenderer = map[i].GetComponentInParent<LineRenderer> ();
			lineRenderer.SetPosition (1, new Vector3 (0,0,0));
			lineRenderer.SetWidth ((float).03, (float).03);
		}

		foreach (GameObject sensor in Particlesensors)
		{
			LineRenderer lineRenderer = sensor.GetComponentInParent<LineRenderer> ();
			lineRenderer.SetPosition (1, new Vector3 (0,0,0));
			lineRenderer.SetWidth ((float).03, (float).03);
		}
	}

	public void SetError(int time_step, int max_time, float x, float y, float yaw)
	{
		average_error.text = "Error: x " + x.ToString ("#.000") + " y " + y.ToString ("#.000") + " yaw " + yaw.ToString ("#.000");

		if (time_step > 100 & !status_check) 
		{
			if (x > 1) 
			{
				status.text = "Your x error is larger than the max"; 
				status_check = true;
			} 
			else if (y > 1) 
			{
				status.text = "Your y error is larger than the max"; 
				status_check = true;
			} 
			else if (yaw > 0.05) 
			{
				status.text = "Your yaw error is larger than the max"; 
				status_check = true;
			}
		}
	}
	public void OpenScript()
	{
		script_running = true;
	}
	public void CloseScript()
	{
		script_running = false;
	}
		
}

