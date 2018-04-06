using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerScript : MonoBehaviour
{
	
	private Rigidbody2D rigidbodyComponent;

	void Start()
	{
		rigidbodyComponent = GetComponent<Rigidbody2D>();
	}
		
	public void MoveCar(float vel_x, float vel_y)
	{
		Vector2 movement = new Vector2(vel_x, vel_y);
		rigidbodyComponent.velocity = movement;
	}

}