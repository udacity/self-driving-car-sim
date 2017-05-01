using System;
using UnityEngine.UI;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

using System.Runtime.InteropServices;

public class robot : MonoBehaviour {

	public float speed; // units per second
	private float angle_tracker;
	public float angle_rate; // radians per second
	private int frame_counter;
	private bool init;

	private List<GameObject> lidar_markers;
	private List<GameObject> radar_markers;

	public GameObject lidar_marker;
	public GameObject radar_marker;

	private long base_timestamp = 1477010443000000; // October 2016
	private long timestamp;
	private long previous_timestamp;

	private double delta_t_us; // delta t in microseconds
	public float lidar_std_laspx;
	public float lidar_std_laspy;
	public float radar_std_radr;
	public float radar_std_radphi;
	public float radar_std_radrd;

	public GameObject hunter;

	//UI
	public Text status;
	public Text time_step;
	public Text dist_target;
	public Text dist_best;
	private double dist_best_value;

	//Lidar Noise
	public InputField std_laspx;
	public Text std_laspx_text;
	public InputField std_laspy;
	public Text std_laspy_text;

	//Radar Noise
	public InputField std_radr;
	public Text std_radr_text;
	public InputField std_radphi;
	public Text std_radphi_text;
	public InputField std_radrd;
	public Text std_radrd_text;

	//script listener
	//if the script closed, need to reset scene
	private bool script_reset = false;

	private bool hunter_respawn;

	// Use this for initialization
	void Start () {

		frame_counter = 0;

		delta_t_us = .5e5;
		timestamp = base_timestamp;
		previous_timestamp = timestamp;

		lidar_markers = new List<GameObject> ();
		radar_markers = new List<GameObject> ();

		angle_tracker = 0;
		transform.position = new Vector3 (0.18f, -0.83f, 0.0f);
		transform.rotation = Quaternion.AngleAxis (0, Vector3.forward);

		init = false;

		//UI
		status.text = "";
		time_step.text = "Time Step: "+frame_counter.ToString ("N0");
		dist_best_value = Distance_Target();
		dist_target.text = "Distance to Target: "+dist_best_value.ToString ("N4");
		dist_best.text = "Best Distance: "+dist_best_value.ToString ("N4");

		hunter_respawn = false;

	}
	public void ScriptClosed()
	{
		script_reset = true;
	}

	public void Rotate()
	{

		double delta_t_s = (timestamp - previous_timestamp)/1e6;
			
		transform.Rotate(0, 0, (float)(delta_t_s*angle_rate));
		angle_tracker += (float)(delta_t_s*angle_rate);

	}
	public void Move()
	{

		double delta_t_s = (timestamp - previous_timestamp)/1e6;

		//Vector3 movement = new Vector3 (speed/60 * Mathf.Cos (angle), speed/60 * Mathf.Sin (angle), 0);
		Vector3 movement = transform.right*((float)(speed*delta_t_s));
		transform.position = transform.position + movement;
	}

	public string Lidar_Measure()
	{
		//GameObject marker = Instantiate(measurement,transform.position,transform.rotation);

		//marker.transform.position = transform.position;
		Vector3 noise = new Vector3(normrand(0,(float)(lidar_std_laspx)),normrand(0,(float)(lidar_std_laspy)),0);

	    GameObject get_lidar_marker = (GameObject)Instantiate (lidar_marker);
		get_lidar_marker.GetComponent<SpriteRenderer>().enabled = true;
		get_lidar_marker.transform.position = transform.position+noise;
		get_lidar_marker.name = "lidar_marker_"+lidar_markers.Count;

		lidar_measurement measure = (lidar_measurement) get_lidar_marker.GetComponent(typeof(lidar_measurement));

		measure.Set (get_lidar_marker.transform.position.x, get_lidar_marker.transform.position.y, timestamp, transform.position.x, transform.position.y, speed * Mathf.Cos ((float)angle_tracker*Mathf.Deg2Rad), speed * Mathf.Sin ((float)angle_tracker*Mathf.Deg2Rad));

		lidar_markers.Add (get_lidar_marker);

		return measure.packet ();

	}
	public string Radar_Measure()
	{
		float posx = transform.position.x;
		float posy = transform.position.y;
		float rho = Mathf.Sqrt (posx * posx + posy * posy)+ normrand (0, radar_std_radr);
		float angle = Mathf.Atan2 (posy, posx) + normrand (0, radar_std_radphi);

		float rho_dot = (posx * (speed * Mathf.Cos ((float)angle_tracker * Mathf.Deg2Rad)) + posy * (speed * Mathf.Sin ((float)angle_tracker * Mathf.Deg2Rad))) / rho + normrand (0, radar_std_radrd);
		float marker_x = rho * Mathf.Cos (angle);
		float marker_y = rho * Mathf.Sin (angle);

		GameObject get_radar_marker = (GameObject)Instantiate (radar_marker);
		get_radar_marker.GetComponent<SpriteRenderer>().enabled = true;
		get_radar_marker.transform.position = new Vector3 (marker_x, marker_y, 0);
		get_radar_marker.transform.rotation = Quaternion.AngleAxis (angle*Mathf.Rad2Deg, Vector3.forward);
		get_radar_marker.name = "radar_marker_"+radar_markers.Count;

		radar_measurement measure = (radar_measurement) get_radar_marker.GetComponent(typeof(radar_measurement));

		measure.Set(rho,angle,rho_dot,timestamp,transform.position.x, transform.position.y, speed * Mathf.Cos ((float)angle_tracker*Mathf.Deg2Rad), speed * Mathf.Sin ((float)angle_tracker*Mathf.Deg2Rad));

		radar_markers.Add (get_radar_marker);

		return measure.packet ();

	}

