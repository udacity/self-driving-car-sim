using System;
using UnityEngine;
using System.IO;
using Random = UnityEngine.Random;
using System.Collections.Generic;
using System.Collections;

namespace UnityStandardAssets.Vehicles.Car
{
    [RequireComponent (typeof(CarController))]
    public class CarAIControl : MonoBehaviour
    {
        public enum BrakeCondition
        {
            NeverBrake,
            // the car simply accelerates at full throttle all the time.
            TargetDirectionDifference,
            // the car will brake according to the upcoming change in direction of the target. Useful for route-based AI, slowing for corners.
            TargetDistance,
            // the car will brake as it approaches its target, regardless of the target's direction. Useful if you want the car to
            // head for a stationary target and come to rest when it arrives there.
        }

        // This script provides input to the car controller in the same way that the user control script does.
        // As such, it is really 'driving' the car, with no special physics or animation tricks to make the car behave properly.

        // "wandering" is used to give the cars a more human, less robotic feel. They can waver slightly
        // in speed and direction while driving towards their target.

        [SerializeField] [Range (0, 1)] private float m_CautiousSpeedFactor = 0.05f;
        // percentage of max speed to use when being maximally cautious
        [SerializeField] [Range (0, 180)] private float m_CautiousMaxAngle = 50f;
        // angle of approaching corner to treat as warranting maximum caution
        [SerializeField] private float m_CautiousMaxDistance = 100f;
        // distance at which distance-based cautiousness begins
        [SerializeField] private float m_CautiousAngularVelocityFactor = 30f;
        // how cautious the AI should be when considering its own current angular velocity (i.e. easing off acceleration if spinning!)
        [SerializeField] private float m_SteerSensitivity = 0.05f;
        // how sensitively the AI uses steering input to turn to the desired direction
        [SerializeField] private float m_AccelSensitivity = 0.04f;
        // How sensitively the AI uses the accelerator to reach the current desired speed
        [SerializeField] private float m_BrakeSensitivity = 1f;
        // How sensitively the AI uses the brake to reach the current desired speed
        [SerializeField] private float m_LateralWanderDistance = 3f;
        // how far the car will wander laterally towards its target
        [SerializeField] private float m_LateralWanderSpeed = 0.1f;
        // how fast the lateral wandering will fluctuate
        [SerializeField] [Range (0, 1)] private float m_AccelWanderAmount = 0.1f;
        // how much the cars acceleration will wander
        [SerializeField] private float m_AccelWanderSpeed = 0.1f;
        // how fast the cars acceleration wandering will fluctuate
        [SerializeField] private BrakeCondition m_BrakeCondition = BrakeCondition.TargetDistance;
        // what should the AI consider when accelerating/braking?
        [SerializeField] private bool m_Driving;
        // whether the AI is currently actively driving or stopped.
		[SerializeField] private List<Transform> waypoints;

		public GameObject front_sensor;
		public List<GameObject> right_sensors;
		public List<GameObject> left_sensors;

		// controls the timing of lane changing
		private int lane_change_time;

        // 'target' the target object to aim for.
        [SerializeField] private bool m_StopWhenTargetReached;
        // should we stop driving when we reach the target?
        [SerializeField] private float m_ReachTargetThreshold = 2;
        // proximity to target to consider we 'reached' it, and stop driving.

		//public Transform m_Start;

		public GameObject mycar;

        private float m_RandomPerlin;
        // A random value for the car to base its wander on (so that AI cars don't all wander in the same pattern)
        private CarController m_CarController;
        // Reference to actual car controller we are controlling
        private float m_AvoidOtherCarTime;
        // time until which to avoid the car we recently collided with
        private float m_AvoidOtherCarSlowdown;
        // how much to slow down due to colliding with another car, whilst avoiding
        private float m_AvoidPathOffset;
        // direction (-1 or 1) in which to offset path to avoid other car, whilst avoiding
        private Rigidbody m_Rigidbody;

