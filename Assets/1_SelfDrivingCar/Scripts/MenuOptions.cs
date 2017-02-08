using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

public class MenuOptions : MonoBehaviour
{
    private int track = 0;
    private Outline[] outlines;

    public void Start ()
    {
        outlines = GetComponentsInChildren<Outline>();
		Debug.Log ("in menu script "+outlines.Length);
		if (outlines.Length > 0) 
		{
			outlines [0].effectColor = new Color (0, 0, 0);
		}
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

    public void StartDrivingMode()
    {
        if (track == 0) {
            SceneManager.LoadScene("LakeTrackTraining");
        } else {
            SceneManager.LoadScene("JungleTrackTraining");
        }

    }

    public void StartAutonomousMode()
    {
        if (track == 0) {
            SceneManager.LoadScene("LakeTrackAutonomous");
        } else {
            SceneManager.LoadScene("JungleTrackAutonomous");
        }
    }

    public void SetLakeTrack()
    {
        outlines [0].effectColor = new Color (0, 0, 0);
        outlines [1].effectColor = new Color (255, 255, 255);
        track = 0;
    }

    public void SetMountainTrack()
    {
        track = 1;
        outlines [1].effectColor = new Color (0, 0, 0);
        outlines [0].effectColor = new Color (255, 255, 255);
    }

}
