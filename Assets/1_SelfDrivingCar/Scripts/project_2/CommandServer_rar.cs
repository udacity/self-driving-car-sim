using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;
using System.Security.AccessControl;

public class CommandServer_rar : MonoBehaviour
{


	private SocketClient client;
	public GameObject player;
	private hunter player_controller;
	public GameObject target;
	private robot sensor;

	// Use this for initialization
	void Start()
	{
		Debug.Log ("trying to connect");
		client = GameObject.Find("SocketClient").GetComponent<SocketClient>();

		client.On("manual", onManual);
		client.On("move_hunter", MoveHunter);

		player_controller = player.GetComponent<hunter> ();
		sensor = target.GetComponent<robot> ();
	}

	// Update is called once per frame
	void Update()
	{
	}

	void onManual(JSONObject jsonObject)
	{
		//EmitTelemetry (obj);
	}

	void MoveHunter(JSONObject jsonObject)
	{

		float turn = float.Parse(jsonObject.GetField("turn").ToString());
		float  dist = float.Parse(jsonObject.GetField("dist").ToString());

		player_controller.setVel (dist);
		player_controller.setYawRate (turn);

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
				if (!sensor.Status() ) {
					
					//client.Send("telemetry", new JSONObject(data));
				}
				else {
					
					// Collect Data from the robot's sensors
					Dictionary<string, string> data = new Dictionary<string, string>();
					data["lidar_measurement"] = sensor.Lidar_Measure();
					data["radar_measurement"] = sensor.Radar_Measure();
					data["hunter_x"] = player_controller.x.ToString("N4");
					data["hunter_y"] = player_controller.y.ToString("N4");
					data["hunter_heading"] = (player_controller.heading).ToString("N4");

					client.Send("telemetry", new JSONObject(data));
				}
		});
	}

}
