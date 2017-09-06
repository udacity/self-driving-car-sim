using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class perfect_controller : MonoBehaviour {

	private Rigidbody rb;
	private Collider collider;
	private Vector3 Offset = new Vector3 (0.3f, 0.0f, 0.0f);
	//Frame rate is 50FPS so delta t is .02
	//track points for the next second so 50 points in reference to global coordinates

	private List<float> x_points;
	private List<float> y_points;


	public GameObject next_point;
	private List<GameObject> way_points;


	private bool simulator_process;
	private bool background_process;
	private bool server_process;
	private bool script_running = false;

	// Use this for initialization
	void Start () {
		
		rb = GetComponent<Rigidbody> (); 
		collider = GetComponent<Collider> ();
		way_points = new List<GameObject> ();

		x_points = new List<float> ();
		y_points = new List<float> ();

		//by default create 50 points
		createPoints (50);

		//flag new data is ready to process
		simulator_process = false;
		server_process = true;

		Time.fixedDeltaTime = 0.02f;
	}

	public void setControlPath(List<float> x_set, List<float> y_set)
	{
		//start at the point that closes to the car

		//add more points if we need to
		if (x_set.Count > way_points.Count) 
		{
			createPoints (x_set.Count - way_points.Count);
		}

		float dist = 10000;
		int start_index = 0;

		for (int i = 0; i < x_set.Count; i++)
		{
			Vector2 car_pos = new Vector2 (transform.position.x, transform.position.z); 
			Vector2 point_pos = new Vector2 (x_set[i], y_set[i]); 
			if (Vector2.Distance (car_pos, point_pos) < dist) {
				dist = Vector2.Distance (car_pos, point_pos);
				start_index = i;
			}
		}
			
		bool behind_path = false;

		if (start_index != 0) {
			x_set = x_set.GetRange (start_index, x_set.Count - start_index);
			y_set = y_set.GetRange (start_index, y_set.Count - start_index);
		} 
		else 
		{
			//Debug.Log ("yes its zero with dist "+dist);
			if(dist > 0)
			{
				behind_path = true;
			}
		}

		x_points = x_set;
		y_points = y_set;

		if (!behind_path) 
		{
			ProgressPath ();
		}

	}

	//move forward along control path by removing first elements
	public void ProgressPath()
	{
		if (x_points.Count > 1 && y_points.Count > 1) 
		{
			
			x_points = x_points.GetRange(1,x_points.Count-1);
			y_points = y_points.GetRange(1,y_points.Count-1);

		} 
		else 
		{
			x_points.Clear ();
			y_points.Clear ();

		}
	}
		
	public void FixedUpdate()
	{
		ControllerUpdate ();
		if (simulator_process) 
		{
			server_process = true;
		}

	}
		
	public void ControllerUpdate()
	{
		
		if (x_points.Count > 1 && y_points.Count > 1) 
		{
			
			
			setNextPoint (x_points, y_points);

			Vector3 target = new Vector3 (x_points [0], rb.position.y, y_points [0]);
			Vector3 target2 = new Vector3 (x_points [1], rb.position.y, y_points [1]);

			rb.MovePosition (target);
			Vector3 inverseVect = transform.InverseTransformPoint (target2); 

			var deltaRotation = Quaternion.Euler(0, Mathf.Atan2 (inverseVect.x, inverseVect.z) * Mathf.Rad2Deg, 0);
			rb.MoveRotation (rb.rotation * deltaRotation);


		}
		ProgressPath ();
	}

	public List<float> previous_path_x()
	{
		return x_points;
	}

	public List<float> previous_path_y()
	{
		return y_points;
	}
		
	public bool isServerProcess()
	{
		return server_process;
	}
	public void ServerPause()
	{
		server_process = false;
	}
	public void SimulatorPause()
	{
		simulator_process = false;
		//Time.timeScale = 0.0f;
		//Time.fixedDeltaTime = 0.02f;


	}
	public void setSimulatorProcess()
	{
		simulator_process = true;
		//Time.timeScale = 1.0f;

	}

	public void setNextPoint(List<float> point_x, List<float> point_y)
	{
		int i = 0;
		while(i < way_points.Count && i < point_x.Count) 
		{
			GameObject new_point = way_points [i];

			// Change the coordinate system from (x,y) to (-y,x) so that zero degrees is the front axis of the car instead of to the right of car
			new_point.transform.position = new Vector3 (point_x [i],transform.position.y+0.5f,point_y[i]);

			if (i < point_x.Count - 2) {

				LineRenderer lineRenderer = new_point.GetComponentInParent<LineRenderer> ();

				Vector3 target = new Vector3 (point_x [i + 1], transform.position.y + 0.5f, point_y [i + 1]);
				lineRenderer.SetPosition (1, 4 * (target - new_point.transform.position));
				lineRenderer.SetWidth ((float).5, (float).5);

			}
			//set the line for the tip endpoint 
			else
			{
				LineRenderer lineRenderer = new_point.GetComponentInParent<LineRenderer> ();
				Vector3 target = new Vector3 (point_x [i], transform.position.y + 0.5f, point_y [i]);
				lineRenderer.SetPosition (1, 4 * (target - new_point.transform.position));
				lineRenderer.SetWidth ((float).5, (float).5);

			}
			i++;

		}
		while (i < way_points.Count) 
		{
			//Hide unused waypoints
			GameObject new_point = way_points [i];
			new_point.transform.position = new Vector3 (900f,-1f,1130f);
			i++;
		}

	}
	private void createPoints(int point_count)
	{
		int start_point = way_points.Count;
		int end_point = start_point + point_count;
		for (int i = start_point; i <  end_point; i++) 
		{
			GameObject new_point = (GameObject)Instantiate (next_point);

			new_point.name = "waypoint_"+i;

			way_points.Add (new_point);

		}
	}


	private void clearNextPoint()
	{
		//Clear points
		if (way_points != null) 
		{
			foreach (GameObject get_way_point in way_points)
			{
				if (get_way_point != null) {
					Destroy (get_way_point);
				}
			}
			way_points.Clear ();
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
