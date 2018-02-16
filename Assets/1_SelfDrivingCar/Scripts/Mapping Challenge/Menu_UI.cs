using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.SceneManagement;

public class Menu_UI : MonoBehaviour {

	private bool have_user = false;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {

		if (Input.GetKey (KeyCode.Escape)) {
			SceneManager.LoadScene ("Mapping_AuthenticationScene");
		}
		
	}

	public void StartChallenge()
	{
		SceneManager.LoadScene ("slam_webgl_icp_hallway");
	}

	public void RegisterUser()
	{
		SceneManager.LoadScene ("Mapping_RegistrationScene");
	}

	public void goToMenu()
	{
		SceneManager.LoadScene ("Mapping_AuthenticationScene");
	}

	public void setUser()
	{
		have_user = true;
	}

	public bool hasUser()
	{
		return have_user;
	}


}