		public CarController follow_car;
		// the car that we are looping with
		public float MaxDistance;

		//keep track of the waypoint from the list
		private int current_waypoint;

		private int lane; // 0,1,2 from left to right

		private bool staged;

		//the driving direction of the car
		public bool forward;

		//use if your the main car
		private bool autodrive;
		public bool maincar;

		//frenet coordinates
		public float frenet_s = -1;
		public float frenet_d = -1;

		//check for collision on main car
		private bool main_collison = false;
		private int collision_display = 0; //the amount of time to display that a collsion happened

		//check for going over speed limit on main car
		private bool main_spdlmt = false;
		private int spdlmt_display = 0; //the amount of time to display incident

		//check if on right hand side of the road
		private bool main_lanekeep = false;
		private int lanekeep_display = 0; //the amount of time to display incident
		private int timer_lanekeep = 0; // time before action is an offense

		//track distance in miles without incident
		private float dist_eval = 0;
		private float time_eval = 0;

		private bool simulator_process;

		private int lane0_clear = 0;
		private int lane1_clear = 0;
		private int lane2_clear = 0;

        private void Awake ()
        {
			//PrintWaypoints ();

			//get max S
			/*
			var s = 0.0;
			for (int i = 0; i < waypoints.Count-1; i++) 
			{
				s += (waypoints[i+1].transform.position - waypoints[i].transform.position).magnitude;
			}
			s += (waypoints[waypoints.Count-1].transform.position - waypoints[0].transform.position).magnitude;
			Debug.Log ("Max S is " + s);
			*/

			staged = false;

			// get the car controller reference
			m_CarController = GetComponent<CarController> ();
            
			m_Rigidbody = GetComponent<Rigidbody> ();

			// give the random perlin a random value
			m_RandomPerlin = Random.value * 100;


			MaxDistance = 200;

			autodrive = false;

			//flag new data is ready to process
			simulator_process = false;

        }

		public void Spawn(List<GameObject> cars)
		{
			int direction = 1;
			if(!forward)
			{
				direction = -1;
			}
				
			int escape_cnt = 0;
			Vector3 spawn_pos = waypoints[0].position;
			int compare_start = 0;
			bool spawn_check = false;
			while(!spawn_check && escape_cnt < 500)
			{
				spawn_check = true;
				lane = Random.Range (0, 3);
			

				lane_change_time = 0;
			
				int waypoint_offset = 0;
				if (forward) 
				{
					waypoint_offset = Random.Range (-3, -1);
					m_CarController.setMaxSpeed (50 + 10 * Random.Range (0.0f, 1.0f));
					int infront_or_behind = Random.Range (0, 2);
					//int infront_or_behind = 0;
					if (infront_or_behind == 1) 
					{
						waypoint_offset = Random.Range (4, 6);
						m_CarController.setMaxSpeed (50 + 10 * Random.Range (-1.0f, 0.0f));
					}
				} 
				else 
				{
					waypoint_offset = Random.Range (3, 7);
					m_CarController.setMaxSpeed (50 + 10 * Random.Range (-1.0f, 1.0f));
				}
				Vector3 follow_car_pos = follow_car.transform.position;	
				compare_start = ListIndex(ClosestWaypoint (follow_car_pos)+waypoint_offset);
				Vector3 start_pos = waypoints[compare_start].position;

				Vector3 start_offset = waypoints [compare_start].right * 2*direction;
				if (lane == 1)
				{
					start_offset = waypoints [compare_start].right * 6*direction;
				} 
				else if(lane==2) 
				{
					start_offset = waypoints [compare_start].right * 10*direction;
				}

				spawn_pos = new Vector3 (start_pos.x + start_offset.x, start_pos.y + start_offset.y, start_pos.z + start_offset.z);

				//make sure there we wont spawn ontop of a existing car
				foreach (GameObject car in cars) 
				{
					if (Vector3.Distance (car.transform.position,spawn_pos) < 6)
					{
						spawn_check = false;
					}
				}
			
				escape_cnt++;
			}

			if (escape_cnt < 500) 
			{
				staged = false;

				m_CarController.transform.position = spawn_pos;

				m_CarController.transform.rotation = waypoints [compare_start].transform.rotation;
				if (!forward) {
					Vector3 rot = m_CarController.transform.rotation.eulerAngles;
					rot = new Vector3 (rot.x, rot.y + 180, rot.z);
					m_CarController.transform.rotation = Quaternion.Euler (rot);
				}
				m_Rigidbody.velocity = waypoints [compare_start].transform.forward * direction * m_CarController.MaxSpeed;
				current_waypoint = ListIndex (compare_start);
			}

			else
			{
				Debug.Log ("BAD SPAWN");
			}

		}

