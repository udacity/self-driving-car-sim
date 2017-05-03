using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using SocketIO;
using System;
using System.Security.AccessControl;

public class CommandServer_ekf : MonoBehaviour
{

	private SocketIOComponent _socket;
	private ekf_generator kalman_filter;
	public GameObject car;

	// Use this for initialization
	void Start()
	{
		Debug.Log ("trying to connect");
		_socket = GameObject.Find("SocketIO").GetComponent<SocketIOComponent>();
		_socket.On("open", OnOpen);
		_socket.On("close", OnClose);
		_socket.On("manual", onManual);
		_socket.On("estimate_marker", Estimate);

		kalman_filter = car.GetComponent<ekf_generator> ();
	}

	// Update is called once per frame
	void Update()
	{
	}

	void OnOpen(SocketIOEvent obj)
	{
		Debug.Log("Connection Open");
		kalman_filter.OpenScript ();
		EmitTelemetry(obj);
	}

	void OnClose(SocketIOEvent obj)
	{
		Debug.Log("Connection Closed");
		kalman_filter.CloseScript ();
	}

	void onManual(SocketIOEvent obj)
	{
		EmitTelemetry (obj);
	}

	void Estimate(SocketIOEvent obj)
	{
		
		JSONObject jsonObject = obj.data;

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

		EmitTelemetry(obj);
	}

	void EmitTelemetry(SocketIOEvent obj)
	{
		//Debug.Log ("call thread");
		UnityMainThreadDispatcher.Instance().Enqueue(() =>
		{
				// Collect Data from the robot
			
				//print("Attempting to Send...");
				// send only if robot is moving
				if (!kalman_filter.isRunning() || !kalman_filter.isReadyProcess()) {
					
					_socket.Emit("telemetry", new JSONObject());
				}
				else {

					kalman_filter.Processed();
					
					// Collect Data from the robot's sensors
					Dictionary<string, string> data = new Dictionary<string, string>();
					data["sensor_measurement"] = kalman_filter.sensor_Measure();

					_socket.Emit("telemetry", new JSONObject(data));
				}
		});
	}

}
