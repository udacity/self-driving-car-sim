using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Vehicles.Car;

public class lidar : MonoBehaviour {

	private List<GameObject> rays;
	private List<GameObject> layers;
	public GameObject ray;
	public GameObject layer;

	// Use this for initialization
	void Start () {

		//rays = new List<GameObject> ();
		layers = new List<GameObject> ();

		/*
		for (int i = 0; i < 20; i++)
		{
			GameObject get_ray = (GameObject)Instantiate (ray);
			get_ray.transform.position = this.transform.position;
			get_ray.transform.parent = this.transform;
			rays.Add (get_ray);
		}
		*/


		for (int j = 0; j < 15; j++) 
		{
			GameObject get_layer = (GameObject)Instantiate (layer);
			layers.Add (get_layer);

		}

	}
	
	// Update is called once per frame
	void FixedUpdate () {
		SenseDistance ();
	}
		
	public void SenseDistance()
	{
		float angle_range = Mathf.PI;

		int size = 100;

		for (int j = 0; j < 15; j++)
		{

			var points = new Vector3[size];

			for (int i = 0; i < size; i++) 
			{
				//GameObject get_ray = rays [i];

				RaycastHit hit;

				float angle = -angle_range / 2 + i * (angle_range) / (size - 1);

				Vector3 raycastDir = transform.TransformPoint ((j*1.0f+2.5f) * Mathf.Cos (angle), -1.0f, (j*1.0f+2.5f)  * Mathf.Sin (angle)) - transform.position;
				Physics.Raycast (transform.position, raycastDir, out hit);

				//LineRenderer lineRenderer = get_ray.GetComponent<LineRenderer> ();

				float base_angle = transform.eulerAngles.y * Mathf.Deg2Rad;
				Vector3 raycastRender = transform.TransformPoint ((j*1.0f+2.5f) * Mathf.Cos (base_angle + angle), -1.0f, (j*1.0f+2.5f) * Mathf.Sin (base_angle + angle)) - transform.position;
			
				if (hit.collider) {
					points [i] = transform.TransformPoint (raycastRender.normalized * hit.distance * 4);
					//lineRenderer.SetPosition (1, raycastRender.normalized * hit.distance * 4);
				} else {
					points [i] = transform.TransformPoint (raycastRender.normalized * 1000);
					//lineRenderer.SetPosition (1, raycastRender.normalized * 1000);
				}

				//lineRenderer.SetWidth (0.1f, 0.1f);
			
			}
			LineRenderer lineRenderer2 = layers[j].GetComponent<LineRenderer> ();
			lineRenderer2.SetPositions (points);
			lineRenderer2.SetWidth (0.1f, 0.1f);
		}
			
	}
}
