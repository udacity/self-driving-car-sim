using UnityEngine;
using System.Collections.Generic;
using System;
using System.IO;
using System.Net;
using System.Threading;

namespace GameSparks.Editor
{
    /// <summary>
    /// Provides access to the rest API of the GameSparks backend. Can be used in the editor only. 
    /// </summary>
    public class GameSparksRestApi {

    	private static string HOST = "https://portal.gamesparks.net/";

    	private static string REST_URL = HOST + "rest/";

    	static GameSparksRestApi() {

    		System.Net.ServicePointManager.ServerCertificateValidationCallback +=
    			delegate(object sender, System.Security.Cryptography.X509Certificates.X509Certificate certificate,
    			         System.Security.Cryptography.X509Certificates.X509Chain chain,
    			         System.Net.Security.SslPolicyErrors sslPolicyErrors)
    		{
    			return true; // **** Always accept
    		};


    	}

    	public static String getDownloadable(string apiKey, string username, string password, string shortCode){
    		string url = REST_URL + apiKey + "/binarycontent/" + shortCode;
    		WebClient wc = new WebClient();
    		NetworkCredential myCreds = new NetworkCredential(username, password);
    		wc.Credentials = myCreds;
    		String ret = null;
    		try{
    			ret = wc.DownloadString(url);
    		}catch(WebException we){
    			ret = "{\"responseCode\":-1,\"errorMessage\":\"" + we.Message + "\"}";
    		}
    		return ret;
    	}

    	public static String setDownloadable(string apiKey, string username, string password, string shortCode, string fileName){
    		string url = REST_URL + apiKey + "/binarycontent/" + shortCode;
    		String ret = null;
    		try{
    			ret = GameSparksEditorFormUpload.UploadFile(url, fileName, username, password);
    		}catch(WebException we){
    			ret = "{\"responseCode\":-1,\"errorMessage\":\"" + we.Message + "\"}";
    		}
    		return ret;
    	}

    	public static String getApi(){
    		string url = HOST + "sdk/" + GameSparksSettings.ApiKey + "/" + GameSparksSettings.ApiSecret + "/GameSparksCustomSDK501.cs";
    		Debug.Log(url);
    		WebClient wc = new WebClient();
    		String ret = null;
    		try{
    			ret = wc.DownloadString(url);
    		}catch(Exception e){
    			Debug.Log(e.ToString());

    		}
    		return ret;
    	}

    	public static System.Xml.XmlReader GetSDKInfo(){
    		string url = HOST + "sdk/unity.xml";
    		Debug.Log(url);
    		WebClient wc = new WebClient();
    		try{
    			String data = wc.DownloadString(url);
    			System.Xml.XmlReader reader = System.Xml.XmlTextReader.Create(new System.IO.StringReader(data));
    			return reader;
    		}catch(Exception e){
    			Debug.Log(e.ToString());
    			
    		}
    		return null;
    	}

    	public static bool UpdateSDKFile(String source, String target){
    		if(!source.StartsWith("http")){
    			source = HOST + "sdk/" + source;
    		}
    		Directory.CreateDirectory(Path.GetDirectoryName(target));
    		WebClient wc = new WebClient();
    		try{
    			wc.DownloadFile(source, target);
    			return true;
    		}catch(Exception e){
    			Debug.Log(e.ToString());
    		}
    		return false;
    	}

    }
}