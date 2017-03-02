using UnityEngine;
using System.Collections.Generic;

namespace UnityStandardAssets.Vehicles.Car
{

	// Way to around multiple returns
	public class Data
	{
		public float cte;
		public float yaw;
		public float our_angle;
		public float ref_angle;

		public Data (float cte, float yaw, float our_angle, float ref_angle) {
			this.cte = cte;
			this.yaw = yaw;
			this.our_angle = our_angle;
			this.ref_angle = ref_angle;
		}
	}

	public class WaypointTracker
	{

		private List<Vector3> waypoints;
		private List<float> angles;
		// TODO: Find a way to automatically detect this
		private int numWaypoints = 30;
		// Progress of distance travelled between two waypoints
		private float progress = 0f;
		// Waypoint id
		private int currentNextWaypoint;

		// Use this for initialization
		public WaypointTracker()
		{
			waypoints = new List<Vector3> ();
			angles = new List<float>();

			for (int i = 0; i < numWaypoints; i++) {
				Transform t = GameObject.Find("Waypoint " + (i).ToString("000")).transform;
				waypoints.Add(t.position);
			}

			Debug.Log(numWaypoints);

			// waypoints = DensifyPath(waypoints, 50);
			// waypoints = SmoothPath(waypoints, 0.1f, 0.05f, 0.000001f);

			// Debug.Log(string.Format("Waypoints count after densify = {0}", waypoints.Count));
			// for (int i = 1; i < waypoints.Count; i++) {
			// 	Debug.Log( string.Format("{0}-{1} {2}", i-1, i, waypoints[i]) );
			// }
			for (int i = 0; i < waypoints.Count; i++) {
                var j = (i + 1) % waypoints.Count;
				var heading = waypoints[j] - waypoints[i];
				float angle = Quaternion.LookRotation (heading).eulerAngles.y;
				angles.Add(angle);
			}
			// for (int i = 1; i < waypoints.Count; i++) {
			// 	var heading = waypoints[i] - waypoints[i-1];
			// 	float angle = Quaternion.LookRotation (heading).eulerAngles.y;
			// 	angles.Add(angle);
			// }
		}

		private List<Vector3> DensifyPath(List<Vector3> path, float dfactor) {
            var newWpts = new List<Vector3>();
			for (int i = 1; i < path.Count; i++) {
				var j = i - 1;
				for (int d = 0; d < dfactor; d++) {
                    var progress = d / (dfactor);
					Vector3 pt = Vector3.Lerp(path[j], path[i], progress);
					newWpts.Add(pt);
				}
			}
			return newWpts;
		}

		private List<Vector3> SmoothPath(List<Vector3> path, float a, float b, float tolerance) {
            var sPath = new List<Vector3>(path);
            var diff = tolerance;

			while (diff < tolerance) {
				diff = 0f;
				for (int i = 1; i < path.Count-1; i++) {
					var tx = sPath[i].x;
					var newx = a * (path[i].x - sPath[i].x) + b * (sPath[i+1].x + sPath[i-1].x - 2 * sPath[i].x);
					diff = Mathf.Abs(tx - newx);

					var tz = sPath[i].z;
					var newz = a * (path[i].z - sPath[i].z) + b * (sPath[i+1].z + sPath[i-1].z - 2 * sPath[i].z);
					diff = Mathf.Abs(tz - newz);

					var v = new Vector3(newx, path[i].y, newz);
					sPath[i] = v;
				}
			}
			return sPath;
		}

		// Compute the next waypoint we should go to
		private int NextWaypoint(CarController cc) {
			Vector3 p = cc.transform.position;
			float closestLen = 100000; // large number
			int closestWaypoint = 0;

			int i = 0;
			foreach (Vector3 t in waypoints) {
				float dist = Vector3.Distance (t, p);
				if (dist < closestLen) {
					closestLen = dist;
					closestWaypoint = i;
				}
				i += 1;
			}

			Vector3 heading = waypoints[closestWaypoint] - p;
			heading.y = 0;
			// This is the angle we have to turn to get to the next waypoint.
			// It should be a small value, if it's large then it means we have to turn around
			// and the waypoint should be actually be the next one.
			float angle = Quaternion.Angle (cc.transform.rotation, Quaternion.LookRotation (heading));
			// We now have the correct waypoint
			if (angle > 90) {
				return (closestWaypoint + 1) % waypoints.Count;
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
				p0 = waypoints.Count-1;
			} else {
				p0 = currentNextWaypoint - 1;
			}

			// next, next waypoint
			var p2 = (currentNextWaypoint + 1) % waypoints.Count;

			// distance between waypoints in meters, pretty sure unity measures in meters but not 100% sure.
			float distToNextWaypoint = Vector3.Distance(pos, waypoints[p1]);
			float waypointDist = Vector3.Distance(waypoints[p0], waypoints[p1]);
			progress = 1f - Mathf.Clamp(distToNextWaypoint / waypointDist, 0, 1);
			// Debug.Log (string.Format ("progress between waypoints {0} and {1}: {2} {3}", p0, p1, distToNextWaypoint, waypointDist));
			// Vector3 b01 = Vector3.Lerp(waypoints[p0], waypoints[p1], progress);
			// Vector3 b12 = Vector3.Lerp(waypoints[p1], waypoints[p2], progress);
			// Vector3 reference = (1f - progress) * b01 + progress * b12;
			Vector3 reference = Vector3.Lerp(waypoints[p0], waypoints[p1], progress);


			var ref_angle = (1f - progress) * angles[p0] + progress * angles[p1];
			// var ref_angle = angles[p0];
			var our_angle = cc.transform.eulerAngles.y;
			// Debug.Log(string.Format("PA {0}, NA {1}, CA {2}, RA {3}", angles[p0], angles[p1], ref_angle2, ref_angle));

			Debug.Log(string.Format("Reference Angle = {0}", ref_angle));
			if (ref_angle == 0) {
				ref_angle = 360;
			}
			if (ref_angle >= 270 && our_angle <= 90) {
				our_angle += 360;
			}
			else if (our_angle >= 270 && ref_angle <= 90) {
				ref_angle += 360;
			}
			var yaw = ref_angle - our_angle;

			reference.y = 0;
			pos.y = 0;

			// Debug.Log(string.Format("Initial Ref = {0} Ref Orientation = {1} Our Orientation = {2}", ref_angle, ref_angle2, cc.transform.eulerAngles.y));
			// Debug.Log (string.Format("Current Position = {0}, Reference = {1}, CTE = {2}",	pos, reference, Vector3.Distance(pos, reference)));
			var cte = Vector3.Distance(pos, reference);
            // Determine the sign of the CTE
			var centerPoint = new Vector3(-14.4f, 0f, 76.9f);
			var centerToPos = Vector3.Distance(centerPoint, pos);
			var centerToRef = Vector3.Distance(centerPoint, reference);
			if (centerToPos >= centerToRef) {
				cte *= -1f;
			}
			return new Data(cte, yaw, our_angle, ref_angle);
		}

	}

}
