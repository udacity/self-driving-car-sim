using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class SocketClient : MonoBehaviour {
	private WebSocket _socket;
	private bool ready = false;
	private Dictionary<string, Action<JSONObject>> routes = new Dictionary<string, Action<JSONObject>>();

	// If running locally (not from inside workspace) , do this instead, and comment out the other declaration
	//IEnumerator Start () {
	// 		_socket = new WebSocket(new Uri("ws://127.0.0.1:4567/"));

	IEnumerator StartConnection (string myurl) {
		_socket = new WebSocket(new Uri(myurl));

		yield return StartCoroutine(_socket.Connect ());

		ready = true;
		while (true)
		{
			string reply = _socket.RecvString();

			if (reply != null)
			{
				
				JSONObject obj = new JSONObject (reply);

				if (String.Compare (obj [1].ToString (), "{}") != 0) 
				{
					
					Debug.Log (obj [1].ToString ());
					routes ["best_particle"] (obj [1]);
				} 
				else 
				{
					routes ["manual"] (obj [1]);
				}

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

	public bool isReady()
	{
		return ready;
	}

	public void Reset()
	{
	}

}
