using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using UnityEngine.SceneManagement;

public class Mapping_UI : MonoBehaviour {

	public Button submit;
	private Menu_UI menu;
	private slam_controller_icp slam;

	// Use this for initialization
	void Start () {


		menu = (Menu_UI) GameObject.Find("GameSparksManager").GetComponent(typeof(Menu_UI));
		slam = (slam_controller_icp) GameObject.Find ("car-blue").GetComponent (typeof(slam_controller_icp));

		if (menu.hasUser())
		{
			submit.interactable = true;
		}
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	public void PostScoreBttn()
	{

		int score = slam.getMapError ();

		Debug.Log ("Posting Score To Leaderboard...");
		new GameSparks.Api.Requests.LogEventRequest ()
			.SetEventKey("SUBMIT_SCORE")
			.SetEventAttribute("Score", score)
			.Send ((response) => {

				if(!response.HasErrors)
				{
					Debug.Log("Score Posted Sucessfully...");
				}
				else
				{
					Debug.Log("Error Posting Score...");
				}
			});
	}


		


}
