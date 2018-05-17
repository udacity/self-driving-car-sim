using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;
using System.Security.AccessControl;

public class CommandServer_ekf : MonoBehaviour
{

	//private SocketIOComponent _socket;
	private SocketClient_ekf client;
	private ekf_generator kalman_filter;
	public GameObject car;
	private bool init_status;

	// Use this for initialization
	void Start()
	{
		// Debug.Log ("trying to connect");
		client = GameObject.Find("SocketClient").GetComponent<SocketClient_ekf>();
		//Debug.Log (client);
		client.On("open", OnOpen);
		client.On("close", OnClose);
		client.On("pass", pass);
		client.On("process_ekf", process_ekf);

		kalman_filter = car.GetComponent<ekf_generator> ();
		// turned off once the first telemetry message is sent
		init_status = true;

		// This function is used when connecting webgl to classroom workspace
		Application.ExternalCall("mySetupFunction");
	}

	// Update is called once per frame
	public void FixedUpdate()
	{

		if (client.isReady() && init_status) {
			init_status = false;
			EmitTelemetry ();

		}

		
	}
	
	void OnOpen(JSONObject obj)
	{
		//Debug.Log("Connection Open");
		kalman_filter.OpenScript ();
	}

	void OnClose(JSONObject obj)
	{
		//Debug.Log("Connection Closed");
		kalman_filter.CloseScript ();
	}

	void pass(JSONObject obj)
	{
		EmitTelemetry ();
	}

	void process_ekf(JSONObject jsonObject)
	{
		//Debug.Log ("hello");
		JSONObject obj = jsonObject;

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

		EmitTelemetry();
	}

	void EmitTelemetry()
	{
				
				// Collect Data from the robot
			
				//print("Attempting to Send...");
				// send only if robot is moving
				if (!kalman_filter.isRunning() || !kalman_filter.isReadyProcess()) {
					
					Dictionary<string, string> data = new Dictionary<string, string>();
					data ["process"] = 0.ToString();
					client.Send("telemetry", new JSONObject(data));

				}
				else {

					kalman_filter.Processed();

					// Collect Data from the robot's sensors
					Dictionary<string, string> data = new Dictionary<string, string>();
					data["process"] = 1.ToString();
					data["sensor_measurement"] = kalman_filter.sensor_Measure();

					client.Send("telemetry", new JSONObject(data));
				}
				
				


	}

}
