using UnityEngine;
using System.Collections.Generic;

namespace UnityStandardAssets.Vehicles.Car
{

	// Way to around multiple returns
	public class WaypointTracker
	{

		private List<Vector3> waypoints;
		// TODO: Find a way to automatically detect this

		// Use this for initialization
		public WaypointTracker()
		{
			waypoints = new List<Vector3> ();
			var wps = GameObject.Find("Waypoints").transform;
			foreach (Transform t in wps) {
				waypoints.Add(t.position);
			}
		}

		private Vector2 BezierPoint(Vector2 p0, Vector2 p1, Vector2 p2, float t) {
			var b01 = (1 - t) * p0 + t * p1;
			var b12 = (1 - t) * p1 + t * p2;
			return (1-t) * b01 + t * b12;
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

		public float CrossTrackError(CarController cc) {
			var next_wp = NextWaypoint (cc);
			var pos = cc.transform.position;

			// Previous waypoint
			int prev_wp;
			prev_wp = next_wp - 1;
			if (next_wp == 0) {
				prev_wp = waypoints.Count - 1;
			}

            // This projects the vehicle position onto the line
			// from the previous waypoint to the next waypoint.
			var n = waypoints[next_wp] - waypoints[prev_wp];
			var x = pos - waypoints[prev_wp];
			var v = new Vector2(n.x, n.z);

			// current vehicle position
			var x0 = new Vector2(x.x, x.z);

			// find the projection of x onto v
			var proj = (Vector2.Dot(x0, v) / Mathf.Abs(v.x*v.x + v.y*v.y)) * v;
            
			var t = Mathf.Clamp(proj.magnitude / n.magnitude, 0, 1);
			// Debug.Log(string.Format("Progress between waypoints {0} and {1} = {2}", prev_wp, next_wp, t));
			// Cross track error
            var threshold = 0.05f;
			if (t >= (1 - threshold)) {
                var p0 = prev_wp;
				var p1 = next_wp;
				var p2 = (next_wp + 1) % waypoints.Count;
				var i1 = threshold * waypoints[p0] + (1-threshold) * waypoints[p1];
				var i2 = (1-threshold) * waypoints[p1] + threshold * waypoints[p2];
				var v0 = new Vector2(i1.x, i1.z);
				var v1 = new Vector2(waypoints[p1].x, waypoints[p1].z);
				var v2 = new Vector2(i2.x, i2.z);
                var newt = (t - (1-threshold)) /  (threshold * 2f);
				var bp = BezierPoint(v0, v1, v2, newt) - new Vector2(waypoints[p0].x, waypoints[p0].z);
				// Debug.Log(string.Format("Bezier point {0}, Actual projection {1}, Position {2} {3} {4}", bp, proj, x0, t, newt));
				proj = bp;
			} else if (t <= threshold) {
                var p2 = next_wp;
                var p1 = prev_wp;
				var p0 = prev_wp - 1;
				if (p1 == 0) {
					p0 = waypoints.Count-1;
				}
				var i1 = threshold * waypoints[p0] + (1-threshold) * waypoints[p1];
				var i2 = (1-threshold) * waypoints[p1] + threshold * waypoints[p2];
				var v0 = new Vector2(i1.x, i1.z);
				var v1 = new Vector2(waypoints[p1].x, waypoints[p1].z);
				var v2 = new Vector2(i2.x, i2.z);
                var newt = (t / (threshold * 2f)) + 0.5f;
				var bp = BezierPoint(v0, v1, v2, newt) - new Vector2(waypoints[p1].x, waypoints[p1].z);
				// Debug.Log(string.Format("Bezier point {0}, Actual projection {1}, Position {2} {3} {4}", bp, proj, x0, t, newt));
				proj = bp;
			}

			var cte = (x0 - proj).magnitude;

            // This compares the projected position and the current vehicle position
			// to a point in the center of the lake.
			// If projected position is closer is means the vehicle is to the right of the line
			// hence the CTE will be negative (turn left).
			// If the vehicle position is closer is means the vehicle is to the left of the line
			// hence the CTE will be positive (turn right).
			var centerPoint = new Vector3(-14.4f, 0f, 76.9f) - waypoints[prev_wp];
			var centerPoint2D = new Vector2(centerPoint.x, centerPoint.z);
			var centerToPos = Vector2.Distance(centerPoint2D, x0);
			var centerToRef = Vector2.Distance(centerPoint2D, proj);
			if (centerToPos <= centerToRef) {
				cte *= -1f;
			}
			return cte;
		}

	}

}
