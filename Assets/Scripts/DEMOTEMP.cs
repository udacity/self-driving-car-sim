using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DEMOTEMP : MonoBehaviour {
/*
	[SerializeField]
	bool isButton ;
	[SerializeField]
	bool isLeftJoyStick ;
	[SerializeField]
	string buttonName;
	private Vector3 startPos;
	private Transform thisTransform;
	private MeshRenderer mr;

//	 Use this for initialization
	void Start () {
		thisTransform = transform;
		startPos = thisTransform.position;
		mr = thisTransform.GetComponent<MeshRenderer> ();
	}
	
	// Update is called once per frame
	void Update () {
		if (isButton) {
			mr.enabled = Input.GetButton (buttonName);
		} else {
			if (isLeftJoyStick) {
				Vector3 inputDirection = Vector3.zero;
				inputDirection.x = Input.GetAxis ("LeftJoystickHorizontal");
				inputDirection.z = Input.GetAxis ("LeftJoystickVertical");
				thisTransform.position = startPos + inputDirection;
			} else {
				Vector3 inputDirection = Vector3.zero;
				inputDirection.x = Input.GetAxis ("RightJoystickHorizontal");
				inputDirection.z = Input.GetAxis ("RightJoystickVertical");
				thisTransform.position = startPos + inputDirection;
			}
		}
	}

*/

void Update ()    {
		if (Input.GetKeyDown(KeyCode.JoystickButton0)) {
			Debug.Log("JoystickButton0");            
		}
		else if (Input.GetKeyDown(KeyCode.JoystickButton1)) {
			Debug.Log("JoystickButton1");            
		}
		else if (Input.GetKeyDown(KeyCode.JoystickButton2)) {
			Debug.Log("JoystickButton2");            
		}else if (Input.GetKeyDown(KeyCode.JoystickButton3)) {
			Debug.Log("JoystickButton3");            
		}else if (Input.GetKeyDown(KeyCode.JoystickButton4)) {
			Debug.Log("JoystickButton4");            
		}else if (Input.GetKeyDown(KeyCode.JoystickButton5)) {
			Debug.Log("JoystickButton5");            
		}else if (Input.GetKeyDown(KeyCode.JoystickButton6)) {
			Debug.Log("JoystickButton6");            
		}else if (Input.GetKeyDown(KeyCode.JoystickButton7)) {
			Debug.Log("JoystickButton7");            
		}else if (Input.GetKeyDown(KeyCode.JoystickButton8)) {
			Debug.Log("JoystickButton8");            
		}else if (Input.GetKeyDown(KeyCode.JoystickButton9)) {
			Debug.Log("JoystickButton9");            
		}else if (Input.GetKeyDown(KeyCode.JoystickButton10)) {
			Debug.Log("JoystickButton10");            
		}else if (Input.GetKeyDown(KeyCode.JoystickButton11)) {
			Debug.Log("JoystickButton11");            
		}
	}
}
