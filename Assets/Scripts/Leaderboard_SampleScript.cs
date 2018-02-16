using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;

public class Leaderboard_SampleScript : MonoBehaviour 
{
	public Text scoreInput, entryCount, outputData;
	public GameObject highScorePopup;

	void Awake () 
	{
		GameSparks.Api.Messages.NewHighScoreMessage.Listener += HighScoreMessageHandler; // assign the New High Score message
	}

	void HighScoreMessageHandler (GameSparks.Api.Messages.NewHighScoreMessage _message)
	{
		Debug.Log ("NEW HIGH SCORE \n "+_message.LeaderboardName);
		highScorePopup.GetComponent<Popup>().CallPopup(_message);
	}

	public void PostScoreBttn()
	{
		Debug.Log ("Posting Score To Leaderboard...");
		new GameSparks.Api.Requests.LogEventRequest ()
			.SetEventKey("SUBMIT_SCORE")
			.SetEventAttribute("Score", scoreInput.text)
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

	public void GetLeaderboard()
	{
		Debug.Log ("Fetching Leaderboard Data...");

		new GameSparks.Api.Requests.LeaderboardDataRequest ()
			.SetLeaderboardShortCode ("Score_Leaderboard")
			.SetEntryCount(int.Parse(entryCount.text)) // we need to parse this text input, since the entry count only takes long
			.Send ((response) => {

					if(!response.HasErrors)
					{
						Debug.Log("Found Leaderboard Data...");
						outputData.text = System.String.Empty; // first clear all the data from the output
						foreach(GameSparks.Api.Responses.LeaderboardDataResponse._LeaderboardData entry in response.Data) // iterate through the leaderboard data
						{
							int rank = (int)entry.Rank; // we can get the rank directly
							string playerName = entry.UserName;
							string score = entry.JSONData["Score"].ToString(); // we need to get the key, in order to get the score
							outputData.text += rank+"   Name: "+playerName+"        Score:"+score +"\n"; // addd the score to the output text
						}
					}
					else
					{
						Debug.Log("Error Retrieving Leaderboard Data...");
					}

		});
	}
}
















