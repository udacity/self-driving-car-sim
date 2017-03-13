using UnityEngine;

namespace UnityStandardAssets.Vehicles.Car
{
    [RequireComponent(typeof(CarController))]
    public class CarRemoteControl : MonoBehaviour
    {
        private CarController m_Car; // the car controller we want to use

        public float SteeringAngle { get; set; }
        public float Acceleration { get; set; }
        private Steering s;
        // private WaypointTracker wpt;

        private void Awake()
        {
            // get the car controller
            m_Car = GetComponent<CarController>();
            s = new Steering();
            s.Start();
            // wpt = new WaypointTracker();
		}

        private void FixedUpdate()
        {
            // wpt.SensorData(m_Car);
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
