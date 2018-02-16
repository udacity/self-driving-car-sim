using System;
using GameSparks;

namespace GameSparks.Platforms
{
    /// <summary>
    /// Interface for a timer which is triggered by a 
    /// <see cref="TimerController"/>.
    /// </summary>
    public interface IControlledTimer : IGameSparksTimer
    {
        void Update(long ticks);

    }

}