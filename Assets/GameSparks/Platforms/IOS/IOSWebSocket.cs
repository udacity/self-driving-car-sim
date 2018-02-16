using UnityEngine;
using System.Collections;
using System;
using GameSparks.Core;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace GameSparks.Platforms.IOS
{
	#if UNITY_IOS
	/// <summary>
	/// iOS specific websocket. This is a wrapper for the native objective-c Websocket. 
	/// </summary>
    public class IOSWebSocket : IControlledWebSocket
    {

        static int nextSocketId;

        public int SocketId
        {
            get;
            private set;
        }

        string url;
        System.Action onOpen;
        System.Action<string> onMessage;
        System.Action<string> onError;
        System.Action onClose;

        WebSocketController controller;
        string controllerName;

        #region IGameSparksWebSocket implementation

        public void Initialize(string url, Action<string> onMessage, Action onClose, Action onOpen, Action<string> onError)
        {
            this.SocketId = nextSocketId++;
            this.onMessage = onMessage;
            this.onClose = onClose;
            this.onOpen = onOpen;
            this.onError = onError;
            this.url = url;

        }

        public void SetController(WebSocketController controller)
        {
            this.controller = controller;
            controllerName = this.controller.GSName;
            controller.AddWebSocket(this);
        }

        public void Open()
        {
            State = GameSparksWebSocketState.Connecting;
            GSExternalOpen(SocketId, url, controllerName);
        }

        public void Close()
        {
            State = GameSparksWebSocketState.Closing;
            GSExternalClose(SocketId);
        }

		public void Terminate()
		{
			Close();
		}
		
		public void Send(string request)
        {
            if (State != GameSparksWebSocketState.Open)
            {
                throw new Exception("Websocket is not open");
            }
            GSExternalSend(SocketId, request);
        }

        public GameSparksWebSocketState State
        {
            get;
            private set;
        }

        #endregion

        #region IControlledWebSocket implementation, triggered by WebSocketController

        public void TriggerOnClose()
        {
            State = GameSparksWebSocketState.Closed;
            controller.RemoveWebSocket(this);

            if (onClose != null)
                onClose();
        }

        public void TriggerOnOpen()
        {
            State = GameSparksWebSocketState.Open;

            if (onOpen != null)
                onOpen();
        }

        public void TriggerOnError(string message)
        {
            State = GameSparksWebSocketState.Closed;

            if (onError != null)
                onError(message);
        }

        public void TriggerOnMessage(string message)
        {
            if (onMessage != null)
                onMessage(message);
        }

        #endregion

        #region calls to external

        [DllImport("__Internal")]
        private static extern void GSExternalOpen(int socketId, string url, string gameObjectName);

        
        [DllImport("__Internal")]
        private static extern void GSExternalClose(int socketId);

        
        [DllImport("__Internal")]
        private static extern void GSExternalSend(int socketId, string message);

        #endregion
    }
	#endif
}

