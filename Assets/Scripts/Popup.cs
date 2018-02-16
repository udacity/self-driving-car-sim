using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class Popup : MonoBehaviour 
{
	public Text title, type, summary;
	public float disappearTime = 5f;
	private float countdownTimer;

	void Awake()
	{
		this.gameObject.SetActive (false);
	}

	void Update () {
		countdownTimer += Time.deltaTime;
		if (countdownTimer >= disappearTime) {
			countdownTimer = 0;
			this.gameObject.SetActive(false);
		}
	}

	public void CallPopup(GameSparks.Api.Messages.NewHighScoreMessage _message)
	{
		this.gameObject.SetActive (true);
		Debug.Log ("Popup Called...");
		title.text = _message.Title;
		type.text = _message.LeaderboardName;
		summary.text = _message.SubTitle;
	}
}
