using UnityEngine;
using UnityEditor;


namespace UnityStandardAssets.Water
{
    [CustomEditor(typeof(PlanarReflection))]
    public class PlanarReflectionEditor : Editor
    {
        private SerializedObject serObj;

        //private SerializedProperty wavesFrequency;

        // reflection
        private SerializedProperty reflectionMask;
        private SerializedProperty reflectSkybox;
        private SerializedProperty clearColor;

        bool showKidsWithReflectionHint = false;

        public void OnEnable()
        {
            serObj = new SerializedObject(target);

            reflectionMask = serObj.FindProperty("reflectionMask");
            reflectSkybox = serObj.FindProperty("reflectSkybox");
            clearColor = serObj.FindProperty("clearColor");
        }

        public override void OnInspectorGUI()
        {
            GUILayout.Label("Render planar reflections and use GrabPass for refractions", EditorStyles.miniBoldLabel);

            if (!SystemInfo.supportsRenderTextures)
                EditorGUILayout.HelpBox("Realtime reflections not supported", MessageType.Warning);

            serObj.Update();

            EditorGUILayout.PropertyField(reflectionMask, new GUIContent("Reflection layers"));
            EditorGUILayout.PropertyField(reflectSkybox, new GUIContent("Use skybox"));
            EditorGUILayout.PropertyField(clearColor, new GUIContent("Clear color"));

            showKidsWithReflectionHint = EditorGUILayout.BeginToggleGroup("Show all tiles", showKidsWithReflectionHint);
            if (showKidsWithReflectionHint)
            {
                int i = 0;
                foreach (Transform t in ((PlanarReflection)target).transform)
                {
                    if (t.GetComponent<WaterTile>())
                    {
                        if (i % 2 == 0)
                            EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.ObjectField(t, typeof(Transform), true);
                        if (i % 2 == 1)
                            EditorGUILayout.EndHorizontal();
                        i = (i + 1) % 2;
                    }
                }
                if (i > 0)
                    EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndToggleGroup();

            serObj.ApplyModifiedProperties();
        }

    }
}