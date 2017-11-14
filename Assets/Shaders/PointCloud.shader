Shader "Unlit/PointCloud"
{
	Properties
	{
		_MainTex ("Texture (RGB)", 2D) = "white" {}
		_Size ("Size", Float) = 0.1
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
			#pragma geometry geom
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			float _Size;

			struct GS_INPUT
			{
				float4 vertex : POSITION;
				float3 normal	: NORMAL;
				float4 color	: COLOR;
				float2 texcoord : TEXCOORD0;
				float2 texcoord1 : TEXCOORD1;
			};

			struct FS_INPUT {
				float4 vertex : SV_POSITION;
				float3 normal : NORMAL;
				float4 color : COLOR;
				float2 texcoord : TEXCOORD0;
			};

			GS_INPUT vert (appdata_full v)
			{
				GS_INPUT o = (GS_INPUT)0;
				o.vertex = v.vertex;
				o.normal = v.normal;
				o.color = v.color;
				return o;
			}


			[maxvertexcount(3)]
			void geom (point GS_INPUT tri[1], inout TriangleStream<FS_INPUT> triStream)
			{
				FS_INPUT pIn = (FS_INPUT)0;
				pIn.normal = mul(unity_ObjectToWorld, tri[0].normal);
				pIn.color = tri[0].color;

				float4 vertex = mul(unity_ObjectToWorld, tri[0].vertex);
				float3 tangent = normalize(cross(float3(0,1,0), pIn.normal));
				float3 up = normalize(cross(tangent, pIn.normal));

				pIn.vertex = mul(UNITY_MATRIX_VP, vertex + float4(tangent * -_Size / 1.5, 0));
				pIn.texcoord = float2(-0.5,0);
				triStream.Append(pIn);

				pIn.vertex = mul(UNITY_MATRIX_VP, vertex + float4(up * _Size, 0));
				pIn.texcoord = float2(0.5,1.5);
				triStream.Append(pIn);

				pIn.vertex = mul(UNITY_MATRIX_VP, vertex + float4(tangent * _Size / 1.5, 0));
				pIn.texcoord = float2(1.5,0);
				triStream.Append(pIn);
			}

			float4 frag (FS_INPUT i) : COLOR
			{
				float4 color = i.color;
				color.a = step(0.5, tex2D(_MainTex, i.texcoord).a);
				return color;
			}
			ENDCG
		}
	}
}
