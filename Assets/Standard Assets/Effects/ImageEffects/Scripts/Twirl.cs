using System;
using UnityEngine;

namespace UnityStandardAssets.ImageEffects
{
    [ExecuteInEditMode]
    [AddComponentMenu("Image Effects/Displacement/Twirl")]
    public class Twirl : ImageEffectBase
    {
        public Vector2 radius = new Vector2(0.3F,0.3F);
        [Range(0.0f,360.0f)]
        public float angle = 50;
        public Vector2 center = new Vector2 (0.5F, 0.5F);


        // Called by camera to apply image effect
        void OnRenderImage (RenderTexture source, RenderTexture destination)
        {
            ImageEffects.RenderDistortion (material, source, destination, angle, center, radius);
        }
    }
}
