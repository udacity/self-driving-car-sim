using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;



namespace UnityStandardAssets.Vehicles.Car
{
    internal enum CarDriveType
    {
        FrontWheelDrive,
        RearWheelDrive,
        FourWheelDrive
    }

    internal enum SpeedType
    {
        MPH,
        KPH
    }

    public class CarController : MonoBehaviour
    {
        [SerializeField] private CarDriveType m_CarDriveType = CarDriveType.FourWheelDrive;
        [SerializeField] private WheelCollider[] m_WheelColliders = new WheelCollider[4];
        [SerializeField] private GameObject[] m_WheelMeshes = new GameObject[4];
        //[SerializeField] private WheelEffects[] m_WheelEffects = new WheelEffects[4];
        [SerializeField] private Vector3 m_CentreOfMassOffset;
        [SerializeField] private float m_MaximumSteerAngle;
        [Range (0, 1)] [SerializeField] private float m_SteerHelper;
        // 0 is raw physics , 1 the car will grip in the direction it is facing
        [Range (0, 1)] [SerializeField] private float m_TractionControl;
        // 0 is no traction control, 1 is full interference
        [SerializeField] private float m_FullTorqueOverAllWheels;
        [SerializeField] private float m_ReverseTorque;
        [SerializeField] private float m_MaxHandbrakeTorque;
        [SerializeField] private float m_Downforce = 100f;
        [SerializeField] private SpeedType m_SpeedType;
        [SerializeField] private float m_Topspeed = 200;
        [SerializeField] private static int NoOfGears = 5;
        [SerializeField] private float m_RevRangeBoundary = 1f;
        [SerializeField] private float m_SlipLimit;
        [SerializeField] private float m_BrakeTorque;


        private Quaternion[] m_WheelMeshLocalRotations;
        private Vector3 m_Prevpos, m_Pos;
        private float m_SteerAngle;
        private int m_GearNum;
        private float m_GearFactor;
        private float m_OldRotation;
        private float m_CurrentTorque;
        private Rigidbody m_Rigidbody;
        private const float k_ReversingThreshold = 0.01f;


        public bool Skidding { get; private set; }

        public float BrakeInput { get; private set; }

		[SerializeField] private List<GameObject> sensors;
		private List<float> sensor_values = new List<float> ();

		public bool sensor_visable;

		public bool main_car;

		// sense acceleration
		private float AccelerationT;
		private float AccelerationN;
		private float Jerk;
		private float lastSpeed;
		private float lastAcc;

		//used to calculate path curvatures
		private List<Vector3> previous_pos = new List<Vector3>();

		private List<float> averageSpeed = new List<float>(); //average speed from previous frames
		private List<float> averageAcc = new List<float>(); //average acceleration from previous frames

		public Vector3 Position () {
			return transform.position;
		}
		public List<int> getGPS(){
			List<int> gps = new List<int> ();
			gps.Add ((int)transform.position.x);
			gps.Add ((int)transform.position.z);
			return gps;
		}

		public Quaternion Orientation () {
			return transform.rotation;
		}

		//toggle sensors visable on/off
		public void ToggleSensorView()
		{
			sensor_visable = !sensor_visable;
			if (sensor_visable) {
				foreach (GameObject sensor in sensors) {
					sensor.GetComponent<LineRenderer>().enabled = true;
				}
			} 
			else {
				foreach (GameObject sensor in sensors) {
					sensor.GetComponent<LineRenderer>().enabled = false;
				}
			}
		}

		public void SenseDistance()
		{
			if (sensors.Count != 0) {
				List<float> distances = new List<float> ();
				int i = 0;
				foreach (GameObject sensor in sensors) {
					RaycastHit hit;
					Physics.Raycast (sensor.transform.position, sensor.transform.forward, out hit);
					LineRenderer lineRenderer = sensor.GetComponentInParent<LineRenderer> ();
					if (sensor_visable) {
						if (hit.collider) {
							lineRenderer.SetPosition (1, new Vector3 (0, 0, 10 * hit.distance));
						} else {
							lineRenderer.SetPosition (1, new Vector3 (0, 0, 1000));
						}
					}
					distances.Add (hit.distance);
					i += 1;
				}
				sensor_values = distances;
			}
		}
		public float SenseAccT()
		{
			return AccelerationT;
		}
		public float SenseAccN()
		{
			return AccelerationN;
		}
		public float SenseAcc()
		{
			return Mathf.Sqrt (AccelerationT * AccelerationT + AccelerationN * AccelerationN);
		}
		public float SenseJerk()
		{
			return Jerk;
		}

