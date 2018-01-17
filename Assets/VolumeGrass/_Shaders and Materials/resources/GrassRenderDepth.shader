// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "GrassRenderDepth" {
	Properties {
		_MainTex ("", 2D) = "white" {}
		_Cutoff ("", Float) = 0.5
		_Color ("", Color) = (1,1,1,1)
	}
	
	// replacement shader for default opaque objects
	//
	// if you blend VolumeGrass with terrain you may want to change SubShader below
	// from "Opaque" to any custom render type (like "OpaqueForGrass")
	// terrain surface won't be unnecessarily rendered into depth buffer (only details like grass, etc.)
	// BUT - you should then change every "Opaque" shaders you use for object insersecting grass too (marking them "OpaqueForGrass")
	//       so than objects will be rendered here and intersect VolumeGrass right way
	//
    SubShader {
		Tags { "RenderType"="Opaque"}
        Pass { ColorMask 0 } // we don't need to render anything but depth (nothing for color buffer)
//    	Pass { Color (1,0,0,0) }
    }

	// replacement shader for built-in trees' barks
    SubShader {
		Tags { "RenderType"="TreeBark"}
        Pass { ColorMask 0 }
    	//Pass { Color (1,0,0,0) }
    }
    
    // replacement shader for terrain trees' barks
    SubShader {
		Tags { "RenderType"="TreeOpaque" }
		Pass {
			ColorMask 0 // we don't need to render anything but depth (nothing for color buffer)		
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			#include "TerrainEngine.cginc"
			struct v2f {
				float4 pos : POSITION;
				//#ifdef UNITY_MIGHT_NOT_HAVE_DEPTH_TEXTURE
				//float2 depth : TEXCOORD0;
				//#endif
			};
			struct appdata {
			    float4 vertex : POSITION;
			    float4 color : COLOR;
			};
			v2f vert( appdata v ) {
				v2f o;
				TerrainAnimateTree(v.vertex, v.color.w);
				o.pos = UnityObjectToClipPos( v.vertex );
			    //UNITY_TRANSFER_DEPTH(o.depth);
				return o;
			}
			half4 frag( v2f i ) : COLOR {
				// we don't need it at color channels since we rely on native depth buffer access
				return half4(1,1,1,1);
			    //UNITY_OUTPUT_DEPTH(i.depth);
			}
			ENDCG
		}
    }
    
    // replacement shader for transparent cutout shaders
	SubShader {
		Tags { "RenderType"="TransparentCutout" }
		Pass {
		ColorMask 0 // we don't need to render anything but depth (nothing for color buffer)		
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			struct v2f {
			    float4 pos : POSITION;
				float2 uv : TEXCOORD0;
				//#ifdef UNITY_MIGHT_NOT_HAVE_DEPTH_TEXTURE
			    //float2 depth : TEXCOORD1;
				//#endif
			};
			uniform float4 _MainTex_ST;
			v2f vert( appdata_base v ) {
			    v2f o;
			    o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
			    //UNITY_TRANSFER_DEPTH(o.depth);
			    return o;
			}
			uniform sampler2D _MainTex;
			uniform float _Cutoff;
			uniform float4 _Color;
			half4 frag(v2f i) : COLOR {
				half4 texcol = tex2D( _MainTex, i.uv );
				clip( texcol.a*_Color.a - _Cutoff );
				// we don't need it at color channels since we rely on native depth buffer access
				return half4(1,1,1,1);
			    //UNITY_OUTPUT_DEPTH(i.depth);
			}
		ENDCG
		}
	}
	
    // replacement shader for terrain grass
    SubShader {
		Tags { "RenderType"="Grass"}
    	// we need to handle grass animation, so we use existing predefined pass
		// assumig grass won't animate - we could replace below UsePass with some kind of simple TransparentCutout pass (see above)
 		Pass {
			Cull Off
			ColorMask 0 // we don't need to render anything but depth (nothing for color buffer)		
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			#include "TerrainEngine.cginc"
			struct v2f {
				float4 pos : POSITION;
				float4 color : COLOR;
				float2 uv : TEXCOORD0;
				//#ifdef UNITY_MIGHT_NOT_HAVE_DEPTH_TEXTURE
				//float2 depth : TEXCOORD1;
				//#endif
			};
			v2f vert (appdata_full v) {
				v2f o;
				WavingGrassVert (v);
				o.color = v.color;
				o.pos = UnityObjectToClipPos (v.vertex);
				o.uv = v.texcoord;
			    //UNITY_TRANSFER_DEPTH(o.depth);
				return o;
			}
			uniform sampler2D _MainTex;
			uniform float _Cutoff;
			half4 frag(v2f i) : COLOR {
				half4 texcol = tex2D( _MainTex, i.uv );
				float alpha = texcol.a * i.color.a;
				clip( alpha - _Cutoff );
				// we don't need it at color channels since we rely on native depth buffer access
				return half4(1,1,1,1);
			    //UNITY_OUTPUT_DEPTH(i.depth);
			}
			ENDCG
		}
	}
	
    // replacement shader for terrain billboard grass
    SubShader {
		Tags { "RenderType"="GrassBillboard"}
    	// we need to handle grass animation, so we use existing predefined pass
		// assumig grass won't animate - we could replace below UsePass with some kind of simple TransparentCutout pass (see above)
		Pass {
			Cull Off		
			ColorMask 0 // we don't need to render anything but depth (nothing for color buffer)		
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			#include "TerrainEngine.cginc"
			
			struct v2f {
				float4 pos : POSITION;
				float4 color : COLOR;
				float2 uv : TEXCOORD0;
				//#ifdef UNITY_MIGHT_NOT_HAVE_DEPTH_TEXTURE
				//float2 depth : TEXCOORD1;
				//#endif
			};
			
			v2f vert (appdata_full v) {
				v2f o;
				WavingGrassBillboardVert (v);
				o.color = v.color;
				o.pos = UnityObjectToClipPos (v.vertex);
				o.uv = v.texcoord.xy;
			    //UNITY_TRANSFER_DEPTH(o.depth);
				return o;
			}
			uniform sampler2D _MainTex;
			uniform float _Cutoff;
			half4 frag( v2f i ) : COLOR {
				half4 texcol = tex2D( _MainTex, i.uv );
				float alpha = texcol.a * i.color.a;
				clip( alpha - _Cutoff );
				// we don't need it at color channels since we rely on native depth buffer access
				return half4(1,1,1,1);
			   // UNITY_OUTPUT_DEPTH(i.depth);
			}
			ENDCG
		}
    }
    
}