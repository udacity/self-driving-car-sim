using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;
using System.Security.AccessControl;

public class CommandServer_ekf : MonoBehaviour
{

	private SocketClient client;
	private ekf_generator kalman_filter;
	public GameObject car;

	// Use this for initialization
	void Start()
	{
		Debug.Log ("trying to connect");
		client = GameObject.Find("SocketClient").GetComponent<SocketClient>();
		client.On("manual", onManual);
		client.On("estimate_marker", Estimate);

		kalman_filter = car.GetComponent<ekf_generator> ();
	}

	// Update is called once per frame
	void Update()
	{
	}

	void onManual(JSONObject jsonObject)
	{
		//EmitTelemetry (obj);
	}

	void Estimate(JSONObject jsonObject)
	{

		if (kalman_filter.isRunning ())
		{
			float estimate_x = float.Parse (jsonObject.GetField ("estimate_x").ToString ());
			float estimate_y = float.Parse (jsonObject.GetField ("estimate_y").ToString ());

			float rmse_x = float.Parse (jsonObject.GetField ("rmse_x").ToString ());
			float rmse_y = float.Parse (jsonObject.GetField ("rmse_y").ToString ());
			float rmse_vx = float.Parse (jsonObject.GetField ("rmse_vx").ToString ());
			float rmse_vy = float.Parse (jsonObject.GetField ("rmse_vy").ToString ());

			kalman_filter.Estimate (estimate_x, estimate_y);
			kalman_filter.SetRmse (rmse_x, rmse_y, rmse_vx, rmse_vy);
		}

		//EmitTelemetry(obj);
	}

	void EmitTelemetry()
	{
		//Debug.Log ("call thread");
		UnityMainThreadDispatcher.Instance().Enqueue(() =>
		{
				// Collect Data from the robot
			
				//print("Attempting to Send...");
				// send only if robot is moving
				if (!kalman_filter.isRunning() || !kalman_filter.isReadyProcess()) {
					
					//client.Send("telemetry", new JSONObject(data));
				}
				else {

					kalman_filter.Processed();
					
					// Collect Data from the robot's sensors
					Dictionary<string, string> data = new Dictionary<string, string>();
					data["sensor_measurement"] = kalman_filter.sensor_Measure();

					client.Send("telemetry", new JSONObject(data));
				}
		});
	}

}
