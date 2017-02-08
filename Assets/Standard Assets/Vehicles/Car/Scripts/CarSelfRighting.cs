using System;
using UnityEngine;

namespace UnityStandardAssets.Vehicles.Car
{
    public class CarSelfRighting : MonoBehaviour
    {
        // Automatically put the car the right way up, if it has come to rest upside-down.
        [SerializeField] private float m_WaitTime = 3f;           // time to wait before self righting
        [SerializeField] private float m_VelocityThreshold = 1f;  // the velocity below which the car is considered stationary for self-righting

        private float m_LastOkTime; // the last time that the car was in an OK state
        private Rigidbody m_Rigidbody;


        private void Start()
        {
            m_Rigidbody = GetComponent<Rigidbody>();
        }


        private void Update()
        {
            // is the car is the right way up
            if (transform.up.y > 0f || m_Rigidbody.velocity.magnitude > m_VelocityThreshold)
            {
                m_LastOkTime = Time.time;
            }

            if (Time.time > m_LastOkTime + m_WaitTime)
            {
                RightCar();
            }
        }


        // put the car back the right way up:
        private void RightCar()
        {
            // set the correct orientation for the car, and lift it off the ground a little
            transform.position += Vector3.up;
            transform.rotation = Quaternion.LookRotation(transform.forward);
        }
    }
}
