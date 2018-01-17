using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ReplayState {Record, PlayBack, None};

public enum ControllerSelected {Keyboard , Joystick }; 

public class GameManager : MonoBehaviour {

	public static GameManager instance;

	public ReplayState replayState = ReplayState.None;

	private bool recording;

	public bool Recording {
		get {
			return recording;
		}
		set {
			if(value == true)
				replayState = ReplayState.Record;
			else 
				replayState = ReplayState.PlayBack;
			recording = value;
		}
	}


	public ControllerSelected controllerSelected = ControllerSelected.Keyboard;
	private string controllerSelectedStr;

	public string ControllerSelectedStr2 {
		get {
			controllerSelectedStr = controllerSelected == ControllerSelected.Keyboard ? "Keyboard" : "Joystick";
			return controllerSelectedStr;
		}
	}

	// Use this for initialization
	void Awake () {
		instance = this;
	}
	
	// Update is called once per frame
	void Update () {
//		if (Input.GetKeyDown (KeyCode.R))
//			Recording = !recording;
	}
}