		public int getLane()
		{
			return lane;
		}

		public void PrintWaypoints()
		{
			int wp = 0;
			foreach (Transform t in waypoints) {
				
				float x_pos = t.position.x;
				float y_pos = t.position.z;

				var s = 0.0;
				for (int i = 0; i < wp; i++) 
				{
					s += (waypoints[i+1].transform.position - waypoints[i].transform.position).magnitude;
				}

				var d_x = t.right.x;
				var d_y = t.right.z;

				string row = string.Format ("{0} {1} {2} {3} {4}\n", x_pos, y_pos, s, d_x, d_y);
				string m_saveLocation = "C:/Users/aaron/Documents/udacity/sdcnd/term2/CarND-Path-Planning/data";
				File.AppendAllText (Path.Combine (m_saveLocation, "highway_map.csv"), row);
				wp++;
			}


		}

		private int ClosestWaypoint(Vector3 p) {
			//Vector3 p = car.Position ();
			//Quaternion o = m_Car.Orientation ();
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


		private int MyClosestWaypoint() {
			Vector3 p = transform.position;
			//Quaternion o = m_Car.Orientation ();
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

		// Compute the next waypoint we should go to
		private int NextWaypoint(float pos_x, float pos_y) {

			Vector3 p = new Vector3(pos_x,0,pos_y);
			
			int closestWaypoint = MyClosestWaypoint();

			Vector3 heading = waypoints[closestWaypoint].transform.position - p;
			float hx = heading.x;
			float hy = heading.z;

			//Normal vector:
			float nx =  waypoints[closestWaypoint].transform.right.x;
			float ny =  waypoints[closestWaypoint].transform.right.z;

			//Vector into the direction of the road (perpendicular to the normal vector)
			float vx = -ny;
			float vy = nx;

			//If the inner product of v and h is positive then we are behind the waypoint so we do not need to
			//increment closestWaypoint, otherwise we are beyond the waypoint and we need to increment closestWaypoint.

			float inner = hx * vx + hy * vy;
			if (inner < 0) 
			{
				return (closestWaypoint + 1) % waypoints.Count;
			} 
			else 
			{
				return closestWaypoint;
			}
				
		}

		public float NextWaypointDistance()
		{
			int nextwp = NextWaypoint(transform.position.x,transform.position.z);
			return Vector3.Distance (transform.position, waypoints [nextwp].transform.position);
		}

		public float getS()
		{
			return frenet_s;
		}

		public float getD()
		{
			return frenet_d;
		}

		public List<float> getThisFrenetFrame()
		{

			List<float> frenet_values2 = getFrenetFrame (transform.position.x, transform.position.z);

			frenet_s = frenet_values2[0];
			frenet_d = frenet_values2[1];

			return frenet_values2;

		}

		public List<float> getFrenetFrame(float pos_x, float pos_y)
		{
			
			// between 0-4 is lane 0, between 4-8 is lane 1, between 8-12 is lane 2

			int next_wp = NextWaypoint (pos_x,pos_y);

			var pos = new Vector2 (pos_x, pos_y);

			// Previous waypoint
			int prev_wp;
			prev_wp = next_wp - 1;
			if (next_wp == 0) {
				prev_wp = waypoints.Count - 1;
			}

			// This projects the vehicle position onto the line
			// from the previous waypoint to the next waypoint.
			var n_x = waypoints [next_wp].transform.position.x - waypoints [prev_wp].transform.position.x;
			var n_y = waypoints [next_wp].transform.position.z - waypoints [prev_wp].transform.position.z;
			var x_x = pos.x - waypoints [prev_wp].transform.position.x;
			var x_y = pos.y - waypoints [prev_wp].transform.position.z;
			var v = new Vector2 (n_x, n_y);

			// current vehicle position
			var x0 = new Vector2 (x_x, x_y);

			// find the projection of x onto v
			var proj = (Vector2.Dot (x0, v) / Mathf.Abs (v.x * v.x + v.y * v.y)) * v;

			var cte = (x0 - proj).magnitude;

			// This compares the projected position and the current vehicle position
			// to a point in the center of the map.
			// If projected position is closer is means the vehicle is to the right of the line
			// hence the CTE will be positive.
			// If the vehicle position is closer is means the vehicle is to the left of the line
			// hence the CTE will be negative.
			var centerPoint = new Vector3 (1000f, 50f, 2000f) - waypoints [prev_wp].transform.position;
			var centerPoint2D = new Vector2 (centerPoint.x, centerPoint.z);
			var centerToPos = Vector2.Distance (centerPoint2D, x0);
			var centerToRef = Vector2.Distance (centerPoint2D, proj);
			if (centerToPos <= centerToRef) {
				cte *= -1f;
			}

			//caculate s value
			var s = 0.0;
			for (int i = 0; i < prev_wp; i++) {
				s += (waypoints [i + 1].transform.position - waypoints [i].transform.position).magnitude;
			}
			s += proj.magnitude;

			List<float> frenet_values1 = new List<float> ();
			frenet_values1.Add ((float)s);
			frenet_values1.Add ((float)cte);

			return frenet_values1;

		}
			
		public bool RegenerateCheck()
		{
			//measure distance from follow car
			float dist = Vector3.Distance (follow_car.Position (), m_CarController.Position ());

			// if not staged to regenerate and distance is far, then flag to regenerate
			if ((dist > MaxDistance) && !staged) {
				return true;

			}

			return false;
		}

		public void setStage()
		{
			staged = true;
		}

		//check if lane is clear and safe for lane change
		private bool lane_clear(int this_lane)
		{

			CarTraffic car_traffic = follow_car.GetComponent<CarTraffic> ();

			return car_traffic.lane_clear(mycar,forward,this_lane);
			//return false;
		}
			
		//wrap index around list
		public int ListIndex(int index)
		{
			int size = waypoints.Count;
			if (index >= size)
			{
				return index%size;
			}
			else if(index < 0)
			{
				return size+index;
			}
			return index;
		}

		public bool BlinkerLight()
		{
			return (lane_change_time < 100);
		}


        public void FixedUpdate ()
        {

			if (!maincar) {

				int direction = 1;
				if (!forward) {
					direction = -1;
				}

				//Debug.Log (current_waypoint);
					
				float waypoint_dist = Vector3.Distance (waypoints [current_waypoint].position, m_CarController.Position ());
				if (waypoint_dist < (5 + (4 * lane))) {
					current_waypoint = ListIndex (current_waypoint + direction);
				}
		
				Transform m_Target = waypoints [current_waypoint];
				Vector3 reference_pos = m_Target.position;
				Vector3 offset_pos = m_Target.right * 2 * direction;
				if (lane == 1) {
					offset_pos = m_Target.right * 6 * direction;
				} else if (lane == 2) {
					offset_pos = m_Target.right * 10 * direction;
				}

				m_Target.position = new Vector3 (reference_pos.x + offset_pos.x, reference_pos.y + offset_pos.y, reference_pos.z + offset_pos.z);
				

				if (m_Target == null || !m_Driving) {
					// Car should not be moving,
					// use handbrake to stop
					m_CarController.Move (0, 0, -1f, 1f);
				} else {
					Vector3 fwd = transform.forward;
					if (m_Rigidbody.velocity.magnitude > m_CarController.MaxSpeed * 0.1f) {
						fwd = m_Rigidbody.velocity;
					}

					float desiredSpeed = m_CarController.MaxSpeed;

					RaycastHit hit;
					Physics.Raycast (front_sensor.transform.position, front_sensor.transform.forward, out hit);


					float hit_avoidance = 10;

					if (hit.rigidbody != null) 
					{
						hit_avoidance = 10*(m_CarController.MaxSpeed-hit.rigidbody.velocity.magnitude)/20;
					}


					//change from 10

					if (hit.distance < hit_avoidance) {



						if (hit.rigidbody != null) 
						{
							desiredSpeed = hit.rigidbody.velocity.magnitude;
						}

						if (m_CarController.CurrentSpeed > desiredSpeed-5) {
							
							m_CarController.Move (0, 0, -1f, 1f);

						}
				

						if (m_CarController.CurrentSpeed > 15 && (lane_change_time >= 100) && NextWaypointDistance() < 15 ) 
						{



							//try to merge right
							if (lane == 0)
							{
								if (lane_clear (lane + 1)) 
								{
									lane1_clear++;
								}

								else 
								{
									lane0_clear = 0;
									lane1_clear = 0;
									lane2_clear = 0;
								}

								if (lane1_clear>50) 
								{
									lane++;
									lane1_clear = 0;
									lane_change_time = 0;
								}
								//try to merge left
							} 
							else if (lane == 1) 
							{
								if (lane_clear (lane - 1)) 
								{
									lane0_clear++;
								}

								else 
								{
									lane0_clear = 0;
									lane1_clear = 0;
									lane2_clear = 0;
								}

								if (lane0_clear>50) 
								{
									lane--;
									lane0_clear = 0;
									lane_change_time = 0;
								} 
								else 
								{
									if (lane_clear (lane + 1)) 
									{
										lane2_clear++;
									}

									else 
									{
										lane0_clear = 0;
										lane1_clear = 0;
										lane2_clear = 0;
									}

									if (lane2_clear>50) {
										lane++;
										lane2_clear = 0;
										lane_change_time = 0;
									}
								}
							} 
							else 
							{
								if (lane_clear (lane - 1)) 
								{
									lane1_clear++;
								}

								else 
								{
									lane0_clear = 0;
									lane1_clear = 0;
									lane2_clear = 0;
								}

								if (lane1_clear>50) {
									lane--;
									lane1_clear = 0;
									lane_change_time = 0;
								}
							}
						}
					} 

					if (lane_change_time < 100) 
					{
						lane_change_time++;
					} 

                

					// now it's time to decide if we should be slowing down...
					switch (m_BrakeCondition) {
					case BrakeCondition.TargetDirectionDifference:
						{
							// the car will brake according to the upcoming change in direction of the target. Useful for route-based AI, slowing for corners.

							// check out the angle of our target compared to the current direction of the car
							float approachingCornerAngle = Vector3.Angle (m_Target.forward, fwd);

							// also consider the current amount we're turning, multiplied up and then compared in the same way as an upcoming corner angle
							float spinningAngle = m_Rigidbody.angularVelocity.magnitude * m_CautiousAngularVelocityFactor;

							// if it's different to our current angle, we need to be cautious (i.e. slow down) a certain amount
							float cautiousnessRequired = Mathf.InverseLerp (0, m_CautiousMaxAngle,
								                             Mathf.Max (spinningAngle,
									                             approachingCornerAngle));
							desiredSpeed = Mathf.Lerp (m_CarController.MaxSpeed, m_CarController.MaxSpeed * m_CautiousSpeedFactor,
								cautiousnessRequired);
							break;
						}

					case BrakeCondition.TargetDistance:
						{
							// the car will brake as it approaches its target, regardless of the target's direction. Useful if you want the car to
							// head for a stationary target and come to rest when it arrives there.

							// check out the distance to target
							Vector3 delta = m_Target.position - transform.position;
							float distanceCautiousFactor = Mathf.InverseLerp (m_CautiousMaxDistance, 0, delta.magnitude);

							// also consider the current amount we're turning, multiplied up and then compared in the same way as an upcoming corner angle
							float spinningAngle = m_Rigidbody.angularVelocity.magnitude * m_CautiousAngularVelocityFactor;

							// if it's different to our current angle, we need to be cautious (i.e. slow down) a certain amount
							float cautiousnessRequired = Mathf.Max (
								                             Mathf.InverseLerp (0, m_CautiousMaxAngle, spinningAngle), distanceCautiousFactor);
							desiredSpeed = Mathf.Lerp (m_CarController.MaxSpeed, m_CarController.MaxSpeed * m_CautiousSpeedFactor,
								cautiousnessRequired);
							break;
						}

					case BrakeCondition.NeverBrake:
						break;
					}

					// Evasive action due to collision with other cars:

					// our target position starts off as the 'real' target position
					Vector3 offsetTargetPos = m_Target.position;

					// if are we currently taking evasive action to prevent being stuck against another car:
					if (Time.time < m_AvoidOtherCarTime) {
						// slow down if necessary (if we were behind the other car when collision occured)
						desiredSpeed *= m_AvoidOtherCarSlowdown;

						// and veer towards the side of our path-to-target that is away from the other car
						offsetTargetPos += m_Target.right * m_AvoidPathOffset;
					} else {
						// no need for evasive action, we can just wander across the path-to-target in a random way,
						// which can help prevent AI from seeming too uniform and robotic in their driving
						offsetTargetPos += m_Target.right *
						(Mathf.PerlinNoise (Time.time * m_LateralWanderSpeed, m_RandomPerlin) * 2 - 1) *
						m_LateralWanderDistance;
					}

					// use different sensitivity depending on whether accelerating or braking:
					float accelBrakeSensitivity = (desiredSpeed < m_CarController.CurrentSpeed)
                                                  ? m_BrakeSensitivity
                                                  : m_AccelSensitivity;

					// decide the actual amount of accel/brake input to achieve desired speed.
					float accel = Mathf.Clamp ((desiredSpeed - m_CarController.CurrentSpeed) * accelBrakeSensitivity, -1, 1);

					// add acceleration 'wander', which also prevents AI from seeming too uniform and robotic in their driving
					// i.e. increasing the accel wander amount can introduce jostling and bumps between AI cars in a race
					accel *= (1 - m_AccelWanderAmount) +
					(Mathf.PerlinNoise (Time.time * m_AccelWanderSpeed, m_RandomPerlin) * m_AccelWanderAmount);

					// calculate the local-relative position of the target, to steer towards
					Vector3 localTarget = transform.InverseTransformPoint (offsetTargetPos);

					// work out the local angle towards the target
					float targetAngle = Mathf.Atan2 (localTarget.x, localTarget.z) * Mathf.Rad2Deg;

					// get the amount of steering needed to aim the car towards the target
					float steer = Mathf.Clamp (targetAngle * m_SteerSensitivity, -1, 1) * Mathf.Sign (m_CarController.CurrentSpeed);

					// feed input to the car controller.
					m_CarController.Move (steer, accel, accel, 0f);

					// if appropriate, stop driving when we're close enough to the target.
					if (m_StopWhenTargetReached && localTarget.magnitude < m_ReachTargetThreshold) {
						m_Driving = false;
					}
				}
				m_Target.position = reference_pos;
			}
			else if(maincar)
			{

				//Add on to the distance travled
				float get_speed = m_CarController.CurrentSpeed;
				dist_eval += (Time.deltaTime * get_speed/2.23693629f)/1609.34f;
				time_eval += Time.deltaTime;


				if(get_speed > 50.0)
				{
					main_spdlmt = true;
				}

				List<float> frenet_values = getThisFrenetFrame();
				if (frenet_d < .8 || frenet_d > 11.2) 
				{
					main_lanekeep = true;
				} 
				else if ((frenet_d > 3.2 && frenet_d < 4.8) || (frenet_d > 7.2 && frenet_d < 8.8)) 
				{
					timer_lanekeep++;
				} 
				else
				{
					timer_lanekeep = 0;
				}

				if (timer_lanekeep > 150) 
				{
					main_lanekeep = true;
				}

			}
        }

		public void SetState(float x, float y, float theta)
		{

			// work out the local angle towards the target
			float targetAngle = Mathf.Atan2 (x, y) * Mathf.Rad2Deg;

			// get the amount of steering needed to aim the car towards the target
			float steer = Mathf.Clamp (targetAngle * m_SteerSensitivity, -1, 1) * Mathf.Sign (m_CarController.CurrentSpeed);

			float desiredSpeed = 50;

			// use different sensitivity depending on whether accelerating or braking:
			float accelBrakeSensitivity = (desiredSpeed < m_CarController.CurrentSpeed)
				? m_BrakeSensitivity
				: m_AccelSensitivity;

			// decide the actual amount of accel/brake input to achieve desired speed.
			float accel = Mathf.Clamp ((desiredSpeed - m_CarController.CurrentSpeed) * accelBrakeSensitivity, -1, 1);

			// feed input to the car controller.
			m_CarController.Move (steer, accel, accel, 0f);

		}

		public void ResetDistance()
		{
			dist_eval = 0;
			time_eval = 0;
		}
		public float DistanceEval()
		{
			return dist_eval;
		}
		public int TimerEval()
		{
			return (int)time_eval;
		}

		public bool CheckCollision()
		{
			if(main_collison)
			{
				if (collision_display > 50) 
				{
					collision_display = 0;
					main_collison = false;
				}
				collision_display += 1;
				return true;
			}
			return false;
		}

		public bool CheckSpeeding()
		{
			if(main_spdlmt)
			{
				if (spdlmt_display > 50) 
				{
					spdlmt_display = 0;
					main_spdlmt = false;
				}
				spdlmt_display += 1;
				return true;
			}
			return false;
		}

		public bool CheckLanePos()
		{
			if(main_lanekeep)
			{
				if (lanekeep_display > 50) 
				{
					lanekeep_display = 0;
					main_lanekeep = false;
				}
				lanekeep_display += 1;
				return true;
			}
			return false;
		}

		


        private void OnCollisionStay (Collision col)
		{
			if (maincar) {
				main_collison = true;
			}

			// detect collision against other cars, so that we can take evasive action
			if (col.rigidbody != null) {
				var otherAI = col.rigidbody.GetComponent<CarAIControl> ();
				if (otherAI != null) {
					// we'll take evasive action for 1 second
					m_AvoidOtherCarTime = Time.time + 1;

					// but who's in front?...
					if (Vector3.Angle (transform.forward, otherAI.transform.position - transform.position) < 90) {
						// the other ai is in front, so it is only good manners that we ought to brake...
						m_AvoidOtherCarSlowdown = 0.5f;
					} else {
						// we're in front! ain't slowing down for anybody...
						m_AvoidOtherCarSlowdown = 1;
					}

					// both cars should take evasive action by driving along an offset from the path centre,
					// away from the other car
					var otherCarLocalDelta = transform.InverseTransformPoint (otherAI.transform.position);
					float otherCarAngle = Mathf.Atan2 (otherCarLocalDelta.x, otherCarLocalDelta.z);
					m_AvoidPathOffset = m_LateralWanderDistance * -Mathf.Sign (otherCarAngle);
				}
			}
			
        }


        //public void SetTarget (Transform target)
        //{
        //    m_Target = target;
        //    m_Driving = true;
        //}
    }

}

