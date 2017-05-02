using System;
using UnityEngine.UI;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

using System.Runtime.InteropServices;

public class hunter : MonoBehaviour {

	public float heading;
	public float x;
	public float y;
	private double delta_t_s; // delta t in seconds

	private float velocity;
	private float yawrate;

	// Use this for initialization
	void Start () {

		delta_t_s = Time.deltaTime;
		velocity = 0;
		yawrate = 0;

	}
	void FixedUpdate()
	{
		Move (yawrate, velocity);
	}
	public void setVel(float vel)
	{
		velocity = vel;
	}
	public void setYawRate(float yr)
	{
		yawrate = yr;
	}
	public bool isRunning()
	{
		return (velocity > 0);
	}

	public void Restart()
	{
		transform.position = new Vector3 (-10.0f, 0.0f, 0.0f);
		transform.rotation = Quaternion.AngleAxis (0, Vector3.forward);
		heading = 0;

		x = transform.position.x;
		y = transform.position.y;

		velocity = 0;
		yawrate = 0;

	}
		
	// turning in radians
	public void Move(float turning, float distance)
	{

		//set distance to its min and max limits
		if (distance > 5.0*delta_t_s) 
		{
			distance = (float)(5.0*delta_t_s);
		} else if (distance < 0) 
		{
			distance = 0f;
		}
			
		transform.Rotate(0, 0, (float)(turning*(180/Math.PI)));
		heading += (float)(turning);

		//Vector3 movement = new Vector3 (speed/60 * Mathf.Cos (angle), speed/60 * Mathf.Sin (angle), 0);
		Vector3 movement = transform.right*((float)(distance));
		transform.position = transform.position + movement;

		x = transform.position.x;
		y = transform.position.y;
	}

}