	public float normrand(float mean, float stdDev)
	{
		float u1 = 1.0f-Random.Range (0.0f, 1.0f); //uniform(0,1] random doubles
		float u2 = 1.0f-Random.Range (0.0f, 1.0f);
		float randStdNormal = Mathf.Sqrt((float)(-2.0 * Mathf.Log((float)u1,(float)2.718))) * Mathf.Sin((float)(2.0 * Mathf.PI * u2)); //random normal(0,1)
		float randNormal = (float)(mean + stdDev * randStdNormal); //random normal(mean,stdDev^2)
		return randNormal;
	}

	
	// Update is called once per frame
	void Update () {
		if (init) {
			Rotate ();
			Move ();
			previous_timestamp = timestamp;
			timestamp += (int)delta_t_us;
			frame_counter++;
			checkStatus ();
		} 
		else 
		{
			if (hunter_respawn) 
			{
				hunter get_hunter = (hunter) hunter.GetComponent(typeof(hunter));
				get_hunter.Restart ();

				dist_best_value = Distance_Target ();
				dist_target.text = "Distance to Target: " + dist_best_value.ToString ("N4");
				dist_best.text = "Best Distance: " + dist_best_value.ToString ("N4");

				hunter_respawn = false;

			}
			if (script_reset) 
			{
				Debug.Log ("scene is reset");
				Restart ();
				script_reset = false;
			}

		}


		//UI
		time_step.text = "Time Step: "+frame_counter.ToString ("N0");
		double dist_value = Distance_Target();
		dist_target.text = "Distance to Target: "+dist_value.ToString ("N4");
		if (dist_value < dist_best_value) 
		{
			dist_best_value = dist_value;
			dist_best.text = "Best Distance: " + dist_value.ToString ("N4");
		}
		cleanMarkers ();

		if (Input.GetKey (KeyCode.Escape)) 
		{
			SceneManager.LoadScene ("MenuScene");
		}
			
	}

	public bool Status()
	{
		return init;
	}


	public void Restart()
	{

		foreach (GameObject get_lidar_marker in lidar_markers)
		{
			if (get_lidar_marker != null) {
				Destroy (get_lidar_marker);
			}
		}
		foreach (GameObject get_radar_marker in radar_markers)
		{
			if (get_radar_marker != null) {
				Destroy (get_radar_marker);
			}
		}

		lidar_markers.Clear ();
		radar_markers.Clear ();

		frame_counter = 0;

		timestamp = base_timestamp;
		previous_timestamp = timestamp;

		lidar_markers = new List<GameObject> ();
		radar_markers = new List<GameObject> ();

		angle_tracker = 0;
		transform.position = new Vector3 (0.18f, -0.83f, 0.0f);
		transform.rotation = Quaternion.AngleAxis (0, Vector3.forward);

		init = false;

		//UI
		status.text = "";
		time_step.text = "Time Step: "+frame_counter.ToString ("N0");
		dist_best_value = Distance_Target();
		dist_target.text = "Distance to Target: "+dist_best_value.ToString ("N4");
		dist_best.text = "Best Distance: "+dist_best_value.ToString ("N4");

		hunter_respawn = true;

	}
	// dont let marker list get too large
	public void cleanMarkers()
	{
		if(lidar_markers.Count > 50)
		{
			for( int i = 0; i < (lidar_markers.Count - 50); i++)
			{
				GameObject get_lidar_marker = lidar_markers[i];
				Destroy (get_lidar_marker);
			}

			lidar_markers.RemoveRange (0, (lidar_markers.Count - 50));
		}
		if(radar_markers.Count > 50)
		{
			for( int i = 0; i < (radar_markers.Count - 50); i++)
			{
				GameObject get_radar_marker = radar_markers[i];
				Destroy (get_radar_marker);
			}

			radar_markers.RemoveRange (0, (radar_markers.Count - 50));
		}
	}


	public void Run()
	{

		if (init) 
		{
			Restart ();
		} 
		else 
		{
			Restart ();
			init = true;
		}

	}

	public double Distance_Target()
	{
		return Math.Sqrt ((transform.position.x - hunter.transform.position.x) * (transform.position.x - hunter.transform.position.x) + (transform.position.y - hunter.transform.position.y) * (transform.position.y - hunter.transform.position.y));
	}

	public void checkStatus()
	{
		
		if ( Distance_Target() < 0.03 * 1.5) 
		{
			hunter_respawn = false;
			status.text = "Success! You caught the robot!"; 
			init = false;
		} 
		else if (frame_counter > 5000) 
		{
			hunter_respawn = false;
			status.text = "You Ran out of time"; 
			init = false;
		}
	}

	public void Set_laspx()
	{
		lidar_std_laspx = float.Parse (std_laspx.text);
		std_laspx_text.text = "std_laspx: " + lidar_std_laspx.ToString ("N4");
	}
	public void Set_laspy()
	{
		lidar_std_laspy = float.Parse (std_laspy.text);
		std_laspy_text.text = "std_laspy: " + lidar_std_laspy.ToString ("N4");
	}
	public void Set_radr()
	{
		radar_std_radr = float.Parse (std_radr.text);
		std_radr_text.text = "std_radr: " + radar_std_radr.ToString ("N4");
	}
	public void Set_radphi()
	{
		radar_std_radphi = float.Parse (std_radphi.text);
		std_radphi_text.text = "std_radphi: " + radar_std_radphi.ToString ("N4");
	}
	public void Set_radrd()
	{
		radar_std_radrd = float.Parse (std_radrd.text);
		std_radrd_text.text = "std_radrd: " + radar_std_radrd.ToString ("N4");
	}

}
