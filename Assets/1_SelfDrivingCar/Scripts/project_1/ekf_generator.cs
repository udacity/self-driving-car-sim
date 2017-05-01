using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Text;
using System.IO;
using UnityEngine.SceneManagement;

public class ekf_generator : MonoBehaviour {

	private List<string> sensors;

	public GameObject lidar_marker;
	public GameObject radar_marker;
	public GameObject estimate_marker;

	public List<GameObject> sensor_markers;
	private List<GameObject> estimate_markers;

	private List<float> x_positions;
	private List<float> y_positions;
	private List<float> t_positions;

	private double scale = .1;
	private int time_step;

	private int dataset = 1;

	private bool running;

	// UI
	public Text time;
	public Text run_button;
	public Text rmse_x;
	public Text rmse_y;
	public Text rmse_vx;
	public Text rmse_vy;

	// Use this for initialization
	void Start () {

		running = false;
		run_button.text = "Start";

		time_step = 0;
		time.text = "Time Step: "+time_step.ToString ();

		//Clear sensor markers
		if (sensor_markers != null) 
		{
			foreach (GameObject get_sensor_marker in sensor_markers)
			{
				if (get_sensor_marker != null) {
					Destroy (get_sensor_marker);
				}
			}
		}
		//Clear estimate markers
		if (estimate_markers != null) 
		{
			foreach (GameObject get_estimate_marker in estimate_markers)
			{
				if (get_estimate_marker != null) {
					Destroy (get_estimate_marker);
				}
			}
		}

		sensor_markers = new List<GameObject> ();
		estimate_markers = new List<GameObject> ();
		sensors = new List<string> ();

		x_positions = new List<float> ();
		y_positions = new List<float> ();
		t_positions = new List<float> ();

		string data1_path = "Assets/1_SelfDrivingCar/resources/sample-laser-radar-measurement-data-1.txt";
		string data2_path = "Assets/1_SelfDrivingCar/resources/sample-laser-radar-measurement-data-2.txt";


		if (dataset == 1) {
			Load (data1_path);
		} 
		else
		{
			Load (data2_path);
		}
			

		transform.position = new Vector3 (x_positions [time_step], y_positions [time_step], 0);
		transform.rotation = Quaternion.AngleAxis (t_positions [time_step] * Mathf.Rad2Deg, Vector3.forward);

		sensor_markers[time_step].GetComponent<SpriteRenderer>().enabled = true;

	}

	public string sensor_Measure()
	{
		return sensors [time_step];
	}

	public void Estimate(float est_x, float est_y)
	{
		GameObject get_estimate_marker = (GameObject)Instantiate (estimate_marker);
		get_estimate_marker.GetComponent<SpriteRenderer>().enabled = true;
		get_estimate_marker.transform.position = new Vector3 ((float)(est_x*scale), (float)(est_y*scale), 0);
		get_estimate_marker.name = "estimate_marker_"+estimate_markers.Count;
		estimate_markers.Add (get_estimate_marker);
	}
	public void SetRmse(float x, float y, float vx, float vy)
	{
		rmse_x.text = "X: " + x.ToString("N4");
		rmse_y.text = "Y: " + y.ToString("N4");
		rmse_vx.text = "VX: " + vx.ToString("N4");
		rmse_vy.text = "VY: " + vy.ToString("N4");
	}
	
