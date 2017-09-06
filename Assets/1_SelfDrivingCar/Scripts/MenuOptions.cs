using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

public class MenuOptions : MonoBehaviour
{
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
		
    public void StartPathPlanning()
    {
		SceneManager.LoadScene("PathPlanning");
    }

}
