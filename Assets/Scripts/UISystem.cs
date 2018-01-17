using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

public class UISystem : MonoBehaviour {

	public Text MPH_Text;
	public Image MPH_Animation;
	public Text Angle_Text;
	public Text RecordStatus_Text;
	public Text SaveStatus_Text;
	public GameObject RecordingPause; 
	public GameObject RecordDisabled;

	public Slider heightAdjustSlider;

	[SerializeField]
	private Sprite[] selectedController;
	[SerializeField]
	private Image selectedControllerButtonImage;

	private bool recording;
	private float topSpeed = 200 ; //Using default value
	private bool saveRecording;

	[SerializeField]
	Text toggleText;

	private Record record;
	// Use this for initialization
	void Start() {
		recording = false;
		RecordingPause.SetActive(false);
		RecordStatus_Text.text = "RECORD";
		SaveStatus_Text.text = "";
		SetAngleValue(0);
		SetMPHValue(0);
		record = GetComponent<Record> ();
	}

	public void SetAngleValue(float value)
	{
		Angle_Text.text = value.ToString("N2") + "°";
	}

	public void SetMPHValue(float value)
	{
		MPH_Text.text = value.ToString("N2");
		MPH_Animation.fillAmount = value/topSpeed;
	}

	public void ToggleRecording()
	{
		// Don't record in autonomous mode
//		if (!isTraining) {
//			return;
//		}
//
		if (!recording)
		{
		if (record.checkSaveLocation()) 
			{
				recording = true;
				RecordingPause.SetActive (true);
				RecordStatus_Text.text = "RECORDING";
				record.IsRecording = true;
			}
		}
		else
		{
			saveRecording = true;
			record.IsRecording = false;
		}
	}

	void UpdateCarValues()
	{
//		SetMPHValue(carController.CurrentSpeed);
//		SetAngleValue(carController.CurrentSteerAngle);
	}

	// Update is called once per frame
	void Update () {

		// Easier than pressing the actual button :-)
		// Should make recording training data more pleasant.

		if (record.getSaveStatus ()) {
			SaveStatus_Text.text = "Capturing Data: " + (int)(100 * record.getSavePercent ()) + "%";
			//Debug.Log ("save percent is: " + carController.getSavePercent ());
		} 
		else if(saveRecording) 
		{
			SaveStatus_Text.text = "";
			recording = false;
			RecordingPause.SetActive(false);
			RecordStatus_Text.text = "RECORD";
			saveRecording = false;
		}

		if (Input.GetKeyDown(KeyCode.R))
		{
			ToggleRecording();
		}
		if (GameManager.instance.controllerSelected == ControllerSelected.Joystick)
			heightAdjustSlider.value = Input.GetAxis ("RightJoystickVertrical");
		
		UpdateCarValues();
	}

	public void ControllerSwap(){
		if (GameManager.instance.controllerSelected == ControllerSelected.Keyboard) {
			GameManager.instance.controllerSelected = ControllerSelected.Joystick;
			toggleText.text = "Select Keyboard";
			selectedControllerButtonImage.sprite = selectedController [0];
		} else {
			GameManager.instance.controllerSelected = ControllerSelected.Keyboard;
			toggleText.text = "Select Joystick";
			selectedControllerButtonImage.sprite = selectedController [1];
		}
	}
	public void ReloadScene(){
		SceneManager.LoadScene(SceneManager.GetActiveScene().name);
	}
}
