using UnityEngine;
using System.Collections;
using System;
using GameSparks.Core;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace GameSparks.Platforms
{
	/// <summary>
	/// Custom, non-threaded timer implementation for use in Unity. 
	/// Should be used with a <see cref="TimerController"/>. 
	/// It is used on multiple platforms where threading is not available or not beneficial. 
	/// </summary>
    public class UnityTimer : IControlledTimer
    {
        System.Action callback;
        int interval;
        long elapsedTicks;
        bool running = false;

        TimerController controller;

        public void SetController(TimerController controller)
        {
            this.controller = controller;
            this.controller.AddTimer(this);
        }

        #region IGameSparksTimer implementation

        public void Initialize(int interval, Action callback)
        {
            this.callback = callback;
            this.interval = interval;
            running = true;
        }

        public void Trigger()
        {
            /*if (callback != null)
            {
                callback();
            }*/
        }

        public void Stop()
        {
            running = false;
            callback = null;
            this.controller.RemoveTimer(this);
        }

        #endregion

        public void Update(long ticks)
        {
            if (running)
            {
                elapsedTicks += ticks;
                if (elapsedTicks > interval)
                {

                    elapsedTicks -= interval;
                    //Trigger();
                    if(this.callback != null)
                    {
                        this.callback();
                    }
                }
            }
        }

    }
}