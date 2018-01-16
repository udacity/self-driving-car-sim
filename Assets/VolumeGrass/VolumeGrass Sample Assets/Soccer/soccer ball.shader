Shader "Soccer Ball" {
	Properties {
		_Color ("Main Color", Color) = (1,1,1,1)
		_SpecColor ("Specular Color", Color) = (0.5, 0.5, 0.5, 1)
		_Shininess ("Shininess", Range (0.03, 1)) = 0.078125
		_MainTex ("Base (RGB) Gloss (A)", 2D) = "white" {}
		_BumpMap ("Normalmap", 2D) = "bump" {}	
		_hgt ("Bottom color height", Float) = 0
		_hgt_len ("Bottom color transition length", Float) = 0
		_hgt_color ("Bottom color value (RGB)", Color) = (0,0,0,1)
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 400
		
		CGPROGRAM
		#pragma surface surf BlinnPhong vertex:vert

		sampler2D _MainTex;
		sampler2D _BumpMap;
		float4 _Color;
		float _Shininess;
		float _hgt;
		float _hgt_len;
		half4 _hgt_color;

		struct Input {
			float2 uv_MainTex;
			float2 uv_BumpMap;
			float3 worldPos;
			float3 col;
		};
		
		void vert (inout appdata_full v, out Input o) {
			UNITY_INITIALIZE_OUTPUT(Input, o);
			o.col=v.color.rgb;
		}		

		void surf (Input IN, inout SurfaceOutput o) {
			half4 tex = tex2D(_MainTex, IN.uv_MainTex);

			half4 c=tex;
			float height_damp=clamp((IN.worldPos.y-_hgt)/_hgt_len,0,1);
			half3 c2=height_damp*(c.rgb-_hgt_color.rgb) + _hgt_color.rgb;
			tex.rgb=c.rgb + _hgt_color.a*(c2.rgb-c.rgb);
			float3 vcol=1-clamp(IN.col*10,0,1);
			vcol*=vcol;
			vcol*=vcol;
			vcol=1-vcol;
			vcol+=_SpecColor.a;
			o.Albedo = tex.rgb * _Color.rgb * vcol;
			o.Gloss = tex.a;
			o.Alpha = tex.a * _Color.a;
			o.Specular = _Shininess;
			o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));
		}
		ENDCG
	} 
	FallBack "Specular"
}
