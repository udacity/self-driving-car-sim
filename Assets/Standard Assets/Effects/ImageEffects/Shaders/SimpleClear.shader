// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'



Shader "Hidden/SimpleClear" {
Properties {
	_MainTex ("Base (RGB)", 2D) = "white" {}
}

SubShader {
	Pass {
		ZTest Always Cull Off ZWrite Off

CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"

uniform sampler2D _MainTex;
uniform float4 _MainTex_TexelSize;
 
struct v2f {
	float4 pos : SV_POSITION;
};

v2f vert( appdata_img v )
{
	v2f o;
	o.pos = UnityObjectToClipPos(v.vertex);
	return o;
}

half4 frag (v2f i) : SV_Target
{
	return half4(0,0,0,0);
}
ENDCG
	}
}

Fallback off

}
