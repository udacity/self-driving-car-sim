using UnityEngine;
using System.Collections;
using UnityEditor;
using GameSparks.Core;

namespace GameSparks.Editor
{
    /// <summary>
    /// Editor class for <see cref="GameSparksUnity"/>. 
    /// </summary>
    [CustomEditor(typeof(GameSparksUnity))]
    public class GameSparksUnityInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            GUI.enabled = false;

            GUILayout.BeginHorizontal();
            GUILayout.Label("SDK Version", GUILayout.Width(EditorGUIUtility.labelWidth));
            GUILayout.Label(GS.Version.ToString());
            GUILayout.EndHorizontal();
            GUI.enabled = true;
            base.OnInspectorGUI();

        }
    }
}