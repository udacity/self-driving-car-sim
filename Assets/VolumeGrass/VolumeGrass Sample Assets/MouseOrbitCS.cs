using UnityEngine;
using System.Collections;

public class MouseOrbitCS : MonoBehaviour {
/*	public Transform target;

	public float distance = 10.0f;

	public float xSpeed = 250.0f;
	public float ySpeed = 120.0f;

	public float yMinLimit = -20;
	public float yMaxLimit = 80;

	private float x = 0.0f;
	private float y = 0.0f;

	private float normal_angle=0.0f;

	void Start () {
	    Vector3 angles = transform.eulerAngles;
	    x = angles.y;
	    y = angles.x;
	
	   	if (GetComponent<Rigidbody>())
			GetComponent<Rigidbody>().freezeRotation = true;
	}
	
	void LateUpdate () {
		if (GameManager.instance.replayState != ReplayState.PlayBack) {
			if ((target) && (!Input.GetKey (KeyCode.Space))) {
				x += Input.GetAxis ("Mouse X") * xSpeed * 0.02f;
				y -= Input.GetAxis ("Mouse Y") * ySpeed * 0.02f;
	 		
				y = ClampAngle (y, yMinLimit + normal_angle, yMaxLimit + normal_angle);
	 		       
				Quaternion rotation = Quaternion.Euler (y, x, 0);
				Vector3 position = rotation * new Vector3 (0.0f, 0.0f, -distance) + target.position;
	        
				transform.rotation = rotation;
				transform.position = position;
			}
		}
	}
	
	static float ClampAngle (float angle, float min, float max) {
		if (angle < -360)
			angle += 360;
		if (angle > 360)
			angle -= 360;
		return Mathf.Clamp (angle, min, max);
	}
	
	public void set_normal_angle(float a) {
		normal_angle=a;
	}
*/
}