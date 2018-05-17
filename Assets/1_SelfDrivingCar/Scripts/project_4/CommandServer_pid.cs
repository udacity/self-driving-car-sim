using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityStandardAssets.Vehicles.Car;
using System;
using UnityEngine.SceneManagement;
using System.Security.AccessControl;




public class CommandServer_pid : MonoBehaviour
{
	public CarRemoteControl CarRemoteControl;
	public Camera FrontFacingCamera;
	// private SocketIOComponent _socket;
	private SocketClient_pid client;
	private CarController _carController;
	private WaypointTracker_pid wpt;
	private bool init_status;

	// Use this for initialization
	void Start()
	{
		// client = GameObject.Find("SocketIO").GetComponent<SocketIOComponent>();
		client = GameObject.Find("SocketClient").GetComponent<SocketClient_pid>();
		client.On("open", OnOpen);
		client.On ("reset", OnReset);
		client.On("steer", OnSteer);
		client.On("manual", onManual);
		_carController = CarRemoteControl.GetComponent<CarController>();
		wpt = new WaypointTracker_pid ();
		init_status = true;

		Application.ExternalCall("mySetupFunction");

	}

	public void FixedUpdate()
  	{

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
        Debug.Log("Manual driving event ...");
		EmitTelemetry ();
	}

	void OnReset(JSONObject jsonObject)
	{
		SceneManager.LoadScene("LakeTrackAutonomous_pid");
		EmitTelemetry ();
	}

	void OnSteer(JSONObject jsonObject)
	{
        Debug.Log("Steering data event ...");
		//JSONObject jsonObject = obj.data;
		JSONObject obj = jsonObject;

		CarRemoteControl.SteeringAngle = float.Parse(jsonObject.GetField("steering_angle").ToString());
		CarRemoteControl.Acceleration = float.Parse(jsonObject.GetField("throttle").ToString());
		var steering_bias = 1.0f * Mathf.Deg2Rad;
		CarRemoteControl.SteeringAngle += steering_bias;
		EmitTelemetry();
	}

	void EmitTelemetry()
	{
		// UnityMainThreadDispatcher.Instance().Enqueue(() =>
		// {
			print("Attempting to Send...");
			// send only if it's not being manually driven
			if ((Input.GetKey(KeyCode.W)) || (Input.GetKey(KeyCode.S))) {
				//client.Emit("telemetry", new JSONObject());
				Dictionary<string, string> data = new Dictionary<string, string>();
				data ["process"] = 0.ToString();
				client.Send("telemetry", new JSONObject(data));
			} else {
				// Collect Data from the Car
				Dictionary<string, string> data = new Dictionary<string, string>();
				var cte = wpt.CrossTrackError (_carController);
				data["steering_angle"] = _carController.CurrentSteerAngle.ToString("N4");
				data["throttle"] = _carController.AccelInput.ToString("N4");
				data["speed"] = _carController.CurrentSpeed.ToString("N4");
				data["cte"] = cte.ToString("N4");
				data ["process"] = 1.ToString();
				//data["image"] = Convert.ToBase64String(CameraHelper.CaptureFrame(FrontFacingCamera));
				//client.Emit("telemetry", new JSONObject(data));
				client.Send("telemetry", new JSONObject(data));
			}
		// });
				
	}
}