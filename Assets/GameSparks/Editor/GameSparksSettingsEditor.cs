using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace GameSparks.Editor
{
    /// <summary>
    /// Editor class for <see cref="GameSparksSettings"/>
    /// </summary>
    [CustomEditor(typeof(GameSparksSettings))]
    public class GameSparksSettingsEditor : UnityEditor.Editor
    {
        GUIContent apiSecretLabel = new GUIContent("Api Secret [?]:", "GameSparks Api Secret can be found at https://portal.gamesparks.net");
        GUIContent apiKeyLabel = new GUIContent("Api Key [?]:", "GameSparks Api Key can be found at https://portal.gamesparks.net");
    	GUIContent previewLabel = new GUIContent("Preview Build [?]:", "Run app against the preview service");
    	GUIContent debugLabel = new GUIContent("Debug Build [?]:", "Run app with extended debugging");

    	const string UnityAssetFolder = "Assets";

        public static GameSparksSettings GetOrCreateSettingsAsset()
    	{
    		string fullPath = Path.Combine(Path.Combine(UnityAssetFolder, GameSparksSettings.gamesparksSettingsPath),
    		                               GameSparksSettings.gamesparksSettingsAssetName + GameSparksSettings.gamesparksSettingsAssetExtension
    		                               );

    		GameSparksSettings instance = AssetDatabase.LoadAssetAtPath(fullPath, typeof(GameSparksSettings)) as GameSparksSettings;

    		if(instance == null)
    		{
    			// no asset found, we need to create it. 

    			if(!Directory.Exists(Path.Combine(UnityAssetFolder, GameSparksSettings.gamesparksSettingsPath)))
    			{
    				AssetDatabase.CreateFolder(Path.Combine(UnityAssetFolder, "GameSparks"), "Resources");
    			}

    			instance = CreateInstance<GameSparksSettings>();
    			AssetDatabase.CreateAsset(instance, fullPath);
    			AssetDatabase.SaveAssets();
    		}
    		return instance;
    	}

    	[MenuItem("GameSparks/Edit Settings")]
        public static void Edit()
        {
            Selection.activeObject = GetOrCreateSettingsAsset();
        }


    	void OnDisable()
    	{
    		// make sure the runtime code will load the Asset from Resources when it next tries to access this. 
    		GameSparksSettings.SetInstance(null);
    	}
    	
    	public override void OnInspectorGUI()
    	{
    		GameSparksSettings settings = (GameSparksSettings)target;
    		GameSparksSettings.SetInstance(settings);

    		GUILayout.TextArea("SDK Version : "+GameSparks.Core.GS.Version, EditorStyles.wordWrappedLabel);

    		EditorGUILayout.HelpBox("Add the GameSparks Api Key and Secret associated with this game", MessageType.None);
    		
    		EditorGUILayout.BeginHorizontal();
    		GameSparksSettings.ApiKey = EditorGUILayout.TextField(apiKeyLabel, GameSparksSettings.ApiKey);
            EditorGUILayout.EndHorizontal();

    		
            EditorGUILayout.BeginHorizontal();
    		GameSparksSettings.ApiSecret = EditorGUILayout.TextField(apiSecretLabel, GameSparksSettings.ApiSecret);
            EditorGUILayout.EndHorizontal();
    		
    		
    		EditorGUILayout.BeginHorizontal();
    		GameSparksSettings.PreviewBuild = EditorGUILayout.Toggle(previewLabel, GameSparksSettings.PreviewBuild);
            EditorGUILayout.EndHorizontal();
    		
    		EditorGUILayout.BeginHorizontal();
    		GameSparksSettings.DebugBuild = EditorGUILayout.Toggle(debugLabel, GameSparksSettings.DebugBuild);
            EditorGUILayout.EndHorizontal();

    		EditorGUILayout.Space();


    		String testScenePath = "Assets/GameSparks/TestUI/GameSparksTestUI.unity";

    		String testButtonText = "Test Configuration";

    		if(EditorApplication.currentScene.Equals(testScenePath) && EditorApplication.isPlaying){
    			testButtonText = "Stop Test";
    		}

    		if(GameSparksSettings.ApiKey != null && GameSparksSettings.ApiSecret != null){
    			String myApiPath = "Assets/GameSparks/MyGameSparks.cs";
    			GUILayout.TextArea("Download your custom data structures into your own SDK. Be sure to update this if you change the structure of Events and Leaderboards within the developer portal", EditorStyles.wordWrappedLabel);
    			if(GUILayout.Button("Get My Custom SDK")){
    				String myApi = GameSparksRestApi.getApi();
    				if(myApi != null){
    					Debug.Log("Updating GameSparks Api for game." + GameSparksSettings.ApiKey);
    					Directory.CreateDirectory(Path.GetDirectoryName(myApiPath));
    					using (StreamWriter outfile = new StreamWriter(myApiPath))
    					{
    						outfile.Write(myApi);
    					}
    				}
    				EditorUtility.SetDirty(settings);
    				AssetDatabase.Refresh();

    			}

    //			GUILayout.TextArea("Get the latest GameSparks SDK version.", EditorStyles.wordWrappedLabel);
    //
    //			if(GUILayout.Button("Update SDK")){
    //				System.Xml.XmlReader sdkInfo = GameSparksRestApi.GetSDKInfo();
    //				if(sdkInfo != null){
    //					while(sdkInfo.Read()){
    //
    //						if((sdkInfo.NodeType == System.Xml.XmlNodeType.Element) && (sdkInfo.Name == "sdk"))
    //						{
    //							string serverVersion = sdkInfo.GetAttribute("version");
    //							Debug.Log ("Server Version " + serverVersion);
    //							if(GameSparksSettings.SdkVersion == null || !GameSparksSettings.SdkVersion.Equals(serverVersion)){
    //								Debug.Log ("Updating GameSparks SDK");
    //								System.Xml.XmlReader files = sdkInfo.ReadSubtree();
    //								while(files.Read()){
    //									if((files.NodeType == System.Xml.XmlNodeType.Element) && (files.Name == "file")){
    //										GameSparksRestApi.UpdateSDKFile(files.GetAttribute("source"), files.GetAttribute("target"));
    //									}
    //								}
    //								Debug.Log ("Updating GameSparks Version: from (" + GameSparksSettings.SdkVersion + ") to (" + serverVersion + ")");
    //								GameSparksSettings.SdkVersion = serverVersion;
    //								EditorUtility.SetDirty(instance);
    //								AssetDatabase.Refresh();
    //							} else {
    //								break;
    //							}
    //						}
    //					}
    //				}
    //
    //			}
    		}

    		GUILayout.TextArea("Run the GameSparks test harness in the editor. ", EditorStyles.wordWrappedLabel);
    		if(GUILayout.Button(testButtonText)){
    			EditorUtility.SetDirty(settings);
    			if(EditorApplication.currentScene.Equals(testScenePath) && EditorApplication.isPlaying){
    				EditorApplication.isPlaying = false;
    			} else {
    				if(!EditorApplication.currentScene.Equals(testScenePath)){
    					if(EditorApplication.SaveCurrentSceneIfUserWantsTo()){
    						if(!EditorApplication.OpenScene(testScenePath)){
    							EditorApplication.NewScene();
    							new GameObject("GameSparks", typeof(GameSparksTestUI), typeof(GameSparksUnity));
    							EditorApplication.SaveScene(testScenePath);
    						}
    						EditorApplication.isPlaying = true;
    					}
    				} else {
    					EditorApplication.isPlaying = true;
    				}
    			}
    		}
    		if(GUI.changed)
    		{
    			EditorUtility.SetDirty(settings);
    			AssetDatabase.SaveAssets();
    		}
            
        }


        
    }
}