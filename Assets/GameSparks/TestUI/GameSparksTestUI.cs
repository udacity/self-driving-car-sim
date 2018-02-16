using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using GameSparks.Api;
using GameSparks.Api.Messages;
using GameSparks.Api.Requests;
using GameSparks.Api.Responses;
using GameSparks.Core;

/// <summary>
/// Using the immediate mode Unity GUI this class allows for testing the 
/// connection to the GameSparks backend in the Unity Editor. 
/// </summary>
public class GameSparksTestUI : MonoBehaviour
{
	
	private Queue<string> myLogQueue = new Queue<string>();
	private string myLog = "";
	private string fbToken = "accessToken";
	private string dismissMessageId = "messageId";
	private int itemHeight = 30;
	private int itemWidth = 200;
	
	void Awake ()
	{
#if UNITY_5
        Application.logMessageReceivedThreaded += HandleLog;
        //Application.logMessageReceived += HandleLog;
#else
		Application.RegisterLogCallbackThreaded (HandleLog);
#endif
		Screen.orientation = ScreenOrientation.AutoRotation;
	}
	
	void Start(){
		GSMessageHandler._AllMessages = HandleGameSparksMessageReceived;
	}
	
	void HandleGameSparksMessageReceived (GSMessage message)
	{
        HandleLog("MSG:" + message.JSONString);
	}

	void HandleLog (string logString){
        GS.GSPlatform.ExecuteOnMainThread(() => {
            HandleLog(logString, null, LogType.Log);
        });
	}
	
	void HandleLog (string logString, string stackTrace, LogType logType)
	{
		if(myLogQueue.Count > 30){
			myLogQueue.Dequeue();
		}
		myLogQueue.Enqueue(logString);
		myLog = "";
		
		foreach(string logEntry in myLogQueue.ToArray()){
			myLog = logEntry + "\n\n" + myLog;	
		}
	}
	
