using System;
using UnityEngine;

namespace UnityStandardAssets.Vehicles.Car
{
    // this script is specific to the car supplied in the the assets
    // it controls the suspension hub to make it move with the wheel are it goes over bumps
    public class Suspension : MonoBehaviour
    {
        public GameObject wheel; // The wheel that the script needs to referencing to get the postion for the suspension


        private Vector3 m_TargetOriginalPosition;
        private Vector3 m_Origin;


        private void Start()
        {
            m_TargetOriginalPosition = wheel.transform.localPosition;
            m_Origin = transform.localPosition;
        }


        private void Update()
        {
            transform.localPosition = m_Origin + (wheel.transform.localPosition - m_TargetOriginalPosition);
        }
    }
}
