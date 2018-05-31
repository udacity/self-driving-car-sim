using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityStandardAssets.Vehicles.Car;
using System;
using System.Security.AccessControl;
using UnityEngine.SceneManagement;



public class CommandServer : MonoBehaviour
{
	public CarRemoteControl CarRemoteControl;
	public Camera FrontFacingCamera;
	private CarController _carController;
	private SocketClient client;
	private bool init_status;

	// Use this for initialization
	void Start()
	{
		// Debug.Log ("trying to connect");
		client = GameObject.Find("SocketClient").GetComponent<SocketClient>();
		client.On("open", OnOpen);
		client.On("steer", OnSteer);
		client.On("manual", onManual);
		_carController = CarRemoteControl.GetComponent<CarController>();
		init_status = true;
		// Debug.Log ("Started");
		// This function is used when connecting webgl to classroom workspace
		Application.ExternalCall("mySetupFunction");
	}

	public void FixedUpdate()
  	{

		// print(client.isReady());
		// print(init_status);
		if (client.isReady() && init_status) {
		init_status = false;
		EmitTelemetry ();

    	}
 	}

	void OnOpen(JSONObject jsonObject)
	{
		Debug.Log("Connection Open");
		EmitTelemetry();
	}

	// 
	void onManual(JSONObject jsonObject)
	{
		Debug.Log("Manual");
		EmitTelemetry ();
	}

	void OnSteer(JSONObject jsonObject)
	{
		// JSONObject jsonObject = obj.data;
		//    print(float.Parse(jsonObject.GetField("steering_angle").str));
		JSONObject obj = jsonObject;
		CarRemoteControl.SteeringAngle = float.Parse(jsonObject.GetField("steering_angle").ToString());
		CarRemoteControl.Acceleration = float.Parse(jsonObject.GetField("throttle").ToString());
		EmitTelemetry();
	}

	void EmitTelemetry()
	{
		// UnityMainThreadDispatcher.Instance().Enqueue(() =>
		// {
			// print("Attempting to Send...");
			// send only if it's not being manually driven
			if ((Input.GetKey(KeyCode.W)) || (Input.GetKey(KeyCode.S))) {
				client.Send("telemetry", new JSONObject());
			}
			else {
				// Collect Data from the Car
				// print("Sending...");
				Dictionary<string, string> data = new Dictionary<string, string>();
				data["steering_angle"] = _carController.CurrentSteerAngle.ToString("N4");
				data["throttle"] = _carController.AccelInput.ToString("N4");
				data["speed"] = _carController.CurrentSpeed.ToString("N4");
				data["image"] = Convert.ToBase64String(CameraHelper.CaptureFrame(FrontFacingCamera));
				// print(data);
				client.Send("telemetry", new JSONObject(data));
				
			}
		
	}
}