	void OnGUI ()
	{

		// center labels
		GUI.skin.label.alignment = TextAnchor.MiddleCenter;
		GUI.skin.textField.alignment = TextAnchor.MiddleCenter;
		
		GUILayout.BeginHorizontal();
		
		GUILayout.Label ((GS.Available ? "AVAILABLE" : "NOT AVAILABLE"), GUILayout.Width (itemWidth), GUILayout.Height (itemHeight));
        GUILayout.Label (("SDK Version: " + GS.Version.ToString()), GUILayout.Width (itemWidth), GUILayout.Height (itemHeight));

        GUILayout.EndHorizontal();

        GUILayout.Label ((GS.Authenticated ? "AUTHENTICATED" : "NOT AUTHENTICATED"), GUILayout.Width (itemWidth), GUILayout.Height (itemHeight));

		if (GUILayout.Button ("Clear Log", GUILayout.Width (itemWidth), GUILayout.Height (itemHeight))) {
			myLog = "";
			myLogQueue.Clear();
		}

		if (GUILayout.Button ("Logout", GUILayout.Width (itemWidth), GUILayout.Height (itemHeight))) {
			GS.Reset();
		}

		if(GS.Available){
			if (GUILayout.Button ("Disconnect", GUILayout.Width (itemWidth), GUILayout.Height (itemHeight))) {
				GS.Disconnect();
			}
		} else {
			if (GUILayout.Button ("Reconnect", GUILayout.Width (itemWidth), GUILayout.Height (itemHeight))) {
				GS.Reconnect();
			}
		}

		if (GUILayout.Button ("DeviceAuthenticationRequest", GUILayout.Width (itemWidth), GUILayout.Height (itemHeight))) {
			new DeviceAuthenticationRequest().Send((response) => {
				HandleLog("DeviceAuthenticationRequest.JSON:" + response.JSONString);
				HandleLog("DeviceAuthenticationRequest.HasErrors:" + response.HasErrors);
				HandleLog("DeviceAuthenticationRequest.UserId:" + response.UserId);
			});
		}

		if (GUILayout.Button ("durableAccountDetailsRequest", GUILayout.Width (itemWidth), GUILayout.Height (itemHeight))) {
			new AccountDetailsRequest().SetDurable(true).Send(null);
		}

		if (GUILayout.Button ("accountDetailsRequest", GUILayout.Width (itemWidth), GUILayout.Height (itemHeight))) {
			new AccountDetailsRequest().Send((response) => {
				HandleLog("AccountDetailsRequest.UserId:" + response.UserId);
			});
		}
		GUILayout.BeginHorizontal ();
		
		if (GUILayout.Button ("facebookConnectRequest", GUILayout.Width (itemWidth), GUILayout.Height (itemHeight))) {
			new FacebookConnectRequest().SetAccessToken(fbToken).Send((response) => {
				HandleLog("FacebookConnectRequest.HasErrors:" + response.HasErrors);
				HandleLog("FacebookConnectRequest.UserId:" + response.UserId);
			});
		}
		
		fbToken = GUILayout.TextField (fbToken, GUILayout.Width (itemWidth), GUILayout.Height (itemHeight));
		
		GUILayout.EndHorizontal ();
		
		if (GUILayout.Button ("listAchievementsRequest", GUILayout.Width (itemWidth), GUILayout.Height (itemHeight))) {
			new ListAchievementsRequest().Send((response) => {
				foreach(var c in response.Achievements){
					HandleLog("ListAchievementsRequest:shortCode:" + c.ShortCode);
				}
			});
		}
		
		if (GUILayout.Button ("listGameFriendsRequest", GUILayout.Width (itemWidth), GUILayout.Height (itemHeight))) {
			new ListGameFriendsRequest().Send((response) => {
				foreach(var c in response.Friends){
					HandleLog("ListGameFriendsRequest.DisplayName:" + c.DisplayName);
				}
			});
		}
		
		if (GUILayout.Button ("listVirtualGoodsRequest", GUILayout.Width (itemWidth), GUILayout.Height (itemHeight))) {
            new ListVirtualGoodsRequest().Send((response) => {

            	foreach(var c in response.VirtualGoods){
    				HandleLog("ListVirtualGoodsRequest.Description:" + c.Description);
    			}
                
            });
		
		}
		
		if (GUILayout.Button ("listChallengeTypeRequest", GUILayout.Width (itemWidth), GUILayout.Height (itemHeight))) {
			new ListChallengeTypeRequest().Send((response) => {
				foreach(var c in response.ChallengeTemplates){
					HandleLog("ListAchievementsRequest.Challenge:" + c.ChallengeShortCode);
				}
			});
		}

		if (GUILayout.Button ("authenticationRequest", GUILayout.Width (itemWidth), GUILayout.Height (itemHeight))) {
            new AuthenticationRequest ().SetUserName ("gabs").SetPassword ("gabs").Send ((AR) => {

                if (AR.HasErrors) {
    				Debug.Log ("Didnt Work");
    			} else {
    				Debug.Log ("Worked");
    			}
            });

		}


		if (GUILayout.Button ("leaderboardData", GUILayout.Width (itemWidth), GUILayout.Height (itemHeight))) {
            new LeaderboardDataRequest ().SetLeaderboardShortCode ("HSCORE").SetEntryCount (10) .Send ((leadResponse) => {

                
    			if (leadResponse.HasErrors) {
    				Debug.Log("Leaderboard data retrieval failed ...");
    			} else {
    				Debug.Log("Leaderboard data retrieval succeeded ..." + leadResponse);
    				
    				// Render the leaderboard entries on the screen
    				
    				foreach (var entry in leadResponse.Data)
    				{
    					string myText = "Rank: " + entry.Rank.ToString() + "    UserName: " + entry.UserName + "    Score: " + entry.GetNumberValue("SCORE").ToString();
    					Debug.Log (myText);
    				}
    				
    			}
            });

		}
		
		if (GUILayout.Button ("listMessageRequest" , GUILayout.Width (itemWidth), GUILayout.Height (itemHeight))) {
			new ListMessageRequest().Send((response) => {
				foreach(var c in response.MessageList){
					HandleLog("ListMessageRequest.MessageList:" + c.GetString("messageId"));
				}
			});
		}
		
		GUILayout.BeginHorizontal ();
		
		if (GUILayout.Button ("dismissMessageRequest", GUILayout.Width (itemWidth), GUILayout.Height (itemHeight))) {
			new DismissMessageRequest().SetMessageId(dismissMessageId).Send((response) => {
				HandleLog("DismissMessageRequest.HasErrors:" + response.HasErrors);

			});
		}


		dismissMessageId = GUILayout.TextField (dismissMessageId, GUILayout.Width (itemWidth), GUILayout.Height (itemHeight));
		
		GUILayout.EndHorizontal ();

		if (GUILayout.Button ("TRACE " + (GS.TraceMessages ? "ON" : "OFF"), GUILayout.Width (itemWidth), GUILayout.Height (itemHeight))) {
			GS.TraceMessages = !GS.TraceMessages;
		}
		

		GUI.TextArea (new Rect (420, 5, Screen.width - 425, Screen.height - 10), myLog);
		
		
	}
}
