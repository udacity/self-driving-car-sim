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

        public float maxz = 0;
        public float maxx = 0;

        private void Awake()
        {
            // get the car controller
            m_Car = GetComponent<CarController>();
            s = new Steering();
            s.Start();
		}

        private void FixedUpdate()
        {
            // For finding the radius of curvature
            // var x = m_Car.Position().x;
            // var z = m_Car.Position().z;
            // if (Mathf.Abs(x) > maxx) {
            //     maxx = x;
            // }
            // if (Mathf.Abs(z) > maxz) {
            //     maxz = z;
            // }
            // Debug.Log(string.Format("Maxiumum X = {0}, Z = {1}", maxx, maxz));

            // If holding down W or S control the car manually
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S))
            {
                s.UpdateValues();
                m_Car.Move(s.H, s.V, s.V, 0f);
            } else
            {
				m_Car.Move(SteeringAngle, Acceleration, Acceleration, 0f);
				// m_Car.Move(5f / 25f, 1f, 1f, 0f);
            }
        }
    }
}
