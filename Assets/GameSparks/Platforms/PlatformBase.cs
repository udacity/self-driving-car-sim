using UnityEngine;
using System.Collections;
using GameSparks.Core;
using System.Collections.Generic;
using System;

namespace GameSparks.Platforms
{
	/// <summary>
	/// This is the base class for all platform specific implementations. 
	/// Depending on your BuildTarget in Unity, GameSparks will automatically determine 
	/// which implementation to use for platform specific code. 
	/// </summary>
	public abstract class PlatformBase : MonoBehaviour, GameSparks.Core.IGSPlatform {
		
		static string PLAYER_PREF_AUTHTOKEN_KEY = "gamesparks.authtoken";
		static string PLAYER_PREF_USERID_KEY = "gamesparks.userid";
		
		
		virtual protected void Start()
		{
			
			DeviceName = SystemInfo.deviceName.ToString();
			DeviceType = SystemInfo.deviceType.ToString();
			DeviceId = SystemInfo.deviceUniqueIdentifier.ToString();
			AuthToken = PlayerPrefs.GetString(PLAYER_PREF_AUTHTOKEN_KEY);
			UserId = PlayerPrefs.GetString(PLAYER_PREF_USERID_KEY);
			Platform = Application.platform.ToString();
			ExtraDebug = GameSparksSettings.DebugBuild;
			ServiceUrl = GameSparksSettings.ServiceUrl;
			GameSparksSecret = GameSparksSettings.ApiSecret;
#if !UNITY_WEBPLAYER
			PersistentDataPath = Application.persistentDataPath;
#endif
			RequestTimeoutSeconds = 5;
			
            GS.Initialise(this);
			
			DontDestroyOnLoad (this);
			
		}
		private List<Action> _actions = new List<Action>();

		/// <summary>
		/// Executes the given Action the on main thread of Unity.
		/// </summary>
		public void ExecuteOnMainThread(Action action){
			lock(_actions){
				_actions.Add(action);
			}
		}
		
		virtual protected void Update(){
			List<Action> _currentActions = new List<Action>();
			lock (_actions)
			{
				_currentActions.AddRange(_actions);
				_actions.Clear();
			}
			foreach(var a in _currentActions)
			{
				if(a != null)
				{
					a();
				}
			}
			
		}
		
		virtual protected void OnApplicationPause(bool paused) 
		{
			if(paused)
			{
				GS.Disconnect();
			}
			else
			{
				GS.Reconnect();
			}
		}
		
		virtual protected void OnDestroy () {
			Update();
			GS.ShutDown();
		}
		
		public String DeviceOS {
			get{
				#if UNITY_IOS
				return "IOS";
				#elif UNITY_ANDROID
				return "ANDROID";
				#elif UNITY_METRO
				return "W8";
				#else
				return "WP8";
				#endif
			}
		}
		
		public String DeviceName  {get; private set;}
		public String DeviceType {get; private set;}
		public virtual String DeviceId  {get; private set;}
		public String Platform {get; private set;}

		/// <summary>
		/// Allow for extra debug output. To set it use the GameSparksSettings editor window. <see cref="GameSparksSettings.DebugBuild"/>
		/// </summary>
		public bool ExtraDebug {get; private set;}

		/// <summary>
		/// The service URL GameSparks connects to. To set it use the GameSparksSettings editor window. <see cref="GameSparksSettings.DebugBuild"/>
		/// </summary>
		/// <value>The service URL.</value>
		public String ServiceUrl  {get; private set;}

		/// <summary>
		/// The Api secret. The Api secret can be obtained from the GameSparks Developer Portal and is game specific. To set it use the GameSparksSettings editor window. 
		/// </summary>
		public String GameSparksSecret  {get; private set;}

		public int RequestTimeoutSeconds  {get; set;}
		public String PersistentDataPath{get; private set;}

        
		/// <summary>
		/// Logs the given string to the Unity debug console. 
		/// </summary>
		public void DebugMsg(String message){
            ExecuteOnMainThread(() => {
                if (message.Length < 1500)
                {
                    Debug.Log("GS: " + message);
                } else
                {
                    Debug.Log("GS: " + message.Substring(0, 1500) + "...");
                }
            });
		}
		
		public String SDK{get;set;}
		
		private String m_authToken="0";

		public String AuthToken {
			get {return m_authToken;}
			set {
				m_authToken = value;
				PlayerPrefs.SetString(PLAYER_PREF_AUTHTOKEN_KEY, value);
			}
		}

		private String m_userId="";

		public String UserId {
			get {return m_userId;}
			set {
				m_userId = value;
				PlayerPrefs.SetString(PLAYER_PREF_USERID_KEY, value);
			}
		}

		/// <summary>
		/// Returns a (platform specific) timer implementation. 
		/// </summary>
		/// <returns>The timer.</returns>
		public abstract IGameSparksTimer GetTimer();

		/// <summary>
		/// Returns a hmac created with SHA-256 based on the given parameters. 
		/// </summary>
		public abstract string MakeHmac(string stringToHmac, string secret);

		/// <summary>
		/// Creates a (platform specific) Websocket and returns the instance. 
		/// </summary>
		public abstract IGameSparksWebSocket GetSocket(string url, Action<string> messageReceived, Action closed, Action opened, Action<string> error);
	}

}