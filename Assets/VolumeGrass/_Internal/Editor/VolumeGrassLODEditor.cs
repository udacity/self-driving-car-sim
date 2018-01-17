using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor (typeof(VolumeGrassLODAdjuster))]
public class VolumeGrassLODEditor : Editor {
	public override void OnInspectorGUI () {
		VolumeGrassLODAdjuster _target=(VolumeGrassLODAdjuster)target;
		bool flag=_target.useSimpleMaterial;
		
		GUILayout.Space(8);
		GUILayout.BeginHorizontal();
			GUILayout.Label ("Use simple material beyond given distance", EditorStyles.label );
			_target.useSimpleMaterial=EditorGUILayout.Toggle(_target.useSimpleMaterial);
		GUILayout.EndHorizontal();
		if (_target.useSimpleMaterial) {
			GUILayout.BeginHorizontal();
				GUILayout.Label("Distance treshold for simple material", EditorStyles.label);
				_target.MaterialDistanceTreshold=EditorGUILayout.FloatField(_target.MaterialDistanceTreshold);
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
				GUILayout.Label("Simple material", EditorStyles.label);
			
				// for UNITY <3.4
				//_target.SimpleMaterial=(Material)EditorGUILayout.ObjectField(_target.SimpleMaterial, typeof(Material));
				// for UNITY >=3.4
				_target.SimpleMaterial=(Material)EditorGUILayout.ObjectField(_target.SimpleMaterial, typeof(Material), allowSceneObjects : true);
			
				if ((!_target.SimpleMaterial) && (!flag)) {
					_target.SimpleMaterial=new Material(Shader.Find("Diffuse"));
				}
			GUILayout.EndHorizontal();
		}
		GUILayout.Space(8);
	}
}
