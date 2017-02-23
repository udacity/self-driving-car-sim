using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

namespace UnityStandardAssets.Vehicles.Car
{
    public class Steering
    {

        public float H { get; private set; }
        public float V { get; private set; }
        public bool Cruising { get; private set; } // cruise control
		public bool mouse_hold;
		public float mouse_start;

        // Use this for initialization
        public void Start()
        {
            H = 0f;
            V = 0f;
            Cruising = false;
			mouse_hold = false;
        }

        // Update is called once per frame
        public void UpdateValues()
        {
            // Cruise Control
            if (Input.GetKeyDown(KeyCode.Space))
            {
                Cruising = !Cruising;
            }

            if (Cruising)
            {
                V = 0.4f; // gets to max speed at a gradual pace
            }
            else
            {
                V = CrossPlatformInputManager.GetAxis("Vertical");
            }

			if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) 
			{
				if (H > -1.0) 
				{
					H -= 0.05f;
				}
			}
			else if (Input.GetKey (KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) 
			{
				if (H < 1.0) 
				{
					H += 0.05f;
				}
			}
            else if (Input.GetMouseButton(0))
            {
				// get the mouse position
				float mousePosition = Input.mousePosition.x;

				// check if its the first time pressing down on mouse button
				if (!mouse_hold)
				{
					// we are now holding down the mouse
					mouse_hold = true;
					// set the start reference position for position tracking
					mouse_start = mousePosition;
				}
			
				// This way h is [-1, -1]
				// it's quite hard to get a max or close to max
				// steering angle unless it's actually wanted.
				H = Mathf.Clamp ( (mousePosition - mouse_start)/(Screen.width/6), -1, 1);
			
            }
            else
            { 

				// reset
				mouse_hold = false;

				H = CrossPlatformInputManager.GetAxis ("Horizontal");

            }
				
        }
    }
}