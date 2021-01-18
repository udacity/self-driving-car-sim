// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Skybox/Procedural" {
Properties {
	_SunTint ("Sun Tint", Color) = (1, 1, 1, 1)
	_SunStrength ("Sun Strength", Float) = 1.0
	_Color ("Atmosphere Tint", Color) = (.5, .5, .5, 1)
	_GroundColor ("Ground", Color) = (.369, .349, .341, 1)
	_HdrExposure("HDR Exposure", Float) = 1.3

}

SubShader {
	Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
	Cull Off ZWrite Off

	Pass {
		
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag

		#include "UnityCG.cginc"
		#include "Lighting.cginc"


		uniform half _HdrExposure;		// HDR exposure
		uniform half3 _GroundColor;

		half3 _Color;
		half3 _SunTint;
		half _SunStrength;

		// RGB wavelengths
		#define GAMMA .454545
		static const float MN = 2;
		static const float MX = .7;
		#define WR (0.65*lerp(MN, MX, pow(_Color.r,GAMMA)))
		#define WG (0.57*lerp(MN, MX, pow(_Color.g,GAMMA)))
		#define WB (0.475*lerp(MN, MX, pow(_Color.b,GAMMA)))
		//#define WR pow(0.65,GAMMA)
		//#define WG pow(0.57,GAMMA)
		//#define WB pow(0.475,GAMMA)
		static const float3 kInvWavelength = float3(1.0 / (WR*WR*WR*WR), 1.0 / (WG*WG*WG*WG), 1.0 / (WB*WB*WB*WB));
		#define OUTER_RADIUS 1.025
		static const float kOuterRadius = OUTER_RADIUS;
		static const float kOuterRadius2 = OUTER_RADIUS*OUTER_RADIUS;
		static const float kInnerRadius = 1.0;
		static const float kInnerRadius2 = 1.0;

		static const float kCameraHeight = 0.0001;

		#define kRAYLEIGH 0.0025		// Rayleigh constant
		#define kMIE 0.0010      		// Mie constant
		#define kSUN_BRIGHTNESS 20.0 	// Sun brightness

		static const float kKrESun = kRAYLEIGH * kSUN_BRIGHTNESS;
		static const float kKmESun = kMIE * kSUN_BRIGHTNESS;
		static const float kKr4PI = kRAYLEIGH * 4.0 * 3.14159265;
		static const float kKm4PI = kMIE * 4.0 * 3.14159265;
		static const float kScale = 1.0 / (OUTER_RADIUS - 1.0);
		static const float kScaleDepth = 0.25;
		static const float kScaleOverScaleDepth = (1.0 / (OUTER_RADIUS - 1.0)) / 0.25;
		static const float kSamples = 2.0; // THIS IS UNROLLED MANUALLY, DON'T TOUCH

		#define MIE_G (-0.990)
		#define MIE_G2 0.9801


		struct appdata_t {
			float4 vertex : POSITION;
		};

		struct v2f {
				float4 pos : SV_POSITION;
				half3 rayDir : TEXCOORD0;	// Vector for incoming ray, normalized ( == -eyeRay )
				half3 cIn : TEXCOORD1; 		// In-scatter coefficient
				half3 cOut : TEXCOORD2;		// Out-scatter coefficient
   		}; 

		float scale(float inCos)
		{
			float x = 1.0 - inCos;
			return 0.25 * exp(-0.00287 + x*(0.459 + x*(3.83 + x*(-6.80 + x*5.25))));
		}

		v2f vert (appdata_t v)
		{
			v2f OUT;
			OUT.pos = UnityObjectToClipPos(v.vertex);

			float3 cameraPos = float3(0,kInnerRadius + kCameraHeight,0); 	// The camera's current position
		
			// Get the ray from the camera to the vertex and its length (which is the far point of the ray passing through the atmosphere)
			float3 eyeRay = normalize(mul((float3x3)unity_ObjectToWorld, v.vertex.xyz));

			OUT.rayDir = half3(-eyeRay);

			float far = 0.0;
			if(eyeRay.y >= 0.0)
			{
				// Sky
				// Calculate the length of the "atmosphere"
				far = sqrt(kOuterRadius2 + kInnerRadius2 * eyeRay.y * eyeRay.y - kInnerRadius2) - kInnerRadius * eyeRay.y;

				float3 pos = cameraPos + far * eyeRay;
				
				// Calculate the ray's starting position, then calculate its scattering offset
				float height = kInnerRadius + kCameraHeight;
				float depth = exp(kScaleOverScaleDepth * (-kCameraHeight));
				float startAngle = dot(eyeRay, cameraPos) / height;
				float startOffset = depth*scale(startAngle);
				
			
				// Initialize the scattering loop variables
				float sampleLength = far / kSamples;
				float scaledLength = sampleLength * kScale;
				float3 sampleRay = eyeRay * sampleLength;
				float3 samplePoint = cameraPos + sampleRay * 0.5;

				// Now loop through the sample rays
				float3 frontColor = float3(0.0, 0.0, 0.0);
				// WTF BBQ: WP8 and desktop FL_9_1 do not like the for loop here
				// (but an almost identical loop is perfectly fine in the ground calculations below)
				// Just unrolling this manually seems to make everything fine again.
//				for(int i=0; i<int(kSamples); i++)
				{
					float height = length(samplePoint);
					float depth = exp(kScaleOverScaleDepth * (kInnerRadius - height));
					float lightAngle = dot(_WorldSpaceLightPos0.xyz, samplePoint) / height;
					float cameraAngle = dot(eyeRay, samplePoint) / height;
					float scatter = (startOffset + depth*(scale(lightAngle) - scale(cameraAngle)));
					float3 attenuate = exp(-scatter * (kInvWavelength * kKr4PI + kKm4PI));

					frontColor += attenuate * (depth * scaledLength);
					samplePoint += sampleRay;
				}
				{
					float height = length(samplePoint);
					float depth = exp(kScaleOverScaleDepth * (kInnerRadius - height));
					float lightAngle = dot(_WorldSpaceLightPos0.xyz, samplePoint) / height;
					float cameraAngle = dot(eyeRay, samplePoint) / height;
					float scatter = (startOffset + depth*(scale(lightAngle) - scale(cameraAngle)));
					float3 attenuate = exp(-scatter * (kInvWavelength * kKr4PI + kKm4PI));

					frontColor += attenuate * (depth * scaledLength);
					samplePoint += sampleRay;
				}



				// Finally, scale the Mie and Rayleigh colors and set up the varying variables for the pixel shader
				OUT.cIn.xyz = frontColor * (kInvWavelength * kKrESun);
				OUT.cOut = frontColor * kKmESun;
			}
			else
			{
				// Ground
				far = (-kCameraHeight) / (min(-0.00001, eyeRay.y));

				float3 pos = cameraPos + far * eyeRay;

				// Calculate the ray's starting position, then calculate its scattering offset
				float depth = exp((-kCameraHeight) * (1.0/kScaleDepth));
				float cameraAngle = dot(-eyeRay, pos);
				float lightAngle = dot(_WorldSpaceLightPos0.xyz, pos);
				float cameraScale = scale(cameraAngle);
				float lightScale = scale(lightAngle);
				float cameraOffset = depth*cameraScale;
				float temp = (lightScale + cameraScale);
				
				// Initialize the scattering loop variables
				float sampleLength = far / kSamples;
				float scaledLength = sampleLength * kScale;
				float3 sampleRay = eyeRay * sampleLength;
				float3 samplePoint = cameraPos + sampleRay * 0.5;
				
				// Now loop through the sample rays
				float3 frontColor = float3(0.0, 0.0, 0.0);
				float3 attenuate;
				for(int i=0; i<int(kSamples); i++)
				{
					float height = length(samplePoint);
					float depth = exp(kScaleOverScaleDepth * (kInnerRadius - height));
					float scatter = depth*temp - cameraOffset;
					attenuate = exp(-scatter * (kInvWavelength * kKr4PI + kKm4PI));
					frontColor += attenuate * (depth * scaledLength);
					samplePoint += sampleRay;
				}
			
				OUT.cIn.xyz = frontColor * (kInvWavelength * kKrESun + kKmESun);
				OUT.cOut.xyz = clamp(attenuate, 0.0, 1.0);
			}


			return OUT;

		}


		// Calculates the Mie phase function
		half getMiePhase(half eyeCos, half eyeCos2)
		{
			half temp = 1.0 + MIE_G2 - 2.0 * MIE_G * eyeCos;
			// A somewhat rough approx for :
			// temp = pow(temp, 1.5);
			temp = smoothstep(0.0, 0.01, temp) * temp;
			temp = max(temp,1.0e-4); // prevent division by zero, esp. in half precision
			return 1.5 * ((1.0 - MIE_G2) / (2.0 + MIE_G2)) * (1.0 + eyeCos2) / temp;
		}

		// Calculates the Rayleigh phase function
		half getRayleighPhase(half eyeCos2)
		{
			return 0.75 + 0.75*eyeCos2;
		}

		half4 frag (v2f IN) : SV_Target
		{
			half3 col;
			if(IN.rayDir.y < 0.0)
			{
				half eyeCos = dot(_WorldSpaceLightPos0.xyz, normalize(IN.rayDir.xyz));
				half eyeCos2 = eyeCos * eyeCos;
				col = getRayleighPhase(eyeCos2) * IN.cIn.xyz + getMiePhase(eyeCos, eyeCos2) * IN.cOut * _LightColor0.xyz * _SunTint * _SunStrength;
			}
			else
			{
				col = IN.cIn.xyz + _GroundColor * IN.cOut;
			}
			//Adjust color from HDR
			col *= _HdrExposure; 
 
			return half4(col,1.0);

		}
		ENDCG 
	}
} 	


Fallback Off

}
