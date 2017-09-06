using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UnityStandardAssets.Vehicles.Car
{
public class AutoToggle : MonoBehaviour {

	public Text myText;
	
	private bool state;


	// Use this for initialization
	void Start () {
		state = true;
		myText.text = "AutoPilot: Enable";

	}
	
	// Update is called once per frame
	public void Toggle(){
		state = !state;
		if(state){
			myText.text = "AutoPilot: Enable";
		}
		else{
			myText.text = "AutoPilot: Disable";
		}
	}
}
}
