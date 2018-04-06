//#define PERFTEST        //For testing performance of parse/stringify.  Turn on editor profiling to see how we're doing

using UnityEngine;
using UnityEditor;

public class JSONChecker : EditorWindow {
	string JSON = @"{
	""TestObject"": {
		""SomeText"": ""Blah"",
		""SomeObject"": {
			""SomeNumber"": 42,
			""SomeBool"": true,
			""SomeNull"": null
		},
		
		""SomeEmptyObject"": { },
		""SomeEmptyArray"": [ ],
		""EmbeddedObject"": ""{\""field\"":\""Value with \\\""escaped quotes\\\""\""}""
	}
}";	  //dat string literal...
	JSONObject j;
	[MenuItem("Window/JSONChecker")]
	static void Init() {
		GetWindow(typeof(JSONChecker));
	}
	void OnGUI() {
		JSON = EditorGUILayout.TextArea(JSON);
		GUI.enabled = !string.IsNullOrEmpty(JSON);
		if(GUILayout.Button("Check JSON")) {
#if PERFTEST
            Profiler.BeginSample("JSONParse");
			j = JSONObject.Create(JSON);
            Profiler.EndSample();
            Profiler.BeginSample("JSONStringify");
            j.ToString(true);
            Profiler.EndSample();
#else
			j = JSONObject.Create(JSON);
#endif
			Debug.Log(j.ToString(true));
		}
		if(j) {
			//Debug.Log(System.GC.GetTotalMemory(false) + "");
			if(j.type == JSONObject.Type.NULL)
				GUILayout.Label("JSON fail:\n" + j.ToString(true));
			else
				GUILayout.Label("JSON success:\n" + j.ToString(true));

		}
	}
}
