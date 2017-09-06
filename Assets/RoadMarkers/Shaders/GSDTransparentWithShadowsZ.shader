// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "GSD/TranShadowMarker" { 

Properties 
{ 
 	// Usual stuffs
	_Color ("Main Color", Color) = (1,1,1,1)
	_SpecColor ("Specular Color", Color) = (0.5, 0.5, 0.5, 0)
	_Shininess ("Shininess", Range (0.01, 1)) = 0.078125
	_MainTex ("Base (RGB) TransGloss (A)", 2D) = "white" {}
 
	// Bump stuffs
	//_Parallax ("Height", Range (0.005, 0.08)) = 0.02
	//_BumpMap ("Normalmap", 2D) = "bump" {}
	//_ParallaxMap ("Heightmap (A)", 2D) = "black" {}
	
	// Shadow Stuff
	_ShadowIntensity ("Shadow Intensity", Range (0, 1)) = 0.6
} 


SubShader 
{ 
	Tags {
	"Queue"="AlphaTest" 
	"IgnoreProjector"="True" 
	"RenderType"="Transparent"
	}

	LOD 300
	Offset -3,-2


// Main Surface Pass (Handles Spot/Point lights)
CGPROGRAM
		#pragma surface surf BlinnPhong alpha vertex:vert fullforwardshadows approxview


		half _Shininess;

		sampler2D _MainTex;
		float4 _Color;
		//sampler2D _BumpMap;
		//sampler2D _ParallaxMap;
		//float _Parallax;
		
		struct v2f { 
			V2F_SHADOW_CASTER; 
			float2 uv : TEXCOORD1;
		};

		struct Input {
			float2 uv_MainTex;
			//float2 uv_BumpMap;
			//float3 viewDir;
		};

		v2f vert (inout appdata_full v) { 
			v2f o; 
			return o; 
		} 

		void surf (Input IN, inout SurfaceOutput o) {
			// Comment the next 4 following lines to get a standard bumped rendering
			// [Without Parallax usage, which can cause strange result on the back side of the plane]
//			/*half h = tex2D (_ParallaxMap, IN.uv_BumpMap).w;
//			float2 offset = ParallaxOffset (h, _Parallax, IN.viewDir);
//			IN.uv_MainTex += offset;
//			IN.uv_BumpMap += offset;*/
 
			fixed4 tex = tex2D(_MainTex, IN.uv_MainTex);
			o.Albedo = tex.rgb * _Color.rgb;
			o.Gloss = tex.a;
			o.Alpha = tex.a * _Color.a;
//			clip(o.Alpha - _Cutoff);
			o.Specular = _Shininess;
			//o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));
		}
ENDCG



		// Shadow Pass : Adding the shadows (from Directional Light)
		// by blending the light attenuation
		Pass {
			Blend SrcAlpha OneMinusSrcAlpha 
			Name "ShadowPass"
			Tags {"LightMode" = "ForwardBase"}
//			ZWrite On ZTest LEqual Cull Off
			  
			CGPROGRAM 
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fwdbase
			#pragma fragmentoption ARB_fog_exp2
			#pragma fragmentoption ARB_precision_hint_fastest
			#include "UnityCG.cginc" 
			#include "AutoLight.cginc" 
 
			struct v2f { 
				float2 uv_MainTex : TEXCOORD1;
				float4 pos : SV_POSITION;
				LIGHTING_COORDS(3,4)
				float3	lightDir : TEXCOORD2;
			};
 
			float4 _MainTex_ST;
			sampler2D _MainTex;
			float4 _Color;
			float _ShadowIntensity;
 
			v2f vert (appdata_full v)
			{
				v2f o;
                o.uv_MainTex = TRANSFORM_TEX(v.texcoord, _MainTex);
				o.pos = UnityObjectToClipPos (v.vertex);
				o.lightDir = ObjSpaceLightDir( v.vertex );
				TRANSFER_VERTEX_TO_FRAGMENT(o);
				return o;
			}

			float4 frag (v2f i) : COLOR
			{
				float atten = LIGHT_ATTENUATION(i);
				half4 c;
				c.rgb =  0;
				c.a = (1-atten) * _ShadowIntensity * (tex2D(_MainTex, i.uv_MainTex).a); 
				return c;
			}
			ENDCG
		}
	
	
}

FallBack "Transparent/Specular"
}