using UnityEngine;
using System.Collections.Generic;

namespace UnityStandardAssets.Vehicles.Car
{

	// Way to around multiple returns
	public class Data
	{
        public List<Vector3> waypointPositions;
		public Vector3 position;
		public float ourY;
		public float refY;

		public Data (Vector3 position, List<Vector3> waypointPositions, float ourY, float refY) {
			this.position = position;
			this.waypointPositions = waypointPositions;
			this.ourY = ourY;
			this.refY = refY;
		}
	}

	public class WaypointTracker
	{

		private List<Transform> waypoints;
		// TODO: Find a way to automatically detect this
		private int numWaypoints = 28;
		// Progress of distance travelled between two waypoints
		private float progress = 0f;
		// Waypoint id
		private int currentNextWaypoint;

		// Use this for initialization
		public WaypointTracker()
		{
			waypoints = new List<Transform> ();

			for (int i = 0; i < numWaypoints; i++) {
				Transform t = GameObject.Find("Waypoint " + (i).ToString("000")).transform;
				waypoints.Add(t);
			}
		}

		// Compute the next waypoint we should go to
		private int NextWaypoint(CarController cc) {
			Vector3 p = cc.transform.position;
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

			Vector3 heading = waypoints[closestWaypoint].position - p;
			heading.y = 0;
			// This is the angle we have to turn to get to the next waypoint.
			// It should be a small value, if it's large then it means we have to turn around
			// and the waypoint should be actually be the next one.
			float angle = Quaternion.Angle (cc.transform.rotation, Quaternion.LookRotation (heading));
			// We now have the correct waypoint
			// 120 is kind of arbitrary
			if (angle > 120) {
				return (closestWaypoint + 1) % numWaypoints;
			}
			return closestWaypoint;
		}

		public Data SensorData(CarController cc) {
			int p1 = NextWaypoint (cc);
			var pos = cc.transform.position;
			// reset Progress if new waypoint
			if (p1 != currentNextWaypoint) {
				progress = 0f;
				currentNextWaypoint = p1;

			}

			// previous waypoint
			int p0;
			if (currentNextWaypoint == 0) {
				p0 = numWaypoints-1;
			} else {
				p0 = currentNextWaypoint - 1;
			}
				
			// distance between waypoints in meters, pretty sure unity measures in meters but not 100% sure.
			// float waypointDist = Vector3.Distance(waypoints[p0].position, waypoints[p1].position);
			// float distToNextWaypoint = Vector3.Distance(pos, waypoints[p1].position);
			// progress = 1f - distToNextWaypoint / waypointDist;
			// Debug.Log (string.Format ("progress between waypoints {0} and {1}: {2}%", p0, p1, progress));
			// Vector3 reference = Vector3.Lerp(waypoints[p0].position, waypoints[p1].position, progress);

			// reference.y = 0;
			// pos.y = 0;
			// Debug.Log (string.Format("Current Position = {0}, Reference = {1}, Distance = {2}, Angle = {3}", 
			// 	pos, reference, Vector3.Distance(pos, reference), 
			// 	Quaternion.FromToRotation(pos, reference)));

            var waypointPositions = new List<Vector3>();
			foreach (Transform t in waypoints) {
                waypointPositions.Add(t.position);
			}

            var relativePos = waypoints[p1].position - waypoints[p0].position;
			var refY = Quaternion.LookRotation(relativePos).eulerAngles.y;
			var ourY = cc.transform.rotation.eulerAngles.y;
			return new Data(pos, waypointPositions, ourY, refY);
		}
			
	}

}