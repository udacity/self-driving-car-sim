using UnityEngine;
using System.Collections;
using System;
using GameSparks.Core;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace GameSparks.Platforms
{
	/// <summary>
	/// Timer controller which can hold and update multiple timers. 
	/// </summary>
    public class TimerController
    {
        long timeOfLastUpdate;
        List<IControlledTimer> timers = new List<IControlledTimer>();

        public void Initialize()
        {
            timeOfLastUpdate = System.DateTime.UtcNow.Ticks;
        }

        public void Update()
        {
            long ticksSinceLastUpdate = System.DateTime.UtcNow.Ticks - timeOfLastUpdate;
            timeOfLastUpdate += ticksSinceLastUpdate;

            foreach (var timer in timers)
            {
                timer.Update(ticksSinceLastUpdate);
            }

        }

        public void AddTimer(IControlledTimer timer)
        {
            timers.Add(timer);
        }

        public void RemoveTimer(IControlledTimer timer)
        {
            timers.Remove(timer);
        }

    }

}