using UnityEngine;
using UnityEditor;

namespace UnityStandardAssets.Water
{
    [CustomEditor(typeof(SpecularLighting))]
    public class SpecularLightingEditor : Editor
    {
        private SerializedObject serObj;
        private SerializedProperty specularLight;

        public void OnEnable()
        {
            serObj = new SerializedObject(target);
            specularLight = serObj.FindProperty("specularLight");
        }

        public override void OnInspectorGUI()
        {
            serObj.Update();

            GameObject go = ((SpecularLighting)serObj.targetObject).gameObject;
            WaterBase wb = (WaterBase)go.GetComponent(typeof(WaterBase));

            if (!wb.sharedMaterial)
                return;

            if (wb.sharedMaterial.HasProperty("_WorldLightDir"))
            {
                GUILayout.Label("Transform casting specular highlights", EditorStyles.miniBoldLabel);
                EditorGUILayout.PropertyField(specularLight, new GUIContent("Specular light"));

                if (wb.sharedMaterial.HasProperty("_SpecularColor"))
                    WaterEditorUtility.SetMaterialColor(
                        "_SpecularColor",
                        EditorGUILayout.ColorField("Specular",
                        WaterEditorUtility.GetMaterialColor("_SpecularColor", wb.sharedMaterial)),
                        wb.sharedMaterial);
                if (wb.sharedMaterial.HasProperty("_Shininess"))
                    WaterEditorUtility.SetMaterialFloat("_Shininess", EditorGUILayout.Slider(
                        "Specular power",
                        WaterEditorUtility.GetMaterialFloat("_Shininess", wb.sharedMaterial),
                        0.0F, 500.0F), wb.sharedMaterial);
            }
            else
                GUILayout.Label("The shader doesn't have the needed _WorldLightDir property.", EditorStyles.miniBoldLabel);

            serObj.ApplyModifiedProperties();
        }

    }
}