		public List<float> getSensors()
		{
			return sensor_values;
		}
			

        public float CurrentSteerAngle {
            get { return m_SteerAngle; }
            set { m_SteerAngle = value; }
        }

        public float CurrentSpeed{ get { return m_Rigidbody.velocity.magnitude * 2.23693629f; } }

        public float MaxSpeed{ get { return m_Topspeed; } }

		public void setMaxSpeed(float Topspeed) 
		{
			m_Topspeed = Topspeed;
		}

        public float Revs { get; private set; }

        public float AccelInput { get; set; }

        // Use this for initialization
        private void Start ()
        {
            m_WheelMeshLocalRotations = new Quaternion[4];
            for (int i = 0; i < 4; i++) {
                m_WheelMeshLocalRotations [i] = m_WheelMeshes [i].transform.localRotation;
            }
            m_WheelColliders [0].attachedRigidbody.centerOfMass = m_CentreOfMassOffset;

            m_MaxHandbrakeTorque = float.MaxValue;

            m_Rigidbody = GetComponent<Rigidbody> ();
            m_CurrentTorque = m_FullTorqueOverAllWheels - (m_TractionControl * m_FullTorqueOverAllWheels);

			lastSpeed = 0;
			lastAcc = 0;
			Jerk = 0;
			AccelerationT = 0;
			AccelerationN = 0;
        }

        private void GearChanging ()
        {
            float f = Mathf.Abs (CurrentSpeed / MaxSpeed);
            float upgearlimit = (1 / (float)NoOfGears) * (m_GearNum + 1);
            float downgearlimit = (1 / (float)NoOfGears) * m_GearNum;

            if (m_GearNum > 0 && f < downgearlimit) {
                m_GearNum--;
            }

            if (f > upgearlimit && (m_GearNum < (NoOfGears - 1))) {
                m_GearNum++;
            }
        }


        // simple function to add a curved bias towards 1 for a value in the 0-1 range
        private static float CurveFactor (float factor)
        {
            return 1 - (1 - factor) * (1 - factor);
        }


        // unclamped version of Lerp, to allow value to exceed the from-to range
        private static float ULerp (float from, float to, float value)
        {
            return (1.0f - value) * from + value * to;
        }


        private void CalculateGearFactor ()
        {
            float f = (1 / (float)NoOfGears);
            // gear factor is a normalised representation of the current speed within the current gear's range of speeds.
            // We smooth towards the 'target' gear factor, so that revs don't instantly snap up or down when changing gear.
            var targetGearFactor = Mathf.InverseLerp (f * m_GearNum, f * (m_GearNum + 1), Mathf.Abs (CurrentSpeed / MaxSpeed));
            m_GearFactor = Mathf.Lerp (m_GearFactor, targetGearFactor, Time.deltaTime * 5f);
        }


        private void CalculateRevs ()
        {
            // calculate engine revs (for display / sound)
            // (this is done in retrospect - revs are not used in force/power calculations)
            CalculateGearFactor ();
            var gearNumFactor = m_GearNum / (float)NoOfGears;
            var revsRangeMin = ULerp (0f, m_RevRangeBoundary, CurveFactor (gearNumFactor));
            var revsRangeMax = ULerp (m_RevRangeBoundary, 1f, gearNumFactor);
            Revs = ULerp (revsRangeMin, revsRangeMax, m_GearFactor);
        }

        public void FixedUpdate()
        {
			if (main_car) 
			{
				//average over last frames
				int time_steps = 10;

				float speed = m_Rigidbody.velocity.magnitude;

				if (averageSpeed.Count >= time_steps) {
				
					float averaged_speed = AverageLastSpeed ();
					AccelerationT = (averaged_speed - lastSpeed) / (time_steps*Time.deltaTime);
					AccelerationN = (averaged_speed) * (averaged_speed) * SenseCurve ();
					lastSpeed = averaged_speed;

					float currentAcc = SenseAcc ();
					averageAcc.Add (currentAcc);

					averageSpeed.Clear ();
					previous_pos.Clear ();
				}
				averageSpeed.Add (speed);
				previous_pos.Add (transform.position);

				if (averageAcc.Count >= 5) 
				{
					float averaged_acc = AverageLastAcc();
					Jerk = (averaged_acc - lastAcc) / ((5*time_steps) * Time.deltaTime);
					lastAcc = averaged_acc;

					averageAcc.Clear ();
				}
					

			}
        }

