using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;
using System.Security.AccessControl;
//using UnitySocketIO;
//using UnitySocketIO.Events;

public class CommandServer_pf1 : MonoBehaviour
{

	private SocketClient client;
	private slam_controller slam_controller;
	public GameObject car;
	private bool init_status;

	//public SocketIOController io;

	// Use this for initialization
	void Start()
	{
		Debug.Log ("trying to connect");
		client = GameObject.Find("SocketClient").GetComponent<SocketClient>();
		client.On("slam", slam_visualizer);
		client.On("manual", manual);

		slam_controller = car.GetComponent<slam_controller> ();

		/*
		io.On("connect", (SocketIOEvent e) => {
			Debug.Log("SocketIO connected");
			EmitTelemetry();

		});
		*/
		/*
		io.On("manual", (SocketIOEvent e) => {
			EmitTelemetry();
		});
		*/
		/*
		io.On("slam", (SocketIOEvent e) => {
			JSONObject jsonObject = new JSONObject(e.data);

			var landmark_i = jsonObject.GetField ("landmark_i");
			var landmark_x = jsonObject.GetField ("landmark_x");
			var landmark_y = jsonObject.GetField ("landmark_y");

			List<float> my_landmark_i = new List<float> ();
			List<float> my_landmark_x = new List<float> ();
			List<float> my_landmark_y = new List<float> ();

			for (int i = 0; i < landmark_i.Count; i++) 
			{
				my_landmark_i.Add (float.Parse((landmark_i [i]).ToString()));
				my_landmark_x.Add (float.Parse((landmark_x [i]).ToString()));
				my_landmark_y.Add (float.Parse((landmark_y [i]).ToString()));
			}

			slam_controller.plot_landmarks (my_landmark_i,my_landmark_x, my_landmark_y);

			float car_x = float.Parse (jsonObject.GetField ("car_x").ToString ());
			float car_y = float.Parse (jsonObject.GetField ("car_y").ToString ());
			float car_t = float.Parse (jsonObject.GetField ("car_t").ToString ());

			slam_controller.plot_car (car_x, car_y, car_t);
			EmitTelemetry();
		});
		*/

		//io.Connect();
		init_status = true;


		Application.ExternalCall("mySetupFunction");
	}


	public void FixedUpdate()
	{
		if (slam_controller.getStatus () == 1 && init_status) {
			init_status = false;
			EmitTelemetry ();
			//slam_controller.resetStatus ();
		}

	}

	void manual(JSONObject jsonObject)
	{
		EmitTelemetry ();
	}

	void slam_visualizer(JSONObject jsonObject)
	{

		var landmark_i = jsonObject.GetField ("landmark_i");
		var landmark_x = jsonObject.GetField ("landmark_x");
		var landmark_y = jsonObject.GetField ("landmark_y");

		List<float> my_landmark_i = new List<float> ();
		List<float> my_landmark_x = new List<float> ();
		List<float> my_landmark_y = new List<float> ();

		for (int i = 0; i < landmark_i.Count; i++) {
			my_landmark_i.Add (float.Parse ((landmark_i [i]).ToString ()));
			my_landmark_x.Add (float.Parse ((landmark_x [i]).ToString ()));
			my_landmark_y.Add (float.Parse ((landmark_y [i]).ToString ()));
		}

		slam_controller.plot_landmarks (my_landmark_i, my_landmark_x, my_landmark_y);

		float car_x = float.Parse (jsonObject.GetField ("car_x").ToString ());
		float car_y = float.Parse (jsonObject.GetField ("car_y").ToString ());
		float car_t = float.Parse (jsonObject.GetField ("car_t").ToString ());

		slam_controller.plot_car (car_x, car_y, car_t);

		EmitTelemetry ();
	}

	/*
	public class TestObject {
		public string update;
		public string x_drive;
		public string t_drive;
		public string sense_observations;
		public string sense_observations_x;
		public string sense_observations_y;
	}
	*/

	void EmitTelemetry()
	{
		//UnityMainThreadDispatcher.Instance().Enqueue(() =>
		//	{
				// Collect Data from the robot
				Dictionary<string, string> data = new Dictionary<string, string>();
				data["update"] = slam_controller.getStatus().ToString("N4");
				slam_controller.resetStatus();
				data["x_drive"] = slam_controller.getOdometry_x().ToString("N4");
				data["t_drive"] = slam_controller.getOdometry_t().ToString("N4");

				data["sense_observations"] = slam_controller.Sense_Obs();
				data["sense_observations_x"] = slam_controller.Sense_Obsx();
				data["sense_observations_y"] = slam_controller.Sense_Obsy();
				
				/*
				TestObject t = new TestObject();
				t.update = slam_controller.getStatus().ToString("N4");
				slam_controller.resetStatus();
				t.x_drive = slam_controller.getOdometry_x ().ToString("N4");
				t.t_drive = slam_controller.getOdometry_t ().ToString("N4");
				t.sense_observations = slam_controller.Sense_Obs();
				t.sense_observations_x = slam_controller.Sense_Obsx();
				t.sense_observations_y = slam_controller.Sense_Obsy();
				*/

				//io.Emit("telemetry", JsonUtility.ToJson(t));
				
				client.Send("telemetry", new JSONObject(data));


		//	});
	}

}
