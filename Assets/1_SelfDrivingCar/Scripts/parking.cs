using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Text;
using System.IO;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class parking : MonoBehaviour {

	private List<float> x_positions;
	private List<float> y_positions;
	private List<float> t_positions;

	private int time_step;
	private double current_time;

	Rigidbody rb;

	public TextAsset parking_data;

	// Use this for initialization
	void Start () {

		rb = GetComponent<Rigidbody> ();

		current_time = 0;

		time_step = 0;

		x_positions = new List<float> ();
		y_positions = new List<float> ();
		t_positions = new List<float> ();

		Load (parking_data);
			
		transform.position = new Vector3 (x_positions [time_step], rb.position.y, y_positions [time_step]);
		transform.rotation = Quaternion.AngleAxis (90-t_positions [time_step] * Mathf.Rad2Deg, Vector3.up);

	}
	
	// Update is called once per frame
	void FixedUpdate () {

		//dont run past time interval and dont run until last data was processed
		if (time_step < x_positions.Count-1) 
		{
			if (current_time % 1 == 0)
			{
				time_step++;
				
				transform.position = new Vector3 (x_positions [time_step], rb.position.y, y_positions [time_step]);
				transform.rotation = Quaternion.AngleAxis (90-t_positions [time_step] * Mathf.Rad2Deg, Vector3.up);

				current_time = 0;

				Debug.Log ("time_step " + time_step);
			}
			current_time++;
		}


	}
		
	private void Load(TextAsset data)
	{
		var arrayString = data.text.Split ('\n');
		foreach (var line in arrayString) 
		{

			if (!String.IsNullOrEmpty (line)) 
			{
				CreateGT (line);
			}

		}
	}

	private void CreateGT(string line)
	{
		string[] entries = line.Split('\t');
		if (entries.Length > 0)
		{


			float pos_x = (float)(float.Parse(entries[0]));
			float pos_y = (float)(float.Parse(entries[1]));
			float pos_t = (float)(float.Parse(entries[2]));

			x_positions.Add (pos_x-(float)(4.47/2));
			y_positions.Add (pos_y);
			t_positions.Add (pos_t);

		}

	}
		
}

