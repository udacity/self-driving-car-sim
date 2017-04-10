using UnityEngine;
using System.Collections.Generic;
using SocketIO;
using UnityStandardAssets.Vehicles.Car;
using System;

public class CommandServer : MonoBehaviour
{
	public CarRemoteControl CarRemoteControl;
	public Camera FrontFacingCamera;
	private SocketIOComponent _socket;
	private CarController _carController;
	private WaypointTracker wpt;

	// Use this for initialization
	void Start()
	{
		_socket = GameObject.Find("SocketIO").GetComponent<SocketIOComponent>();
		_socket.On("open", OnOpen);
		_socket.On("steer", OnSteer);
		_socket.On("manual", onManual);
		_carController = CarRemoteControl.GetComponent<CarController>();
		wpt = new WaypointTracker ();
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

	// 
	void onManual(SocketIOEvent obj)
	{
        Debug.Log("Manual driving event ...");
		EmitTelemetry (obj);
	}

	void OnSteer(SocketIOEvent obj)
	{
        Debug.Log("Steering data event ...");
		JSONObject jsonObject = obj.data;
		CarRemoteControl.SteeringAngle = float.Parse(jsonObject.GetField("steering_angle").ToString());
		CarRemoteControl.Acceleration = float.Parse(jsonObject.GetField("throttle").ToString());
		EmitTelemetry(obj);
	}

	void EmitTelemetry(SocketIOEvent obj)
	{
		UnityMainThreadDispatcher.Instance().Enqueue(() =>
		{
			print("Attempting to Send...");
			// send only if it's not being manually driven
			if ((Input.GetKey(KeyCode.W)) || (Input.GetKey(KeyCode.S))) {
				_socket.Emit("telemetry", new JSONObject());
			} else {
				// Collect Data from the Car
				Dictionary<string, string> data = new Dictionary<string, string>();
				var cte = wpt.CrossTrackError (_carController);
				Debug.Log(string.Format("In between waypoint {0} and {1}", wpt.prev_wp, wpt.next_wp));
				var pos = _carController.Position();
                var heading = wpt.waypoints[wpt.next_wp] - wpt.waypoints[wpt.prev_wp];
				var psi_ref = Quaternion.LookRotation(heading).eulerAngles.y;
				var psi = _carController.Orientation().eulerAngles.y;
				Debug.Log(string.Format("Psi ref = {0}", psi_ref));

				if (psi == 0) {
					psi = 360;
				}
				if (psi_ref == 0) {
					psi_ref = 360;
				}
				if (psi_ref >= 270 && psi <= 90) {
					psi += 360;
				} else if (psi >= 270 && psi_ref <= 90) {
					psi_ref += 360;
				}
				var epsi = (psi - psi_ref) * Mathf.Deg2Rad;

                data["wp1"] = string.Format("{0}", wpt.prev_wp);
                data["wp2"] = string.Format("{0}", wpt.next_wp);
                data["x"] = string.Format("{0}", pos.x);
                data["y"] = string.Format("{0}", pos.z);
                data["psi"] = string.Format("{0}", psi);
                data["psi_ref"] = string.Format("{0}", psi_ref);
                data["epsi"] = string.Format("{0}", epsi);
				data["steering_angle"] = string.Format("{0}", _carController.CurrentSteerAngle * Mathf.Deg2Rad);
				data["throttle"] = string.Format("{0}", _carController.AccelInput);
				data["speed"] = string.Format("{0}", _carController.CurrentSpeed);
				data["cte"] = string.Format("{0}", cte);
				data["image"] = Convert.ToBase64String(CameraHelper.CaptureFrame(FrontFacingCamera));
				_socket.Emit("telemetry", new JSONObject(data));
			}
		});
				
	}
}