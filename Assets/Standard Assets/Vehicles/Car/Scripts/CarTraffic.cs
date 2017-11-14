using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityStandardAssets.Vehicles.Car
{
	[RequireComponent (typeof(CarController))]
	public class CarTraffic : MonoBehaviour {

	//forward cars
	[SerializeField] private List<GameObject> cars;
	private Queue<GameObject> inactive_cars;

	public GameObject maincar;

	//reverse cars
	[SerializeField] private List<GameObject> carsR;
	private Queue<GameObject> inactive_carsR;
	
	//use counter to update every second
	private int counter;
	//number of cars to push at update, max is 3
	private bool init;
	private int count_max;

	// Use this for initialization
	void Start () {

		inactive_cars = new Queue<GameObject>();
		inactive_carsR = new Queue<GameObject>();
		counter = 0;
		// push two cars at a time

		init = true;
		count_max = Random.Range (20, 60);
		
	}
			
	// Update is called once per frame
	void Update () {
		if (counter >= count_max || init) {
			init = false;
			UpdateForward ();
			UpdateReverse ();
			counter = 0;
			count_max = Random.Range (20, 60);
		}
		else 
		{
			counter++;
		}
	}
	public void UpdateForward(){
		//add any inactive car to inactive car list
		foreach (GameObject car in cars) 
		{
			CarAIControl carAI = (CarAIControl) car.GetComponent(typeof(CarAIControl));
			if (carAI.RegenerateCheck ()) {
				//turn off the car and add it to a list
				carAI.setStage();
				inactive_cars.Enqueue (car);
			} 

		}
		// count how many cars we have pushed
		int push = Random.Range(1,4);
		int pushed = 0;
		
		while ((inactive_cars.Count > 0) && (pushed < push)) {
			GameObject inactive_car = inactive_cars.Dequeue();

			CarAIControl carAI = (CarAIControl) inactive_car.GetComponent(typeof(CarAIControl));
		
			carAI.Spawn (cars);

			pushed++;
		}
		
	}
	public void UpdateReverse(){
		//add any inactive car to inactive car list
		foreach (GameObject car in carsR) 
		{
			CarAIControl carAI = (CarAIControl) car.GetComponent(typeof(CarAIControl));
			if (carAI.RegenerateCheck ()) {
				//turn off the car and add it to a list
				carAI.setStage();
				inactive_carsR.Enqueue (car);
			} 

		}
		// count how many cars we have pushed
		int push = Random.Range(1,4);
		int pushed = 0;
		
		while ((inactive_carsR.Count > 0) && (pushed < push)) {
				GameObject inactive_car = inactive_carsR.Dequeue();

				CarAIControl carAI = (CarAIControl) inactive_car.GetComponent(typeof(CarAIControl));

				carAI.Spawn (carsR);

				pushed++;
		}
	}

	public bool lane_clear(GameObject mycar,bool forward,int lane)
	{
			List<float> s_values = new List<float> ();
			int s_index = -1;


			if (forward) 
			{
				
				foreach(GameObject car in cars) 
				{
					
					if (mycar.GetInstanceID() == car.GetInstanceID()) 
					{
						s_index = s_values.Count;
					}
					CarAIControl carAI = (CarAIControl)car.GetComponent (typeof(CarAIControl));

					List<float> frenet_values = carAI.getThisFrenetFrame ();
					s_values.Add (frenet_values[0]);


				}

				CarAIControl carAImain = (CarAIControl)maincar.GetComponent (typeof(CarAIControl));

				List<float> frenet_values_main = carAImain.getThisFrenetFrame ();
				float d_value = frenet_values_main[1];
				//check if main car is in lane
				if(d_value < (2+lane*4+2) && d_value > (2+lane*4-2))
				{
					
					s_values.Add (frenet_values_main[0]);

				}

			} 
			else 
			{
				
				foreach (GameObject car in carsR) 
				{
					if (mycar.GetInstanceID() == car.GetInstanceID()) 
					{
						s_index = s_values.Count;
					}
					CarAIControl carAI = (CarAIControl)car.GetComponent (typeof(CarAIControl));

					List<float> frenet_values = carAI.getThisFrenetFrame ();
					s_values.Add (frenet_values[0]);

				}
					
			}

			if (s_index == -1) 
			{
				CarAIControl carAImain = (CarAIControl)maincar.GetComponent (typeof(CarAIControl));
				List<float> frenet_values_main = carAImain.getThisFrenetFrame ();
				s_index = s_values.Count;
				s_values.Add (frenet_values_main[0]);

			}

			bool clear = true;
			float safe_dist = 20.0f;
			for (int i = 0; i < s_values.Count; i++) 
			{
				if (i != s_index) 
				{
					
					clear = clear && (Mathf.Abs (s_values [i] - s_values [s_index]) > safe_dist);
				}
			}
			return clear;

	}

	float convertAngle(float psi) {
		if (psi >= 0 && psi <= 90) {
			return 90 - psi;
		}
		else if (psi > 90 && psi <= 180) {
			return 90 + 270 - (psi - 90);
		}
		else if (psi > 180 && psi <= 270) {
			return 180 + 90 - (psi - 180);
		}
		return 270 - 90 - (psi - 270);
	}

	public string example_sensor_fusion()
	{
			string result = "[";
			int car_id = 0;
			foreach (GameObject car in cars) 
			{
				CarAIControl carAI = (CarAIControl) car.GetComponent(typeof(CarAIControl));

				if(car_id > 0)
				{
					result += ",";
				}

				List<float> frenet_values = carAI.getThisFrenetFrame ();

				result += "[" + car.transform.position.x + "," + car.transform.position.z +","+ car.transform.position.y+"]";

				car_id++;
			}
			result += "]";
			return result;
	}

}
}
