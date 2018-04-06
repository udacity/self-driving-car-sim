using UnityEngine;
using UnityEditor;


namespace UnityStandardAssets.Water
{
    [CustomEditor(typeof(WaterBase))]
    public class WaterBaseEditor : Editor
    {
        public GameObject oceanBase;
        private WaterBase waterBase;
        private Material oceanMaterial = null;

        private SerializedObject serObj;
        private SerializedProperty sharedMaterial;


        public SerializedProperty waterQuality;
        public SerializedProperty edgeBlend;

        public void OnEnable()
        {
            serObj = new SerializedObject(target);
            sharedMaterial = serObj.FindProperty("sharedMaterial");
            waterQuality = serObj.FindProperty("waterQuality");
            edgeBlend = serObj.FindProperty("edgeBlend");
        }

        public override void OnInspectorGUI()
        {
            serObj.Update();

            waterBase = (WaterBase)serObj.targetObject;
            oceanBase = ((WaterBase)serObj.targetObject).gameObject;
            if (!oceanBase)
                return;

            GUILayout.Label("This script helps adjusting water material properties", EditorStyles.miniBoldLabel);

            EditorGUILayout.PropertyField(sharedMaterial, new GUIContent("Material"));
            oceanMaterial = (Material)sharedMaterial.objectReferenceValue;

            if (!oceanMaterial)
            {
                sharedMaterial.objectReferenceValue = (Object)WaterEditorUtility.LocateValidWaterMaterial(oceanBase.transform);
                serObj.ApplyModifiedProperties();
                oceanMaterial = (Material)sharedMaterial.objectReferenceValue;
                if (!oceanMaterial)
                    return;
            }

            EditorGUILayout.Separator();

            GUILayout.Label("Overall Quality", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(waterQuality, new GUIContent("Quality"));
            EditorGUILayout.PropertyField(edgeBlend, new GUIContent("Edge blend?"));

            if (waterQuality.intValue > (int)WaterQuality.Low && !SystemInfo.supportsRenderTextures)
                EditorGUILayout.HelpBox("Water features not supported", MessageType.Warning);
            if (edgeBlend.boolValue && !SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.Depth))
                EditorGUILayout.HelpBox("Edge blend not supported", MessageType.Warning);

            EditorGUILayout.Separator();

            bool hasShore = oceanMaterial.HasProperty("_ShoreTex");

            GUILayout.Label("Main Colors", EditorStyles.boldLabel);
            GUILayout.Label("Alpha values define blending with realtime textures", EditorStyles.miniBoldLabel);

            WaterEditorUtility.SetMaterialColor("_BaseColor", EditorGUILayout.ColorField("Refraction", WaterEditorUtility.GetMaterialColor("_BaseColor", oceanMaterial)), oceanMaterial);
            WaterEditorUtility.SetMaterialColor("_ReflectionColor", EditorGUILayout.ColorField("Reflection", WaterEditorUtility.GetMaterialColor("_ReflectionColor", oceanMaterial)), oceanMaterial);

            EditorGUILayout.Separator();

            GUILayout.Label("Main Textures", EditorStyles.boldLabel);
            GUILayout.Label("Used for small waves (bumps), foam and white caps", EditorStyles.miniBoldLabel);

            WaterEditorUtility.SetMaterialTexture("_BumpMap", (Texture)EditorGUILayout.ObjectField("Normals", WaterEditorUtility.GetMaterialTexture("_BumpMap", waterBase.sharedMaterial), typeof(Texture), false), waterBase.sharedMaterial);
            if (hasShore)
                WaterEditorUtility.SetMaterialTexture("_ShoreTex", (Texture)EditorGUILayout.ObjectField("Shore & Foam", WaterEditorUtility.GetMaterialTexture("_ShoreTex", waterBase.sharedMaterial), typeof(Texture), false), waterBase.sharedMaterial);

            Vector4 animationTiling;
            Vector4 animationDirection;

            Vector2 firstTiling;
            Vector2 secondTiling;
            Vector2 firstDirection;
            Vector2 secondDirection;

            animationTiling = WaterEditorUtility.GetMaterialVector("_BumpTiling", oceanMaterial);
            animationDirection = WaterEditorUtility.GetMaterialVector("_BumpDirection", oceanMaterial);

            firstTiling = new Vector2(animationTiling.x * 100.0F, animationTiling.y * 100.0F);
            secondTiling = new Vector2(animationTiling.z * 100.0F, animationTiling.w * 100.0F);

            firstTiling = EditorGUILayout.Vector2Field("Tiling 1", firstTiling);
            secondTiling = EditorGUILayout.Vector2Field("Tiling 2", secondTiling);

            //firstTiling.x = EditorGUILayout.FloatField("1st Tiling U", firstTiling.x);
            //firstTiling.y = EditorGUILayout.FloatField("1st Tiling V", firstTiling.y);
            //secondTiling.x = EditorGUILayout.FloatField("2nd Tiling U", secondTiling.x);
            //secondTiling.y = EditorGUILayout.FloatField("2nd Tiling V", secondTiling.y);

            firstDirection = new Vector2(animationDirection.x, animationDirection.y);
            secondDirection = new Vector2(animationDirection.z, animationDirection.w);

            //firstDirection.x = EditorGUILayout.FloatField("1st Animation U", firstDirection.x);
            //firstDirection.y = EditorGUILayout.FloatField("1st Animation V", firstDirection.y);
            //secondDirection.x = EditorGUILayout.FloatField("2nd Animation U", secondDirection.x);
            //secondDirection.y = EditorGUILayout.FloatField("2nd Animation V", secondDirection.y);

            firstDirection = EditorGUILayout.Vector2Field("Direction 1", firstDirection);
            secondDirection = EditorGUILayout.Vector2Field("Direction 2", secondDirection);

            animationTiling = new Vector4(firstTiling.x / 100.0F, firstTiling.y / 100.0F, secondTiling.x / 100.0F, secondTiling.y / 100.0F);
            animationDirection = new Vector4(firstDirection.x, firstDirection.y, secondDirection.x, secondDirection.y);

            WaterEditorUtility.SetMaterialVector("_BumpTiling", animationTiling, oceanMaterial);
            WaterEditorUtility.SetMaterialVector("_BumpDirection", animationDirection, oceanMaterial);

            Vector4 displacementParameter = WaterEditorUtility.GetMaterialVector("_DistortParams", oceanMaterial);
            Vector4 fade = WaterEditorUtility.GetMaterialVector("_InvFadeParemeter", oceanMaterial);

            EditorGUILayout.Separator();

            GUILayout.Label("Normals", EditorStyles.boldLabel);
            GUILayout.Label("Displacement for fresnel, specular and reflection/refraction", EditorStyles.miniBoldLabel);

            float gerstnerNormalIntensity = WaterEditorUtility.GetMaterialFloat("_GerstnerIntensity", oceanMaterial);
            gerstnerNormalIntensity = EditorGUILayout.Slider("Per Vertex", gerstnerNormalIntensity, -2.5F, 2.5F);
            WaterEditorUtility.SetMaterialFloat("_GerstnerIntensity", gerstnerNormalIntensity, oceanMaterial);

            displacementParameter.x = EditorGUILayout.Slider("Per Pixel", displacementParameter.x, -4.0F, 4.0F);
            displacementParameter.y = EditorGUILayout.Slider("Distortion", displacementParameter.y, -0.5F, 0.5F);
            // fade.z = EditorGUILayout.Slider("Distance fade", fade.z, 0.0f, 0.5f);

            EditorGUILayout.Separator();

            GUILayout.Label("Fresnel", EditorStyles.boldLabel);
            GUILayout.Label("Defines reflection to refraction relation", EditorStyles.miniBoldLabel);

            if (!oceanMaterial.HasProperty("_Fresnel"))
            {
                if (oceanMaterial.HasProperty("_FresnelScale"))
                {
                    float fresnelScale = EditorGUILayout.Slider("Intensity", WaterEditorUtility.GetMaterialFloat("_FresnelScale", oceanMaterial), 0.1F, 4.0F);
                    WaterEditorUtility.SetMaterialFloat("_FresnelScale", fresnelScale, oceanMaterial);
                }
                displacementParameter.z = EditorGUILayout.Slider("Power", displacementParameter.z, 0.1F, 10.0F);
                displacementParameter.w = EditorGUILayout.Slider("Bias", displacementParameter.w, -3.0F, 3.0F);
            }
            else
            {
                Texture fresnelTex = (Texture)EditorGUILayout.ObjectField(
                        "Ramp",
                        (Texture)WaterEditorUtility.GetMaterialTexture("_Fresnel",
                        oceanMaterial),
                        typeof(Texture),
                        false);
                WaterEditorUtility.SetMaterialTexture("_Fresnel", fresnelTex, oceanMaterial);
            }

            EditorGUILayout.Separator();

            WaterEditorUtility.SetMaterialVector("_DistortParams", displacementParameter, oceanMaterial);

            if (edgeBlend.boolValue)
            {
                GUILayout.Label("Fading", EditorStyles.boldLabel);

                fade.x = EditorGUILayout.Slider("Edge fade", fade.x, 0.001f, 3.0f);
                if (hasShore)
                    fade.y = EditorGUILayout.Slider("Shore fade", fade.y, 0.001f, 3.0f);
                fade.w = EditorGUILayout.Slider("Extinction fade", fade.w, 0.0f, 2.5f);

                WaterEditorUtility.SetMaterialVector("_InvFadeParemeter", fade, oceanMaterial);
            }
            EditorGUILayout.Separator();

            if (oceanMaterial.HasProperty("_Foam"))
            {
                GUILayout.Label("Foam", EditorStyles.boldLabel);

                Vector4 foam = WaterEditorUtility.GetMaterialVector("_Foam", oceanMaterial);

                foam.x = EditorGUILayout.Slider("Intensity", foam.x, 0.0F, 1.0F);
                foam.y = EditorGUILayout.Slider("Cutoff", foam.y, 0.0F, 1.0F);

                WaterEditorUtility.SetMaterialVector("_Foam", foam, oceanMaterial);
            }

            serObj.ApplyModifiedProperties();
        }

    }
}