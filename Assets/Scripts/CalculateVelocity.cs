using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class CalculateVelocity : MonoBehaviour {

	internal enum SpeedType {MPH, KPH};
	private Rigidbody m_Rigidbody; 
	public float outputSpeed;
	UISystem uiSystem;
	[SerializeField] private SpeedType m_SpeedType;
	[SerializeField] private float m_Topspeed = 200;



	void Start ()
	{
//		StartCoroutine( CalcVelocity() );
		m_Rigidbody = GetComponent<Rigidbody>();
		uiSystem = FindObjectOfType<UISystem> ();
		Debug.Log (gameObject.name);
	}

	IEnumerator CalcVelocity()
	{
		while( Application.isPlaying )
		{
			// Position at frame start
			var prevPos = transform.position;
			// Wait till it the end of the frame
			yield return new WaitForEndOfFrame();
			// Calculate velocity: Velocity = DeltaPosition / DeltaTime
			var currVel = (prevPos - transform.position) / Time.deltaTime;
			CapSpeed (currVel.magnitude);
//			Debug.Log( "Cuttent Speed Coroutine :"+outputSpeed);
		}
	}

	void Update(){
		CapSpeed (m_Rigidbody.velocity.magnitude);
//		Debug.Log( "Cuttent Speed Update (With RB) :"+outputSpeed );
	} 

	void FixedUpdate(){
//		CapSpeed (m_Rigidbody.velocity.magnitude);
//		Debug.Log( "Cuttent Speed FixedUpdate (With RB) :"+outputSpeed );
	}

	private void CapSpeed (float _magnitude )
	{
		float speed = _magnitude;
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
		outputSpeed = speed;
		uiSystem.SetMPHValue (speed);
	}

}
