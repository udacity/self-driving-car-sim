using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor (typeof(SetupForGrassRendering))]
public class SetupForGrassRenderingEditor : Editor {
	public override void OnInspectorGUI () {
		SetupForGrassRendering _target=(SetupForGrassRendering)target;
		
		DrawDefaultInspector();
		if (GUILayout.Button("Fill Motion Blur Grass Objects array")) {
			_target.autoFillMBlurArray();
			if (_target.motionBlurGrassObjects.Length==0) {
				EditorUtility.DisplayDialog("Info", "No VolumeGrass objects found in scene...", "Proceed", "");
			}
		}
	}
}