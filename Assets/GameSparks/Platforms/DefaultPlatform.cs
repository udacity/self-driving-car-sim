using UnityEngine;
using System.Collections.Generic;
using System;
using GameSparks;
using GameSparks.Core;

namespace GameSparks.Platforms
{
	/// <summary>
	/// The default implementation for platform specific functionality. 
	/// This is used for Android, Windows Store, Windows Standalone and Windows Phone. 
	/// </summary>
	public class DefaultPlatform : PlatformBase {
		
		public override IGameSparksTimer GetTimer()
		{
			return new GameSparksTimer();
		}
		
		public override string MakeHmac(string stringToHmac, string secret)
		{
			return GameSparksUtil.MakeHmac(stringToHmac, secret);
		}
		
		public override IGameSparksWebSocket GetSocket(string url, Action<string> messageReceived, Action closed, Action opened, Action<string> error)
		{
			GameSparksWebSocket socket = new GameSparksWebSocket();
			socket.Initialize(url, messageReceived, closed, opened, error);
			return socket;
		}
	}
}

// namespace documentation
    
/// <summary>
/// Namespace for GameSparks classes
/// </summary>
namespace GameSparks
{
}


/// <summary>
/// Namespace for platform dependent code
/// </summary>
namespace GameSparks.Platforms
{
}