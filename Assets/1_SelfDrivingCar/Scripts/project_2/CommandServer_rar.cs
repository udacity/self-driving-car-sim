using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using SocketIO;
using System;
using System.Security.AccessControl;

public class CommandServer_rar : MonoBehaviour
{


	private SocketIOComponent _socket;
	public GameObject player;
	private hunter player_controller;
	public GameObject target;
	private robot sensor;

	// Use this for initialization
	void Start()
	{
		Debug.Log ("trying to connect");
		_socket = GameObject.Find("SocketIO").GetComponent<SocketIOComponent>();
		_socket.On("open", OnOpen);
		_socket.On("close", OnClose);
		_socket.On("manual", onManual);
		_socket.On("move_hunter", MoveHunter);

		player_controller = player.GetComponent<hunter> ();
		sensor = target.GetComponent<robot> ();
	}

	// Update is called once per frame
	void Update()
	{
	}

	void OnOpen(SocketIOEvent obj)
	{
		Debug.Log("Connection Open");
		EmitTelemetry(obj);
	}

	void OnClose(SocketIOEvent obj)
	{
		Debug.Log("Connection Closed");
		sensor.ScriptClosed();
	}

	void onManual(SocketIOEvent obj)
	{
		EmitTelemetry (obj);
	}

	void MoveHunter(SocketIOEvent obj)
	{
		
		JSONObject jsonObject = obj.data;

		float turn = float.Parse(jsonObject.GetField("turn").ToString());
		float  dist = float.Parse(jsonObject.GetField("dist").ToString());

		player_controller.setVel (dist);
		player_controller.setYawRate (turn);

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
				if (!sensor.Status() ) {
					
					_socket.Emit("telemetry", new JSONObject());
				}
				else {
					
					// Collect Data from the robot's sensors
					Dictionary<string, string> data = new Dictionary<string, string>();
					data["lidar_measurement"] = sensor.Lidar_Measure();
					data["radar_measurement"] = sensor.Radar_Measure();
					data["hunter_x"] = player_controller.x.ToString("N4");
					data["hunter_y"] = player_controller.y.ToString("N4");
					data["hunter_heading"] = (player_controller.heading).ToString("N4");

					_socket.Emit("telemetry", new JSONObject(data));
				}
		});
	}

}
