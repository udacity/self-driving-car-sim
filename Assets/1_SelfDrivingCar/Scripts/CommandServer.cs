using UnityEngine;
using System.Collections.Generic;
using UnityStandardAssets.Vehicles.Car;
using System;
using UnityEngine.SceneManagement;

public class CommandServer_pid : MonoBehaviour
{
	public CarRemoteControl CarRemoteControl;
	public Camera FrontFacingCamera;
	private CarController _carController;
	private WaypointTracker_pid wpt;
	private SocketClient client;

	// Use this for initialization
	void Start()
	{
		client = GameObject.Find("SocketClient").GetComponent<SocketClient>();

		client.On("steer", OnSteer);
		client.On("manual", onManual);
		_carController = CarRemoteControl.GetComponent<CarController>();
		wpt = new WaypointTracker_pid ();
	}

	// Update is called once per frame
	void Update()
	{
	}
		

	// 
	void onManual(JSONObject jsonObject)
	{
        Debug.Log("Manual driving event ...");
		//EmitTelemetry (obj);
	}

	void OnReset(JSONObject jsonObject)
	{
		SceneManager.LoadScene("LakeTrackAutonomous_pid");
		//EmitTelemetry (obj);
	}

	void OnSteer(JSONObject jsonObject)
	{
        Debug.Log("Steering data event ...");

		CarRemoteControl.SteeringAngle = float.Parse(jsonObject.GetField("steering_angle").ToString());
		CarRemoteControl.Acceleration = float.Parse(jsonObject.GetField("throttle").ToString());
		var steering_bias = 1.0f * Mathf.Deg2Rad;
		CarRemoteControl.SteeringAngle += steering_bias;
		//EmitTelemetry(obj);
	}

	void EmitTelemetry()
	{
		UnityMainThreadDispatcher.Instance().Enqueue(() =>
		{
			print("Attempting to Send...");
			// send only if it's not being manually driven
			if ((Input.GetKey(KeyCode.W)) || (Input.GetKey(KeyCode.S))) {
				//client.Send("telemetry", new JSONObject(data));
			} else {
				// Collect Data from the Car
				Dictionary<string, string> data = new Dictionary<string, string>();
				var cte = wpt.CrossTrackError (_carController);
				data["steering_angle"] = _carController.CurrentSteerAngle.ToString("N4");
				data["throttle"] = _carController.AccelInput.ToString("N4");
				data["speed"] = _carController.CurrentSpeed.ToString("N4");
				data["cte"] = cte.ToString("N4");
				data["image"] = Convert.ToBase64String(CameraHelper.CaptureFrame(FrontFacingCamera));
				client.Send("telemetry", new JSONObject(data));
			}
		});
				
	}
}