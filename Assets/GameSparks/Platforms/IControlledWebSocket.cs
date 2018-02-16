using System;
using GameSparks;

namespace GameSparks.Platforms
{
    /// <summary>
    /// Interface for a web-socket which is controlled by a <see cref="WebSocketController"/>
    /// </summary>
    public interface IControlledWebSocket : IGameSparksWebSocket
    {
        void TriggerOnClose();
        void TriggerOnOpen();
        void TriggerOnError(string message);
        void TriggerOnMessage(string message);
        int SocketId { get; }
    }

}