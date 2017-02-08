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
        [SerializeField] private WheelEffects[] m_WheelEffects = new WheelEffects[4];
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

        public const string CSVFileName = "driving_log.csv";
        public const string DirFrames = "IMG";

        [SerializeField] private Camera CenterCamera;
        [SerializeField] private Camera LeftCamera;
        [SerializeField] private Camera RightCamera;

        private Quaternion[] m_WheelMeshLocalRotations;
        private Vector3 m_Prevpos, m_Pos;
        private float m_SteerAngle;
        private int m_GearNum;
        private float m_GearFactor;
        private float m_OldRotation;
        private float m_CurrentTorque;
        private Rigidbody m_Rigidbody;
        private const float k_ReversingThreshold = 0.01f;
        private string m_saveLocation = "";
        private Queue<CarSample> carSamples;
		private int TotalSamples;
		private bool isSaving;
		private Vector3 saved_position;
		private Quaternion saved_rotation;

        public bool Skidding { get; private set; }

        public float BrakeInput { get; private set; }

        private bool m_isRecording = false;
        public bool IsRecording {
            get
            {
                return m_isRecording;
            }

            set
            {
                m_isRecording = value;
                if(value == true)
                { 
					Debug.Log("Starting to record");
					carSamples = new Queue<CarSample>();
					StartCoroutine(Sample());             
                } 
				else
                {
                    Debug.Log("Stopping record");
                    StopCoroutine(Sample());
                    Debug.Log("Writing to disk");
					//save the cars coordinate parameters so we can reset it to this properly after capturing data
					saved_position = transform.position;
					saved_rotation = transform.rotation;
					//see how many samples we captured use this to show save percentage in UISystem script
					TotalSamples = carSamples.Count;
					isSaving = true;
					StartCoroutine(WriteSamplesToDisk());

                };
            }

        }


		public bool checkSaveLocation()
		{
			if (m_saveLocation != "") 
			{
				return true;
			}
			else
			{
				SimpleFileBrowser.ShowSaveDialog (OpenFolder, null, true, null, "Select Output Folder", "Select");
			}
			return false;
		}

        public float CurrentSteerAngle {
            get { return m_SteerAngle; }
            set { m_SteerAngle = value; }
        }

        public float CurrentSpeed{ get { return m_Rigidbody.velocity.magnitude * 2.23693629f; } }

        public float MaxSpeed{ get { return m_Topspeed; } }

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

        public void Update()
        {
            if (IsRecording)
            {
                //Dump();
            }
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
                if (CurrentSpeed > 5 && Vector3.Angle (transform.forward, m_Rigidbody.velocity) < 50f) {
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
                if (Mathf.Abs (wheelHit.forwardSlip) >= m_SlipLimit || Mathf.Abs (wheelHit.sidewaysSlip) >= m_SlipLimit) {
                    m_WheelEffects [i].EmitTyreSmoke ();
                    continue;
                }

                // if it wasnt slipping stop all the audio
                if (m_WheelEffects [i].PlayingAudio) {
                    m_WheelEffects [i].StopAudio ();
                }
                // end the trail generation
                m_WheelEffects [i].EndSkidTrail ();
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


		//Changed the WriteSamplesToDisk to a IEnumerator method that plays back recording along with percent status from UISystem script 
		//instead of showing frozen screen until all data is recorded
		public IEnumerator WriteSamplesToDisk()
		{
			yield return new WaitForSeconds(0.000f); //retrieve as fast as we can but still allow communication of main thread to screen and UISystem
			if (carSamples.Count > 0) {
				//pull off a sample from the que
				CarSample sample = carSamples.Dequeue();

				//pysically moving the car to get the right camera position
				transform.position = sample.position;
				transform.rotation = sample.rotation;

				// Capture and Persist Image
				string centerPath = WriteImage (CenterCamera, "center", sample.timeStamp);
				string leftPath = WriteImage (LeftCamera, "left", sample.timeStamp);
				string rightPath = WriteImage (RightCamera, "right", sample.timeStamp);

				string row = string.Format ("{0},{1},{2},{3},{4},{5},{6}\n", centerPath, leftPath, rightPath, sample.steeringAngle, sample.throttle, sample.brake, sample.speed);
				File.AppendAllText (Path.Combine (m_saveLocation, CSVFileName), row);
			}
			if (carSamples.Count > 0) {
				//request if there are more samples to pull
				StartCoroutine(WriteSamplesToDisk()); 
			}
			else 
			{
				//all samples have been pulled
				StopCoroutine(WriteSamplesToDisk());
				isSaving = false;

				//need to reset the car back to its position before ending recording, otherwise sometimes the car ended up in strange areas
				transform.position = saved_position;
				transform.rotation = saved_rotation;
				m_Rigidbody.velocity = new Vector3(0f,-10f,0f);
				Move(0f, 0f, 0f, 0f);

			}
		}

		public float getSavePercent()
		{
			return (float)(TotalSamples-carSamples.Count)/TotalSamples;
		}

		public bool getSaveStatus()
		{
			return isSaving;
		}


        public IEnumerator Sample()
        {
            // Start the Coroutine to Capture Data Every Second.
            // Persist that Information to a CSV and Perist the Camera Frame
            yield return new WaitForSeconds(0.0666666666666667f);

            if (m_saveLocation != "")
            {
                CarSample sample = new CarSample();

                sample.timeStamp = System.DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss_fff");
                sample.steeringAngle = m_SteerAngle / m_MaximumSteerAngle;
                sample.throttle = AccelInput;
                sample.brake = BrakeInput;
                sample.speed = CurrentSpeed;
                sample.position = transform.position;
                sample.rotation = transform.rotation;

                carSamples.Enqueue(sample);

                sample = null;
                //may or may not be needed
            }

            // Only reschedule if the button hasn't toggled
            if (IsRecording)
            {
                StartCoroutine(Sample());
            }
				
        }

        private void OpenFolder(string location)
        {
            m_saveLocation = location;
            Directory.CreateDirectory (Path.Combine(m_saveLocation, DirFrames));
        }

        private string WriteImage (Camera camera, string prepend, string timestamp)
        {
            //needed to force camera update 
            camera.Render();
            RenderTexture targetTexture = camera.targetTexture;
            RenderTexture.active = targetTexture;
            Texture2D texture2D = new Texture2D (targetTexture.width, targetTexture.height, TextureFormat.RGB24, false);
            texture2D.ReadPixels (new Rect (0, 0, targetTexture.width, targetTexture.height), 0, 0);
            texture2D.Apply ();
            byte[] image = texture2D.EncodeToJPG ();
            UnityEngine.Object.DestroyImmediate (texture2D);
            string directory = Path.Combine(m_saveLocation, DirFrames);
            string path = Path.Combine(directory, prepend + "_" + timestamp + ".jpg");
            File.WriteAllBytes (path, image);
            image = null;
            return path;
        }
    }

    internal class CarSample
    {
        public Quaternion rotation;
        public Vector3 position;
        public float steeringAngle;
        public float throttle;
        public float brake;
        public float speed;
        public string timeStamp;
    }

}
