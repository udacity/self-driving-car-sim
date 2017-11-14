using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointTracker : MonoBehaviour {

	public GameObject next_point;
	public GameObject mpc_point;
	public GameObject car;

	private List<GameObject> way_points;
	private List<GameObject> mpc_points;

	// Use this for initialization
	void Start () {

		way_points = new List<GameObject> ();
		mpc_points = new List<GameObject> ();
	}

	public void setNextPoint(List<float> point_x, List<float> point_y)
	{

		clearNextPoint ();

		for (int i = 0; i <  point_x.Count; i++) 
		{
			GameObject new_point = (GameObject)Instantiate (next_point);

			// Change the coordinate system from (x,y) to (-y,x) so that zero degrees is the front axis of the car instead of to the right of car
			new_point.transform.position = car.transform.TransformPoint (new Vector3 (-1*point_y [i],0.0f,point_x [i]));

			new_point.name = "waypoint_"+i;


			if (i <  point_x.Count-1)
			{

				LineRenderer lineRenderer = new_point.GetComponentInParent<LineRenderer> ();

				Vector3 target = car.transform.TransformPoint (new Vector3 (-1*point_y [i+1],0.0f,point_x [i+1]));
				lineRenderer.SetPosition (1,4*(target-new_point.transform.position));
				lineRenderer.SetWidth ((float).5, (float).5);

			}

			way_points.Add (new_point);

		}
	}


	public void setMpcPoint(List<float> point_x, List<float> point_y)
	{

		clearMpcPoint ();

		for (int i = 0; i <  point_x.Count; i++) 
		{
			GameObject new_point = (GameObject)Instantiate (mpc_point);

			// Change the coordinate system from (x,y) to (-y,x) so that zero degrees is the front axis of the car instead of to the right of car
			new_point.transform.position = car.transform.TransformPoint (new Vector3 (-1*point_y [i],0.5f,point_x [i]));

			new_point.name = "mpcpoint_"+i;


			if (i <  point_x.Count-1)
			{

				LineRenderer lineRenderer = new_point.GetComponentInParent<LineRenderer> ();

				Vector3 target = car.transform.TransformPoint (new Vector3 (-1*point_y [i+1],0.5f,point_x [i+1]));
				lineRenderer.SetPosition (1,4*(target-new_point.transform.position));
				lineRenderer.SetWidth ((float).5, (float).5);

			}

			mpc_points.Add (new_point);

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
		}


	}

	private void clearMpcPoint()
	{
		//Clear points
		if (mpc_points != null) 
		{
			foreach (GameObject get_mpc_point in mpc_points)
			{
				if (get_mpc_point != null) {
					Destroy (get_mpc_point);
				}
			}
		}


	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
