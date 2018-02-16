using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class AuthenticatePlayer_SampleScript : MonoBehaviour 
{
	public Text userNameInput, passwordInput; // these are set through the editor
	public Text signInUser;


	public void AuthorizePlayerBttn()
	{
		Debug.Log ("Authorizing Player...");
		new GameSparks.Api.Requests.AuthenticationRequest ()
			.SetUserName (userNameInput.text)
			.SetPassword (passwordInput.text)
			.Send ((response) => {

					if(!response.HasErrors)
					{
						Debug.Log("Player Authenticated... \n User Name: "+response.DisplayName);
						signInUser.text = "Signed in as: "+response.DisplayName;
						Menu_UI menu = (Menu_UI) GameObject.Find("GameSparksManager").GetComponent(typeof(Menu_UI));
						menu.setUser();
					}
					else
					{
						Debug.Log("Error Authenticating Player... \n "+response.Errors.JSON.ToString());
					}

		});
	}

	public void AuthenticateDeviceBttn()
	{
		Debug.Log ("Authenticating Device...");
		new GameSparks.Api.Requests.DeviceAuthenticationRequest ()
			.SetDisplayName (userNameInput.text)
			.Send ((response) => {

					if(!response.HasErrors)
					{
						Debug.Log("Device Authenticated...");
					}
					else 
					{
						Debug.Log("Error Authenticating Device...");
					}
		});
	}
}

