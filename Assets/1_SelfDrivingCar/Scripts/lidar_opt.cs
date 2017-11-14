using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Vehicles.Car;
using System;

public class lidar_opt : MonoBehaviour {

	private List<GameObject> rays;
	private List<GameObject> layers;
	public GameObject ray;
	public GameObject layer;

	private List<float> lidar_points_x;
	private List<float> lidar_points_y;
	private List<float> lidar_points_z;

	private List<List<Vector3>> lidar_points;
	private int Raysize;
	private int Layersize;
	private bool isRay;
	private bool isLayer;
	private int time_cycle;
	private double[] lidar_angles;
	private float angle_range;
	private float offset_angle;

	//optimize code
	private List<Vector3> raycastDir_collection;

	// Use this for initialization
	void Start () {

		isRay = false;
		isLayer = true;

		Raysize = 2500;


		lidar_points_x = new List<float> ();
		lidar_points_y = new List<float> ();
		lidar_points_z = new List<float> ();

		lidar_points = new List<List<Vector3>>();
		raycastDir_collection = new List<Vector3> ();

		lidar_angles = new double[] {-30.67,-9.33,-29.33,-8,-28,-6.66,-26.66,-5.33,-25.33,-4,-24,-2.67,-22.67,-1.33,-21.33,0,-20,1.33,-18.67,2.67,-17.33,4,-16,5.33,-14.67,6.67,-13.33,8,-12,9.33,-10.67,10.67};

		Layersize = lidar_angles.Length;


		if (isRay)
		{
			rays = new List<GameObject> ();

			for (int i = 0; i < Raysize; i++)
			{
				GameObject get_ray = (GameObject)Instantiate (ray);
				get_ray.transform.position = this.transform.position;
				get_ray.transform.parent = this.transform;
				rays.Add (get_ray);
			}
		}

		if (isLayer) 
		{
			layers = new List<GameObject> ();

			for (int j = 0; j < Layersize; j++) 
			{
				GameObject get_layer = (GameObject)Instantiate (layer);
				layers.Add (get_layer);

			}
		}

		angle_range = 2*Mathf.PI;
		offset_angle = 0f;

	}
	
	// Update is called once per frame

	void FixedUpdate () {
		
		if (time_cycle % 5 == 0) {
			Debug.Log ("0 "+lidar_points_x.Count);
			raycastDir_collection.Clear ();
			lidar_points_x.Clear ();
			lidar_points_y.Clear ();
			lidar_points_z .Clear ();
			SenseDirection ();
			time_cycle = 0;
			offset_angle = (offset_angle + (angle_range / (Raysize * 10))) % (angle_range / (Raysize));
			Debug.Log ("0 "+lidar_points_x.Count);
			SenseDistance(0,16000);
		} 
		else if (time_cycle % 5 == 1) {
			Debug.Log ("1 "+lidar_points_x.Count);
			SenseDistance(16000,16000);
		}
		else if (time_cycle % 5 == 2) {
			Debug.Log ("2 "+lidar_points_x.Count);
			SenseDistance(32000,16000);
		}
		else if (time_cycle % 5 == 3) {
			Debug.Log ("3 "+lidar_points_x.Count);
			SenseDistance(48000,16000);
		}
		else if (time_cycle % 5 == 4) {
			Debug.Log ("4 "+lidar_points_x.Count);
			SenseDistance(64000,16000);
		}
		time_cycle++;


	}
		
	public void SenseDirection()
	{

		for (int j = 0; j < Layersize; j++) {
			
			for (int i = 0; i < Raysize; i++) {
				
				float angle = (-angle_range / 2) + i * (angle_range) / (float)(Raysize - 1) + offset_angle;

				float vertical_angle_x = 1f / Mathf.Tan ((float)(Mathf.Deg2Rad * lidar_angles [j]));
				float vertical_angle_y = 1f * Mathf.Sign ((float)lidar_angles [j]);

				Vector3 raycastDir = transform.TransformPoint (vertical_angle_x * Mathf.Cos (angle), vertical_angle_y, vertical_angle_x * Mathf.Sin (angle)) - transform.position;

				raycastDir_collection.Add (raycastDir);

			}
		}
			
	}

	public void SenseDistance(int start, int size)
	{

		for(int i = start; i < (start+size); i++)
		{
			
			Vector3 raycastDir = raycastDir_collection [i];

			RaycastHit hit;
			Physics.Raycast (transform.position, raycastDir, out hit, 70);

			if (hit.collider) { 
			//if (hit.collider && hit.distance > 3) {


				//Vector3 raycastRender = transform.TransformPoint (vertical_angle_x * Mathf.Cos (base_angle + angle), vertical_angle_y, vertical_angle_x * Mathf.Sin (base_angle + angle)) - transform.position;

				//Vector3 vector_point = transform.TransformPoint (raycastRender.normalized * hit.distance * 4);


				Vector3 vector_point = transform.TransformPoint (raycastDir.normalized * hit.distance * 4);

				lidar_points_x.Add (vector_point.x);
				lidar_points_y.Add (vector_point.z);
				lidar_points_z.Add (vector_point.y);

				/*
				if (isLayer) 
				{
					points [i] = vector_point;
				}
				*/
			}  


		}
		/*
		if (isLayer) 
		{
			LineRenderer lineRenderer2 = layers [i%Raysize].GetComponent<LineRenderer> ();
			lineRenderer2.SetPositions (points);
			lineRenderer2.SetWidth (0.1f, 0.1f);
		}
		*/
	}
			

	public List<float> SendLidarDatax()
	{
		return lidar_points_x;
	}
	public List<float> SendLidarDatay()
	{
		return lidar_points_y;
	}
	public List<float> SendLidarDataz()
	{
		return lidar_points_z;
	}



	public List<List<Vector3>> SendLidarData()
	{
		return lidar_points;
	}

}
