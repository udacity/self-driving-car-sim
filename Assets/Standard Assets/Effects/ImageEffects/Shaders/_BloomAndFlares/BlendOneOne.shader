Shader "Hidden/BlendOneOne" {
	Properties {
		_MainTex ("-", 2D) = "" {}
	}
	
	CGINCLUDE

	#include "UnityCG.cginc"
	
	struct v2f {
		float4 pos : SV_POSITION;
		float2 uv : TEXCOORD0;
	};
		
	sampler2D _MainTex;
	half _Intensity;
		
	v2f vert( appdata_img v ) {
		v2f o;
		o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
		o.uv =  v.texcoord.xy;
		return o;
	}
	
	half4 frag(v2f i) : SV_Target {
		return tex2D(_MainTex, i.uv) * _Intensity;
	}

	ENDCG
	
Subshader {

  Pass {
  		BlendOp Add
  		Blend One One
  
	  ZTest Always Cull Off ZWrite Off

      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      ENDCG
  }
}

Fallback off
	
}
