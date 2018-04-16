using UnityEngine;
using System.Collections.Generic;
using System.Collections;
//using SocketIO;
using System;
using System.Security.AccessControl;


public class CommandServer_pf : MonoBehaviour
{

	//private SocketIOComponent _socket;
	private SocketClient client;
	private particle_filter_v2 particle_filter;
	public GameObject car;
	private bool init_status;


	// Use this for initialization
	void Start()
	{
		Debug.Log ("trying to connect");
		client = GameObject.Find("SocketClient").GetComponent<SocketClient>();
		

		//_socket = GameObject.Find("SocketIO").GetComponent<SocketIOComponent>();
		// client.On("open", OnOpen);
		// client.On("close", OnClose);
		client.On("manual", onManual);
		client.On("best_particle", BestParticle);

		particle_filter = car.GetComponent<particle_filter_v2> ();

		init_status = true;
		// This function is used when connecting webgl to classroom workspace
		Application.ExternalCall("mySetupFunction");
	}

	 // Update is called once per frame
  public void FixedUpdate()
  {

    if (client.isReady() && init_status) {
      init_status = false;
      EmitTelemetry ();

    }
  }
		

	// void OnOpen(JSONObject jsonObject)
	// {
	// 	Debug.Log("Connection Open");
	// 	particle_filter.OpenScript ();
	// 	EmitTelemetry();
	// }

	// void OnClose(JSONObject jsonObject)
	// {
	// 	Debug.Log("Connection Closed");
	// 	particle_filter.CloseScript ();
	// }

	void onManual(JSONObject jsonObject)
	{
		EmitTelemetry ();
	}

	void BestParticle(JSONObject jsonObject)
	{

		Debug.Log ("hello");
    JSONObject obj = jsonObject;

		if (particle_filter.isRunning ())
		{
			float estimate_x = float.Parse (jsonObject.GetField ("best_particle_x").ToString ());
			float estimate_y = float.Parse (jsonObject.GetField ("best_particle_y").ToString ());
			float estimate_theta = float.Parse (jsonObject.GetField ("best_particle_theta").ToString ());

			string associations = jsonObject.GetField ("best_particle_associations").ToString ();
			string sense_x = jsonObject.GetField ("best_particle_sense_x").ToString ();
			string sense_y = jsonObject.GetField ("best_particle_sense_y").ToString ();

			particle_filter.Estimate (estimate_x, estimate_y, estimate_theta);

			particle_filter.SenseParticleDistance (associations, sense_x, sense_y);

			particle_filter.setSimulatorProcess();

		}

		EmitTelemetry();
	}

	void EmitTelemetry()
	{
		//Debug.Log ("call thread");
		//UnityMainThreadDispatcher.Instance().Enqueue(() =>
		//{
				// Collect Data from the robot
			
				//print("Attempting to Send...");
				// send only if robot is moving
				if (!particle_filter.isRunning() || !particle_filter.isServerProcess()) {
					Dictionary<string, string> data = new Dictionary<string, string>();
					data ["process"] = 0.ToString();
					client.Send("telemetry", new JSONObject(data));
				}
				else {

					//particle_filter.ServerPause();
					particle_filter.ServerPause();
					// Collect Data from the robot's sensors
					Dictionary<string, string> data = new Dictionary<string, string>();
					data["process"] = 1.ToString();

					data["sense_x"] = particle_filter.Sense_x().ToString("N4");
					data["sense_y"] = particle_filter.Sense_y().ToString("N4");
					data["sense_theta"] = particle_filter.Sense_theta().ToString("N4");

					data["previous_velocity"] = particle_filter.getVelocity().ToString("N4");
					data["previous_yawrate"] = particle_filter.getYawrate().ToString("N4");

					data["sense_observations_x"] = particle_filter.Sense_Obsx();
					data["sense_observations_y"] = particle_filter.Sense_Obsy();

					client.Send("telemetry", new JSONObject(data));

				}
		//});
	}

}
