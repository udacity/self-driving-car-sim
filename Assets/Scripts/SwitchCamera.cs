using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwitchCamera : MonoBehaviour {

	[SerializeField]
	GameObject c1,c2,c3;
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		if (Input.GetKeyDown (KeyCode.Alpha1)) {
			if (c1.activeInHierarchy)
				c1.SetActive (false);
			else
				c1.SetActive (true);
		}
			
		if(Input.GetKeyDown(KeyCode.Alpha2))
		{
			if (c2.activeInHierarchy)
				c2.SetActive (false);
			else
				c2.SetActive (true);
		}

		if(Input.GetKeyDown(KeyCode.Alpha3))
		{
			if (c3.activeInHierarchy)
				c3.SetActive (false);
			else
				c3.SetActive (true);
		}
	}
}
