using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using SocketIO;
using UnityStandardAssets.Vehicles.Car;
using System;
using System.Security.AccessControl;

public class CommandServer : MonoBehaviour
{
	public GameObject Car;
	public Camera FrontFacingCamera;
	private SocketIOComponent _socket;
	private CarController _carController;
	private perfect_controller point_path;
	private CarTraffic car_traffic;


	// Convert angle (degrees) from Unity orientation to 
	//            90
	//
	//  180                   0/360
	//
	//            270
	//
	// This is the standard format used in mathematical functions.
	float convertAngle(float psi) {
		if (psi >= 0 && psi <= 90) {
			return 90 - psi;
		}
		else if (psi > 90 && psi <= 180) {
			return 90 + 270 - (psi - 90);
		}
		else if (psi > 180 && psi <= 270) {
			return 180 + 90 - (psi - 180);
		}
		return 270 - 90 - (psi - 270);
	}

	// Use this for initialization
	void Start()
	{
		_socket = GameObject.Find("SocketIO").GetComponent<SocketIOComponent>();
		_socket.On("open", OnOpen);
		_socket.On("manual", onManual);
		_socket.On("control", Control);
		_carController = Car.GetComponent<CarController>();
		point_path = Car.GetComponent<perfect_controller>();
		car_traffic = Car.GetComponent<CarTraffic>();

	}

	void OnOpen(SocketIOEvent obj)
	{
		Debug.Log("Connection Open");
		point_path.OpenScript ();
		EmitTelemetry(obj);
	}
	void OnClose(SocketIOEvent obj)
	{
		Debug.Log("Connection Closed");
		point_path.CloseScript ();

	}

	// 
	void onManual(SocketIOEvent obj)
	{
		EmitTelemetry (obj);
	}

	void Control(SocketIOEvent obj)
	{
		JSONObject jsonObject = obj.data;

		//Debug.Log ("sending control");


		var next_x = jsonObject.GetField ("next_x");
		var next_y = jsonObject.GetField ("next_y");
		List<float> my_next_x = new List<float> ();
		List<float> my_next_y = new List<float> ();

		for (int i = 0; i < next_x.Count; i++) 
		{
			my_next_x.Add (float.Parse((next_x [i]).ToString()));
			my_next_y.Add (float.Parse((next_y [i]).ToString()));
		}

		point_path.setControlPath (my_next_x, my_next_y);
		//point_path.ProgressPath ();

		point_path.setSimulatorProcess();

		EmitTelemetry (obj);
	}
		

	void EmitTelemetry(SocketIOEvent obj)
	{
		UnityMainThreadDispatcher.Instance().Enqueue(() =>
		{

			//print("Attempting to Send...");
			// send only if it's not being manually driven
			if ( !point_path.isServerProcess() ) {
				_socket.Emit("telemetry", new JSONObject());
				

			}
			else {

				point_path.ServerPause();
				
				// Collect Data from the Car
				Dictionary<string, JSONObject> data = new Dictionary<string, JSONObject>();

				// localization of car
				data["x"] = new JSONObject(Car.transform.position.x);
				data["y"] = new JSONObject(Car.transform.position.z);
				data["yaw"] = new JSONObject (convertAngle(Car.transform.rotation.eulerAngles.y));
				data["speed"] = new JSONObject(_carController.CurrentSpeed);

				CarAIControl carAI = (CarAIControl) Car.GetComponent(typeof(CarAIControl));

				List<float> frenet_values = carAI.getThisFrenetFrame();

				data["s"] = new JSONObject(frenet_values[0]);
				data["d"] = new JSONObject(frenet_values[1]);

				// Previous Path data
				JSONObject arr_x = new JSONObject(JSONObject.Type.ARRAY);
				JSONObject arr_y = new JSONObject(JSONObject.Type.ARRAY);
				var previous_path_x = point_path.previous_path_x();
				var previous_path_y = point_path.previous_path_y();

				for( int i = 0; i < previous_path_x.Count; i++)
				{
						arr_x.Add(previous_path_x[i]);
						arr_y.Add(previous_path_y[i]);
				}

				var previous_y = JsonUtility.ToJson(point_path.previous_path_y());
				data["previous_path_x"] = arr_x;
				data["previous_path_y"] = arr_y;
				
				var end_path_s = 0.0f;
				var end_path_d = 0.0f;

				if(previous_path_x.Count > 0)
				{
					List<float> frenet_values_others = carAI.getFrenetFrame(previous_path_x[previous_path_x.Count-1],previous_path_y[previous_path_y.Count-1]);
					end_path_s = frenet_values_others[0];
					end_path_d = frenet_values_others[1];
				}

				//End path S and D values
				data["end_path_s"] = new JSONObject(end_path_s);
				data["end_path_d"] = new JSONObject(end_path_d);

				
				//data["v_x"] = new JSONObject((Car.GetComponent<Rigidbody>().velocity.x));  
				//data["v_y"] = new JSONObject((Car.GetComponent<Rigidbody>().velocity.z));
				//Vector3 vdir = Car.GetComponent<Rigidbody>().velocity;
				//data["v_yaw"] = new JSONObject((float)convertAngle(Mathf.Atan2(vdir.x,vdir.z)*Mathf.Rad2Deg));
				//data["a_x"] = new JSONObject(_carController.SenseAcc().x);
				//data["a_y"] = new JSONObject(_carController.SenseAcc().z);
				//Vector3 adir = _carController.SenseAcc();
				//data["a_yaw"] = new JSONObject(((float)convertAngle(Mathf.Atan2(adir.x,adir.z)*Mathf.Rad2Deg)));

				CarTraffic cars = (CarTraffic) Car.GetComponent(typeof(CarTraffic));
				data["sensor_fusion"] = new JSONObject(cars.example_sensor_fusion());
				

				//data["steering_angle"] = new JSONObject(_carController.CurrentSteerAngle);
				//data["throttle"] = new JSONObject(_carController.AccelInput);
				//data["speed"] = new JSONObject(_carController.CurrentSpeed);
				_socket.Emit("telemetry", new JSONObject(data));
			}
		});

		//    UnityMainThreadDispatcher.Instance().Enqueue(() =>
		//    {
		//      	
		//      
		//
		//		// send only if it's not being manually driven
		//		if ((Input.GetKey(KeyCode.W)) || (Input.GetKey(KeyCode.S))) {
		//			_socket.Emit("telemetry", new JSONObject());
		//		}
		//		else {
		//			// Collect Data from the Car
		//			Dictionary<string, string> data = new Dictionary<string, string>();
		//			data["steering_angle"] = _carController.CurrentSteerAngle.ToString("N4");
		//			data["throttle"] = _carController.AccelInput.ToString("N4");
		//			data["speed"] = _carController.CurrentSpeed.ToString("N4");
		//			data["image"] = Convert.ToBase64String(CameraHelper.CaptureFrame(FrontFacingCamera));
		//			_socket.Emit("telemetry", new JSONObject(data));
		//		}
		//      
		////      
		//    });
	}
}