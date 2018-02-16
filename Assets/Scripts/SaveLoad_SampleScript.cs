using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using GameSparks.Core;
using System.Collections.Generic;

public class SaveLoad_SampleScript : MonoBehaviour {

	public Text experiancePoints, playerGold;
	public GameObject playerRefrence;
	public Text[] loadedData;


	public void SavePlayerBttn()
	{
		new GameSparks.Api.Requests.LogEventRequest ()
			.SetEventKey ("SAVE_PLAYER")
			.SetEventAttribute ("XP", experiancePoints.text)
			.SetEventAttribute ("POS", playerRefrence.transform.position.ToString())
			.SetEventAttribute ("GOLD", playerGold.text)
			.Send ((response) => {

					if(!response.HasErrors)
					{
						Debug.Log("Player Saved To GameSparks...");
					}
					else
					{
						Debug.Log("Error Saving Player Data...");
					}
		});
	}

	public void LoadPlayerBttn()
	{
		new GameSparks.Api.Requests.LogEventRequest ()
			.SetEventKey ("LOAD_PLAYER")
				.Send ((response) => {
					
					if(!response.HasErrors)
					{
						Debug.Log("Recieved Player Data From GameSparks...");
						GSData data = response.ScriptData.GetGSData("player_Data");
						loadedData[0].text = "Player ID: "+data.GetString("playerID");
						loadedData[1].text = "Player XP: "+data.GetString("playerXP");
						loadedData[2].text = "Player Gold: "+data.GetString("playerGold");
						loadedData[3].text = "Player Pos: "+data.GetString("playerPos");
					}
					else
					{
						Debug.Log("Error Loading Player Data...");
					}
				});
	}
}








