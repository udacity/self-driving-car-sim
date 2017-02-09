using System;
using UnityEngine;
using System.Collections.Generic;
using UnityStandardAssets.CrossPlatformInput;

namespace UnityStandardAssets.Vehicles.Car
{
    [RequireComponent(typeof(CarController))]
    public class CarRemoteControl : MonoBehaviour
    {
        private CarController m_Car; // the car controller we want to use

        public float SteeringAngle { get; set; }
        public float Acceleration { get; set; }
        private Steering s;
		public List<Transform> waypoints;



		private int ClosestWaypoint() {
			Vector3 p = m_Car.Position ();
//			Quaternion o = m_Car.Orientation ();
			float closestLen = 100000; // large number
			int closestWaypoint = 0;

			int i = 0;
			foreach (Transform t in waypoints) {
				float dist = Vector3.Distance (t.position, p);
				if (dist < closestLen) {
					closestLen = dist;
					closestWaypoint = i;
				}
				i += 1;
			}

			return closestWaypoint;
		}

        private void Awake()
        {
            // get the car controller
            m_Car = GetComponent<CarController>();
            s = new Steering();

			// Pretty hacky, magic number and stuff
			for (int i = 0; i < 28; i++) {
				Transform t = GameObject.Find("Waypoint " + (i).ToString("000")).transform;
				waypoints.Add(t);
			}

            s.Start();
        }

        private void FixedUpdate()
        {
			int i = ClosestWaypoint ();
			Vector3 heading = waypoints[i].position - m_Car.Position();
			heading.y = 0;
			// This is the angle we have to turn to get to the next waypoint.
			// It should be a small value, if it's large then it means we have to turn around
			// and the waypoint should be actually be the next one.
			float angle = Quaternion.Angle (m_Car.Orientation (), Quaternion.LookRotation (heading));
			// We now have the correct waypoint
//			Debug.Log (string.Format ("{0} {1}", i, angle));
			// 120 is kind of arbitrary 
			int prevWaypoint = i;
			int nextWaypoint = i;
			if (angle > 120) {
				nextWaypoint = (i + 1) % 28;
			} else {
				if (i == 0) {
					prevWaypoint = 27;
				} else {
					prevWaypoint = i - 1;
				}
			}

			Debug.Log (string.Format ("In between waypoints {0} and {1}", prevWaypoint, nextWaypoint));
			// we have can do Lerp between waypoint i and waypoint j
//			Vector3.Lerp(waypoints[i].position, waypoints[j].position, 0.5f);


            // If holding down W or S control the car manually
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S))
            {
                s.UpdateValues();
                m_Car.Move(s.H, s.V, s.V, 0f);
            } else
            {
				m_Car.Move(SteeringAngle, Acceleration, Acceleration, 0f);
            }
        }
    }
}
