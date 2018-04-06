using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;
using System.Security.AccessControl;

public class CommandServer_maze: MonoBehaviour
{
	public GameObject car;

	private SocketClient client;
	private PlayerScript my_car;
	private bool init_status;

	void Start()
	{
		Debug.Log ("trying to connect");
		client = GameObject.Find("SocketClient").GetComponent<SocketClient>();
		client.On("bounce", bounce);
		client.On("pass", pass);
		my_car = car.GetComponent<PlayerScript> ();

		// turned off once the first telemetry message is sent
		init_status = true;

		// This function is used when connecting webgl to classroom workspace
		Application.ExternalCall("mySetupFunction");
	}
	public void FixedUpdate()
	{

		if (client.isReady() && init_status) {
			init_status = false;
			EmitTelemetry ();
		
		}
	}
	void pass(JSONObject jsonObject)
	{
		EmitTelemetry ();
	}
	void bounce(JSONObject jsonObject)
	{
		
		float vel_x = float.Parse (jsonObject.GetField ("vel_x").ToString ());
		float vel_y = float.Parse (jsonObject.GetField ("vel_y").ToString ());
		my_car.MoveCar (vel_x, vel_y);

		EmitTelemetry ();
	}

	void EmitTelemetry()
	{
		
		Dictionary<string, string> data = new Dictionary<string, string>();
		data ["pos_x"] = car.transform.position.x.ToString();
		data ["pos_y"] = car.transform.position.y.ToString();

		client.Send("telemetry", new JSONObject(data));

	}
}
