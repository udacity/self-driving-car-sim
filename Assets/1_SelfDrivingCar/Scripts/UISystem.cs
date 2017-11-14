using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityStandardAssets.Vehicles.Car;
using UnityEngine.SceneManagement;

public class UISystem : MonoSingleton<UISystem> {

    public CarController carController;
	public Camera mainCamera;
    public string GoodCarStatusMessage;
    public string BadSCartatusMessage;
    public Text MPH_Text;
    public Image MPH_Animation;
	public Text AccT_Text;
	public Text AccN_Text;
	public Text Acc_Text;
	public Text Jerk_Text;

	public Text AccStatus_Text;
	public Text JerkStatus_Text;

    private bool recording;
    private float topSpeed;
	private bool saveRecording;

	private bool auto_drive;

	public Slider slider_x;
	public Slider slider_y;
	public Slider slider_lx;
	public Slider slider_ly;

    // Use this for initialization
    void Start() {
		
        topSpeed = carController.MaxSpeed;
        
		AccStatus_Text.text = "";
		JerkStatus_Text.text = "";

        SetMPHValue(0);

		auto_drive = true;
		 
    }

    public void SetMPHValue(float value)
    {
        MPH_Text.text = value.ToString("N2");
        //Do something with value for fill amounts
        MPH_Animation.fillAmount = value/topSpeed;
    }
	public void SetAccTValue(float value)
	{
		AccT_Text.text = "AccT: "+value.ToString ("N0")+" m/s^2";
	}
	public void SetAccNValue(float value)
	{
		AccN_Text.text = "AccN: "+value.ToString ("N0")+" m/s^2";
	}
	public void SetAccValue(float value)
	{
		Acc_Text.text = "AccTotal: "+value.ToString ("N0")+" m/s^2";
		if (value >= 10) 
		{
			Acc_Text.color = Color.red;
			AccStatus_Text.text = "Max Acceleration Exceeded!";
		} 
		else
		{
			Acc_Text.color = Color.white;
			AccStatus_Text.text = "";
		}

	}
	public void SetJerkValue(float value)
	{
		Jerk_Text.text = "Jerk: "+value.ToString ("N0")+" m/s^3";
		if (Mathf.Abs(value) >= 10) 
		{
			Jerk_Text.color = Color.red;
			JerkStatus_Text.text = "Max Jerk Exceeded!";
		} 
		else
		{
			Jerk_Text.color = Color.white;
			JerkStatus_Text.text = "";
		}
	}
	
    void UpdateCarValues()
    {
        SetMPHValue(carController.CurrentSpeed);
		SetAccTValue(carController.SenseAccT());
		SetAccNValue(carController.SenseAccN());
		SetAccValue(carController.SenseAcc());
		SetJerkValue(carController.SenseJerk());
    }

	// Update is called once per frame
	void Update () {

	    if(Input.GetKeyDown(KeyCode.Escape))
        {
            //Do Menu Here
            SceneManager.LoadScene("MenuScene");
        }

        UpdateCarValues();
    }

	public void ToggleDriveMode()
	{
		auto_drive = !auto_drive;

		if (!auto_drive) 
		{
			carController.GetComponent<Rigidbody> ().isKinematic = false;
			carController.GetComponent<CarUserControl> ().enabled = true;
			mainCamera.GetComponent<MouseOrbitImproved> ().enabled = false;


			mainCamera.transform.position = carController.transform.TransformPoint( new Vector3 (0f, 3.2f, -8.2f));
			mainCamera.transform.localEulerAngles = new Vector3 (15f, 0f, 0f);
		} 
		else 
		{
			carController.GetComponent<Rigidbody> ().isKinematic = true;
			carController.GetComponent<CarUserControl> ().enabled = false;
			mainCamera.GetComponent<MouseOrbitImproved> ().enabled = true;
		}
	}
			
}
