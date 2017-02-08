Shader "Hidden/BrightPassFilter2"
{
	Properties 
	{
		_MainTex ("Base (RGB)", 2D) = "" {}
	}
	
	CGINCLUDE
	
	#include "UnityCG.cginc"
	
	struct v2f 
	{
		float4 pos : SV_POSITION;
		float2 uv : TEXCOORD0;
	};
	
	sampler2D _MainTex;	
	
	half4 _Threshhold;
		
	v2f vert( appdata_img v ) 
	{
		v2f o;
		o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
		o.uv =  v.texcoord.xy;
		return o;
	} 
	
	half4 fragScalarThresh(v2f i) : SV_Target 
	{
		half4 color = tex2D(_MainTex, i.uv);
		color.rgb = color.rgb;
		color.rgb = max(half3(0,0,0), color.rgb-_Threshhold.xxx);
		return color;
	}

	half4 fragColorThresh(v2f i) : SV_Target 
	{
		half4 color = tex2D(_MainTex, i.uv);
		color.rgb = max(half3(0,0,0), color.rgb-_Threshhold.rgb);
		return color;
	}	

	ENDCG 
	
	Subshader 
	{
		Pass 
 		{
			ZTest Always Cull Off ZWrite Off

			CGPROGRAM

			#pragma vertex vert
			#pragma fragment fragScalarThresh

			ENDCG
		}

		Pass 
 		{
			ZTest Always Cull Off ZWrite Off

			CGPROGRAM

			#pragma vertex vert
			#pragma fragment fragColorThresh

			ENDCG
		}		
	}
	Fallback off
}
