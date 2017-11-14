using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Vehicles.Car;
using System;

public class lidar : MonoBehaviour {

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

	// Use this for initialization
	void Start () {

		isRay = false;
		isLayer = true;

		Raysize = 250;


		lidar_points_x = new List<float> ();
		lidar_points_y = new List<float> ();
		lidar_points_z = new List<float> ();

		lidar_points = new List<List<Vector3>>();

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
		
		if (time_cycle%5 == 0) {
			lidar_points_x.Clear ();
			lidar_points_y.Clear ();
			lidar_points_z.Clear ();
			time_cycle = 0;
			SenseDistance();
			//Debug.Log ("lidar points "+lidar_points_x.Count);
			offset_angle = (offset_angle + (angle_range / (Raysize * 10))) % (angle_range / (Raysize));
		} 
		time_cycle++;


	}
		
	public void SenseDistance()
	{

		//long milliseconds = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
		float base_angle = transform.eulerAngles.y * Mathf.Deg2Rad;

		for (int j = 0; j < Layersize; j++)
		{
			//List<Vector3> layer_points = new List<Vector3> ();

			var points = new Vector3[Raysize];

			for (int i = 0; i < Raysize; i++) 
			{
				//LineRenderer lineRenderer = new LineRenderer();

				if (isRay) 
				{
					LineRenderer lineRenderer = new LineRenderer();

					GameObject get_ray = rays [i];
					lineRenderer = get_ray.GetComponent<LineRenderer> ();

					lineRenderer.startWidth = 0.1f;
					lineRenderer.endWidth = 0.1f;
				}
					
				RaycastHit hit;

				float angle = (-angle_range / 2) + i * (angle_range) / (float)(Raysize - 1) + offset_angle;

				float vertical_angle_x = 1f / Mathf.Tan ((float)(Mathf.Deg2Rad * lidar_angles [j]) );
				float vertical_angle_y = 1f * Mathf.Sign ((float)lidar_angles [j]);

				Vector3 raycastDir = transform.TransformPoint (vertical_angle_x * Mathf.Cos (angle), vertical_angle_y, vertical_angle_x  * Mathf.Sin (angle)) - transform.position;

				Physics.Raycast (transform.position, raycastDir, out hit, 70);


				if (hit.collider && (hit.distance > 3 || isLayer)) { 

					//global
					Vector3 raycastRender = transform.TransformPoint (vertical_angle_x * Mathf.Cos (base_angle + angle), vertical_angle_y, vertical_angle_x * Mathf.Sin (base_angle + angle)) - transform.position;
					Vector3 vector_point = transform.TransformPoint (raycastRender.normalized * hit.distance * 4);

					//local
					//Vector3 vector_point = raycastDir.normalized * hit.distance * 4;

					lidar_points_x.Add (vector_point.x);
					lidar_points_y.Add (vector_point.z);
					lidar_points_z.Add (vector_point.y);

					if (isLayer) {
						points [i] = vector_point;
					}
					if (isRay) {
						//lineRenderer.SetPosition (1, raycastRender.normalized * hit.distance * 4);
					}

				} 
				//only needed for visuals
				if(isLayer && !hit.collider) {


					//global
					Vector3 raycastRender = transform.TransformPoint (vertical_angle_x * Mathf.Cos (base_angle + angle), vertical_angle_y, vertical_angle_x * Mathf.Sin (base_angle + angle)) - transform.position;
					Vector3 vector_point = transform.TransformPoint (raycastRender.normalized * 70 * 4);

					//local
					//Vector3 vector_point = raycastDir.normalized * 70 * 4;

					lidar_points_x.Add (vector_point.x);
					lidar_points_y.Add (vector_point.z);
					lidar_points_z.Add (vector_point.y);

					if (isLayer) {
						points [i] = vector_point;
					}
					if (isRay) {
						//lineRenderer.SetPosition (1, raycastRender.normalized * hit.distance * 4);
					}
				}


			
			}
			if (isLayer) 
			{
				LineRenderer lineRenderer2 = layers [j].GetComponent<LineRenderer> ();
				lineRenderer2.SetPositions (points);
				lineRenderer2.SetWidth (0.1f, 0.1f);
			}

			//lidar_points.Add (layer_points);
		}

		//long m2 = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
		//Debug.Log("Lidar Read \n" + (m2 - milliseconds).ToString());


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
