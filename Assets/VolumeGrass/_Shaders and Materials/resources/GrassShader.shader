//
// Surface shader implementation of volume ray-traced algorithm
// (based on idea found in the paper "Instant Animated Grass" by Ralf Habel et al, published in WSCG Journal 2007)
//
// tomaszek (tomasz stobierski) 2011
//
// d3d9 / glsl (#pragma glsl) - version that benefits esp. from far distance optimization (distant areas are rendered simple way)
// works on every PC with SM3.0 hardware
// works on SOME Macs (works for sure on nVidia GF8000+ cards)
//
Shader "Grass Shader" {
	Properties {
		//
		// keep in mind that not all of properties are used, depending on shader configuration in defines section below
		//
		_Color ("Ground Color (RGB)", Color) = (0.5,0.5,0.5,1)
		_MainTex ("Ground texture (RGB)", 2D) = "black" {}
		_fardistancetiling ("Texture tiling at far distances", Float) = 3
		_far_distance ("Far distance", Float) = 10
		_far_distance_transition ("Far distance transition length", Float) = 4
		_BladesColor ("Blades Color (RGB)", Color) = (0.5,0.5,0.5,1)
		_BladesTex ("Grass blades (RGBA)", 2D) = "black" {}
		_BladesBackColor ("Grass blades back color (RGB)", Color) = (0.32156,0.513725,0.0941176,1.0)
		_BladesBackTex ("Grass blades back texture (RGB)", 2D) = "black" {}
		_view_angle_damper ("Bending for sharp view angles", Range(0,1)) = 0.85
		_blur_distance ("Blades blurring distance", Float) = 2
		_blur_distance_transition ("Blades blurring transition length", Float) = 1
		_MIP_distance_limit ("Blades blurring MIP distance limit", Float) = 2
		_glowing_value ("Glow to contrast ballance", Range(-0.5,0.5)) = 0.1
		_bottom_coloring_value ("Slice bottom coloring (RGBA)", Color) = (0,0,0,0.5)
		_bottom_coloring_noise_tiling ("Slice bottom coloring noise tiling", Vector) = (0.2, 0.2, 0, 0)
		_bottom_coloring_distance_fade ("Slice bottom coloring distance fade", Float) = 25
		_bottom_coloring_far ("Slice bottom coloring for far distance", Range(0,1)) = 0.5
		_bottom_coloring_border_damp ("Slice bottom coloring border damp", Range(0,1)) = 0.5
		_coloring_value ("Noise coloring (RGBA)", Color) = (0, 0, 0, 1)
		_coloring_noise_tiling ("Noise coloring tiling", Vector) = (0.1, 0.1, 0, 0)
		_NoiseTex ("Noise", 2D) = "black" {}
		_NoiseTex2 ("Noise2", 2D) = "black" {}
		_NoiseTexHash ("Noise for slices hash", 2D) = "black" {}
		_wind_amp ("Wind amplitude", Float) = 0.02
		_wind_speed ("Wind speed", Float) = 0.1
		_wind_freq ("Wind frequency (NoiseTex tiling)", Float) = 1
		_border_fray_strength ("Border fray strength", Range(0,1)) = 0.5
		_border_fray_tiling ("Border fray noise tiling", float) = 1
		_Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
		_mblur_val ("Linear motion blur like effect", Range(0,1)) = 0
		_mblur_distance ("Motion blur distance", Float) = 3
		_mblur_transition ("Motion blur distance transition", Float) = 2
		
		_mblur_dir ("Motion blur direction - (cam steered) (xyz)", Vector) = (0,-1,0.05,0)
		_tiling_factor ("(internal use)", Float) = 1
		PLANE_NUM ("(internal use)", Float) = 8
		GRASS_SLICE_NUM ("(internal use)", Float) = 8
		
		PLANE_NUM_INV ("(internal use)", Float) = 0.125
		GRASS_SLICE_NUM_INV ("(internal use)", Float) = 0.125
		GRASSDEPTH ("(internal use)", Float) = 0.125
		PREMULT ("(internal use)", Float) = 1.0
		UV_RATIO ("(internal use)", Float) = 1.0

		_mod ("(internal use)", Float) = 0
		
		_AuxTex ("Aux Texture", 2D) = "black" {}
		_auxtex_tiling ("AuxTexture tiling", Vector) = (0.1, 0.1, 0, 0)
		_auxtex_modulator_value ("AuxTexture modulator value", Float) = 0.15
	}

	//
	//
	// Full version of shader suitable for new GPU hardware (SM3.0 + new OpenGL(SL) compatible cards)
	//
	//
	SubShader {
		Tags { "Queue"="Geometry+250" "IgnoreProjector"="False" "RenderType"="VolumeGrass"} // custom render type specified if you need to write special shader replacement functionality
		LOD 700

		CGPROGRAM
		#pragma surface surf Lambert alphatest:_Cutoff vertex:vert fullforwardshadows
		#pragma target 3.0
		// (too slow on mobile hardware)
		#pragma exclude_renderers gles
		//#pragma debug
		
		// we need tex2Dlod in conditionals for GLSL
		#pragma glsl
		
		#include "UnityCG.cginc"

		//////////////////////////////////////////////////////////////////////////////////////////////////
		//
		// defines section (configuration of grass functionality against performance)
		//
		
		//
		// Max number of iterations (intersections of ray vs planes in tangent space in pesimistic case)
		// generaly iterations shouldn't reach the limit as far as grass blades texture is "dense" enough and angle of view won't be much grazing
		// to lower averange number of iterations you should consider lowering "Slice planes per world unit" in VolumeGrass properties (this is because traced ray has better chance to hit the ground and break computations)
		//
		// on low MAX_RAYDEPTH set on BACK_COLOR_TEXTURE below to improve apperance of unresolved pixels
		//
		#define MAX_RAYDEPTH 4
		
		//
		// Turn it on if you want to see how many iterations per pixel occur
		// full red means that loop iteration for that pixel reached DEBUG_RED_LIMIT
		// full green means that shader breaks on first iteration
		// white - shader exits just at the beginning without performing any unecessary operation
		// so, white - BEST, more green - GOOD, more red - BAD :) (esp. on large and very jagged areas when MAX_RAYDEPTH is high)
		//
		// furthermore, for BEST PERFORMANCE we should try to keep it as "smooth" as possible
		// so adjanced pixels should have the most similar values (the same color at large areas)
		// GPU parallel computing will be best optimized then (as "the slowest" pixel thread makes other threads to stall)
		// you may simply assume that single red pixel chokes back all adjanced greens (as they "wait" for slow, red neighbour)
		//
		// unfortunately, due to specificity of the shader, you'll quickly find out that making it "smooth" is almost impossible :(
		// however - good practice is to make grass slices texture dense (fully opaque, at least at the bottom part of it)
		//
		//#define DEBUG_PERFORMANCE
		#define DEBUG_RED_LIMIT 6
		
		//
		// if not defined ground will be filled with empty color, MainTex will used only for far distances
		//
		#define FILL_GROUND
		// if FILL_GROUND defined and FILL_GROUND_BY_TEX defined, ground will be filled with MainTex
		// if FILL_GROUND defined and FILL_GROUND_BY_TEX not defined, ground will be filled with _bottom_coloring_value.rgb
		//#define FILL_GROUND_BY_TEX
		
		//
		// If defined _BladesBackTex will be used to fill remaining opacity + _BladesBackColor blened multiply
		// (when we reach max number of iterations and opacity is still below 100%)
		// if not defined _BladesBackColor alone will be used to fill remaining opacity
		// 
		#define BACK_COLOR_TEXTURE
		
		//
		// When transparency gets over this value we stop increasing ray length used for z-test (intersection of other objects vs grass)
		// you may treat it as "internal" alpha cutoff value for pixels that have to be discarded (and reveal underlaying objects geometry)
		//
		#define TRANSPARENCY_ZTEST_VALUE 0.6
		//
		// When transparency gets over this value we break fragment program execution (before reaching MAX_RAYDEPTH iterations)
		// we don't need to lookup further as we consider current pixel value "opaque"
		// lower values leads to performance gain when viewing at grazing angles at cost of fidelity loss
		// however it's hardly possible to enhance speed basing on this factor (look at DEBUG_PERFORMANCE comment)
		//
		#define TRANSPARENCY_BREAK_VALUE 0.95
		
		//
		// If you want to save a few GPU ticks per iteration you may try to comment define below,
		// but distinct patterns become apparent over large areas
		// hashing (when CUSTOM_HASH_FUNCTION is disabled) cost 1 texture lookup per iteration, but tex access is based on small interpolated bitmap
		//
		#define HASH_SLICES
		//
		// you may define your own (non hash texture dependent) hash function if result will be acceptable and custom function will be faster
		// function(rayPos_tmp.x) should give possibly random value
		#define CUSTOM_HASH_FUNCTION (frac(rayPos_tmp.x)*rayPos_tmp.x)
		//#define CUSTOM_HASH_FUNCTION (sin(rayPos_tmp.x))
		
		//
		// Fades out grass slices that are parallel to view direction
		// this hides small artifacts of grass blades texture viewed at grazing angles
		// seting this off saves a few ALUs per iteration
		//
		#define FADE_PARALLELS
		
		//
		// Define it for precise z-test cutoff (intersection of other objects vs grass)
		// disabling this define will save a few GPU ticks
		// note that this corector has nothing to do with buggy z-testing when uv space distances don't fit real space distances (UVs stretched at slopes)
		// this corector also helps a bit with far distance optimization, so turning it off when moving thru large grass areas may result in a little loss of performance
		//
		// if your mesh is highly tesselated this corector shouldn't be needed (distance interpolation passed from vertex shader is more accurate in such case)
		//
		#define ZTEST_CORECTOR		
		
		//
		// When defined you'll be able to adjust "distance" blur (costs a few ALUs + 1 tex lookup per iteration)
		// this can finetune grass apperance at far distance as we filter noise of low level MIP
		// you'll probably want to turn it off using depth of field filter (as DOF handles bluring itself)
		//
		// keep in mind:
		//
		// A. setting high blurring distance/transition (at level of far distance) will unnecessarily drag performance, so comment DISTANCE_BLURING define
		//
		// B. when blurring distance/transition are low (close areas are much blurred) disabling this feature probably won't increase performance
		// because 2 tex lookups for high MIPs could be cheaper than 1 tex lookup for MIP 0 (esp. when tex is of high resolution like 1024x1024)
		//
		// this also means that using smaller blades tex with smaller number of slices will work faster than using big textures
		// the only drawback is that using smaller blades tex is prone to "patternization" when we decrease number of slices in blades texture
		// if we don't decrease slices number and the same time decrease texture size we loose details (blades get blurred on camera close-ups)
		//
		#define DISTANCE_BLURING
		
		//
		// When defined you'll be able to adjust "glowing" effect (based on mixing with GLOWING_MIP_LEVEL MIP map level)
		// (costs another tex lookup per iteration)
		// NOTE: holding "Glow contrast" parameter low makes possible to enhance contrast of grass blades
		//
		//#define GLOWING
		#define GLOWING_MIP_LEVEL 4
		
		// when you don't use  DISTANCE_BLURING nor GLOWING
		// - disable mipmaping in blades/blades back textures to save mem
		// and uncheck read/write enabled for these textures
		
		//
		// Gradually drakening of grass slice bottom thru properties _bottom_coloring_value, _bottom_coloring_noise_tiling, _bottom_coloring_far
		// it's good for fine tuning grass apperance, esp. by adding spatial modulator (if BOTTOM_COLORING_NOISE defined)
		// costs: few ALUs + 1 tex lookup per pixel (if BOTTOM_COLORING_NOISE defined)
		//        few ALUs per iteration
		//
		// NOTE: tex modulation is defined by NoiseTex.b channel
		//       if you don't use BOTTOM_COLORING_NOISE you may get similar results by coloring grass slice bottoms in editing software
		//
		#define BOTTOM_COLORING
		#define BOTTOM_COLORING_NOISE_DRIVEN
		//
		// when you look at grass at very sharp, near vertical, angles you will notice
		// grid structure filled with dark cells
		// (they are dark by default, because BOTTOM_COLORING is defined and its color is defined dark and its value is set mid-high by alpha value of _bottom_coloring_value)
		// to weaken this effect and better hide grid structure visible at sharp angles,
		// the following parameters are:
		//
		// the higher radius multiplier, the smaller is radius of bottom coloring damped area
		// (setting it to 1 will completely damp bottom coloring effect on whole field of view at sharp angles)
 	 	#define BOTTOM_COLORING_SHARP_ANGLE_RADIUS_MULTIPLIER 6
 	 	// the higher below threshold, the lower damping effect is
 	 	// (for 0.0 the center of spot at 90 viewing angle will be completely uncolored in the bottom)
 	 	// (for 0.08 - default value, the spot center is still bottom colored, but not at 100% strength, so dark grid is not so distinct)
 	 	#define BOTTOM_COLORING_SHARP_ANGLE_LOW_THRESHOLD 0.08
		
		//
		// Coloring with _coloring_value by NoiseTex.a channel and properties _coloring_value, _coloring_noise_tiling
		// color blending is of MULTIPLY type (+_coloring_value.a)
		// (costs 1 tex lookup + a few ALUs per pixel)
		//
		#define COLORING
		// if defined coloring will be based on ADDITIVE blending instead of MULTIPLY
		//#define COLORING_ADDITIVE
		
		//
		// Defining wind movement thru NoiseTex.rg and properties _wind_amp, _wind_speed,_wind_freq
		// (costs 1 tex lookup + a few ALUs per iteration)
		//
		#define WIND
		
		//
		// Enables border fray - works on sidewall borders viewed from outside and areas of reduced height (transition between grass full height and zero height)
		// makes volume borders look not so "straight", but gently frayed
		// this cuts sidewalls bottom pixels vie _NoiseTex.r channel and properties _border_fray_strength, _border_fray_tiling
		// costs 1 tex lookup + a few ALUs per pixel
		//
		// TIP: when you fray borders try to adjust _bottom_coloring_border_damp property too
		//      (this cuts bottom coloring for border bottoms so that fray holes are not so distinct on ground geometry behind)
		//
		#define BORDER_FRAY
		
		//
		// pseudo motion blur effect (look at SteerCamTarget script to get idea how to use it basing on camera movement)
		//
		//#define MBLUR
		
		//
		// Below you can override default NoiseTex channel bindings and noise textures used
		// (so, for example, wind X/Y could be driven by noise channels other than red/green,
		//  and can use _NoiseTex or _NoiseTex2, etc.)
		//
		#define NOISE_CHANNEL_WINDX r
		#define NOISE_CHANNEL_WINDY g
		#define NOISE_CHANNEL_BCOLORING b
		#define NOISE_CHANNEL_COLORING a
		#define NOISE_CHANNEL_FRAY r
		
		#define NOISE_WIND_TEX _NoiseTex2 // texture binding (_NoiseTex or _NoiseTex2)
		#define NOISE_BCOLORING_TEX _NoiseTex2
		#define NOISE_COLORING_TEX _NoiseTex2
		#define NOISE_FRAY_TEX _NoiseTex
		
		//
		// IMPORTANT NOTE: look at the end of SubShader to tweak shadows receiving for forward rendering
		//
		
		//
		// EOF defines section
		//
		//////////////////////////////////////////////////////////////////////////////////////////////////

		float PLANE_NUM;
		float GRASS_SLICE_NUM;
		
		float PLANE_NUM_INV;
		float GRASS_SLICE_NUM_INV;
		float GRASSDEPTH;
		float PREMULT;
		
		float UV_RATIO;

		half4 _Color;
		half4 _BladesColor;
		sampler2D _MainTex;
		sampler2D _BladesTex;
		sampler2D _BladesBackTex;
		half4 _BladesBackColor;
		sampler2D _NoiseTex;
		sampler2D _NoiseTex2;
		sampler2D _NoiseTexHash;		
		sampler2D _GrassDepthTex;
		//
		// if you turn this shader into pure transparent (at cost of realtime shadows and right z-sort)
		// you may use _CameraDepthTexture instead of _GrassDepthTex
		// in deferred rendering _CameraDepthTexture is always availbalbe for shader, and grass as an transparent won't be rendered into z-buffer
		// (this case SetupGrassForRendering script and second camera won't be needed)
		//
		// one may try do the trick to use _CameraDepthTexture with 2 cameras setup
		// first one renders whole scene WITHOUT grass in deferred so we have depth info (_CameraDepthTexture) by definition,
		// second one renders only grass and rely on _CameraDepthTexture
		// there is however bug second camera can't get depth info from first one even if clearflags is set to "don't clear"
		// dig into forums to get discussion between NoiseCrime and somebody else (can't remember this guy)
		// for possible workaround, then _GrassDepthTex would not be needed at all (SetupGrassForRendering won't have to add dedicated camera only setup clipping params form main camera)
//		sampler2D _CameraDepthTexture; 

		float _ZBufferParamA;
		float _ZBufferParamB;
		float _ZBufferFarClip;
		float _fardistancetiling;
		float _view_angle_damper;
		
		float _mod;
		float _tiling_factor;
		float _far_distance;
		float _far_distance_transition;
		float _blur_distance;
		float _blur_distance_transition;
		float _MIP_distance_limit;
		float _glowing_value;
		half4 _bottom_coloring_value;
		float4 _bottom_coloring_noise_tiling;
		float _bottom_coloring_far;
		float _bottom_coloring_distance_fade;
		float _bottom_coloring_border_damp;
		half4 _coloring_value;
		float4 _coloring_noise_tiling;
		float _wind_amp;
		float _wind_speed;
		float _wind_freq;
		
		float _border_fray_strength;
		float _border_fray_tiling;
		
		float _mblur_val;
		float _mblur_distance;
		float _mblur_transition;
		float4 _mblur_dir;
		
		struct Input {
			float2 uv_MainTex;

			float3 viewDir;
			float3 worldPos;
			float4 screenPos;
			
			float4 params;
		};

		void vert (inout appdata_full v, out Input o) {
			UNITY_INITIALIZE_OUTPUT(Input, o);
			COMPUTE_EYEDEPTH(o.params.x);
			o.params.yz=v.color.rg;
			
			float height=GRASSDEPTH/UV_RATIO;
			v.vertex.xyz -= v.normal * height * v.color.g;
		}

		#ifdef ZTEST_CORECTOR
			#define ZTEST_MULTIPLICATOR ((IN.params.x/dist) * _tiling_factor)
		#else
			#define ZTEST_MULTIPLICATOR _tiling_factor
		#endif
		
		void surf (Input IN, inout SurfaceOutput o) {
			clip(0.999-IN.params.z);
			o.Normal=float3(0,0,1);

			#ifdef ZTEST_CORECTOR
				float dist=distance(IN.worldPos, _WorldSpaceCameraPos);
				float distanceT=clamp((dist-_far_distance)/_far_distance_transition,0,1);
			#else
				float distanceT=clamp((IN.params.x-_far_distance)/_far_distance_transition,0,1);
			#endif
			half3 base_col=tex2D(_MainTex, IN.uv_MainTex*_fardistancetiling).rgb;
			
		 	float zoffset = IN.params.y + IN.params.z;
		 	zoffset = (zoffset>1) ? 1 : zoffset;
			
			#ifdef BOTTOM_COLORING
				float bottom_cut=_bottom_coloring_value.a*4*(1-zoffset*_bottom_coloring_border_damp);
				#ifdef BOTTOM_COLORING_NOISE_DRIVEN
					bottom_cut*=tex2Dlod(NOISE_BCOLORING_TEX, float4(IN.uv_MainTex*_bottom_coloring_noise_tiling.xy+_bottom_coloring_noise_tiling.zw,0,0)).NOISE_CHANNEL_BCOLORING;
				#endif
 				float cval=1-bottom_cut*_bottom_coloring_far;
 				base_col=_bottom_coloring_value.rgb+cval*(base_col - _bottom_coloring_value.rgb);
			#endif
			#ifdef COLORING
				float cval2=tex2D(NOISE_COLORING_TEX, IN.uv_MainTex*_coloring_noise_tiling.xy+_coloring_noise_tiling.zw).NOISE_CHANNEL_COLORING*_coloring_value.a;
				#ifdef COLORING_ADDITIVE
					half3 coloring=_coloring_value.rgb*cval2;
	 				base_col+=coloring;
				#else
					half3 coloring=_coloring_value.rgb*2*cval2 + (1-cval2);
	 				base_col*=coloring;
				#endif
			#endif
		 	if (distanceT>0.99) {
		 		#ifdef DEBUG_PERFORMANCE
		 			o.Albedo=0;
		 			o.Emission.rgb=1;
		 		#else
			 		o.Albedo=base_col*_Color.rgb*2;
		 		#endif
				o.Alpha=1;//0.999-IN.params.z;
				return;
	 		}
			#ifdef BOTTOM_COLORING
				#ifdef ZTEST_CORECTOR
					bottom_cut*=(1-dist/_bottom_coloring_distance_fade);
				#else
					bottom_cut*=(1-IN.params.x/_bottom_coloring_distance_fade);
				#endif	
			#endif	 		
	 		
			float3 sceneUVZ;
			sceneUVZ.xy = IN.screenPos.xy / IN.screenPos.w;
			// custom LinearEyeDepth() parametrization
			sceneUVZ.z = 1.0 / ((_ZBufferParamA  * tex2Dlod(_GrassDepthTex, float4(sceneUVZ.xy,0,0)).r) + _ZBufferParamB );
			//sceneUVZ.z = 1.0 / ((_ZBufferParamA * tex2Dlod(_CameraDepthTexture, float4(sceneUVZ.xy,0,0)).r) + _ZBufferParamB );
			sceneUVZ.z=(sceneUVZ.z>_ZBufferFarClip)?10000:sceneUVZ.z; // don't clip beyond depth buffer distance
			
	 		#ifdef DISTANCE_BLURING
				float4 distanceS;
				distanceS.y = (IN.params.x-_blur_distance)/_blur_distance_transition;
				distanceS.y = modf( clamp(distanceS.y, 0, _MIP_distance_limit), distanceS.x);
				distanceS.z = 1-distanceS.y;
				distanceS.w = distanceS.x+1;
			#endif
			
			float3 EyeDirTan = -normalize(IN.viewDir); // eye vector in tangent space
 	 		float angle_fade=EyeDirTan.z;
 	 		
 	 		#define BOTTOM_COLORING_SHARP_ANGLE_LOW_THRESHOLD_VAL (1.0 + BOTTOM_COLORING_SHARP_ANGLE_LOW_THRESHOLD)
			#ifdef BOTTOM_COLORING 	 		
				bottom_cut*=clamp((BOTTOM_COLORING_SHARP_ANGLE_LOW_THRESHOLD_VAL + angle_fade)*BOTTOM_COLORING_SHARP_ANGLE_RADIUS_MULTIPLIER, 0, 1);
			#endif
 	 		
 	 		angle_fade*=angle_fade;
 	 		angle_fade=1-angle_fade;
 	 		
			angle_fade=(angle_fade<0.1) ? 0.1 : angle_fade;
			EyeDirTan.z*=angle_fade*_view_angle_damper+(1-_view_angle_damper);
			#ifdef MBLUR
				if (_mblur_val>0.02) {
					_mblur_val*=1-clamp((IN.params.x-_mblur_distance)/_mblur_transition, 0, 1);
					EyeDirTan-=_mblur_val*(EyeDirTan + _mblur_dir.xyz);
				}
			#endif
			#ifdef ZTEST_CORECTOR
				EyeDirTan=normalize(EyeDirTan);
			#endif
			
			float3 EyeDirTanAbs=abs(EyeDirTan);
			#ifdef FADE_PARALLELS
				float2 fade_parallels=angle_fade*EyeDirTanAbs.xy-angle_fade+1;
				fade_parallels+=IN.params.z*(1-fade_parallels);
			#endif
			#ifdef BOTTOM_COLORING
				bottom_cut*=GRASS_SLICE_NUM;
			#endif
			
		 	float3 rayPos = float3(IN.uv_MainTex, -zoffset*GRASSDEPTH);
		 	float hgt=GRASSDEPTH*IN.params.z;

			float rayLength=0;
			float3 delta_next=float3(PLANE_NUM_INV,PLANE_NUM_INV,GRASS_SLICE_NUM_INV);
			
			// evaluated pixel color
			half4 c = half4(0.0,0.0,0.0,0.0);
			
		 	float3 rayPosN=float3(rayPos.xy*PLANE_NUM, rayPos.z*GRASS_SLICE_NUM);
			float3 delta=-frac(rayPosN);
			delta=(EyeDirTan>0) ? frac(-rayPosN) : delta;
	 		delta*=delta_next;
	 		delta_next/=EyeDirTanAbs;
	 		delta/=EyeDirTan;
			delta.z=(rayPos.z<-0.001)?delta.z:delta_next.z;
						
			float2 _uv;
			half4 _col;
			int hitcount;
			bool zhit=false;
		 	for(hitcount=0; hitcount < MAX_RAYDEPTH; hitcount++) {
		 	
		 		if ((delta.z<delta.x) && (delta.z<delta.y)) {
		 			rayLength=(c.w>TRANSPARENCY_ZTEST_VALUE) ? rayLength : (rayLength+delta.z);
		 			#ifdef FILL_GROUND
			 			rayPos+=delta.z*EyeDirTan;
			 			#ifdef FILL_GROUND_BY_TEX
		 					_col=tex2Dlod(_MainTex,float4(rayPos.xy,0, 0 ));
		 					_col.rgb*=_Color.rgb*2;
		 				#else
		 					_col.rgb=_bottom_coloring_value.rgb;
		 				#endif
	 					_col.w=(1-zoffset);
		 				#ifdef BOTTOM_COLORING
		 					cval=1+rayPos.z*bottom_cut;
		 					cval=clamp(cval,0,1);
			 				_col.rgb=_bottom_coloring_value.rgb+cval*(_col.rgb - _bottom_coloring_value.rgb);
		 				#endif
		 				c=(rayPos.z>-0.001) ? c : (c+(1-c.w)*_col);
		 			#endif
		 			zhit=true;
					break;
		 		} else {
		 			bool xy_flag=delta.x<delta.y;

		 			float delta_tmp=xy_flag ? delta.x : delta.y;

		 			rayLength=(c.w>TRANSPARENCY_ZTEST_VALUE) ? rayLength : (rayLength+delta_tmp);
		 			rayPos+=delta_tmp*EyeDirTan;
		 			
		 			float3 rayPos_tmp = xy_flag ? rayPos.xyz : rayPos.yxz;
		 			
					#ifdef WIND
						float4 wtmp=tex2Dlod(NOISE_WIND_TEX, float4(rayPos.xy*_wind_freq+_Time.xx*_wind_speed,0,0));
						float wind_offset=xy_flag ? wtmp.NOISE_CHANNEL_WINDX : wtmp.NOISE_CHANNEL_WINDY;
						wind_offset-=0.5;
						wind_offset*=_wind_amp;
						wind_offset*=1+rayPos.z*GRASS_SLICE_NUM;
					#endif
		 			
		 			#ifdef HASH_SLICES
		 				#ifdef CUSTOM_HASH_FUNCTION
							#define HASH_OFFSET CUSTOM_HASH_FUNCTION
						#else
		 					float2 htmp=tex2Dlod(_NoiseTexHash, float4(rayPos_tmp.x+0.013,0,0,0)).rg;
							float HASH_OFFSET=xy_flag ? htmp.x : htmp.y;
						#endif
					#else
						#define HASH_OFFSET 0
					#endif
					
					#ifdef WIND
						_uv=rayPos_tmp.yz+float2(HASH_OFFSET+wind_offset,rayPos_tmp.x*PREMULT+hgt);
					#else
						_uv=rayPos_tmp.yz+float2(HASH_OFFSET,rayPos_tmp.x*PREMULT+hgt);
					#endif
					#ifdef DISTANCE_BLURING
				 		_col=distanceS.z * tex2Dlod(_BladesTex, float4(_uv, 0, distanceS.x ));
				 		_col+=distanceS.y * tex2Dlod(_BladesTex,float4(_uv, 0, distanceS.w )); // distanceS.w==distanceS.x+1
						float mipcut=clamp(-rayPos.z*70,0,1);
		 				_col.a*=mipcut;
					#else
				 		_col=tex2Dlod(_BladesTex, float4(_uv, 0, 0 ));
					#endif
					_col.rgb*=_col.a;
					#ifdef GLOWING
						#ifndef DISTANCE_BLURING
							float mipcut=clamp(-rayPos.z*70,0,1);
						#endif
						_col+=_glowing_value*(tex2Dlod(_BladesTex,float4(_uv, 0, GLOWING_MIP_LEVEL )) - _col)*mipcut;
					#endif
		 			#ifdef FADE_PARALLELS
		 				float fade_parallelsXY=xy_flag ? fade_parallels.x : fade_parallels.y;
		 				_col*=fade_parallelsXY;
		 			#endif
		 					 			
		 			delta.xyz=xy_flag ? float3(delta_next.x, delta.yz-delta.x) : float3(delta.x-delta.y, delta_next.y, delta.z-delta.y);
		 			
		 			#ifdef BOTTOM_COLORING
		 				cval=1+rayPos.z*bottom_cut;
		 				cval=clamp(cval,0,1);
	 					half3 ca=_bottom_coloring_value.rgb*_col.a;
		 				_col.rgb=ca+cval*(_col.rgb - ca);
		 			#endif
		 			
		 			c+=(1-c.w)*_col;
		 		}
		 		if (c.w>=TRANSPARENCY_BREAK_VALUE) break;
			}
			#ifdef MBLUR
	 			rayLength*=(1-_mblur_val);
	 		#endif
			float partZ = IN.params.x + rayLength * ZTEST_MULTIPLICATOR;
			clip(sceneUVZ.z-partZ);
			
			#ifdef BACK_COLOR_TEXTURE
				_uv.y*=GRASS_SLICE_NUM;
				#ifdef DISTANCE_BLURING
 					_col=tex2Dlod(_BladesBackTex, float4(_uv, 0, distanceS.x ));
 				#else
 					_col=tex2Dlod(_BladesBackTex, float4(_uv, 0, 0 ));
 				#endif
	 			_col.rgb*=_BladesBackColor.rgb;
 			#else
	 			_col.rgb=_BladesBackColor.rgb;
	 			_col.a=1;
 			#endif
 			#ifdef BOTTOM_COLORING
 				_col.rgb=_bottom_coloring_value.rgb+cval*(_col.rgb - _bottom_coloring_value.rgb);
 			#endif			
 			c=zhit ? c : (c+(1-c.w)*_col);
	 						
	 		#ifdef DEBUG_PERFORMANCE
	 			float ht=1.0*hitcount/DEBUG_RED_LIMIT;
	 			ht=clamp(ht,0,1);
	 			o.Albedo=0;
	 			o.Emission.rgb=float3(clamp(ht*2,0,1),clamp(2-ht*2,0,1),0);
	 			o.Alpha=1;
	 		#else
				#ifdef COLORING
					#ifdef COLORING_ADDITIVE
	 					c.rgb+=coloring;
	 				#else
	 					c.rgb*=coloring;
	 				#endif
				#endif
				#ifdef BORDER_FRAY
					if (IN.params.z>0) {
						c.w*=IN.params.z*(tex2Dlod(NOISE_FRAY_TEX,float4(rayPos.xy*_border_fray_tiling, 0,0)).NOISE_CHANNEL_FRAY-_border_fray_strength*2) + 1;
					}
				#endif
				c.rgb*=_BladesColor.rgb*2;
				base_col*=_Color.rgb*2;
			 	o.Albedo=(c.rgb+distanceT*(base_col-c.rgb));
				o.Alpha=c.w;
			#endif
		}
		ENDCG
		
		//////////////////////////////////////////////////////////////////////////
		//
		// shadow casting/collecting
		//
		// note that there is NO FULL shadow caster/collector passes defined for VolumeGrass
		// shadow casting is realized by rendering solid geometry (pass used from fallback, but not always work and grass as shadow caster is not very good idea due to performance issues)
		// shadow collecting is realized as solid geometry below (as transparent cutout in deferred rendering)
		//
		// in DEFERRED rendering shadows are collected automaticaly (separate pass doesn't have to be run for this)
		// so below pass does not make any difference
		// as in deferred - you can't disable receiving shadows, unless you make object unlit regular way (for example by lightmapping object)
		//
		// special problems with shadow collecting occurs in FORWARD rendering as grass cutout areas (that are empty) are badly shaded
		// further - you won't be able to turn off shadows in cutout areas unless you:
		// A. disable custom pass below
		// B1. change render queue to "Geometry+501" (or above)
		// or
		// B2. disable (comment) Fallback "Diffuse" (look at the very end of shader code)
		//    by doing this grass can also receive "dumb" shadows (shadows visible on grass but received by underlying geometry)
		
		//
		// custom shadow collector for FORWARD rendering (we need it because vertices are pushed to the ground by height modificator - vertex.color.g)
		// but without cutoff areas just because it DOES NOT work (as in Unity3.2)
		// trying to make it by setting "addshadow" in surface shader (following surface shaders reference)
		// won't be any good - shadow collectors just does not work when any pixels are clipped
		// I don't know why (smells like Unity shadows bug)...
		//
		Pass {
			Name "ShadowCollector"
			Tags { "LightMode" = "ShadowCollector" }
			
			Fog {Mode Off}
			ZWrite On ZTest Less
			
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma multi_compile_shadowcollector
			
			#define SHADOW_COLLECTOR_PASS
			#include "UnityCG.cginc"
			
			float GRASSDEPTH;
			float UV_RATIO;
			float _mod;
			
			struct appdata {
				float4 vertex : POSITION;
			};
			
			struct v2f {
				V2F_SHADOW_COLLECTOR;
			};
			
			v2f vert (appdata_full v)
			{
				v2f o;
				float height=GRASSDEPTH/UV_RATIO;
				v.vertex.xyz -= v.normal * height * v.color.g;				
				TRANSFER_SHADOW_COLLECTOR(o)
				return o;
			}
			
			half4 frag (v2f i) : COLOR
			{
				SHADOW_COLLECTOR_FRAGMENT(i)
			}
			ENDCG
		}		
		
		//
		// if you really need shadows resolved at grass borders (sharp sidewalls) in forward, do it by making blob shadows:
		//
		// 1. make another copy of grass shader and discard all color information
		//    (by removing everything which refers to color) which will render faster
		// 2. using this shader you have to render grass object from camera point of view into custom depth render texture
		//    (like it's done in GrassDepthCamera attached via SetupForGrassRendering script)
		// 3. use this texture in "ZTest Projector Material" instead of using Unity's _CameraDepthTexture
		//
		
		//
		// EOF shadow casting/collecting discussion
		//
		///////////////////////////////////////////////////////////////////////////
	}

	//
	//
	// fallback shader used in soccer example
	// has additional/tweaked feature that allows to bend/shade grass under the ball
	//
	//
	SubShader {
		Tags { "Queue"="Geometry+250" "IgnoreProjector"="False" "RenderType"="VolumeGrass"} // custom render type specified if you need to write special shader replacement functionality
		LOD 699

		CGPROGRAM
		#pragma surface surf Lambert vertex:vert alphatest:_Cutoff fullforwardshadows
		#pragma target 3.0
		// (too slow on mobile hardware)
		#pragma exclude_renderers gles
		//#pragma debug
		
		// we need tex2Dlod in conditionals for GLSL
		#pragma glsl
		
		#include "UnityCG.cginc"

		#define MAX_RAYDEPTH 4
		#define FILL_GROUND
//		#define FILL_GROUND_BY_TEX
		#define BACK_COLOR_TEXTURE
		#define TRANSPARENCY_ZTEST_VALUE 0.6
		#define TRANSPARENCY_BREAK_VALUE 0.95
		#define HASH_SLICES
		#define CUSTOM_HASH_FUNCTION (frac(rayPos_tmp.x)*rayPos_tmp.x)
//		#define FADE_PARALLELS
//		#define ZTEST_CORECTOR
		#define DISTANCE_BLURING
//		#define GLOWING
//		#define GLOWING_MIP_LEVEL 4
		
		#define BOTTOM_COLORING
		#define BOTTOM_COLORING_NOISE_DRIVEN
 	 	#define BOTTOM_COLORING_SHARP_ANGLE_RADIUS_MULTIPLIER 6
 	 	#define BOTTOM_COLORING_SHARP_ANGLE_LOW_THRESHOLD 0.08
 	 			
		#define COLORING
		#define COLORING_ADDITIVE
//		#define WIND
		
//		#define BORDER_FRAY
//		#define MBLUR
		
		#define NOISE_CHANNEL_WINDX r
		#define NOISE_CHANNEL_WINDY g
		#define NOISE_CHANNEL_BCOLORING b
		#define NOISE_CHANNEL_COLORING r
		#define NOISE_CHANNEL_FRAY r
		
		#define NOISE_WIND_TEX _NoiseTex2 // texture binding (_NoiseTex or _NoiseTex2)
		#define NOISE_BCOLORING_TEX _NoiseTex
		#define NOISE_COLORING_TEX _NoiseTex2
		#define NOISE_FRAY_TEX _NoiseTex
		
		float PLANE_NUM;
		float GRASS_SLICE_NUM;
		
		float PLANE_NUM_INV;
		float GRASS_SLICE_NUM_INV;
		float GRASSDEPTH;
		float PREMULT;
		
		float UV_RATIO;

		half4 _Color;
		half4 _BladesColor;
		sampler2D _MainTex;
		sampler2D _BladesTex;
		sampler2D _BladesBackTex;
		half4 _BladesBackColor;
		sampler2D _NoiseTex;
		sampler2D _NoiseTex2;
		sampler2D _NoiseTexHash;		
		sampler2D _GrassDepthTex;
		sampler2D _CameraDepthTexture; 

		float _ZBufferParamA;
		float _ZBufferParamB;
		float _ZBufferFarClip;
		float _fardistancetiling;
		float _view_angle_damper;
		
		float _mod;
		float _tiling_factor;
		float _far_distance;
		float _far_distance_transition;
		float _blur_distance;
		float _blur_distance_transition;
		float _MIP_distance_limit;
		float _glowing_value;
		half4 _bottom_coloring_value;
		float4 _bottom_coloring_noise_tiling;
		float _bottom_coloring_far;
		float _bottom_coloring_distance_fade;
		float _bottom_coloring_border_damp;
		half4 _coloring_value;
		float4 _coloring_noise_tiling;
		float _wind_amp;
		float _wind_speed;
		float _wind_freq;
		
		float _border_fray_strength;
		float _border_fray_tiling;
		
		float _mblur_val;
		float _mblur_distance;
		float _mblur_transition;
		float4 _mblur_dir;
		
		// position of ball in tangent space
		float4 _ballpos;
		
		struct Input {
			float2 uv_MainTex;

			float3 viewDir;
			float3 worldPos;
			float4 screenPos;
			
			float4 params;
		};

		void vert (inout appdata_full v, out Input o) {
			UNITY_INITIALIZE_OUTPUT(Input, o);
			COMPUTE_EYEDEPTH(o.params.x);
			o.params.yz=v.color.rg;
			
			float height=GRASSDEPTH/UV_RATIO;
			v.vertex.xyz -= v.normal * height * v.color.g;
		}

		#ifdef ZTEST_CORECTOR
			#define ZTEST_MULTIPLICATOR ((IN.params.x/dist) * _tiling_factor)
		#else
			#define ZTEST_MULTIPLICATOR _tiling_factor
		#endif

		void surf (Input IN, inout SurfaceOutput o) {
			clip(0.999-IN.params.z);
			o.Normal=float3(0,0,1);

			#ifdef ZTEST_CORECTOR
				float dist=distance(IN.worldPos, _WorldSpaceCameraPos);
				float distanceT=clamp((dist-_far_distance)/_far_distance_transition,0,1);
			#else
				float distanceT=clamp((IN.params.x-_far_distance)/_far_distance_transition,0,1);
			#endif
			half3 base_col=tex2D(_MainTex, IN.uv_MainTex*_fardistancetiling).rgb;
			
		 	float zoffset = IN.params.y + IN.params.z;
		 	zoffset = (zoffset>1) ? 1 : zoffset;
		 	
			#ifdef BOTTOM_COLORING
				float bottom_cut=_bottom_coloring_value.a*4*(1-zoffset*_bottom_coloring_border_damp);
				#ifdef BOTTOM_COLORING_NOISE_DRIVEN
					bottom_cut*=tex2Dlod(NOISE_BCOLORING_TEX, float4(IN.uv_MainTex*_bottom_coloring_noise_tiling.xy+_bottom_coloring_noise_tiling.zw,0,0)).NOISE_CHANNEL_BCOLORING;
				#endif
 				float cval=1-bottom_cut*_bottom_coloring_far;
 				base_col=_bottom_coloring_value.rgb+cval*(base_col - _bottom_coloring_value.rgb);
			#endif
			#ifdef COLORING
				float cval2=tex2D(NOISE_COLORING_TEX, IN.uv_MainTex*_coloring_noise_tiling.xy+_coloring_noise_tiling.zw).NOISE_CHANNEL_COLORING*_coloring_value.a;
				#ifdef COLORING_ADDITIVE
					half3 coloring=_coloring_value.rgb*cval2;
	 				base_col+=coloring;
				#else
					half3 coloring=_coloring_value.rgb*2*cval2 + (1-cval2);
	 				base_col*=coloring;
				#endif
			#endif
		 	if (distanceT>0.99) {
		 		#ifdef DEBUG_PERFORMANCE
		 			o.Albedo=0;
		 			o.Emission.rgb=1;
		 		#else
			 		o.Albedo=base_col*_Color.rgb*2;
		 		#endif
				o.Alpha=1;//0.999-IN.params.z
				return;
	 		}
			#ifdef BOTTOM_COLORING
				#ifdef ZTEST_CORECTOR
					bottom_cut*=(1-dist/_bottom_coloring_distance_fade);
				#else
					bottom_cut*=(1-IN.params.x/_bottom_coloring_distance_fade);
				#endif	
			#endif	 		
	 		
			float3 sceneUVZ;
			sceneUVZ.xy = IN.screenPos.xy / IN.screenPos.w;
			// custom LinearEyeDepth() parametrization
			sceneUVZ.z = 1.0 / ((_ZBufferParamA  * tex2Dlod(_GrassDepthTex, float4(sceneUVZ.xy,0,0)).r) + _ZBufferParamB );
			sceneUVZ.z=(sceneUVZ.z>_ZBufferFarClip)?10000:sceneUVZ.z; // don't clip beyond depth buffer distance
			
	 		#ifdef DISTANCE_BLURING
				float4 distanceS;
				distanceS.y = (IN.params.x-_blur_distance)/_blur_distance_transition;
				distanceS.y = modf( clamp(distanceS.y, 0, _MIP_distance_limit), distanceS.x);
				distanceS.z = 1-distanceS.y;
				distanceS.w = distanceS.x+1;
			#endif
			
			float3 EyeDirTan = -normalize(IN.viewDir); // eye vector in tangent space
 	 		float angle_fade=EyeDirTan.z;
 	 		
 	 		#define BOTTOM_COLORING_SHARP_ANGLE_LOW_THRESHOLD_VAL (1.0 + BOTTOM_COLORING_SHARP_ANGLE_LOW_THRESHOLD)
			#ifdef BOTTOM_COLORING
				bottom_cut*=clamp((BOTTOM_COLORING_SHARP_ANGLE_LOW_THRESHOLD_VAL + angle_fade)*BOTTOM_COLORING_SHARP_ANGLE_RADIUS_MULTIPLIER, 0, 1);
			#endif
			
 	 		angle_fade*=angle_fade;
 	 		angle_fade=1-angle_fade;
 	 		
			angle_fade=(angle_fade<0.1) ? 0.1 : angle_fade;
			EyeDirTan.z*=angle_fade*_view_angle_damper+(1-_view_angle_damper);
			#ifdef MBLUR
				if (_mblur_val>0.02) {
					_mblur_val*=1-clamp((IN.params.x-_mblur_distance)/_mblur_transition, 0, 1);
					EyeDirTan-=_mblur_val*(EyeDirTan + _mblur_dir.xyz);
				}
			#endif
			
			float2 bd=IN.uv_MainTex-_ballpos.xy;
			float ball_dist=bd.x*bd.x+bd.y*bd.y;
			float ball_occlusion=1.0-0.25/ball_dist*_ballpos.z;
			ball_occlusion=clamp(ball_occlusion,1-_ballpos.z,1);
			ball_occlusion=(1-ball_occlusion);
			ball_occlusion=ball_occlusion*ball_occlusion;
			ball_occlusion=(1-ball_occlusion);
			EyeDirTan.xy=ball_occlusion*EyeDirTan.xy-(1-ball_occlusion)*bd;
					
			#ifdef ZTEST_CORECTOR
				EyeDirTan=normalize(EyeDirTan);
			#endif
			
			float3 EyeDirTanAbs=abs(EyeDirTan);
			#ifdef FADE_PARALLELS
				float2 fade_parallels=angle_fade*EyeDirTanAbs.xy-angle_fade+1;
				fade_parallels+=IN.params.z*(1-fade_parallels);
			#endif
			#ifdef BOTTOM_COLORING
				bottom_cut*=GRASS_SLICE_NUM;
			#endif
			
		 	float3 rayPos = float3(IN.uv_MainTex, -zoffset*GRASSDEPTH);
		 	float hgt=GRASSDEPTH*IN.params.z;

			float rayLength=0;
			float3 delta_next=float3(PLANE_NUM_INV,PLANE_NUM_INV,GRASS_SLICE_NUM_INV);
			
			// evaluated pixel color
			half4 c = half4(0.0,0.0,0.0,0.0);
			
		 	float3 rayPosN=float3(rayPos.xy*PLANE_NUM, rayPos.z*GRASS_SLICE_NUM);
			float3 delta=-frac(rayPosN);
			delta=(EyeDirTan>0) ? frac(-rayPosN) : delta;
	 		delta*=delta_next;
	 		delta_next/=EyeDirTanAbs;
	 		delta/=EyeDirTan;
			delta.z=(rayPos.z<-0.001)?delta.z:delta_next.z;
						
			float2 _uv;
			half4 _col;
			int hitcount;
			bool zhit=false;
		 	for(hitcount=0; hitcount < MAX_RAYDEPTH; hitcount++) {
		 	
		 		if ((delta.z<delta.x) && (delta.z<delta.y)) {
		 			rayLength=(c.w>TRANSPARENCY_ZTEST_VALUE) ? rayLength : (rayLength+delta.z);
		 			#ifdef FILL_GROUND
			 			rayPos+=delta.z*EyeDirTan;
			 			#ifdef FILL_GROUND_BY_TEX
		 					_col=tex2Dlod(_MainTex,float4(rayPos.xy,0, 0 ));
		 					_col.rgb*=_Color.rgb*2;
		 				#else
		 					_col.rgb=_bottom_coloring_value.rgb;
		 				#endif
	 					_col.w=(1-IN.params.z);
		 				#ifdef BOTTOM_COLORING
		 					cval=1+rayPos.z*bottom_cut;
		 					cval=clamp(cval,0,1);
			 				_col.rgb=_bottom_coloring_value.rgb+cval*(_col.rgb - _bottom_coloring_value.rgb);
		 				#endif
		 				c=(rayPos.z>-0.001) ? c : (c+(1-c.w)*_col);
		 			#endif
		 			zhit=true;
					break;
		 		} else {
		 			bool xy_flag=delta.x<delta.y;

		 			float delta_tmp=xy_flag ? delta.x : delta.y;

		 			rayLength=(c.w>TRANSPARENCY_ZTEST_VALUE) ? rayLength : (rayLength+delta_tmp);
		 			rayPos+=delta_tmp*EyeDirTan;
		 			
		 			float3 rayPos_tmp = xy_flag ? rayPos.xyz : rayPos.yxz;
		 			
					#ifdef WIND
						float4 wtmp=tex2Dlod(NOISE_WIND_TEX, float4(rayPos.xy*_wind_freq+_Time*_wind_speed,0,0));
						float wind_offset=xy_flag ? wtmp.NOISE_CHANNEL_WINDX : wtmp.NOISE_CHANNEL_WINDY;
						wind_offset-=0.5;
						wind_offset*=_wind_amp;
						wind_offset*=1+rayPos.z*GRASS_SLICE_NUM;
					#endif
		 			
		 			#ifdef HASH_SLICES
		 				#ifdef CUSTOM_HASH_FUNCTION
							#define HASH_OFFSET CUSTOM_HASH_FUNCTION
						#else
		 					float2 htmp=tex2Dlod(_NoiseTexHash, float4(rayPos_tmp.x+0.013,0,0,0)).rg;
							float HASH_OFFSET=xy_flag ? htmp.x : htmp.y;
						#endif
					#else
						#define HASH_OFFSET 0
					#endif
					
					#ifdef WIND
						_uv=rayPos_tmp.yz+float2(HASH_OFFSET+wind_offset,rayPos_tmp.x*PREMULT+hgt);
					#else
						_uv=rayPos_tmp.yz+float2(HASH_OFFSET,rayPos_tmp.x*PREMULT+hgt);
					#endif
					#ifdef DISTANCE_BLURING
				 		_col=distanceS.z * tex2Dlod(_BladesTex, float4(_uv, 0, distanceS.x ));
				 		_col+=distanceS.y * tex2Dlod(_BladesTex,float4(_uv, 0, distanceS.w )); // distanceS.w==distanceS.x+1
						float mipcut=clamp(-rayPos.z*70,0,1);
						_col.a*=mipcut;
					#else
				 		_col=tex2Dlod(_BladesTex, float4(_uv, 0, 0 ));
					#endif
					_col.rgb*=_col.a;
					#ifdef GLOWING
						#ifndef DISTANCE_BLURING
							float mipcut=clamp(-rayPos.z*70,0,1);
						#endif
						_col+=_glowing_value*(tex2Dlod(_BladesTex,float4(_uv, 0, GLOWING_MIP_LEVEL )) - _col)*mipcut;
					#endif
		 			#ifdef FADE_PARALLELS
		 				float fade_parallelsXY=xy_flag ? fade_parallels.x : fade_parallels.y;
		 				_col*=fade_parallelsXY;
		 			#endif
		 					 			
		 			delta.xyz=xy_flag ? float3(delta_next.x, delta.yz-delta.x) : float3(delta.x-delta.y, delta_next.y, delta.z-delta.y);
		 			
		 			#ifdef BOTTOM_COLORING
		 				cval=1+rayPos.z*bottom_cut;
		 				cval=clamp(cval,0,1);
	 					half3 ca=_bottom_coloring_value.rgb*_col.a;
		 				_col.rgb=ca+cval*(_col.rgb - ca);
		 			#endif
		 			
		 			c+=(1-c.w)*_col;
		 		}
		 		if (c.w>=TRANSPARENCY_BREAK_VALUE) break;
			}
			#ifdef MBLUR
	 			rayLength*=(1-_mblur_val);
	 		#endif
			float partZ = IN.params.x + rayLength * ZTEST_MULTIPLICATOR;
			clip(sceneUVZ.z-partZ);
			
			#ifdef BACK_COLOR_TEXTURE
				_uv.y*=GRASS_SLICE_NUM;
				#ifdef DISTANCE_BLURING
 					_col=tex2Dlod(_BladesBackTex, float4(_uv, 0, distanceS.x ));
 				#else
 					_col=tex2Dlod(_BladesBackTex, float4(_uv, 0, 0 ));
 				#endif
	 			_col.rgb*=_BladesBackColor.rgb;
 			#else
	 			_col.rgb=_BladesBackColor.rgb;
	 			_col.a=1;
 			#endif
 			#ifdef BOTTOM_COLORING
 				_col.rgb=_bottom_coloring_value.rgb+cval*(_col.rgb - _bottom_coloring_value.rgb);
 			#endif			
 			c=zhit ? c : (c+(1-c.w)*_col);
	 						
	 		#ifdef DEBUG_PERFORMANCE
	 			float ht=1.0*hitcount/DEBUG_RED_LIMIT;
	 			ht=clamp(ht,0,1);
	 			o.Albedo=0;
	 			o.Emission.rgb=float3(clamp(ht*2,0,1),clamp(2-ht*2,0,1),0);
	 			o.Alpha=1;
	 		#else
				#ifdef COLORING
					#ifdef COLORING_ADDITIVE
	 					c.rgb+=coloring;
	 				#else
	 					c.rgb*=coloring;
	 				#endif
				#endif
				#ifdef BORDER_FRAY
					if (IN.params.z>0) {
						c.w*=IN.params.z*(tex2Dlod(NOISE_FRAY_TEX,float4(rayPos.xy*_border_fray_tiling, 0,0)).NOISE_CHANNEL_FRAY-_border_fray_strength*2) + 1;
					}
				#endif
				c.rgb*=_BladesColor.rgb*2;
				c.rgb*=ball_occlusion;
								
				base_col*=_Color.rgb*2;
			 	o.Albedo=(c.rgb+distanceT*(base_col-c.rgb));
				o.Alpha=c.w;
			#endif
		}
		ENDCG
	}
	
	//
	//
	// fallback shader used in lawn example
	// has additional/tweaked feature called MOW_ENABLED in defines section that allows mowing grass
	//
	// mowing is handled by render texture (_AuxTex) which defines grass height in its red channel
	// another important difference, is when ray hits MAX_RAYDEPTH limit (and MOW_ENABLED if defined),
	// remaining opacity will be resolved by ground texture color instead of _BladesBackTex or _BladesBackColor
	//
	SubShader {
		Tags { "Queue"="Geometry+250" "IgnoreProjector"="False" "RenderType"="VolumeGrass"} // custom render type specified if you need to write special shader replacement functionality
		LOD 698

		CGPROGRAM
		#pragma surface surf Lambert alphatest:_Cutoff vertex:vert fullforwardshadows
		#pragma target 3.0
		// (too slow on mobile hardware)
		#pragma exclude_renderers gles
		//#pragma debug
		
		// we need tex2Dlod in conditionals for GLSL
		#pragma glsl
		
		#include "UnityCG.cginc"

		//////////////////////////////////////////////////////////////////////////////////////////////////
		//
		// defines section (configuration of grass functionality against performance)
		//
		
		#define MAX_RAYDEPTH 7
		
		//#define DEBUG_PERFORMANCE
		#define DEBUG_RED_LIMIT 6
		
		#define FILL_GROUND
		#define FILL_GROUND_BY_TEX
		
		//
		// this is overwritten by MOW_ENABLED, when ray hits MAX_RAYDEPTH limit,
		// remaining opacity will be resolved by ground texture color instead of _BladesBackTex or _BladesBackColor
		//
		//#define BACK_COLOR_TEXTURE
		
		#define TRANSPARENCY_ZTEST_VALUE 0.6
		#define TRANSPARENCY_BREAK_VALUE 0.95
		

		#define HASH_SLICES
		#define CUSTOM_HASH_FUNCTION (frac(rayPos_tmp.x)*rayPos_tmp.x)
		
		//#define FADE_PARALLELS
		
		#define ZTEST_CORECTOR		
		//#define DISTANCE_BLURING
		
		//#define GLOWING
		#define GLOWING_MIP_LEVEL 4
		
		#define BOTTOM_COLORING
		#define BOTTOM_COLORING_NOISE_DRIVEN
 	 	#define BOTTOM_COLORING_SHARP_ANGLE_RADIUS_MULTIPLIER 6
 	 	#define BOTTOM_COLORING_SHARP_ANGLE_LOW_THRESHOLD 0.08
		
		#define COLORING
		//#define COLORING_ADDITIVE
		
		//#define WIND
		
		//#define BORDER_FRAY
		
		//#define MBLUR
		
		#define NOISE_CHANNEL_WINDX r
		#define NOISE_CHANNEL_WINDY g
		#define NOISE_CHANNEL_BCOLORING b
		#define NOISE_CHANNEL_COLORING a
		#define NOISE_CHANNEL_FRAY r
		
		#define NOISE_WIND_TEX _NoiseTex2 // texture binding (_NoiseTex or _NoiseTex2)
		#define NOISE_BCOLORING_TEX _NoiseTex2
		#define NOISE_COLORING_TEX _NoiseTex2
		#define NOISE_FRAY_TEX _NoiseTex
		
		//
		// if enabled _AuxTex red channel will represent grass height
		// and _auxtex_tiling will define placement of above mow buffer in UV space
		// _auxtex_tiling is set by VolumeGrass script and further adjusted by SetupCamForMowing
		//
		#define MOW_ENABLED
		
		//
		// EOF defines section
		//
		//////////////////////////////////////////////////////////////////////////////////////////////////

		float PLANE_NUM;
		float GRASS_SLICE_NUM;
		
		float PLANE_NUM_INV;
		float GRASS_SLICE_NUM_INV;
		float GRASSDEPTH;
		float PREMULT;
		
		float UV_RATIO;

		half4 _Color;
		half4 _BladesColor;
		sampler2D _MainTex;
		sampler2D _BladesTex;
		sampler2D _BladesBackTex;
		half4 _BladesBackColor;
		sampler2D _NoiseTex;
		sampler2D _NoiseTex2;
		sampler2D _NoiseTexHash;		
		sampler2D _GrassDepthTex;

		float _ZBufferParamA;
		float _ZBufferParamB;
		float _ZBufferFarClip;
		float _fardistancetiling;
		float _view_angle_damper;
		
		float _mod;
		float _tiling_factor;
		float _far_distance;
		float _far_distance_transition;
		float _blur_distance;
		float _blur_distance_transition;
		float _MIP_distance_limit;
		float _glowing_value;
		half4 _bottom_coloring_value;
		float4 _bottom_coloring_noise_tiling;
		float _bottom_coloring_far;
		float _bottom_coloring_distance_fade;
		float _bottom_coloring_border_damp;
		half4 _coloring_value;
		float4 _coloring_noise_tiling;
		float _wind_amp;
		float _wind_speed;
		float _wind_freq;
		
		float _border_fray_strength;
		float _border_fray_tiling;
		
		float _mblur_val;
		float _mblur_distance;
		float _mblur_transition;
		float4 _mblur_dir;
		
		#ifdef MOW_ENABLED
			sampler2D _AuxTex;
			float4 _auxtex_tiling;		
			half _auxtex_modulator_value;
		#endif
		
		struct Input {
			float2 uv_MainTex;

			float3 viewDir;
			float3 worldPos;
			float4 screenPos;
			
			float4 params;
		};

		void vert (inout appdata_full v, out Input o) {
			UNITY_INITIALIZE_OUTPUT(Input, o);
			COMPUTE_EYEDEPTH(o.params.x);
			o.params.yz=v.color.rg;
			
			float height=GRASSDEPTH/UV_RATIO;
			v.vertex.xyz -= v.normal * height * v.color.g;
		}

		#ifdef ZTEST_CORECTOR
			#define ZTEST_MULTIPLICATOR ((IN.params.x/dist) * _tiling_factor)
		#else
			#define ZTEST_MULTIPLICATOR _tiling_factor
		#endif
		
		void surf (Input IN, inout SurfaceOutput o) {
			clip(0.999-IN.params.z);
			o.Normal=float3(0,0,1);

			#ifdef ZTEST_CORECTOR
				float dist=distance(IN.worldPos, _WorldSpaceCameraPos);
				float distanceT=clamp((dist-_far_distance)/_far_distance_transition,0,1);
			#else
				float distanceT=clamp((IN.params.x-_far_distance)/_far_distance_transition,0,1);
			#endif
			half3 base_col=tex2D(_MainTex, IN.uv_MainTex*_fardistancetiling).rgb;
			
		 	float zoffset = IN.params.y + IN.params.z;
		 	zoffset = (zoffset>1) ? 1 : zoffset;
			
			#ifdef BOTTOM_COLORING
				float bottom_cut=_bottom_coloring_value.a*4*(1-zoffset*_bottom_coloring_border_damp);
				#ifdef BOTTOM_COLORING_NOISE_DRIVEN
					bottom_cut*=tex2Dlod(NOISE_BCOLORING_TEX, float4(IN.uv_MainTex*_bottom_coloring_noise_tiling.xy+_bottom_coloring_noise_tiling.zw,0,0)).NOISE_CHANNEL_BCOLORING;
				#endif
 				float cval=1-bottom_cut*_bottom_coloring_far;
 				base_col=_bottom_coloring_value.rgb+cval*(base_col - _bottom_coloring_value.rgb);
			#endif
			#ifdef COLORING
				float cval2=tex2D(NOISE_COLORING_TEX, IN.uv_MainTex*_coloring_noise_tiling.xy+_coloring_noise_tiling.zw).NOISE_CHANNEL_COLORING*_coloring_value.a;
				#ifdef COLORING_ADDITIVE
					half3 coloring=_coloring_value.rgb*cval2;
	 				base_col+=coloring;
				#else
					half3 coloring=_coloring_value.rgb*2*cval2 + (1-cval2);
	 				base_col*=coloring;
				#endif
			#endif
 			#ifdef MOW_ENABLED
 				half auxtexval;
 				auxtexval=tex2Dlod(_AuxTex, float4((IN.uv_MainTex+_auxtex_tiling.zw)*_auxtex_tiling.xy,0,0)).r;
 				base_col*=(1+auxtexval*_auxtex_modulator_value);
 			#endif
		 	if (distanceT>0.99) {
		 		#ifdef DEBUG_PERFORMANCE
		 			o.Albedo=0;
		 			o.Emission.rgb=1;
		 		#else
			 		o.Albedo=base_col*_Color.rgb*2;
		 		#endif
				o.Alpha=1;//0.999-IN.params.z;
				return;
	 		}
			#ifdef BOTTOM_COLORING
				#ifdef ZTEST_CORECTOR
					bottom_cut*=(1-dist/_bottom_coloring_distance_fade);
				#else
					bottom_cut*=(1-IN.params.x/_bottom_coloring_distance_fade);
				#endif	
			#endif	 		
	 		
			float3 sceneUVZ;
			sceneUVZ.xy = IN.screenPos.xy / IN.screenPos.w;
			// custom LinearEyeDepth() parametrization
			sceneUVZ.z = 1.0 / ((_ZBufferParamA  * tex2Dlod(_GrassDepthTex, float4(sceneUVZ.xy,0,0)).r) + _ZBufferParamB );
			sceneUVZ.z=(sceneUVZ.z>_ZBufferFarClip)?10000:sceneUVZ.z; // don't clip beyond depth buffer distance
			
	 		#ifdef DISTANCE_BLURING
				float4 distanceS;
				distanceS.y = (IN.params.x-_blur_distance)/_blur_distance_transition;
				distanceS.y = modf( clamp(distanceS.y, 0, _MIP_distance_limit), distanceS.x);
				distanceS.z = 1-distanceS.y;
				distanceS.w = distanceS.x+1;
			#endif
			
			float3 EyeDirTan = -normalize(IN.viewDir); // eye vector in tangent space
 	 		float angle_fade=EyeDirTan.z;
 	 		
 	 		#define BOTTOM_COLORING_SHARP_ANGLE_LOW_THRESHOLD_VAL (1.0 + BOTTOM_COLORING_SHARP_ANGLE_LOW_THRESHOLD)
			#ifdef BOTTOM_COLORING
				bottom_cut*=clamp((BOTTOM_COLORING_SHARP_ANGLE_LOW_THRESHOLD_VAL + angle_fade)*BOTTOM_COLORING_SHARP_ANGLE_RADIUS_MULTIPLIER, 0, 1);
			#endif
			
 	 		angle_fade*=angle_fade;
 	 		angle_fade=1-angle_fade;	 		
 	 		
			angle_fade=(angle_fade<0.1) ? 0.1 : angle_fade;
			EyeDirTan.z*=angle_fade*_view_angle_damper+(1-_view_angle_damper);
			#ifdef MBLUR
				if (_mblur_val>0.02) {
					_mblur_val*=1-clamp((IN.params.x-_mblur_distance)/_mblur_transition, 0, 1);
					EyeDirTan-=_mblur_val*(EyeDirTan + _mblur_dir.xyz);
				}
			#endif
			#ifdef ZTEST_CORECTOR
				EyeDirTan=normalize(EyeDirTan);
			#endif
			
			float3 EyeDirTanAbs=abs(EyeDirTan);
			#ifdef FADE_PARALLELS
				float2 fade_parallels=angle_fade*EyeDirTanAbs.xy-angle_fade+1;
				fade_parallels+=IN.params.z*(1-fade_parallels);
			#endif
			#ifdef BOTTOM_COLORING
				bottom_cut*=GRASS_SLICE_NUM;
			#endif
			
		 	float3 rayPos = float3(IN.uv_MainTex, -zoffset*GRASSDEPTH);
		 	float hgt=GRASSDEPTH*IN.params.z;
		 	
			float rayLength=0;
			float3 delta_next=float3(PLANE_NUM_INV,PLANE_NUM_INV,GRASS_SLICE_NUM_INV);
			
			// evaluated pixel color
			half4 c = half4(0.0,0.0,0.0,0.0);
			
		 	float3 rayPosN=float3(rayPos.xy*PLANE_NUM, rayPos.z*GRASS_SLICE_NUM);
			float3 delta=-frac(rayPosN);
			delta=(EyeDirTan>0) ? frac(-rayPosN) : delta;
	 		delta*=delta_next;
	 		delta_next/=EyeDirTanAbs;
	 		delta/=EyeDirTan;
			delta.z=(rayPos.z<-0.001)?delta.z:delta_next.z;
			
		 	#ifdef MOW_ENABLED
		 		// ground color at the intersection of ray to resolve remaining opacity at the end of ray
		 		// (instead of solid color or backBolorTexture)
				float3 rayPos_ground=float3(rayPos.xy + EyeDirTan.xy * delta.z, ((EyeDirTan.z<0) ? -GRASSDEPTH : 0));
				half4 ground_col=tex2Dlod(_MainTex, float4(rayPos_ground.xy,0,0));
				ground_col.rgb*=_Color.rgb*2;
	 			ground_col.w=(1-zoffset);
		 	#endif
						
		 	float2 _uv;
			half4 _col;
			int hitcount;
			bool zhit=false;
		 	for(hitcount=0; hitcount < MAX_RAYDEPTH; hitcount++) {
		 	
		 		if ((delta.z<delta.x) && (delta.z<delta.y)) {
		 			rayLength=(c.w>TRANSPARENCY_ZTEST_VALUE) ? rayLength : (rayLength+delta.z);
		 			#ifdef FILL_GROUND
			 			rayPos+=delta.z*EyeDirTan;
			 			#ifdef FILL_GROUND_BY_TEX
		 					_col=tex2Dlod(_MainTex,float4(rayPos.xy,0, 0 ));
		 					_col.rgb*=_Color.rgb*2;
		 				#else
		 					_col.rgb=_bottom_coloring_value.rgb;
		 				#endif
	 					_col.w=(1-zoffset);
		 				#ifdef BOTTOM_COLORING
				 			#ifdef MOW_ENABLED
				 				auxtexval=tex2Dlod(_AuxTex, float4((rayPos.xy+_auxtex_tiling.zw)*_auxtex_tiling.xy,0,0)).r;
			 					cval=1+rayPos.z*bottom_cut*(1-auxtexval);
			 				#else
			 					cval=1+rayPos.z*bottom_cut;
				 			#endif
		 					cval=clamp(cval,0,1);
			 				_col.rgb=_bottom_coloring_value.rgb+cval*(_col.rgb - _bottom_coloring_value.rgb);
		 				#endif
		 				c=(rayPos.z>-0.001) ? c : (c+(1-c.w)*_col);
		 			#endif
		 			zhit=true;
					break;
		 		} else {
		 			bool xy_flag=delta.x<delta.y;

		 			float delta_tmp=xy_flag ? delta.x : delta.y;

		 			rayLength=(c.w>TRANSPARENCY_ZTEST_VALUE) ? rayLength : (rayLength+delta_tmp);
		 			rayPos+=delta_tmp*EyeDirTan;
		 			
		 			float3 rayPos_tmp = xy_flag ? rayPos.xyz : rayPos.yxz;
		 			
					#ifdef WIND
						float4 wtmp=tex2Dlod(NOISE_WIND_TEX, float4(rayPos.xy*_wind_freq+_Time*_wind_speed,0,0));
						float wind_offset=xy_flag ? wtmp.NOISE_CHANNEL_WINDX : wtmp.NOISE_CHANNEL_WINDY;
						wind_offset-=0.5;
						wind_offset*=_wind_amp;
						wind_offset*=1+rayPos.z*GRASS_SLICE_NUM;
					#endif
		 			
		 			#ifdef HASH_SLICES
		 				#ifdef CUSTOM_HASH_FUNCTION
							#define HASH_OFFSET CUSTOM_HASH_FUNCTION
						#else
		 					float2 htmp=tex2Dlod(_NoiseTexHash, float4(rayPos_tmp.x+0.013,0,0,0)).rg;
							float HASH_OFFSET=xy_flag ? htmp.x : htmp.y;
						#endif
					#else
						#define HASH_OFFSET 0
					#endif
					
					#ifdef WIND
						_uv=rayPos_tmp.yz+float2(HASH_OFFSET+wind_offset,rayPos_tmp.x*PREMULT+hgt);
					#else
						_uv=rayPos_tmp.yz+float2(HASH_OFFSET,rayPos_tmp.x*PREMULT+hgt);
					#endif
					
		 			#ifdef MOW_ENABLED
		 				auxtexval=tex2Dlod(_AuxTex, float4((rayPos.xy+_auxtex_tiling.zw)*_auxtex_tiling.xy,0,0)).r;
	 					_uv.y+=GRASSDEPTH*auxtexval*0.75; // push grass slices into ground by maximum value of 75%
		 			#endif
		 								
					#ifdef DISTANCE_BLURING
				 		_col=distanceS.z * tex2Dlod(_BladesTex, float4(_uv, 0, distanceS.x ));
				 		_col+=distanceS.y * tex2Dlod(_BladesTex,float4(_uv, 0, distanceS.w )); // distanceS.w==distanceS.x+1
						float mipcut=clamp(-rayPos.z*70,0,1);
		 				_col.a*=mipcut;
					#else
				 		_col=tex2Dlod(_BladesTex, float4(_uv, 0, 0 ));
					#endif
					_col.rgb*=_col.a;
					#ifdef GLOWING
						#ifndef DISTANCE_BLURING
							float mipcut=clamp(-rayPos.z*70,0,1);
						#endif
						_col+=_glowing_value*(tex2Dlod(_BladesTex,float4(_uv, 0, GLOWING_MIP_LEVEL )) - _col)*mipcut;
					#endif
		 			#ifdef FADE_PARALLELS
		 				float fade_parallelsXY=xy_flag ? fade_parallels.x : fade_parallels.y;
		 				_col*=fade_parallelsXY;
		 			#endif
		 					 			
		 			delta.xyz=xy_flag ? float3(delta_next.x, delta.yz-delta.x) : float3(delta.x-delta.y, delta_next.y, delta.z-delta.y);
		 			
		 			#ifdef MOW_ENABLED
		 				_col = (rayPos.z<-GRASSDEPTH*auxtexval) ? _col : half4(0,0,0,0);
		 				_col.rgb*=(1+auxtexval*_auxtex_modulator_value);
		 			#endif
		 			
		 			#ifdef BOTTOM_COLORING
		 				#ifdef MOW_ENABLED
			 				cval=1+rayPos.z*bottom_cut*(1-auxtexval);
			 			#else
			 				cval=1+rayPos.z*bottom_cut;
		 				#endif
		 				cval=clamp(cval,0,1);
	 					half3 ca=_bottom_coloring_value.rgb*_col.a;
		 				_col.rgb=ca+cval*(_col.rgb - ca);
		 			#endif
		 			
		 			c+=(1-c.w)*_col;
		 		}
		 		if (c.w>=TRANSPARENCY_BREAK_VALUE) break;
			}
			#ifdef MBLUR
	 			rayLength*=(1-_mblur_val);
	 		#endif
			float partZ = IN.params.x + rayLength * ZTEST_MULTIPLICATOR;
			clip(sceneUVZ.z-partZ);
			
			#ifdef MOW_ENABLED
				_col.rgb=ground_col.rgb;
				_col.a=1;
			#else
				#ifdef BACK_COLOR_TEXTURE
					_uv.y*=GRASS_SLICE_NUM;
					#ifdef DISTANCE_BLURING
	 					_col=tex2Dlod(_BladesBackTex, float4(_uv, 0, distanceS.x ));
	 				#else
	 					_col=tex2Dlod(_BladesBackTex, float4(_uv, 0, 0 ));
	 				#endif
		 			_col.rgb*=_BladesBackColor;
	 			#else
		 			_col.rgb=_BladesBackColor;
		 			_col.a=1;
	 			#endif
	 		#endif
 			#ifdef BOTTOM_COLORING
 				_col.rgb=_bottom_coloring_value.rgb+cval*(_col.rgb - _bottom_coloring_value.rgb);
 			#endif			
 			c=zhit ? c : (c+(1-c.w)*_col);
	 						
	 		#ifdef DEBUG_PERFORMANCE
	 			float ht=1.0*hitcount/DEBUG_RED_LIMIT;
	 			ht=clamp(ht,0,1);
	 			o.Albedo=0;
	 			o.Emission.rgb=float3(clamp(ht*2,0,1),clamp(2-ht*2,0,1),0);
	 			o.Alpha=1;
	 		#else
				#ifdef COLORING
					#ifdef COLORING_ADDITIVE
	 					c.rgb+=coloring;
	 				#else
	 					c.rgb*=coloring;
	 				#endif
				#endif
				#ifdef BORDER_FRAY
					if (IN.params.z>0) {
						c.w*=IN.params.z*(tex2Dlod(NOISE_FRAY_TEX,float4(rayPos.xy*_border_fray_tiling, 0,0)).NOISE_CHANNEL_FRAY-_border_fray_strength*2) + 1;
					}
				#endif
				c.rgb*=_BladesColor.rgb*2;
				base_col*=_Color.rgb*2;
			 	o.Albedo=(c.rgb+distanceT*(base_col-c.rgb));
				o.Alpha=c.w;
			#endif
		}
		ENDCG
	}	
	
	Fallback "Diffuse"
}