		public float AverageLastSpeed()
		{
			
			float averaged_speed = 0.0f;

			for (int i = 0; i < averageSpeed.Count; i++) 
			{
				averaged_speed += averageSpeed[i];
			}

			return averaged_speed / (float)(averageSpeed.Count);

		}
		public float AverageLastAcc()
		{

			float averaged_acc = 0.0f;

			for (int i = 0; i < averageAcc.Count; i++) 
			{
				averaged_acc += averageAcc[i];
			}

			return averaged_acc / (float)(averageAcc.Count);

		}
		public float SenseCurve()
		{
			float averaged_curve = 0.0f;

			for (int i = 0; i < previous_pos.Count-2; i++) 
			{
				

				float x1 = previous_pos [i].x;
				float x2 = previous_pos [i + 1].x;
				float x3 = previous_pos [i + 2].x;

				float y1 = previous_pos [i].z;
				float y2 = previous_pos [i + 1].z;
				float y3 = previous_pos [i + 2].z;

				Vector2 ray1 = new Vector2 (x2 - x1, y2 - y1);
				Vector2 ray2 = new Vector2 (x3 - x2, y3 - y2);

				if (ray1.magnitude != 0 && ray2.magnitude != 0) 
				{

					Vector2 ray3 = new Vector2 (x3 - x1, y3 - y1);

					float corner_angle = Mathf.Abs (Vector2.Angle (ray1, ray2));

					if (ray3.magnitude != 0 && corner_angle != 180) 
					{
						averaged_curve += 2 * Mathf.Sin (corner_angle*Mathf.Deg2Rad) / ray3.magnitude;
					}
					else
					{
						
						//the curve is infinite, this move is totally illegal, just going to return 1000000
						averaged_curve += 1000000;
					}

				}
				//else skip and just say that curve is zero since its stopped
			}

			return averaged_curve / ( (float)(previous_pos.Count-2));

		}

        public void Move (float steering, float accel, float footbrake, float handbrake)
        {
            for (int i = 0; i < 4; i++) {
                Quaternion quat;
                Vector3 position;
                m_WheelColliders [i].GetWorldPose (out position, out quat);
                m_WheelMeshes [i].transform.position = position;
                m_WheelMeshes [i].transform.rotation = quat;
            }

            //clamp input values
            steering = Mathf.Clamp (steering, -1, 1);
            AccelInput = accel = Mathf.Clamp (accel, 0, 1);
            BrakeInput = footbrake = -1 * Mathf.Clamp (footbrake, -1, 0);
            handbrake = Mathf.Clamp (handbrake, 0, 1);


            //Set the steer on the front wheels.
            //Assuming that wheels 0 and 1 are the front wheels.
            m_SteerAngle = steering * m_MaximumSteerAngle;
            m_WheelColliders [0].steerAngle = m_SteerAngle;
            m_WheelColliders [1].steerAngle = m_SteerAngle;



            SteerHelper ();
            ApplyDrive (accel, footbrake);
            CapSpeed ();

            //Set the handbrake.
            //Assuming that wheels 2 and 3 are the rear wheels.
            if (handbrake > 0f)
            {
                var hbTorque = handbrake * m_MaxHandbrakeTorque;
                m_WheelColliders [2].brakeTorque = hbTorque;
                m_WheelColliders [3].brakeTorque = hbTorque;
            }

            CalculateRevs ();
            GearChanging ();

            AddDownForce ();
            CheckForWheelSpin ();
            TractionControl ();
        }
       

        private void CapSpeed ()
        {
            float speed = m_Rigidbody.velocity.magnitude;
            switch (m_SpeedType) {
            case SpeedType.MPH:

                speed *= 2.23693629f;
                if (speed > m_Topspeed)
                    m_Rigidbody.velocity = (m_Topspeed / 2.23693629f) * m_Rigidbody.velocity.normalized;
                break;

            case SpeedType.KPH:
                speed *= 3.6f;
                if (speed > m_Topspeed)
                    m_Rigidbody.velocity = (m_Topspeed / 3.6f) * m_Rigidbody.velocity.normalized;
                break;
            }
        }


