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
	public Text Collision_Text;
	public Text Speeding_Text;
	public Text Lane_Text;

	//Distance Evaluation
	public Text Best_Distance_Text;
	public Text Curr_Distance_Text;
	public Text Curr_Time_Text;
	private float best_dist_eval = 0;
	private bool check_incidents;

	public Text AccStatus_Text;
	public Text JerkStatus_Text;

    private bool recording;
    private float topSpeed;
	private bool saveRecording;

	private bool auto_drive;

	private CarAIControl carAI;

    // Use this for initialization
    void Start() {
		
        topSpeed = carController.MaxSpeed;
        
		AccStatus_Text.text = "";
		JerkStatus_Text.text = "";

        SetMPHValue(0);

		carAI = (CarAIControl) carController.GetComponent(typeof(CarAIControl));

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
			check_incidents = true;
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
			check_incidents = true;
		} 
		else
		{
			Jerk_Text.color = Color.white;
			JerkStatus_Text.text = "";
		}
	}
	public void SetCollisionValue(bool collision)
	{
		if (collision) 
		{
			Collision_Text.color = Color.red;
			Collision_Text.text = "Collision!";
			check_incidents = true;
		} 
		else 
		{
			Collision_Text.color = Color.white;
			Collision_Text.text = "";
		}
	}

	public void SetSpeedingValue(bool speeding)
	{
		if (speeding) 
		{
			Speeding_Text.color = Color.red;
			Speeding_Text.text = "Violated Speed Limit!";
			check_incidents = true;
		} 
		else 
		{
			Speeding_Text.color = Color.white;
			Speeding_Text.text = "";
		}
	}

	public void SetLaneValue(bool outside_lane)
	{
		if (outside_lane) 
		{
			Lane_Text.color = Color.red;
			Lane_Text.text = "Outside of Lane!";
			check_incidents = true;
		} 
		else 
		{
			Lane_Text.color = Color.white;
			Lane_Text.text = "";
		}
	}

	public void SetDistanceValue(float dist_eval)
	{
		if (auto_drive && (dist_eval > best_dist_eval) )
		{
			best_dist_eval = dist_eval;
			Best_Distance_Text.text = "Best: "+dist_eval.ToString("N2")+" Miles";
		} 

		Curr_Distance_Text.text = "Curr: "+dist_eval.ToString("N2")+" Miles";

	}
	public void SetTimerValue(int seconds)
	{
		int hours = (seconds / 3600);
		seconds -= hours * 3600;
		int minutes = seconds / 60;
		seconds -= minutes * 60;

		Curr_Time_Text.text = "Timer: " + (hours).ToString ("0") + ":" + (minutes).ToString ("00") + ":" + (seconds).ToString ("00");

	}
	
    void UpdateCarValues()
    {
		check_incidents = false; // are there any incidents?
        SetMPHValue(carController.CurrentSpeed);
		SetAccTValue(carController.SenseAccT());
		SetAccNValue(carController.SenseAccN());
		SetAccValue(carController.SenseAcc());
		SetJerkValue(carController.SenseJerk());
		SetCollisionValue(carAI.CheckCollision());
		SetSpeedingValue(carAI.CheckSpeeding());
		SetLaneValue(carAI.CheckLanePos());
		if (check_incidents) 
		{
			carAI.ResetDistance ();
		}
		SetDistanceValue(carAI.DistanceEval());
		SetTimerValue(carAI.TimerEval());
			
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

		carAI.ResetDistance ();
	}
			
}
