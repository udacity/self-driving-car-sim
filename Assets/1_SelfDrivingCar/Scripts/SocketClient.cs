using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityStandardAssets.Vehicles.Car;

public class SocketClient : MonoBehaviour {
	private WebSocket _socket;
	private bool ready = false;
	private Dictionary<string, Action<JSONObject>> routes = new Dictionary<string, Action<JSONObject>>();

	// Use this for initialization
	// Declaration for running locally using static IP E.G 4567
	IEnumerator Start () {
	// Declaration for WebGL classroom workspace for dynamic IP
	//IEnumerator StartConnection (string myurl) {
		// Declaration for running locally using static IP E.G 4567
		_socket = new WebSocket(new Uri("ws://127.0.0.1:4567/"));
		// Declaration for WebGL classroom workspace for dynamic IP
		//_socket = new WebSocket(new Uri(myurl));
		Debug.Log("starting SocketClient");
		yield return StartCoroutine(_socket.Connect ());

		ready = true;
		while (true)
		{
			string reply = _socket.RecvString();
			if (reply != null)
			{
				//Debug.Log ("reply is " + reply);
				//Debug.LogError ("hello");
				//Debug.LogError (reply);
				JSONObject obj = new JSONObject (reply);
				if (String.Compare (obj [0].ToString (), "{}") != 0) 
				{
					routes ["slam"] (obj [0]);
				} 
				else 
				{
					routes ["manual"] (obj [0]);
				}

				//string key = obj.GetField ("slam").ToString ();
				//if (routes.ContainsKey(key)) {
				
				//Debug.Log ("secret sauce " + obj.GetField ("data"));
				//Debug.Log ("secret sauce " + obj[0]);
				
				//}

			}
			if (_socket.error != null)
			{
				Debug.LogError ("Error: "+_socket.error);
				break;
			}
			yield return 0;
		}
		_socket.Close();
	}

	public void On(string topic, Action<JSONObject> func)
	{
		routes.Add (topic, func);
	}

	public bool Send(string topic, JSONObject obj)
	{
		if (ready) {
			Dictionary<string, JSONObject> data = new Dictionary<string, JSONObject>();
			data [topic] = obj;
			JSONObject toSend = new JSONObject (data);
			_socket.SendString (toSend.ToString());
			return true;
		}
		return false;
	}

	public void Reset()
	{
	}

}
