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
	private int polyOrder;

	// Use this for initialization
	void Start()
	{
		_socket = GameObject.Find("SocketIO").GetComponent<SocketIOComponent>();
		_socket.On("open", OnOpen);
		_socket.On("steer", OnSteer);
		_socket.On("manual", onManual);
		_carController = CarRemoteControl.GetComponent<CarController>();
		wpt = new WaypointTracker ();
		polyOrder = 3;
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
				Dictionary<string, JSONObject> data = new Dictionary<string, JSONObject>();
				var cte = wpt.CrossTrackError (_carController);
				Debug.Log(string.Format("In between waypoint {0} and {1}", wpt.prev_wp, wpt.next_wp));
				var pos = _carController.Position();
                var heading = wpt.waypoints[wpt.next_wp] - wpt.waypoints[wpt.prev_wp];
				var psi_ref = Quaternion.LookRotation(heading).eulerAngles.y;
				var psi = _carController.Orientation().eulerAngles.y;
				Debug.Log(string.Format("Psi ref = {0}, Psi = {1}", psi_ref, psi));
				Debug.Log(string.Format("Cross track error = {0}", cte));
				
				// waypoints data
				var ptsx = new List<JSONObject>();
				var ptsy = new List<JSONObject>();
				for (int i = wpt.prev_wp; i < wpt.prev_wp+polyOrder+1; i++) {
					ptsx.Add(new JSONObject(wpt.waypoints[i%wpt.waypoints.Count].x));
					ptsy.Add(new JSONObject(wpt.waypoints[i%wpt.waypoints.Count].z));
				}
				data["ptsx"] = new JSONObject(ptsx.ToArray());
				data["ptsy"] = new JSONObject(ptsy.ToArray());

                data["psi"] = new JSONObject(psi * Mathf.Deg2Rad);
                data["psi_ref"] = new JSONObject(psi_ref * Mathf.Deg2Rad);

                // Angle sanity
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
				// Debug.Log(string.Format("EPsi = {0}", epsi * Mathf.Rad2Deg));

                data["x"] = new JSONObject(pos.x);
                data["y"] = new JSONObject(pos.z);
                data["epsi"] = new JSONObject(epsi);
				data["steering_angle"] = new JSONObject(_carController.CurrentSteerAngle * Mathf.Deg2Rad);
				data["throttle"] = new JSONObject(_carController.AccelInput);
				data["speed"] = new JSONObject(_carController.CurrentSpeed);
				data["cte"] = new JSONObject(cte);
				_socket.Emit("telemetry", new JSONObject(data));
			}
		});
				
	}
}