Shader "Tessellation/Bumped Specular (displacement)" {
Properties {
	_Color ("Main Color", Color) = (1,1,1,1)
	_SpecColor ("Specular Color", Color) = (0.5, 0.5, 0.5, 1)
	_Shininess ("Shininess", Range (0.03, 1)) = 0.078125
	_Parallax ("Height", Range (0.0, 1.0)) = 0.5
	_MainTex ("Base (RGB) Gloss (A)", 2D) = "white" {}
	_BumpMap ("Normalmap", 2D) = "bump" {}
	_ParallaxMap ("Heightmap (A)", 2D) = "black" {}

	_EdgeLength ("Edge length", Range(3,50)) = 10
}
SubShader { 
	Tags { "RenderType"="Opaque" }
	LOD 800
	
CGPROGRAM
#pragma surface surf BlinnPhong addshadow vertex:disp tessellate:tessEdge
#include "Tessellation.cginc"

struct appdata {
	float4 vertex : POSITION;
	float4 tangent : TANGENT;
	float3 normal : NORMAL;
	float2 texcoord : TEXCOORD0;
	float2 texcoord1 : TEXCOORD1;
	float2 texcoord2 : TEXCOORD2;
};

float _EdgeLength;
float _Parallax;

float4 tessEdge (appdata v0, appdata v1, appdata v2)
{
	return UnityEdgeLengthBasedTessCull (v0.vertex, v1.vertex, v2.vertex, _EdgeLength, _Parallax * 1.5f);
}

sampler2D _ParallaxMap;

void disp (inout appdata v)
{
	float d = tex2Dlod(_ParallaxMap, float4(v.texcoord.xy,0,0)).a * _Parallax;
	v.vertex.xyz += v.normal * d;
}

sampler2D _MainTex;
sampler2D _BumpMap;
fixed4 _Color;
half _Shininess;

struct Input {
	float2 uv_MainTex;
	float2 uv_BumpMap;
};

void surf (Input IN, inout SurfaceOutput o) {
	fixed4 tex = tex2D(_MainTex, IN.uv_MainTex);
	o.Albedo = tex.rgb * _Color.rgb;
	o.Gloss = tex.a;
	o.Alpha = tex.a * _Color.a;
	o.Specular = _Shininess;
	o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));
}
ENDCG
}

FallBack "Bumped Specular"
}