        private void ApplyDrive (float accel, float footbrake)
        {

			for (int i = 0; i < 4; i++) 
			{
				m_WheelColliders [i].motorTorque = 0f;
				m_WheelColliders [i].brakeTorque = 0f;
			}

            float thrustTorque;
            switch (m_CarDriveType) {
            case CarDriveType.FourWheelDrive:
                thrustTorque = accel * (m_CurrentTorque / 4f);
                for (int i = 0; i < 4; i++) {
                    m_WheelColliders [i].motorTorque = thrustTorque;
                }
                break;

            case CarDriveType.FrontWheelDrive:
                thrustTorque = accel * (m_CurrentTorque / 2f);
                m_WheelColliders [0].motorTorque = m_WheelColliders [1].motorTorque = thrustTorque;
                break;

            case CarDriveType.RearWheelDrive:
                thrustTorque = accel * (m_CurrentTorque / 2f);
                m_WheelColliders [2].motorTorque = m_WheelColliders [3].motorTorque = thrustTorque;
                break;

            }

            for (int i = 0; i < 4; i++) {
                if (CurrentSpeed > 0 && Vector3.Angle (transform.forward, m_Rigidbody.velocity) < 50f) {
                    m_WheelColliders [i].brakeTorque = m_BrakeTorque * footbrake;
                } else if (footbrake > 0) {
                    m_WheelColliders [i].brakeTorque = 0f;
                    m_WheelColliders [i].motorTorque = -m_ReverseTorque * footbrake;
                }
            }
        }


        private void SteerHelper ()
        {
            for (int i = 0; i < 4; i++) {
                WheelHit wheelhit;
                m_WheelColliders [i].GetGroundHit (out wheelhit);
                if (wheelhit.normal == Vector3.zero)
                    return; // wheels arent on the ground so dont realign the rigidbody velocity
            }

            // this if is needed to avoid gimbal lock problems that will make the car suddenly shift direction
            if (Mathf.Abs (m_OldRotation - transform.eulerAngles.y) < 10f) {
                var turnadjust = (transform.eulerAngles.y - m_OldRotation) * m_SteerHelper;
                Quaternion velRotation = Quaternion.AngleAxis (turnadjust, Vector3.up);
                m_Rigidbody.velocity = velRotation * m_Rigidbody.velocity;
            }
            m_OldRotation = transform.eulerAngles.y;
        }


        // this is used to add more grip in relation to speed
        private void AddDownForce ()
        {
            m_WheelColliders [0].attachedRigidbody.AddForce (-transform.up * m_Downforce *
            m_WheelColliders [0].attachedRigidbody.velocity.magnitude);
        }


        // checks if the wheels are spinning and is so does three things
        // 1) emits particles
        // 2) plays tiure skidding sounds
        // 3) leaves skidmarks on the ground
        // these effects are controlled through the WheelEffects class
        private void CheckForWheelSpin ()
        {
            // loop through all wheels
            for (int i = 0; i < 4; i++) {
                WheelHit wheelHit;
                m_WheelColliders [i].GetGroundHit (out wheelHit);

                // is the tire slipping above the given threshhold
                //if (Mathf.Abs (wheelHit.forwardSlip) >= m_SlipLimit || Mathf.Abs (wheelHit.sidewaysSlip) >= m_SlipLimit) {
                //    m_WheelEffects [i].EmitTyreSmoke ();
                //    continue;
                //}

                // if it wasnt slipping stop all the audio
                //if (m_WheelEffects [i].PlayingAudio) {
                //    m_WheelEffects [i].StopAudio ();
                //}
                // end the trail generation
                //m_WheelEffects [i].EndSkidTrail ();
            }
        }

        // crude traction control that reduces the power to wheel if the car is wheel spinning too much
        private void TractionControl ()
        {
            WheelHit wheelHit;
            switch (m_CarDriveType) {
            case CarDriveType.FourWheelDrive:
                    // loop through all wheels
                for (int i = 0; i < 4; i++) {
                    m_WheelColliders [i].GetGroundHit (out wheelHit);

                    AdjustTorque (wheelHit.forwardSlip);
                }
                break;

            case CarDriveType.RearWheelDrive:
                m_WheelColliders [2].GetGroundHit (out wheelHit);
                AdjustTorque (wheelHit.forwardSlip);

                m_WheelColliders [3].GetGroundHit (out wheelHit);
                AdjustTorque (wheelHit.forwardSlip);
                break;

            case CarDriveType.FrontWheelDrive:
                m_WheelColliders [0].GetGroundHit (out wheelHit);
                AdjustTorque (wheelHit.forwardSlip);

                m_WheelColliders [1].GetGroundHit (out wheelHit);
                AdjustTorque (wheelHit.forwardSlip);
                break;
            }
        }


        private void AdjustTorque (float forwardSlip)
        {
            if (forwardSlip >= m_SlipLimit && m_CurrentTorque >= 0) {
                m_CurrentTorque -= 10 * m_TractionControl;
            } else {
                m_CurrentTorque += 10 * m_TractionControl;
                if (m_CurrentTorque > m_FullTorqueOverAllWheels) {
                    m_CurrentTorque = m_FullTorqueOverAllWheels;
                }
            }
        }
			
    }
}
