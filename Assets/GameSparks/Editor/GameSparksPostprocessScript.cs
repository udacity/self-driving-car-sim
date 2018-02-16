#if UNITY_IPHONE

using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;

using System;
using System.Diagnostics;

namespace GameSparks.Editor
{
    public class GameSparksPostprocessScript : MonoBehaviour
    {

    	//IMPORTANT!!!
    	//100 is order , it means this one will execute after e.g 1 as default one is 1 
    	//it means our script will run after all other scripts got run
    	[PostProcessBuild(100)]
    	public static void OnPostprocessBuild(BuildTarget target, string pathToBuildProject)
    	{
            
    		UnityEngine.Debug.Log("----Custome Script---Executing post process build phase."); 		
    		Process myCustomProcess = new Process();		
    		myCustomProcess.StartInfo.FileName = "python";
            myCustomProcess.StartInfo.Arguments = string.Format("Assets/GameSparks/Editor/post_process.py \"{0}\"", pathToBuildProject);
            myCustomProcess.StartInfo.UseShellExecute = false;
            myCustomProcess.StartInfo.RedirectStandardOutput = true;
    		myCustomProcess.Start(); 
    		myCustomProcess.WaitForExit();
    		string output = myCustomProcess.StandardOutput.ReadToEnd();
    		UnityEngine.Debug.Log(output);
    		UnityEngine.Debug.Log("----Custome Script--- Finished executing post process build phase.");  
    		
           
    	}
    }
}
#endif
