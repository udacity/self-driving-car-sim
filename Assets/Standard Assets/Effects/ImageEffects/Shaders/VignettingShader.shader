// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Hidden/Vignetting" {
	Properties {
		_MainTex ("Base", 2D) = "white" {}
		_VignetteTex ("Vignette", 2D) = "white" {}
	}
	
	CGINCLUDE
	
	#include "UnityCG.cginc"
	
	struct v2f {
		float4 pos : SV_POSITION;
		float2 uv : TEXCOORD0;
		float2 uv2 : TEXCOORD1;
	};
	
	sampler2D _MainTex;
	sampler2D _VignetteTex;
	
	half _Intensity;
	half _Blur;

	float4 _MainTex_TexelSize;
		
	v2f vert( appdata_img v ) {
		v2f o;
		o.pos = UnityObjectToClipPos(v.vertex);
		o.uv = v.texcoord.xy;
		o.uv2 = v.texcoord.xy;

		#if UNITY_UV_STARTS_AT_TOP
		if (_MainTex_TexelSize.y < 0)
			 o.uv2.y = 1.0 - o.uv2.y;
		#endif

		return o;
	} 
	
	half4 frag(v2f i) : SV_Target {
		half2 coords = i.uv;
		half2 uv = i.uv;
		
		coords = (coords - 0.5) * 2.0;		
		half coordDot = dot (coords,coords);
		half4 color = tex2D (_MainTex, uv);	 

		float mask = 1.0 - coordDot * _Intensity; 
		
		half4 colorBlur = tex2D (_VignetteTex, i.uv2);
		color = lerp (color, colorBlur, saturate (_Blur * coordDot));
		
		return color * mask;
	}

	ENDCG 
	
Subshader {
 Pass {
	  ZTest Always Cull Off ZWrite Off

      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      ENDCG
  }
}

Fallback off	
} 
