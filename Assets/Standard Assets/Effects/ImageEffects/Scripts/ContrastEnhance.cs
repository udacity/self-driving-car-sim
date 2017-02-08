using System;
using UnityEngine;

namespace UnityStandardAssets.ImageEffects
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]
    [AddComponentMenu("Image Effects/Color Adjustments/Contrast Enhance (Unsharp Mask)")]
    public class ContrastEnhance : PostEffectsBase
	{
        [Range(0.0f, 1.0f)]
        public float intensity = 0.5f;
        [Range(0.0f,0.999f)]
        public float threshold = 0.0f;

        private Material separableBlurMaterial;
        private Material contrastCompositeMaterial;

        [Range(0.0f,1.0f)]
        public float blurSpread = 1.0f;

        public Shader separableBlurShader = null;
        public Shader contrastCompositeShader = null;


        public override bool CheckResources ()
		{
            CheckSupport (false);

            contrastCompositeMaterial = CheckShaderAndCreateMaterial (contrastCompositeShader, contrastCompositeMaterial);
            separableBlurMaterial = CheckShaderAndCreateMaterial (separableBlurShader, separableBlurMaterial);

            if (!isSupported)
                ReportAutoDisable ();
            return isSupported;
        }

        void OnRenderImage (RenderTexture source, RenderTexture destination)
		{
            if (CheckResources()==false)
			{
                Graphics.Blit (source, destination);
                return;
            }

            int rtW = source.width;
            int rtH = source.height;

            RenderTexture color2 = RenderTexture.GetTemporary (rtW/2, rtH/2, 0);

            // downsample

            Graphics.Blit (source, color2);
            RenderTexture color4a = RenderTexture.GetTemporary (rtW/4, rtH/4, 0);
            Graphics.Blit (color2, color4a);
            RenderTexture.ReleaseTemporary (color2);

            // blur

            separableBlurMaterial.SetVector ("offsets", new Vector4 (0.0f, (blurSpread * 1.0f) / color4a.height, 0.0f, 0.0f));
            RenderTexture color4b = RenderTexture.GetTemporary (rtW/4, rtH/4, 0);
            Graphics.Blit (color4a, color4b, separableBlurMaterial);
            RenderTexture.ReleaseTemporary (color4a);

            separableBlurMaterial.SetVector ("offsets", new Vector4 ((blurSpread * 1.0f) / color4a.width, 0.0f, 0.0f, 0.0f));
            color4a = RenderTexture.GetTemporary (rtW/4, rtH/4, 0);
            Graphics.Blit (color4b, color4a, separableBlurMaterial);
            RenderTexture.ReleaseTemporary (color4b);

            // composite

            contrastCompositeMaterial.SetTexture ("_MainTexBlurred", color4a);
            contrastCompositeMaterial.SetFloat ("intensity", intensity);
            contrastCompositeMaterial.SetFloat ("threshold", threshold);
            Graphics.Blit (source, destination, contrastCompositeMaterial);

            RenderTexture.ReleaseTemporary (color4a);
        }
    }
}
