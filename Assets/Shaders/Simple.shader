Shader "Unlit/Simple"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "Queue"="AlphaTest" "RenderType"="Transparent" "IgnoreProjector"="True" }
		Blend One OneMinusSrcAlpha
		AlphaToMask On
		Cull Off

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			float4 _MainTex_ST;

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float4 color : COLOR;
				float3 normal : NORMAL;
				float2 texcoord : TEXCOORD0;
			};
			
			v2f vert (appdata_full v)
			{
				v2f o;
				float4 vertex = mul(unity_ObjectToWorld, v.vertex);
				o.vertex = mul(UNITY_MATRIX_VP, vertex);
				o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
				o.color = v.color;
				o.normal = v.normal;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 color = tex2D(_MainTex, i.texcoord);
				return color;
			}
			ENDCG
		}
	}
}
