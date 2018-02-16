using System;
using System.Runtime.InteropServices;
using GameSparks.Core;
using System.Collections.Generic;
using UnityEngine;


namespace GameSparks.Platforms.WebGL
{
	
	#if UNITY_WEBGL
	/// <summary>
	/// WebGL websocket wrapping a javascript Websocket. 
	/// </summary>
	public class WebGLWebSocket : IControlledWebSocket
	{
		static int socketCount = 0;


		string url;
		System.Action<string> messageCallback;
		System.Action closeCallback;
		System.Action openCallback;
		System.Action<string> errorCallback;

		WebSocketController controller;

		public int SocketId
		{
			get;
			private set;
		}

		#region IGameSparksWebSocket implementation

		public void Initialize (string url, Action<string> onMessage, Action onClose, Action onOpen, Action<string> onError)
		{

			this.SocketId = socketCount;
			socketCount++;

			this.url = url;
			this.messageCallback = onMessage;
			this.closeCallback = onClose;
			this.openCallback = onOpen;
			this.errorCallback = onError;
		}

		public void SetController(WebSocketController controller)
		{
			this.controller = controller;
			this.controller.AddWebSocket(this);
			GSSocketInitialize(SocketId, controller.name);
		}

		public void Open ()
		{
			this.State = GameSparksWebSocketState.Connecting;
			GSSocketOpen(this.SocketId, url);
		}

		public void Close ()
		{
			this.State = GameSparksWebSocketState.Closing;
			GSSocketClose(this.SocketId);
		}

		public void Terminate ()
		{
			Close();
		}

		public void Send (string request)
		{
			if(this.State == GameSparksWebSocketState.Open)
			{
				GSSocketSend(this.SocketId, request);
			}
			else
			{
				throw new Exception("Websocket is in " + this.State + " and cannot send. ");
			}
		}

		public GameSparksWebSocketState State {
			get;
			private set;
		}

		#endregion


		public void TriggerOnError(string error)
		{
			this.State = GameSparksWebSocketState.Closed;
			if(errorCallback != null)
				errorCallback(error);
		}

		public void TriggerOnMessage(string message)
		{
			if(messageCallback != null)
				messageCallback(message);
		}

		public void TriggerOnOpen()
		{
			this.State = GameSparksWebSocketState.Open;
			if(openCallback != null)
				openCallback();
		}

		public void TriggerOnClose()
		{
			this.State = GameSparksWebSocketState.Closed;

			this.controller.RemoveWebSocket(this);

			if(closeCallback != null)
				closeCallback();
		}


		
		[DllImport("__Internal")]
		private static extern void GSSocketInitialize(int id, string name);
		
		[DllImport("__Internal")]
		private static extern void GSSocketSend(int id, string data);
		
		[DllImport("__Internal")]
		private static extern void GSSocketOpen(int id, string url);
		
		[DllImport("__Internal")]
		private static extern void GSSocketClose(int id);
	}
#endif

}
    
