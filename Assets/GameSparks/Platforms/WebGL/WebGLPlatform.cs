using System;
using GameSparks.Core;
using System.Collections.Generic;
using System.Runtime.InteropServices;


namespace GameSparks.Platforms.WebGL
{
	#if UNITY_WEBGL 
	/// <summary>
	/// WebGL specific implementations. 
	/// </summary>
	public class WebGLPlatform : PlatformBase
	{
		TimerController timerController;
		WebSocketController webSocketController;

		protected override void Start ()
		{

			
			timerController = new TimerController();
			timerController.Initialize();



			// Register crypto implementation
			GSInitializeCrypto();
			
			// Register socket implementation
			GSInitializeGSSocket();
			
			webSocketController = gameObject.AddComponent<WebSocketController>();

			var gameSparksUnity = GetComponent<GameSparksUnity>();
			GameSparksSettings.SetInstance(gameSparksUnity.settings);
            
			base.Start();

		}
		#region implemented abstract members of PlatformBase
		public override IGameSparksTimer GetTimer ()
		{
			var timer = new UnityTimer();
			timer.SetController(timerController);
			return timer;
		}
		public override string MakeHmac (string stringToHmac, string secret)
		{
			int ptr = GSHmacSHA256(stringToHmac, secret);
			
			IntPtr resultPtr = new IntPtr(ptr);
			string result = Marshal.PtrToStringAuto(resultPtr);
			GSFreePtr(ptr);
			
			return result;

		}

		public override IGameSparksWebSocket GetSocket (string url, Action<string> messageReceived, Action closed, Action opened, Action<string> error)
		{
			var socket = new WebGLWebSocket();

			socket.Initialize(url, messageReceived, closed, opened, error);
			socket.SetController(webSocketController);
			socket.Open();
			return socket;
		}
		#endregion




		protected override void Update()
		{
			base.Update();
			timerController.Update();
			
		}


		[DllImport("__Internal")]
		private static extern void GSFreePtr(int ptr);
		
		[DllImport("__Internal")]
		private static extern int GSHmacSHA256(string message, string key);

		
		
		[DllImport("__Internal")]
		private static extern void GSInitializeCrypto();
		
		
		[DllImport("__Internal")]
		private static extern void GSInitializeGSSocket();


		[DllImport("__Internal")]
		private static extern int GSGetDeviceId();
	
		const string DeviceIdKey = "GameSparks.DeviceId";

		public override string DeviceId {
			get {

				string deviceId = UnityEngine.PlayerPrefs.GetString(DeviceIdKey,"");

				if(string.IsNullOrEmpty(deviceId))
				{
					DebugMsg("Generating new Device ID");
					int ptr = GSGetDeviceId();
					IntPtr resultPtr = new IntPtr(ptr);
					deviceId = Marshal.PtrToStringAuto(resultPtr);

					GSFreePtr(ptr);
					UnityEngine.PlayerPrefs.SetString(DeviceIdKey, deviceId);
					UnityEngine.PlayerPrefs.Save();
				}
				DebugMsg("Device ID was: " + deviceId);
				return deviceId;
			}
		}
	}
#endif
}

//namespace documentation

/// <summary>
/// WebGL specific classes.
/// </summary>
namespace GameSparks.Platforms.WebGL
{
}
