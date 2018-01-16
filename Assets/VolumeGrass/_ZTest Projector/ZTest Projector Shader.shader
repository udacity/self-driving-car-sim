// Upgrade NOTE: replaced '_Projector' with 'unity_Projector'
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

//
// shader for resolving problems with projecting on transparent cutout objects
//
// writen specialy to use with VolumeGrass, but you can use it to pcorectly project on any transparent cutout
//
// note that right projection will work for VolumeGrass ONLY in DEFERRED
// regular transparent cutout objects will work fine with this projector either in forward or deferred
//
Shader "Projector/Projector MultiplyCG"
{ 
	Properties
	{
		_Color ("Main Colour", Color) = (1,1,1,0)
		_ShadowTex ("Cookie", 2D) = "gray" { TexGen ObjectLinear }
		_dist_treshold ("Distance ZTest treshold", Range(0.001,0.5)) = 0.01
		_mod_hgt ("Volume height for Grass", Float) = 0.2 // used only when projecting on VolumeGrass object
	}
	
	Subshader
	{
		Tags { "RenderType"="Transparent"  "Queue"="Transparent+100"}
		Pass
		{
			ZWrite Off
			Offset 1, 1

			Fog { Mode Off }
			
			AlphaTest Less 1
			ColorMask RGB
			Blend One SrcAlpha
			
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma fragmentoption ARB_fog_exp2
			#pragma fragmentoption ARB_precision_hint_fastest
			
			#include "UnityCG.cginc"
			
			//
			// should be defined for using with VolumeGrass as it modify vertices in vertex program
			// if you want to use it with another transparent cutout object - comment define below
			//
			#define MODIFY_VERTICES_BY_COLOR
			
			struct v2f
			{
				float4 pos : SV_POSITION;
				float4 uv_Main : TEXCOORD0;
				float4 uv : TEXCOORD1;
			};

			float4 MyComputeScreenPos (float4 pos) {
				float4 o = pos * 0.5f;
				#if defined(UNITY_HALF_TEXEL_OFFSET)
				o.xy = float2(o.x, o.y*_ProjectionParams.x) + o.w * _ScreenParams.zw;
				#else
				o.xy = float2(o.x, o.y*_ProjectionParams.x) + o.w;
				#endif
				o.zw = pos.zw;
				return o;
			}
			
			sampler2D _ShadowTex;
			sampler2D _CameraDepthTexture; 
			float4 _Color;
			float4x4 unity_Projector;
			float _dist_treshold;
			float _mod_hgt;
			
			v2f vert(appdata_full v)
			{
				v2f o;
				#ifdef MODIFY_VERTICES_BY_COLOR
					v.vertex.xyz -= v.normal * _mod_hgt * v.color.g;
				#endif
				o.pos = UnityObjectToClipPos (v.vertex);
				o.uv_Main = mul (unity_Projector, v.vertex);
				o.uv = MyComputeScreenPos(o.pos);
				COMPUTE_EYEDEPTH(o.uv.z);
				return o;
			}
			
			half4 frag (v2f i) : COLOR
			{
				half4 tex = tex2Dproj(_ShadowTex, UNITY_PROJ_COORD(i.uv_Main)) * _Color;
				
				i.uv.xy=i.uv.xy/i.uv.w;
				float sceneZ=LinearEyeDepth(tex2D(_CameraDepthTexture, i.uv.xy).r);
				tex.a = (i.uv.z+_dist_treshold)<sceneZ ? 1:1-tex.a;
				//tex.rgb = sceneZ;
				//tex.a = 0;
				return tex;
			}
			ENDCG
			
		}
	}
}