	// Update is called once per frame
	void Update () {

		if (running && time_step < x_positions.Count-1) 
		{

				time_step++;
				time.text = "Time Step: "+time_step.ToString ();

				transform.position = new Vector3 (x_positions [time_step], y_positions [time_step], 0);
				transform.rotation = Quaternion.AngleAxis (t_positions [time_step] * Mathf.Rad2Deg, Vector3.forward);

				sensor_markers[time_step].GetComponent<SpriteRenderer>().enabled = true;

		}
		if (running && time_step >= sensors.Count - 1) {
			ToggleRunning();
		}

		if (Input.GetKey (KeyCode.Escape)) 
		{
			SceneManager.LoadScene ("MenuScene");
		}
	}
	public void SetDataset1()
	{
		dataset = 1;
		Start ();
	}
	public void SetDataset2()
	{
		dataset = 2;
		Start();
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
		
	private bool Load(string fileName)
	{
		
		// Handle any problems that might arise when reading the text
		try
		{
			string line;
			// Create a new StreamReader, tell it which file to read and what encoding the file
			// was saved as
			StreamReader theReader = new StreamReader(fileName, Encoding.Default);
			// Immediately clean up the reader after this block of code is done.
			// You generally use the "using" statement for potentially memory-intensive objects
			// instead of relying on garbage collection.
			// (Do not confuse this with the using directive for namespace at the 
			// beginning of a class!)
			using (theReader)
			{
				// While there's lines left in the text file, do this:
				do
				{
					line = theReader.ReadLine();
					//Debug.Log (line);

					if (line != null)
					{
						CreateAttributes(line);
					}
				}
				while (line != null);
				// Done reading, close the reader and return true to broadcast success    

				theReader.Close();
				return true;
			}
		}
		// If anything broke in the try block, we throw an exception with information
		// on what didn't work
		catch (Exception e)
		{
			Console.WriteLine("{0}\n", e.Message);
			return false;
		}


	}

	private void CreateAttributes(string line)
	{
		string[] entries = line.Split('\t');
		if (entries.Length > 0)
		{
			if (String.Compare (entries [0], "R") == 0) 
			{
				//Create Path
				float pos_x = (float)(float.Parse (entries [5]) * scale);
				float pos_y = (float)(float.Parse (entries [6]) * scale);
				float vx = (float)(float.Parse (entries [7]));
				float vy = (float)(float.Parse (entries [8]));
				float pos_t = Mathf.Atan2 (vy, vx);

				x_positions.Add (pos_x);
				y_positions.Add (pos_y);
				t_positions.Add (pos_t);

				//Create Sense
				float rho = (float)(float.Parse (entries [1]));
				float angle = (float)(float.Parse (entries [2]));

				float marker_x = (float)(rho * Mathf.Cos (angle)*scale);
				float marker_y = (float)(rho * Mathf.Sin (angle)*scale);

				GameObject get_radar_marker = (GameObject)Instantiate (radar_marker);
				//get_radar_marker.GetComponent<SpriteRenderer>().enabled = true;
				get_radar_marker.transform.position = new Vector3 (marker_x, marker_y, 0);
				get_radar_marker.transform.rotation = Quaternion.AngleAxis (angle*Mathf.Rad2Deg, Vector3.forward);
				get_radar_marker.name = "radar_marker_"+sensor_markers.Count;

				sensor_markers.Add (get_radar_marker);

				//string radar_packet = line.Replace("\\n","");
				string radar_packet = entries[0]+"\\t"+entries[1]+"\\t"+entries[2]+"\\t"+entries[3]+"\\t"+entries[4]+"\\t"+entries[5]+"\\t"+entries[6]+"\\t"+entries[7]+"\\t"+entries[8];

				sensors.Add (radar_packet);
			} 
			else if (String.Compare (entries [0], "L") == 0) 
			{
				//Create Path
				float pos_x = (float)(float.Parse (entries [4]) * scale);
				float pos_y = (float)(float.Parse (entries [5]) * scale);
				float vx = (float)(float.Parse (entries [6]));
				float vy = (float)(float.Parse (entries [7]));
				float pos_t = Mathf.Atan2 (vy, vx);

				x_positions.Add (pos_x);
				y_positions.Add (pos_y);
				t_positions.Add (pos_t);

				//Create Sense
				float marker_x = (float)(float.Parse (entries [1])*scale);
				float marker_y = (float)(float.Parse (entries [2])*scale);

				GameObject get_lidar_marker = (GameObject)Instantiate (lidar_marker);
				//get_lidar_marker.GetComponent<SpriteRenderer>().enabled = true;
				get_lidar_marker.transform.position = new Vector3 (marker_x, marker_y, 0);
				get_lidar_marker.name = "lidar_marker_"+sensor_markers.Count;

				sensor_markers.Add (get_lidar_marker);

				string lidar_packet = entries[0]+"\\t"+entries[1]+"\\t"+entries[2]+"\\t"+entries[3]+"\\t"+entries[4]+"\\t"+entries[5]+"\\t"+entries[6]+"\\t"+entries[7];

				sensors.Add (lidar_packet);
			} 
			else 
			{
				Debug.Log ("recived an unexpected data line");
			}
				

		}

	}

		
}

