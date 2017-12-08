using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;
using System.Security.AccessControl;
//using UnitySocketIO;
//using UnitySocketIO.Events;

public class CommandServer_icp : MonoBehaviour
{

	private SocketClient client;
	private slam_controller_icp slam_controller_icp;
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

		slam_controller_icp = car.GetComponent<slam_controller_icp> ();

		//io.Connect();
		init_status = true;


		//Application.ExternalCall("mySetupFunction");
	}


	public void FixedUpdate()
	{
		if (slam_controller_icp.getStatus () == 1 && init_status) {
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
		
		var scan_x = jsonObject.GetField ("transform_observations_x");
		var scan_y = jsonObject.GetField ("transform_observations_y");


		List<float> my_scan_x = new List<float> ();
		List<float> my_scan_y = new List<float> ();


		for (int i = 0; i < scan_x.Count; i++) {
			my_scan_x.Add (float.Parse ((scan_x [i]).ToString ()));
			my_scan_y.Add (float.Parse ((scan_y [i]).ToString ()));
		}

		slam_controller_icp.GraphMeasure(my_scan_x, my_scan_y);

		var ref_x = jsonObject.GetField ("reference_observations_x");
		var ref_y = jsonObject.GetField ("reference_observations_y");


		List<float> my_ref_x = new List<float> ();
		List<float> my_ref_y = new List<float> ();


		for (int i = 0; i < ref_x.Count; i++) {
			my_ref_x.Add (float.Parse ((ref_x [i]).ToString ()));
			my_ref_y.Add (float.Parse ((ref_y [i]).ToString ()));
		}

		slam_controller_icp.GraphRef(my_ref_x, my_ref_y);


		float car_x = float.Parse (jsonObject.GetField ("car_x").ToString ());
		float car_y = float.Parse (jsonObject.GetField ("car_y").ToString ());
		float car_t = float.Parse (jsonObject.GetField ("car_t").ToString ());

		float min_x = float.Parse (jsonObject.GetField ("grid_min_x").ToString ());
		float min_y = float.Parse (jsonObject.GetField ("grid_min_y").ToString ());

		slam_controller_icp.setGridCenter (min_x, min_y);

		slam_controller_icp.plot_car (car_x, car_y, car_t);

		var key_x = jsonObject.GetField ("keyframe_x");
		var key_y = jsonObject.GetField ("keyframe_y");
		var key_t = jsonObject.GetField ("keyframe_t");

		List<float> my_key_x = new List<float> ();
		List<float> my_key_y = new List<float> ();
		List<float> my_key_t = new List<float> ();

		for (int i = 0; i < key_x.Count; i++) {
			my_key_x.Add (float.Parse ((key_x [i]).ToString ()));
			my_key_y.Add (float.Parse ((key_y [i]).ToString ()));
			my_key_t.Add (float.Parse ((key_t [i]).ToString ()));
		}

		int ref_keyframe = (int)float.Parse (jsonObject.GetField ("reference_keyframe").ToString ());
		//Debug.Log (ref_keyframe);

		slam_controller_icp.plot_keyframes(my_key_x, my_key_y, my_key_t,ref_keyframe);

		EmitTelemetry ();
	}

	void EmitTelemetry()
	{
		//UnityMainThreadDispatcher.Instance().Enqueue(() =>
		//	{
				// Collect Data from the robot
				Dictionary<string, string> data = new Dictionary<string, string>();
				data["update"] = slam_controller_icp.getStatus().ToString("N4");

				data["x_drive"] = slam_controller_icp.getDelta_x().ToString("N4");
				data["t_drive"] = slam_controller_icp.getDelta_t().ToString("N4");
				slam_controller_icp.resetStatus();

				data["pos_x"] = slam_controller_icp.get_x().ToString("N4");
				data["pos_y"] = slam_controller_icp.get_y().ToString("N4");
				data["pos_t"] = slam_controller_icp.get_t().ToString("N4");

				data["sense_observations"] = slam_controller_icp.Sense_Obs();
				
				client.Send("telemetry", new JSONObject(data));


		//	});
	}

}
