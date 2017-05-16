using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

public class MenuOptions : MonoBehaviour
{
    private int project;
    private Outline[] outlines;
	public Text project_name;
	public Image project_image;
	public Sprite project_1;
	public Sprite project_2;
	public Sprite project_3;
	public Sprite project_4;
	public Sprite project_5;

    public void Start ()
    {
		project = 0;
		project_name.text = "Project 1: Bicycle Tracker with EKF";
		project_image.sprite = project_1;
    }

	public void ControlMenu()
	{
		SceneManager.LoadScene ("ControlMenu");
	}

	public void MainMenu()
	{
		Debug.Log ("go to main menu");
		SceneManager.LoadScene ("MenuScene");
	}

    public void SelectMode()
    {
		if (project == 0) {
			SceneManager.LoadScene("EKF_project");
		}
		else if (project == 1) {
			SceneManager.LoadScene("UKF_project");
		} 
		else if (project == 2) {
			SceneManager.LoadScene("particle_filter_v2");
		} 
        else if (project == 3) {
			SceneManager.LoadScene("LakeTrackAutonomous_pid");
        } 
		else if (project == 4) {
			SceneManager.LoadScene("LakeTrackAutonomous_mpc");
		} 

    }

	public void Next()
	{
		project = (project + 1) % 5;

		if(project == 0)
		{
			project_name.text = "Project 1: Bicycle Tracker with EKF";
			project_image.sprite = project_1;
		}
		else if(project == 1)
		{
			project_name.text = "Project 2: Run Away Robot with UKF";
			project_image.sprite = project_2;
		}
		else if(project == 2)
		{
			project_name.text = "Project 3: Kidnapped Vehicle";
			project_image.sprite = project_3;
		}
		else if(project == 3)
		{
			project_name.text = "Project 4: PID Controller";
			project_image.sprite = project_4;
		}
		else if(project == 4)
		{
			project_name.text = "Project 5: MPC Controller";
			project_image.sprite = project_5;
		}
	}

	public void Previous()
	{
		if (project == 0)
		{
			project = 4;
		} 
		else 
		{
			project = (project - 1) % 5;
		}

		if(project == 0)
		{
			project_name.text = "Project 1: Bicycle Tracker with EKF";
			project_image.sprite = project_1;
		}
		else if(project == 1)
		{
			project_name.text = "Project 2: Run Away Robot with UKF";
			project_image.sprite = project_2;
		}
		else if(project == 2)
		{
			project_name.text = "Project 3: Kidnapped Vehicle";
			project_image.sprite = project_3;
		}
		else if(project == 3)
		{
			project_name.text = "Project 4: PID Controller";
			project_image.sprite = project_4;
		}
		else if(project == 4)
		{
			project_name.text = "Project 5: MPC Controller";
			project_image.sprite = project_5;
		}
	}
		

}
