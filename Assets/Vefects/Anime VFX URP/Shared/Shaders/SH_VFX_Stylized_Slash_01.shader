// Made with Amplify Shader Editor v1.9.9.5
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Vefects/SH_VFX_Stylized_Slash_01"
{
	Properties
	{
		[HideInInspector] _EmissionColor("Emission Color", Color) = (1,1,1,1)
		[Space(33)][Header(Main Texture)][Space(13)] _Texture( "Texture", 2D ) = "white" {}
		_TextureChannel( "Texture Channel", Vector ) = ( 0, 1, 0, 0 )
		_TextureRotation( "Texture Rotation", Float ) = 0
		_TexturePanSpeed( "Texture Pan Speed", Vector ) = ( 0, 0, 0, 0 )
		[Space(33)][Header(Distortion)][Space(13)] _DistortionMask( "Distortion Mask", 2D ) = "white" {}
		_DistortionMaskChannel( "Distortion Mask Channel", Vector ) = ( 0, 1, 0, 0 )
		_DistortionMaskRotation( "Distortion Mask Rotation", Float ) = 0
		_DistortionMaskPanSpeed( "Distortion Mask Pan Speed", Vector ) = ( 0, 0, 0, 0 )
		_DistortionIntensity( "Distortion Intensity", Float ) = 0
		[Space(33)][Header(Dissolve)][Space(13)] _DissolveMask( "Dissolve Mask", 2D ) = "white" {}
		_DissolveMaskChannel( "Dissolve Mask Channel", Vector ) = ( 0, 1, 0, 0 )
		_DissolveMaskRotation( "Dissolve Mask Rotation", Float ) = 0
		_DissolveMaskPanSpeed( "Dissolve Mask Pan Speed", Vector ) = ( 0, 0, 0, 0 )
		_DissolveMaskInvert( "Dissolve Mask Invert", Range( 0, 1 ) ) = 0
		_DissolveOffset( "Dissolve Offset", Float ) = 0
		[Space(33)][Header(Properties)][Space(13)] _EmissionIntensity( "Emission Intensity", Float ) = 1
		_CoreColor( "Core Color", Color ) = ( 1, 1, 1, 0 )
		_DifferentCoreColor( "Different Core Color", Float ) = 0
		_CorePower( "Core Power", Float ) = 1
		_CoreIntensity( "Core Intensity", Float ) = 0
		_GlowIntensity( "Glow Intensity", Float ) = 1
		_AlphaBoldness( "Alpha Boldness", Float ) = 1
		[Toggle( _CUSTOMPANSWITCH_ON )] _CustomPanSwitch( "CustomPanSwitch", Float ) = 0
		[Toggle( _MESHVERTEXCOLOR_ON )] _MeshVertexColor( "MeshVertexColor", Float ) = 0
		[Toggle( _STEP_ON )] _Step( "Step", Float ) = 0
		_ValueStep( "Value Step", Float ) = 0
		_ValueStepAdd( "Value Step Add", Float ) = 0.1
		[Space(33)][Header(Depth Fade)][Space(13)][Toggle( _USEDEPTHFADE_ON )] _UseDepthFade( "Use Depth Fade", Float ) = 0
		_DepthFadeIntensity( "Depth Fade Intensity", Float ) = 0
		[Space(33)][Header(Cutout)][Space(13)] _Cutout( "Cutout", 2D ) = "white" {}
		_CutoutErosion( "Cutout Erosion", Float ) = 0
		_CutoutErosionSmoothness( "Cutout Erosion Smoothness", Float ) = 0.05
		_CutoutRotation( "Cutout Rotation", Float ) = 0
		_CutoutOffset( "Cutout Offset", Vector ) = ( 0, 0, 0, 0 )
		[Space(33)][Header(AR)][Space(13)] _Cull( "Cull", Float ) = 2
		_Src( "Src", Float ) = 5
		_Dst( "Dst", Float ) = 10
		_ZWrite( "ZWrite", Float ) = 0
		_ZTest( "ZTest", Float ) = 2


		//_TessPhongStrength( "Tess Phong Strength", Range( 0, 1 ) ) = 0.5
		//_TessValue( "Tess Max Tessellation", Range( 1, 32 ) ) = 16
		//_TessMin( "Tess Min Distance", Float ) = 10
		//_TessMax( "Tess Max Distance", Float ) = 25
		//_TessEdgeLength ( "Tess Edge length", Range( 2, 50 ) ) = 16
		//_TessMaxDisp( "Tess Max Displacement", Float ) = 25

		[HideInInspector] _QueueOffset("_QueueOffset", Float) = 0
        [HideInInspector] _QueueControl("_QueueControl", Float) = -1

        [HideInInspector][NoScaleOffset] unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset] unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset] unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}

		//[HideInInspector][ToggleUI] _AddPrecomputedVelocity("Add Precomputed Velocity", Float) = 1
		//[ToggleUI] _ReceiveShadows("Receive Shadows", Float) = 1.0

		//[HideInInspector] _XRMotionVectorsPass("_XRMotionVectorsPass", Float) = 1
	}

	SubShader
	{
		LOD 0

		

		Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" "UniversalMaterialType"="Unlit" }

		Cull [_Cull]
		AlphaToMask Off

		

		HLSLINCLUDE
#pragma target 3.5
		#pragma prefer_hlslcc gles
		// ensure rendering platforms toggle list is visible

		#if ( SHADER_TARGET > 35 ) && defined( SHADER_API_GLES3 )
			#error For WebGL2/GLES3, please set your shader target to 3.5 via SubShader options. URP shaders in ASE use target 4.5 by default.
		#endif

		#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
		#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"

		#ifndef ASE_TESS_FUNCS
		#define ASE_TESS_FUNCS
		float4 FixedTess( float tessValue )
		{
			return tessValue;
		}

		float CalcDistanceTessFactor (float4 vertex, float minDist, float maxDist, float tess, float4x4 o2w, float3 cameraPos )
		{
			float3 wpos = mul(o2w,vertex).xyz;
			float dist = distance (wpos, cameraPos);
			float f = clamp(1.0 - (dist - minDist) / (maxDist - minDist), 0.01, 1.0) * tess;
			return f;
		}

		float4 CalcTriEdgeTessFactors (float3 triVertexFactors)
		{
			float4 tess;
			tess.x = 0.5 * (triVertexFactors.y + triVertexFactors.z);
			tess.y = 0.5 * (triVertexFactors.x + triVertexFactors.z);
			tess.z = 0.5 * (triVertexFactors.x + triVertexFactors.y);
			tess.w = (triVertexFactors.x + triVertexFactors.y + triVertexFactors.z) / 3.0f;
			return tess;
		}

		float CalcEdgeTessFactor (float3 wpos0, float3 wpos1, float edgeLen, float3 cameraPos, float4 scParams )
		{
			float dist = distance (0.5 * (wpos0+wpos1), cameraPos);
			float len = distance(wpos0, wpos1);
			float f = max(len * scParams.y / (edgeLen * dist), 1.0);
			return f;
		}

		float DistanceFromPlane (float3 pos, float4 plane)
		{
			float d = dot (float4(pos,1.0f), plane);
			return d;
		}

		bool WorldViewFrustumCull (float3 wpos0, float3 wpos1, float3 wpos2, float cullEps, float4 planes[6] )
		{
			float4 planeTest;
			planeTest.x = (( DistanceFromPlane(wpos0, planes[0]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos1, planes[0]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos2, planes[0]) > -cullEps) ? 1.0f : 0.0f );
			planeTest.y = (( DistanceFromPlane(wpos0, planes[1]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos1, planes[1]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos2, planes[1]) > -cullEps) ? 1.0f : 0.0f );
			planeTest.z = (( DistanceFromPlane(wpos0, planes[2]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos1, planes[2]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos2, planes[2]) > -cullEps) ? 1.0f : 0.0f );
			planeTest.w = (( DistanceFromPlane(wpos0, planes[3]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos1, planes[3]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos2, planes[3]) > -cullEps) ? 1.0f : 0.0f );
			return !all (planeTest);
		}

		float4 DistanceBasedTess( float4 v0, float4 v1, float4 v2, float tess, float minDist, float maxDist, float4x4 o2w, float3 cameraPos )
		{
			float3 f;
			f.x = CalcDistanceTessFactor (v0,minDist,maxDist,tess,o2w,cameraPos);
			f.y = CalcDistanceTessFactor (v1,minDist,maxDist,tess,o2w,cameraPos);
			f.z = CalcDistanceTessFactor (v2,minDist,maxDist,tess,o2w,cameraPos);

			return CalcTriEdgeTessFactors (f);
		}

		float4 EdgeLengthBasedTess( float4 v0, float4 v1, float4 v2, float edgeLength, float4x4 o2w, float3 cameraPos, float4 scParams )
		{
			float3 pos0 = mul(o2w,v0).xyz;
			float3 pos1 = mul(o2w,v1).xyz;
			float3 pos2 = mul(o2w,v2).xyz;
			float4 tess;
			tess.x = CalcEdgeTessFactor (pos1, pos2, edgeLength, cameraPos, scParams);
			tess.y = CalcEdgeTessFactor (pos2, pos0, edgeLength, cameraPos, scParams);
			tess.z = CalcEdgeTessFactor (pos0, pos1, edgeLength, cameraPos, scParams);
			tess.w = (tess.x + tess.y + tess.z) / 3.0f;
			return tess;
		}

		float4 EdgeLengthBasedTessCull( float4 v0, float4 v1, float4 v2, float edgeLength, float maxDisplacement, float4x4 o2w, float3 cameraPos, float4 scParams, float4 planes[6] )
		{
			float3 pos0 = mul(o2w,v0).xyz;
			float3 pos1 = mul(o2w,v1).xyz;
			float3 pos2 = mul(o2w,v2).xyz;
			float4 tess;

			if (WorldViewFrustumCull(pos0, pos1, pos2, maxDisplacement, planes))
			{
				tess = 0.0f;
			}
			else
			{
				tess.x = CalcEdgeTessFactor (pos1, pos2, edgeLength, cameraPos, scParams);
				tess.y = CalcEdgeTessFactor (pos2, pos0, edgeLength, cameraPos, scParams);
				tess.z = CalcEdgeTessFactor (pos0, pos1, edgeLength, cameraPos, scParams);
				tess.w = (tess.x + tess.y + tess.z) / 3.0f;
			}
			return tess;
		}
		#endif //ASE_TESS_FUNCS
		ENDHLSL

		
		Pass
		{
			
			Name "Forward"
			Tags { "LightMode"="UniversalForwardOnly" }

			Blend [_Src] [_Dst], One OneMinusSrcAlpha
			ZWrite Off
			ZTest [_ZTest]
			Offset 0 , 0
			ColorMask RGBA

			

			HLSLPROGRAM

			#pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
			#pragma multi_compile_instancing
			#pragma instancing_options renderinglayer
			#pragma multi_compile _ LOD_FADE_CROSSFADE
			#define _SURFACE_TYPE_TRANSPARENT 1
			#define ASE_VERSION 19905
			#define ASE_SRP_VERSION 170100
			#define REQUIRE_DEPTH_TEXTURE 1


			#pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3

			#pragma multi_compile_fragment _ DEBUG_DISPLAY

			#pragma vertex vert
			#pragma fragment frag

			#define SHADERPASS SHADERPASS_UNLIT

			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/Debugging3D.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl"

			#if defined(LOD_FADE_CROSSFADE)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #endif

			#define ASE_NEEDS_TEXTURE_COORDINATES0
			#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES0
			#define ASE_NEEDS_TEXTURE_COORDINATES1
			#define ASE_NEEDS_FRAG_COLOR
			#define ASE_NEEDS_FRAG_SCREEN_POSITION_NORMALIZED
			#pragma shader_feature_local _USEDEPTHFADE_ON
			#pragma shader_feature_local _CUSTOMPANSWITCH_ON
			#pragma shader_feature_local _STEP_ON
			#pragma shader_feature_local _MESHVERTEXCOLOR_ON


			#if defined(ASE_EARLY_Z_DEPTH_OPTIMIZE) && (SHADER_TARGET >= 45)
				#define ASE_SV_DEPTH SV_DepthLessEqual
				#define ASE_SV_POSITION_QUALIFIERS linear noperspective centroid
			#else
				#define ASE_SV_DEPTH SV_Depth
				#define ASE_SV_POSITION_QUALIFIERS
			#endif

			struct Attributes
			{
				float4 positionOS : POSITION;
				half3 normalOS : NORMAL;
				float4 ase_color : COLOR;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_texcoord1 : TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				ASE_SV_POSITION_QUALIFIERS float4 positionCS : SV_POSITION;
				float4 positionWSAndFogFactor : TEXCOORD0;
				half3 normalWS : TEXCOORD1;
				float4 ase_color : COLOR;
				float4 ase_texcoord2 : TEXCOORD2;
				float4 ase_texcoord3 : TEXCOORD3;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float4 _TextureChannel;
			float4 _Texture_ST;
			float4 _CoreColor;
			float4 _DissolveMask_ST;
			float4 _DissolveMaskChannel;
			float4 _DistortionMask_ST;
			float4 _DistortionMaskChannel;
			float2 _DissolveMaskPanSpeed;
			float2 _TexturePanSpeed;
			float2 _DistortionMaskPanSpeed;
			float2 _CutoutOffset;
			float _CoreIntensity;
			float _CutoutErosion;
			float _CutoutErosionSmoothness;
			float _CutoutRotation;
			float _EmissionIntensity;
			float _GlowIntensity;
			float _AlphaBoldness;
			float _ValueStep;
			float _DifferentCoreColor;
			float _Cull;
			float _DissolveOffset;
			float _TextureRotation;
			float _ValueStepAdd;
			float _DissolveMaskInvert;
			float _DistortionIntensity;
			float _DistortionMaskRotation;
			float _DissolveMaskRotation;
			float _ZTest;
			float _ZWrite;
			float _Dst;
			float _Src;
			float _CorePower;
			float _DepthFadeIntensity;
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			sampler2D _DissolveMask;
			sampler2D _DistortionMask;
			sampler2D _Texture;
			sampler2D _Cutout;


			
			PackedVaryings VertexFunction( Attributes input  )
			{
				PackedVaryings output = (PackedVaryings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				output.ase_color = input.ase_color;
				output.ase_texcoord2 = input.ase_texcoord;
				output.ase_texcoord3.xy = input.ase_texcoord1.xy;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				output.ase_texcoord3.zw = 0;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = input.positionOS.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = defaultVertexValue;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					input.positionOS.xyz = vertexValue;
				#else
					input.positionOS.xyz += vertexValue;
				#endif

				input.normalOS = input.normalOS;

				VertexPositionInputs vertexInput = GetVertexPositionInputs( input.positionOS.xyz );
				VertexNormalInputs normalInput = GetVertexNormalInputs( input.normalOS );

				float fogFactor = 0;
				#if defined(ASE_FOG) && !defined(_FOG_FRAGMENT)
					fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
				#endif

				output.positionCS = vertexInput.positionCS;
				output.positionWSAndFogFactor = float4( vertexInput.positionWS, fogFactor );
				output.normalWS = normalInput.normalWS;
				return output;
			}

			#if defined(ASE_TESSELLATION)
			struct VertexControl
			{
				float4 positionOS : INTERNALTESSPOS;
				half3 normalOS : NORMAL;
				float4 ase_color : COLOR;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_texcoord1 : TEXCOORD1;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( Attributes input )
			{
				VertexControl output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				output.positionOS = input.positionOS;
				output.normalOS = input.normalOS;
				output.ase_color = input.ase_color;
				output.ase_texcoord = input.ase_texcoord;
				output.ase_texcoord1 = input.ase_texcoord1;
				return output;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> input)
			{
				TessellationFactors output;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				output.edge[0] = tf.x; output.edge[1] = tf.y; output.edge[2] = tf.z; output.inside = tf.w;
				return output;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
				return patch[id];
			}

			[domain("tri")]
			PackedVaryings DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				Attributes output = (Attributes) 0;
				output.positionOS = patch[0].positionOS * bary.x + patch[1].positionOS * bary.y + patch[2].positionOS * bary.z;
				output.normalOS = patch[0].normalOS * bary.x + patch[1].normalOS * bary.y + patch[2].normalOS * bary.z;
				output.ase_color = patch[0].ase_color * bary.x + patch[1].ase_color * bary.y + patch[2].ase_color * bary.z;
				output.ase_texcoord = patch[0].ase_texcoord * bary.x + patch[1].ase_texcoord * bary.y + patch[2].ase_texcoord * bary.z;
				output.ase_texcoord1 = patch[0].ase_texcoord1 * bary.x + patch[1].ase_texcoord1 * bary.y + patch[2].ase_texcoord1 * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = output.positionOS.xyz - patch[i].normalOS * (dot(output.positionOS.xyz, patch[i].normalOS) - dot(patch[i].positionOS.xyz, patch[i].normalOS));
				float phongStrength = _TessPhongStrength;
				output.positionOS.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * output.positionOS.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], output);
				return VertexFunction(output);
			}
			#else
			PackedVaryings vert ( Attributes input )
			{
				return VertexFunction( input );
			}
			#endif

			half4 frag ( PackedVaryings input
						#if defined( ASE_DEPTH_WRITE_ON )
						,out float outputDepth : ASE_SV_DEPTH
						#endif
						#ifdef _WRITE_RENDERING_LAYERS
						, out float4 outRenderingLayers : SV_Target1
						#endif
						 ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

				#if defined( _SURFACE_TYPE_TRANSPARENT )
					const bool isTransparent = true;
				#else
					const bool isTransparent = false;
				#endif

				#if defined(LOD_FADE_CROSSFADE)
					LODFadeCrossFade( input.positionCS );
				#endif

				#if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
					float4 shadowCoord = TransformWorldToShadowCoord( input.positionWSAndFogFactor.xyz );
				#else
					float4 shadowCoord = float4(0, 0, 0, 0);
				#endif

				float3 PositionWS = input.positionWSAndFogFactor.xyz;
				float3 PositionRWS = GetCameraRelativePositionWS( PositionWS );
				half3 ViewDirWS = GetWorldSpaceNormalizeViewDir( PositionWS );
				float4 ShadowCoord = shadowCoord;
				float4 ScreenPosNorm = float4( GetNormalizedScreenSpaceUV( input.positionCS ), input.positionCS.zw );
				float4 ClipPos = ComputeClipSpacePosition( ScreenPosNorm.xy, input.positionCS.z ) * input.positionCS.w;
				float4 ScreenPos = ComputeScreenPos( ClipPos );
				half3 NormalWS = normalize( input.normalWS );

				float3 temp_output_157_0 = (input.ase_color).rgb;
				float2 uv_DissolveMask = input.ase_texcoord2.xy * _DissolveMask_ST.xy + _DissolveMask_ST.zw;
				float cos112 = cos( radians( _DissolveMaskRotation ) );
				float sin112 = sin( radians( _DissolveMaskRotation ) );
				float2 rotator112 = mul( uv_DissolveMask - float2( 0.5,0.5 ) , float2x2( cos112 , -sin112 , sin112 , cos112 )) + float2( 0.5,0.5 );
				float4 uvs4_DissolveMask = input.ase_texcoord2;
				uvs4_DissolveMask.xy = input.ase_texcoord2.xy * _DissolveMask_ST.xy + _DissolveMask_ST.zw;
				float2 temp_cast_2 = (0.0).xx;
				float2 texCoord83 = input.ase_texcoord3.xy * float2( 1,1 ) + float2( 0,0 );
				#ifdef _CUSTOMPANSWITCH_ON
				float2 staticSwitch85 = texCoord83;
				#else
				float2 staticSwitch85 = temp_cast_2;
				#endif
				float2 CustomUV89 = staticSwitch85;
				float2 uv_DistortionMask = input.ase_texcoord2.xy * _DistortionMask_ST.xy + _DistortionMask_ST.zw;
				float cos95 = cos( radians( _DistortionMaskRotation ) );
				float sin95 = sin( radians( _DistortionMaskRotation ) );
				float2 rotator95 = mul( uv_DistortionMask - float2( 0.5,0.5 ) , float2x2( cos95 , -sin95 , sin95 , cos95 )) + float2( 0.5,0.5 );
				float4 uvs4_DistortionMask = input.ase_texcoord2;
				uvs4_DistortionMask.xy = input.ase_texcoord2.xy * _DistortionMask_ST.xy + _DistortionMask_ST.zw;
				float dotResult100 = dot( tex2D( _DistortionMask, ( rotator95 + uvs4_DistortionMask.w + CustomUV89 + ( _TimeParameters.x * _DistortionMaskPanSpeed ) ) ) , _DistortionMaskChannel );
				float Disto107 = ( saturate( dotResult100 ) * _DistortionIntensity );
				float dotResult122 = dot( tex2D( _DissolveMask, ( rotator112 + uvs4_DissolveMask.w + CustomUV89 + ( _TimeParameters.x * _DissolveMaskPanSpeed ) + Disto107 ) ) , _DissolveMaskChannel );
				float temp_output_126_0 = saturate( dotResult122 );
				float lerpResult138 = lerp( temp_output_126_0 , saturate( ( 1.0 - temp_output_126_0 ) ) , _DissolveMaskInvert);
				float4 texCoord141 = input.ase_texcoord2;
				texCoord141.xy = input.ase_texcoord2.xy * float2( 1,1 ) + float2( 0,0 );
				float2 uv_Texture = input.ase_texcoord2.xy * _Texture_ST.xy + _Texture_ST.zw;
				float cos129 = cos( radians( _TextureRotation ) );
				float sin129 = sin( radians( _TextureRotation ) );
				float2 rotator129 = mul( uv_Texture - float2( 0.5,0.5 ) , float2x2( cos129 , -sin129 , sin129 , cos129 )) + float2( 0.5,0.5 );
				float dotResult140 = dot( tex2D( _Texture, ( rotator129 + ( _TimeParameters.x * _TexturePanSpeed ) + CustomUV89 + Disto107 ) ) , _TextureChannel );
				float temp_output_147_0 = ( ( saturate( lerpResult138 ) + texCoord141.z + _DissolveOffset ) * saturate( dotResult140 ) );
				float temp_output_261_0 = saturate( ( pow( temp_output_147_0 , _CorePower ) * _CoreIntensity ) );
				float2 texCoord248 = input.ase_texcoord2.xy * float2( 1,1 ) + float2( 0,0 );
				float2 temp_output_266_0 = ( texCoord248 + _CutoutOffset );
				float cos258 = cos( radians( _CutoutRotation ) );
				float sin258 = sin( radians( _CutoutRotation ) );
				float2 rotator258 = mul( temp_output_266_0 - float2( 0.5,0.5 ) , float2x2( cos258 , -sin258 , sin258 , cos258 )) + float2( 0.5,0.5 );
				float smoothstepResult252 = smoothstep( _CutoutErosion , ( _CutoutErosion + _CutoutErosionSmoothness ) , tex2D( _Cutout, rotator258 ).g);
				float cutout256 = smoothstepResult252;
				float4 lerpResult188 = lerp( float4( temp_output_157_0 , 0.0 ) , _CoreColor , saturate( ( temp_output_261_0 * cutout256 ) ));
				float4 lerpResult217 = lerp( float4( temp_output_157_0 , 0.0 ) , saturate( lerpResult188 ) , _DifferentCoreColor);
				float3 temp_cast_6 = (1.0).xxx;
				#ifdef _MESHVERTEXCOLOR_ON
				float3 staticSwitch159 = temp_output_157_0;
				#else
				float3 staticSwitch159 = temp_cast_6;
				#endif
				float3 temp_output_167_0 = saturate( ( ( input.ase_color.a * saturate( ( saturate( ( temp_output_261_0 + saturate( ( temp_output_147_0 * _GlowIntensity ) ) ) ) * cutout256 ) ) * staticSwitch159 ) * _AlphaBoldness ) );
				float3 temp_cast_7 = (_ValueStep).xxx;
				float3 temp_cast_8 = (( _ValueStep + _ValueStepAdd )).xxx;
				float3 smoothstepResult170 = smoothstep( temp_cast_7 , temp_cast_8 , temp_output_167_0);
				#ifdef _STEP_ON
				float3 staticSwitch183 = saturate( smoothstepResult170 );
				#else
				float3 staticSwitch183 = temp_output_167_0;
				#endif
				float4 temp_output_234_0 = ( ( saturate( lerpResult217 ) * _EmissionIntensity ) * float4( staticSwitch183 , 0.0 ) );
				float screenDepth213 = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH( ScreenPosNorm.xy ),_ZBufferParams);
				float distanceDepth213 = ( screenDepth213 - LinearEyeDepth( ScreenPosNorm.z,_ZBufferParams ) ) / ( _DepthFadeIntensity );
				float temp_output_236_0 = saturate( distanceDepth213 );
				#ifdef _USEDEPTHFADE_ON
				float4 staticSwitch238 = ( temp_output_234_0 * temp_output_236_0 );
				#else
				float4 staticSwitch238 = temp_output_234_0;
				#endif
				
				#ifdef _USEDEPTHFADE_ON
				float3 staticSwitch239 = ( staticSwitch183 * temp_output_236_0 );
				#else
				float3 staticSwitch239 = staticSwitch183;
				#endif
				
				float3 BakedAlbedo = 0;
				float3 BakedEmission = 0;
				float3 Color = staticSwitch238.rgb;
				float Alpha = staticSwitch239.x;
				float AlphaClipThreshold = 0.5;
				float AlphaClipThresholdShadow = 0.5;

				#if defined( ASE_DEPTH_WRITE_ON )
					float DeviceDepth = input.positionCS.z;
				#endif

				#if defined( _ALPHATEST_ON )
					AlphaDiscard( Alpha, AlphaClipThreshold );
				#endif

				#if defined(MAIN_LIGHT_CALCULATE_SHADOWS) && defined(ASE_CHANGES_WORLD_POS)
					ShadowCoord = TransformWorldToShadowCoord( PositionWS );
				#endif

				InputData inputData = (InputData)0;
				inputData.positionWS = PositionWS;
				inputData.positionCS = float4( input.positionCS.xy, ClipPos.zw / ClipPos.w );
				inputData.normalizedScreenSpaceUV = ScreenPosNorm.xy;
				inputData.normalWS = NormalWS;
				inputData.viewDirectionWS = ViewDirWS;

				#if defined(_SCREEN_SPACE_OCCLUSION) && !defined(_SURFACE_TYPE_TRANSPARENT)
					float2 normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
					AmbientOcclusionFactor aoFactor = GetScreenSpaceAmbientOcclusion(normalizedScreenSpaceUV);
					Color.rgb *= aoFactor.directAmbientOcclusion;
				#endif

				#ifdef ASE_FOG
					inputData.fogCoord = InitializeInputDataFog(float4(inputData.positionWS, 1.0), input.positionWSAndFogFactor.w);
				#endif

				#if defined(_DBUFFER)
					ApplyDecalToBaseColor(input.positionCS, Color);
				#endif

				#ifdef ASE_FOG
					#ifdef TERRAIN_SPLAT_ADDPASS
						Color.rgb = MixFogColor(Color.rgb, half3(0,0,0), inputData.fogCoord);
					#else
						Color.rgb = MixFog(Color.rgb, inputData.fogCoord);
					#endif
				#endif

				#if defined( ASE_DEPTH_WRITE_ON )
					outputDepth = DeviceDepth;
				#endif

				#ifdef _WRITE_RENDERING_LAYERS
					uint renderingLayers = GetMeshRenderingLayer();
					outRenderingLayers = float4( EncodeMeshRenderingLayer( renderingLayers ), 0, 0, 0 );
				#endif

				#if defined( ASE_OPAQUE_KEEP_ALPHA )
					return half4( Color, Alpha );
				#else
					return half4( Color, OutputAlpha( Alpha, isTransparent ) );
				#endif
			}
			ENDHLSL
		}

		
		Pass
		{
			
			Name "DepthOnly"
			Tags { "LightMode"="DepthOnly" }

			ZWrite On
			ColorMask 0
			AlphaToMask Off

			HLSLPROGRAM

			#pragma multi_compile_instancing
			#pragma multi_compile _ LOD_FADE_CROSSFADE
			#define _SURFACE_TYPE_TRANSPARENT 1
			#define ASE_VERSION 19905
			#define ASE_SRP_VERSION 170100
			#define REQUIRE_DEPTH_TEXTURE 1


			#pragma vertex vert
			#pragma fragment frag

			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"

			#if defined(LOD_FADE_CROSSFADE)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #endif

			#define ASE_NEEDS_TEXTURE_COORDINATES0
			#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES0
			#define ASE_NEEDS_TEXTURE_COORDINATES1
			#define ASE_NEEDS_FRAG_COLOR
			#define ASE_NEEDS_FRAG_SCREEN_POSITION_NORMALIZED
			#pragma shader_feature_local _USEDEPTHFADE_ON
			#pragma shader_feature_local _STEP_ON
			#pragma shader_feature_local _CUSTOMPANSWITCH_ON
			#pragma shader_feature_local _MESHVERTEXCOLOR_ON


			#if defined(ASE_EARLY_Z_DEPTH_OPTIMIZE) && (SHADER_TARGET >= 45)
				#define ASE_SV_DEPTH SV_DepthLessEqual
				#define ASE_SV_POSITION_QUALIFIERS linear noperspective centroid
			#else
				#define ASE_SV_DEPTH SV_Depth
				#define ASE_SV_POSITION_QUALIFIERS
			#endif

			struct Attributes
			{
				float4 positionOS : POSITION;
				half3 normalOS : NORMAL;
				float4 ase_color : COLOR;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_texcoord1 : TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				ASE_SV_POSITION_QUALIFIERS float4 positionCS : SV_POSITION;
				float4 ase_color : COLOR;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_texcoord1 : TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float4 _TextureChannel;
			float4 _Texture_ST;
			float4 _CoreColor;
			float4 _DissolveMask_ST;
			float4 _DissolveMaskChannel;
			float4 _DistortionMask_ST;
			float4 _DistortionMaskChannel;
			float2 _DissolveMaskPanSpeed;
			float2 _TexturePanSpeed;
			float2 _DistortionMaskPanSpeed;
			float2 _CutoutOffset;
			float _CoreIntensity;
			float _CutoutErosion;
			float _CutoutErosionSmoothness;
			float _CutoutRotation;
			float _EmissionIntensity;
			float _GlowIntensity;
			float _AlphaBoldness;
			float _ValueStep;
			float _DifferentCoreColor;
			float _Cull;
			float _DissolveOffset;
			float _TextureRotation;
			float _ValueStepAdd;
			float _DissolveMaskInvert;
			float _DistortionIntensity;
			float _DistortionMaskRotation;
			float _DissolveMaskRotation;
			float _ZTest;
			float _ZWrite;
			float _Dst;
			float _Src;
			float _CorePower;
			float _DepthFadeIntensity;
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			sampler2D _DissolveMask;
			sampler2D _DistortionMask;
			sampler2D _Texture;
			sampler2D _Cutout;


			
			PackedVaryings VertexFunction( Attributes input  )
			{
				PackedVaryings output = (PackedVaryings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				output.ase_color = input.ase_color;
				output.ase_texcoord = input.ase_texcoord;
				output.ase_texcoord1.xy = input.ase_texcoord1.xy;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				output.ase_texcoord1.zw = 0;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = input.positionOS.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = defaultVertexValue;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					input.positionOS.xyz = vertexValue;
				#else
					input.positionOS.xyz += vertexValue;
				#endif

				VertexPositionInputs vertexInput = GetVertexPositionInputs( input.positionOS.xyz );

				output.positionCS = vertexInput.positionCS;
				return output;
			}

			#if defined(ASE_TESSELLATION)
			struct VertexControl
			{
				float4 positionOS : INTERNALTESSPOS;
				half3 normalOS : NORMAL;
				float4 ase_color : COLOR;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_texcoord1 : TEXCOORD1;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( Attributes input )
			{
				VertexControl output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				output.positionOS = input.positionOS;
				output.normalOS = input.normalOS;
				output.ase_color = input.ase_color;
				output.ase_texcoord = input.ase_texcoord;
				output.ase_texcoord1 = input.ase_texcoord1;
				return output;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> input)
			{
				TessellationFactors output;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				output.edge[0] = tf.x; output.edge[1] = tf.y; output.edge[2] = tf.z; output.inside = tf.w;
				return output;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
				return patch[id];
			}

			[domain("tri")]
			PackedVaryings DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				Attributes output = (Attributes) 0;
				output.positionOS = patch[0].positionOS * bary.x + patch[1].positionOS * bary.y + patch[2].positionOS * bary.z;
				output.normalOS = patch[0].normalOS * bary.x + patch[1].normalOS * bary.y + patch[2].normalOS * bary.z;
				output.ase_color = patch[0].ase_color * bary.x + patch[1].ase_color * bary.y + patch[2].ase_color * bary.z;
				output.ase_texcoord = patch[0].ase_texcoord * bary.x + patch[1].ase_texcoord * bary.y + patch[2].ase_texcoord * bary.z;
				output.ase_texcoord1 = patch[0].ase_texcoord1 * bary.x + patch[1].ase_texcoord1 * bary.y + patch[2].ase_texcoord1 * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = output.positionOS.xyz - patch[i].normalOS * (dot(output.positionOS.xyz, patch[i].normalOS) - dot(patch[i].positionOS.xyz, patch[i].normalOS));
				float phongStrength = _TessPhongStrength;
				output.positionOS.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * output.positionOS.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], output);
				return VertexFunction(output);
			}
			#else
			PackedVaryings vert ( Attributes input )
			{
				return VertexFunction( input );
			}
			#endif

			half4 frag(PackedVaryings input
						#if defined( ASE_DEPTH_WRITE_ON )
						,out float outputDepth : ASE_SV_DEPTH
						#endif
						 ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( input );

				float4 ScreenPosNorm = float4( GetNormalizedScreenSpaceUV( input.positionCS ), input.positionCS.zw );
				float4 ClipPos = ComputeClipSpacePosition( ScreenPosNorm.xy, input.positionCS.z ) * input.positionCS.w;
				float4 ScreenPos = ComputeScreenPos( ClipPos );

				float2 uv_DissolveMask = input.ase_texcoord.xy * _DissolveMask_ST.xy + _DissolveMask_ST.zw;
				float cos112 = cos( radians( _DissolveMaskRotation ) );
				float sin112 = sin( radians( _DissolveMaskRotation ) );
				float2 rotator112 = mul( uv_DissolveMask - float2( 0.5,0.5 ) , float2x2( cos112 , -sin112 , sin112 , cos112 )) + float2( 0.5,0.5 );
				float4 uvs4_DissolveMask = input.ase_texcoord;
				uvs4_DissolveMask.xy = input.ase_texcoord.xy * _DissolveMask_ST.xy + _DissolveMask_ST.zw;
				float2 temp_cast_0 = (0.0).xx;
				float2 texCoord83 = input.ase_texcoord1.xy * float2( 1,1 ) + float2( 0,0 );
				#ifdef _CUSTOMPANSWITCH_ON
				float2 staticSwitch85 = texCoord83;
				#else
				float2 staticSwitch85 = temp_cast_0;
				#endif
				float2 CustomUV89 = staticSwitch85;
				float2 uv_DistortionMask = input.ase_texcoord.xy * _DistortionMask_ST.xy + _DistortionMask_ST.zw;
				float cos95 = cos( radians( _DistortionMaskRotation ) );
				float sin95 = sin( radians( _DistortionMaskRotation ) );
				float2 rotator95 = mul( uv_DistortionMask - float2( 0.5,0.5 ) , float2x2( cos95 , -sin95 , sin95 , cos95 )) + float2( 0.5,0.5 );
				float4 uvs4_DistortionMask = input.ase_texcoord;
				uvs4_DistortionMask.xy = input.ase_texcoord.xy * _DistortionMask_ST.xy + _DistortionMask_ST.zw;
				float dotResult100 = dot( tex2D( _DistortionMask, ( rotator95 + uvs4_DistortionMask.w + CustomUV89 + ( _TimeParameters.x * _DistortionMaskPanSpeed ) ) ) , _DistortionMaskChannel );
				float Disto107 = ( saturate( dotResult100 ) * _DistortionIntensity );
				float dotResult122 = dot( tex2D( _DissolveMask, ( rotator112 + uvs4_DissolveMask.w + CustomUV89 + ( _TimeParameters.x * _DissolveMaskPanSpeed ) + Disto107 ) ) , _DissolveMaskChannel );
				float temp_output_126_0 = saturate( dotResult122 );
				float lerpResult138 = lerp( temp_output_126_0 , saturate( ( 1.0 - temp_output_126_0 ) ) , _DissolveMaskInvert);
				float4 texCoord141 = input.ase_texcoord;
				texCoord141.xy = input.ase_texcoord.xy * float2( 1,1 ) + float2( 0,0 );
				float2 uv_Texture = input.ase_texcoord.xy * _Texture_ST.xy + _Texture_ST.zw;
				float cos129 = cos( radians( _TextureRotation ) );
				float sin129 = sin( radians( _TextureRotation ) );
				float2 rotator129 = mul( uv_Texture - float2( 0.5,0.5 ) , float2x2( cos129 , -sin129 , sin129 , cos129 )) + float2( 0.5,0.5 );
				float dotResult140 = dot( tex2D( _Texture, ( rotator129 + ( _TimeParameters.x * _TexturePanSpeed ) + CustomUV89 + Disto107 ) ) , _TextureChannel );
				float temp_output_147_0 = ( ( saturate( lerpResult138 ) + texCoord141.z + _DissolveOffset ) * saturate( dotResult140 ) );
				float temp_output_261_0 = saturate( ( pow( temp_output_147_0 , _CorePower ) * _CoreIntensity ) );
				float2 texCoord248 = input.ase_texcoord.xy * float2( 1,1 ) + float2( 0,0 );
				float2 temp_output_266_0 = ( texCoord248 + _CutoutOffset );
				float cos258 = cos( radians( _CutoutRotation ) );
				float sin258 = sin( radians( _CutoutRotation ) );
				float2 rotator258 = mul( temp_output_266_0 - float2( 0.5,0.5 ) , float2x2( cos258 , -sin258 , sin258 , cos258 )) + float2( 0.5,0.5 );
				float smoothstepResult252 = smoothstep( _CutoutErosion , ( _CutoutErosion + _CutoutErosionSmoothness ) , tex2D( _Cutout, rotator258 ).g);
				float cutout256 = smoothstepResult252;
				float3 temp_cast_4 = (1.0).xxx;
				float3 temp_output_157_0 = (input.ase_color).rgb;
				#ifdef _MESHVERTEXCOLOR_ON
				float3 staticSwitch159 = temp_output_157_0;
				#else
				float3 staticSwitch159 = temp_cast_4;
				#endif
				float3 temp_output_167_0 = saturate( ( ( input.ase_color.a * saturate( ( saturate( ( temp_output_261_0 + saturate( ( temp_output_147_0 * _GlowIntensity ) ) ) ) * cutout256 ) ) * staticSwitch159 ) * _AlphaBoldness ) );
				float3 temp_cast_5 = (_ValueStep).xxx;
				float3 temp_cast_6 = (( _ValueStep + _ValueStepAdd )).xxx;
				float3 smoothstepResult170 = smoothstep( temp_cast_5 , temp_cast_6 , temp_output_167_0);
				#ifdef _STEP_ON
				float3 staticSwitch183 = saturate( smoothstepResult170 );
				#else
				float3 staticSwitch183 = temp_output_167_0;
				#endif
				float screenDepth213 = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH( ScreenPosNorm.xy ),_ZBufferParams);
				float distanceDepth213 = ( screenDepth213 - LinearEyeDepth( ScreenPosNorm.z,_ZBufferParams ) ) / ( _DepthFadeIntensity );
				float temp_output_236_0 = saturate( distanceDepth213 );
				#ifdef _USEDEPTHFADE_ON
				float3 staticSwitch239 = ( staticSwitch183 * temp_output_236_0 );
				#else
				float3 staticSwitch239 = staticSwitch183;
				#endif
				

				float Alpha = staticSwitch239.x;
				float AlphaClipThreshold = 0.5;

				#if defined( ASE_DEPTH_WRITE_ON )
					float DeviceDepth = input.positionCS.z;
				#endif

				#if defined( _ALPHATEST_ON )
					AlphaDiscard( Alpha, AlphaClipThreshold );
				#endif

				#if defined(LOD_FADE_CROSSFADE)
					LODFadeCrossFade( input.positionCS );
				#endif

				#if defined( ASE_DEPTH_WRITE_ON )
					outputDepth = DeviceDepth;
				#endif

				return 0;
			}
			ENDHLSL
		}

		
		Pass
		{
			
			Name "SceneSelectionPass"
			Tags { "LightMode"="SceneSelectionPass" }

			Cull Off
			AlphaToMask Off

			HLSLPROGRAM

			#define _SURFACE_TYPE_TRANSPARENT 1
			#define ASE_VERSION 19905
			#define ASE_SRP_VERSION 170100
			#define REQUIRE_DEPTH_TEXTURE 1


			#pragma vertex vert
			#pragma fragment frag

			#define ATTRIBUTES_NEED_NORMAL
			#define ATTRIBUTES_NEED_TANGENT
			#define SHADERPASS SHADERPASS_DEPTHONLY

			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#define ASE_NEEDS_TEXTURE_COORDINATES0
			#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES0
			#define ASE_NEEDS_TEXTURE_COORDINATES1
			#define ASE_NEEDS_FRAG_COLOR
			#pragma shader_feature_local _USEDEPTHFADE_ON
			#pragma shader_feature_local _STEP_ON
			#pragma shader_feature_local _CUSTOMPANSWITCH_ON
			#pragma shader_feature_local _MESHVERTEXCOLOR_ON


			struct Attributes
			{
				float4 positionOS : POSITION;
				half3 normalOS : NORMAL;
				float4 ase_color : COLOR;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_texcoord1 : TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				float4 positionCS : SV_POSITION;
				float4 ase_color : COLOR;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_texcoord1 : TEXCOORD1;
				float4 ase_texcoord2 : TEXCOORD2;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float4 _TextureChannel;
			float4 _Texture_ST;
			float4 _CoreColor;
			float4 _DissolveMask_ST;
			float4 _DissolveMaskChannel;
			float4 _DistortionMask_ST;
			float4 _DistortionMaskChannel;
			float2 _DissolveMaskPanSpeed;
			float2 _TexturePanSpeed;
			float2 _DistortionMaskPanSpeed;
			float2 _CutoutOffset;
			float _CoreIntensity;
			float _CutoutErosion;
			float _CutoutErosionSmoothness;
			float _CutoutRotation;
			float _EmissionIntensity;
			float _GlowIntensity;
			float _AlphaBoldness;
			float _ValueStep;
			float _DifferentCoreColor;
			float _Cull;
			float _DissolveOffset;
			float _TextureRotation;
			float _ValueStepAdd;
			float _DissolveMaskInvert;
			float _DistortionIntensity;
			float _DistortionMaskRotation;
			float _DissolveMaskRotation;
			float _ZTest;
			float _ZWrite;
			float _Dst;
			float _Src;
			float _CorePower;
			float _DepthFadeIntensity;
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			sampler2D _DissolveMask;
			sampler2D _DistortionMask;
			sampler2D _Texture;
			sampler2D _Cutout;


			
			int _ObjectId;
			int _PassValue;

			struct SurfaceDescription
			{
				float Alpha;
				float AlphaClipThreshold;
			};

			PackedVaryings VertexFunction(Attributes input  )
			{
				PackedVaryings output;
				ZERO_INITIALIZE(PackedVaryings, output);

				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				float4 ase_positionCS = TransformObjectToHClip( ( input.positionOS ).xyz );
				float4 screenPos = ComputeScreenPos( ase_positionCS );
				output.ase_texcoord2 = screenPos;
				
				output.ase_color = input.ase_color;
				output.ase_texcoord = input.ase_texcoord;
				output.ase_texcoord1.xy = input.ase_texcoord1.xy;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				output.ase_texcoord1.zw = 0;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = input.positionOS.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = defaultVertexValue;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					input.positionOS.xyz = vertexValue;
				#else
					input.positionOS.xyz += vertexValue;
				#endif

				VertexPositionInputs vertexInput = GetVertexPositionInputs( input.positionOS.xyz );

				output.positionCS = vertexInput.positionCS;
				return output;
			}

			#if defined(ASE_TESSELLATION)
			struct VertexControl
			{
				float4 positionOS : INTERNALTESSPOS;
				half3 normalOS : NORMAL;
				float4 ase_color : COLOR;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_texcoord1 : TEXCOORD1;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( Attributes input )
			{
				VertexControl output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				output.positionOS = input.positionOS;
				output.normalOS = input.normalOS;
				output.ase_color = input.ase_color;
				output.ase_texcoord = input.ase_texcoord;
				output.ase_texcoord1 = input.ase_texcoord1;
				return output;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> input)
			{
				TessellationFactors output;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				output.edge[0] = tf.x; output.edge[1] = tf.y; output.edge[2] = tf.z; output.inside = tf.w;
				return output;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
				return patch[id];
			}

			[domain("tri")]
			PackedVaryings DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				Attributes output = (Attributes) 0;
				output.positionOS = patch[0].positionOS * bary.x + patch[1].positionOS * bary.y + patch[2].positionOS * bary.z;
				output.normalOS = patch[0].normalOS * bary.x + patch[1].normalOS * bary.y + patch[2].normalOS * bary.z;
				output.ase_color = patch[0].ase_color * bary.x + patch[1].ase_color * bary.y + patch[2].ase_color * bary.z;
				output.ase_texcoord = patch[0].ase_texcoord * bary.x + patch[1].ase_texcoord * bary.y + patch[2].ase_texcoord * bary.z;
				output.ase_texcoord1 = patch[0].ase_texcoord1 * bary.x + patch[1].ase_texcoord1 * bary.y + patch[2].ase_texcoord1 * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = output.positionOS.xyz - patch[i].normalOS * (dot(output.positionOS.xyz, patch[i].normalOS) - dot(patch[i].positionOS.xyz, patch[i].normalOS));
				float phongStrength = _TessPhongStrength;
				output.positionOS.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * output.positionOS.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], output);
				return VertexFunction(output);
			}
			#else
			PackedVaryings vert ( Attributes input )
			{
				return VertexFunction( input );
			}
			#endif

			half4 frag(PackedVaryings input ) : SV_Target
			{
				SurfaceDescription surfaceDescription = (SurfaceDescription)0;

				float2 uv_DissolveMask = input.ase_texcoord.xy * _DissolveMask_ST.xy + _DissolveMask_ST.zw;
				float cos112 = cos( radians( _DissolveMaskRotation ) );
				float sin112 = sin( radians( _DissolveMaskRotation ) );
				float2 rotator112 = mul( uv_DissolveMask - float2( 0.5,0.5 ) , float2x2( cos112 , -sin112 , sin112 , cos112 )) + float2( 0.5,0.5 );
				float4 uvs4_DissolveMask = input.ase_texcoord;
				uvs4_DissolveMask.xy = input.ase_texcoord.xy * _DissolveMask_ST.xy + _DissolveMask_ST.zw;
				float2 temp_cast_0 = (0.0).xx;
				float2 texCoord83 = input.ase_texcoord1.xy * float2( 1,1 ) + float2( 0,0 );
				#ifdef _CUSTOMPANSWITCH_ON
				float2 staticSwitch85 = texCoord83;
				#else
				float2 staticSwitch85 = temp_cast_0;
				#endif
				float2 CustomUV89 = staticSwitch85;
				float2 uv_DistortionMask = input.ase_texcoord.xy * _DistortionMask_ST.xy + _DistortionMask_ST.zw;
				float cos95 = cos( radians( _DistortionMaskRotation ) );
				float sin95 = sin( radians( _DistortionMaskRotation ) );
				float2 rotator95 = mul( uv_DistortionMask - float2( 0.5,0.5 ) , float2x2( cos95 , -sin95 , sin95 , cos95 )) + float2( 0.5,0.5 );
				float4 uvs4_DistortionMask = input.ase_texcoord;
				uvs4_DistortionMask.xy = input.ase_texcoord.xy * _DistortionMask_ST.xy + _DistortionMask_ST.zw;
				float dotResult100 = dot( tex2D( _DistortionMask, ( rotator95 + uvs4_DistortionMask.w + CustomUV89 + ( _TimeParameters.x * _DistortionMaskPanSpeed ) ) ) , _DistortionMaskChannel );
				float Disto107 = ( saturate( dotResult100 ) * _DistortionIntensity );
				float dotResult122 = dot( tex2D( _DissolveMask, ( rotator112 + uvs4_DissolveMask.w + CustomUV89 + ( _TimeParameters.x * _DissolveMaskPanSpeed ) + Disto107 ) ) , _DissolveMaskChannel );
				float temp_output_126_0 = saturate( dotResult122 );
				float lerpResult138 = lerp( temp_output_126_0 , saturate( ( 1.0 - temp_output_126_0 ) ) , _DissolveMaskInvert);
				float4 texCoord141 = input.ase_texcoord;
				texCoord141.xy = input.ase_texcoord.xy * float2( 1,1 ) + float2( 0,0 );
				float2 uv_Texture = input.ase_texcoord.xy * _Texture_ST.xy + _Texture_ST.zw;
				float cos129 = cos( radians( _TextureRotation ) );
				float sin129 = sin( radians( _TextureRotation ) );
				float2 rotator129 = mul( uv_Texture - float2( 0.5,0.5 ) , float2x2( cos129 , -sin129 , sin129 , cos129 )) + float2( 0.5,0.5 );
				float dotResult140 = dot( tex2D( _Texture, ( rotator129 + ( _TimeParameters.x * _TexturePanSpeed ) + CustomUV89 + Disto107 ) ) , _TextureChannel );
				float temp_output_147_0 = ( ( saturate( lerpResult138 ) + texCoord141.z + _DissolveOffset ) * saturate( dotResult140 ) );
				float temp_output_261_0 = saturate( ( pow( temp_output_147_0 , _CorePower ) * _CoreIntensity ) );
				float2 texCoord248 = input.ase_texcoord.xy * float2( 1,1 ) + float2( 0,0 );
				float2 temp_output_266_0 = ( texCoord248 + _CutoutOffset );
				float cos258 = cos( radians( _CutoutRotation ) );
				float sin258 = sin( radians( _CutoutRotation ) );
				float2 rotator258 = mul( temp_output_266_0 - float2( 0.5,0.5 ) , float2x2( cos258 , -sin258 , sin258 , cos258 )) + float2( 0.5,0.5 );
				float smoothstepResult252 = smoothstep( _CutoutErosion , ( _CutoutErosion + _CutoutErosionSmoothness ) , tex2D( _Cutout, rotator258 ).g);
				float cutout256 = smoothstepResult252;
				float3 temp_cast_4 = (1.0).xxx;
				float3 temp_output_157_0 = (input.ase_color).rgb;
				#ifdef _MESHVERTEXCOLOR_ON
				float3 staticSwitch159 = temp_output_157_0;
				#else
				float3 staticSwitch159 = temp_cast_4;
				#endif
				float3 temp_output_167_0 = saturate( ( ( input.ase_color.a * saturate( ( saturate( ( temp_output_261_0 + saturate( ( temp_output_147_0 * _GlowIntensity ) ) ) ) * cutout256 ) ) * staticSwitch159 ) * _AlphaBoldness ) );
				float3 temp_cast_5 = (_ValueStep).xxx;
				float3 temp_cast_6 = (( _ValueStep + _ValueStepAdd )).xxx;
				float3 smoothstepResult170 = smoothstep( temp_cast_5 , temp_cast_6 , temp_output_167_0);
				#ifdef _STEP_ON
				float3 staticSwitch183 = saturate( smoothstepResult170 );
				#else
				float3 staticSwitch183 = temp_output_167_0;
				#endif
				float4 screenPos = input.ase_texcoord2;
				float4 ase_positionSSNorm = screenPos / screenPos.w;
				ase_positionSSNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_positionSSNorm.z : ase_positionSSNorm.z * 0.5 + 0.5;
				float screenDepth213 = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH( ase_positionSSNorm.xy ),_ZBufferParams);
				float distanceDepth213 = ( screenDepth213 - LinearEyeDepth( ase_positionSSNorm.z,_ZBufferParams ) ) / ( _DepthFadeIntensity );
				float temp_output_236_0 = saturate( distanceDepth213 );
				#ifdef _USEDEPTHFADE_ON
				float3 staticSwitch239 = ( staticSwitch183 * temp_output_236_0 );
				#else
				float3 staticSwitch239 = staticSwitch183;
				#endif
				

				surfaceDescription.Alpha = staticSwitch239.x;
				surfaceDescription.AlphaClipThreshold = 0.5;

				#ifdef _ALPHATEST_ON
					clip(surfaceDescription.Alpha - surfaceDescription.AlphaClipThreshold);
				#endif

				half4 outColor = half4(_ObjectId, _PassValue, 1.0, 1.0);
				return outColor;
			}
			ENDHLSL
		}

		
		Pass
		{
			
			Name "ScenePickingPass"
			Tags { "LightMode"="Picking" }

			AlphaToMask Off

			HLSLPROGRAM

			#define _SURFACE_TYPE_TRANSPARENT 1
			#define ASE_VERSION 19905
			#define ASE_SRP_VERSION 170100
			#define REQUIRE_DEPTH_TEXTURE 1


			#pragma vertex vert
			#pragma fragment frag

			#define ATTRIBUTES_NEED_NORMAL
			#define ATTRIBUTES_NEED_TANGENT

			#define SHADERPASS SHADERPASS_DEPTHONLY

			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#if defined(LOD_FADE_CROSSFADE)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #endif

			#define ASE_NEEDS_TEXTURE_COORDINATES0
			#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES0
			#define ASE_NEEDS_TEXTURE_COORDINATES1
			#define ASE_NEEDS_FRAG_COLOR
			#pragma shader_feature_local _USEDEPTHFADE_ON
			#pragma shader_feature_local _STEP_ON
			#pragma shader_feature_local _CUSTOMPANSWITCH_ON
			#pragma shader_feature_local _MESHVERTEXCOLOR_ON


			struct Attributes
			{
				float4 positionOS : POSITION;
				half3 normalOS : NORMAL;
				float4 ase_color : COLOR;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_texcoord1 : TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				float4 positionCS : SV_POSITION;
				float4 ase_color : COLOR;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_texcoord1 : TEXCOORD1;
				float4 ase_texcoord2 : TEXCOORD2;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float4 _TextureChannel;
			float4 _Texture_ST;
			float4 _CoreColor;
			float4 _DissolveMask_ST;
			float4 _DissolveMaskChannel;
			float4 _DistortionMask_ST;
			float4 _DistortionMaskChannel;
			float2 _DissolveMaskPanSpeed;
			float2 _TexturePanSpeed;
			float2 _DistortionMaskPanSpeed;
			float2 _CutoutOffset;
			float _CoreIntensity;
			float _CutoutErosion;
			float _CutoutErosionSmoothness;
			float _CutoutRotation;
			float _EmissionIntensity;
			float _GlowIntensity;
			float _AlphaBoldness;
			float _ValueStep;
			float _DifferentCoreColor;
			float _Cull;
			float _DissolveOffset;
			float _TextureRotation;
			float _ValueStepAdd;
			float _DissolveMaskInvert;
			float _DistortionIntensity;
			float _DistortionMaskRotation;
			float _DissolveMaskRotation;
			float _ZTest;
			float _ZWrite;
			float _Dst;
			float _Src;
			float _CorePower;
			float _DepthFadeIntensity;
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			sampler2D _DissolveMask;
			sampler2D _DistortionMask;
			sampler2D _Texture;
			sampler2D _Cutout;


			
			float4 _SelectionID;

			struct SurfaceDescription
			{
				float Alpha;
				float AlphaClipThreshold;
			};

			PackedVaryings VertexFunction(Attributes input  )
			{
				PackedVaryings output;
				ZERO_INITIALIZE(PackedVaryings, output);

				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				float4 ase_positionCS = TransformObjectToHClip( ( input.positionOS ).xyz );
				float4 screenPos = ComputeScreenPos( ase_positionCS );
				output.ase_texcoord2 = screenPos;
				
				output.ase_color = input.ase_color;
				output.ase_texcoord = input.ase_texcoord;
				output.ase_texcoord1.xy = input.ase_texcoord1.xy;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				output.ase_texcoord1.zw = 0;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = input.positionOS.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = defaultVertexValue;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					input.positionOS.xyz = vertexValue;
				#else
					input.positionOS.xyz += vertexValue;
				#endif

				VertexPositionInputs vertexInput = GetVertexPositionInputs( input.positionOS.xyz );

				output.positionCS = vertexInput.positionCS;
				return output;
			}

			#if defined(ASE_TESSELLATION)
			struct VertexControl
			{
				float4 positionOS : INTERNALTESSPOS;
				half3 normalOS : NORMAL;
				float4 ase_color : COLOR;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_texcoord1 : TEXCOORD1;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( Attributes input )
			{
				VertexControl output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				output.positionOS = input.positionOS;
				output.normalOS = input.normalOS;
				output.ase_color = input.ase_color;
				output.ase_texcoord = input.ase_texcoord;
				output.ase_texcoord1 = input.ase_texcoord1;
				return output;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> input)
			{
				TessellationFactors output;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				output.edge[0] = tf.x; output.edge[1] = tf.y; output.edge[2] = tf.z; output.inside = tf.w;
				return output;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
				return patch[id];
			}

			[domain("tri")]
			PackedVaryings DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				Attributes output = (Attributes) 0;
				output.positionOS = patch[0].positionOS * bary.x + patch[1].positionOS * bary.y + patch[2].positionOS * bary.z;
				output.normalOS = patch[0].normalOS * bary.x + patch[1].normalOS * bary.y + patch[2].normalOS * bary.z;
				output.ase_color = patch[0].ase_color * bary.x + patch[1].ase_color * bary.y + patch[2].ase_color * bary.z;
				output.ase_texcoord = patch[0].ase_texcoord * bary.x + patch[1].ase_texcoord * bary.y + patch[2].ase_texcoord * bary.z;
				output.ase_texcoord1 = patch[0].ase_texcoord1 * bary.x + patch[1].ase_texcoord1 * bary.y + patch[2].ase_texcoord1 * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = output.positionOS.xyz - patch[i].normalOS * (dot(output.positionOS.xyz, patch[i].normalOS) - dot(patch[i].positionOS.xyz, patch[i].normalOS));
				float phongStrength = _TessPhongStrength;
				output.positionOS.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * output.positionOS.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], output);
				return VertexFunction(output);
			}
			#else
			PackedVaryings vert ( Attributes input )
			{
				return VertexFunction( input );
			}
			#endif

			half4 frag(PackedVaryings input ) : SV_Target
			{
				SurfaceDescription surfaceDescription = (SurfaceDescription)0;

				float2 uv_DissolveMask = input.ase_texcoord.xy * _DissolveMask_ST.xy + _DissolveMask_ST.zw;
				float cos112 = cos( radians( _DissolveMaskRotation ) );
				float sin112 = sin( radians( _DissolveMaskRotation ) );
				float2 rotator112 = mul( uv_DissolveMask - float2( 0.5,0.5 ) , float2x2( cos112 , -sin112 , sin112 , cos112 )) + float2( 0.5,0.5 );
				float4 uvs4_DissolveMask = input.ase_texcoord;
				uvs4_DissolveMask.xy = input.ase_texcoord.xy * _DissolveMask_ST.xy + _DissolveMask_ST.zw;
				float2 temp_cast_0 = (0.0).xx;
				float2 texCoord83 = input.ase_texcoord1.xy * float2( 1,1 ) + float2( 0,0 );
				#ifdef _CUSTOMPANSWITCH_ON
				float2 staticSwitch85 = texCoord83;
				#else
				float2 staticSwitch85 = temp_cast_0;
				#endif
				float2 CustomUV89 = staticSwitch85;
				float2 uv_DistortionMask = input.ase_texcoord.xy * _DistortionMask_ST.xy + _DistortionMask_ST.zw;
				float cos95 = cos( radians( _DistortionMaskRotation ) );
				float sin95 = sin( radians( _DistortionMaskRotation ) );
				float2 rotator95 = mul( uv_DistortionMask - float2( 0.5,0.5 ) , float2x2( cos95 , -sin95 , sin95 , cos95 )) + float2( 0.5,0.5 );
				float4 uvs4_DistortionMask = input.ase_texcoord;
				uvs4_DistortionMask.xy = input.ase_texcoord.xy * _DistortionMask_ST.xy + _DistortionMask_ST.zw;
				float dotResult100 = dot( tex2D( _DistortionMask, ( rotator95 + uvs4_DistortionMask.w + CustomUV89 + ( _TimeParameters.x * _DistortionMaskPanSpeed ) ) ) , _DistortionMaskChannel );
				float Disto107 = ( saturate( dotResult100 ) * _DistortionIntensity );
				float dotResult122 = dot( tex2D( _DissolveMask, ( rotator112 + uvs4_DissolveMask.w + CustomUV89 + ( _TimeParameters.x * _DissolveMaskPanSpeed ) + Disto107 ) ) , _DissolveMaskChannel );
				float temp_output_126_0 = saturate( dotResult122 );
				float lerpResult138 = lerp( temp_output_126_0 , saturate( ( 1.0 - temp_output_126_0 ) ) , _DissolveMaskInvert);
				float4 texCoord141 = input.ase_texcoord;
				texCoord141.xy = input.ase_texcoord.xy * float2( 1,1 ) + float2( 0,0 );
				float2 uv_Texture = input.ase_texcoord.xy * _Texture_ST.xy + _Texture_ST.zw;
				float cos129 = cos( radians( _TextureRotation ) );
				float sin129 = sin( radians( _TextureRotation ) );
				float2 rotator129 = mul( uv_Texture - float2( 0.5,0.5 ) , float2x2( cos129 , -sin129 , sin129 , cos129 )) + float2( 0.5,0.5 );
				float dotResult140 = dot( tex2D( _Texture, ( rotator129 + ( _TimeParameters.x * _TexturePanSpeed ) + CustomUV89 + Disto107 ) ) , _TextureChannel );
				float temp_output_147_0 = ( ( saturate( lerpResult138 ) + texCoord141.z + _DissolveOffset ) * saturate( dotResult140 ) );
				float temp_output_261_0 = saturate( ( pow( temp_output_147_0 , _CorePower ) * _CoreIntensity ) );
				float2 texCoord248 = input.ase_texcoord.xy * float2( 1,1 ) + float2( 0,0 );
				float2 temp_output_266_0 = ( texCoord248 + _CutoutOffset );
				float cos258 = cos( radians( _CutoutRotation ) );
				float sin258 = sin( radians( _CutoutRotation ) );
				float2 rotator258 = mul( temp_output_266_0 - float2( 0.5,0.5 ) , float2x2( cos258 , -sin258 , sin258 , cos258 )) + float2( 0.5,0.5 );
				float smoothstepResult252 = smoothstep( _CutoutErosion , ( _CutoutErosion + _CutoutErosionSmoothness ) , tex2D( _Cutout, rotator258 ).g);
				float cutout256 = smoothstepResult252;
				float3 temp_cast_4 = (1.0).xxx;
				float3 temp_output_157_0 = (input.ase_color).rgb;
				#ifdef _MESHVERTEXCOLOR_ON
				float3 staticSwitch159 = temp_output_157_0;
				#else
				float3 staticSwitch159 = temp_cast_4;
				#endif
				float3 temp_output_167_0 = saturate( ( ( input.ase_color.a * saturate( ( saturate( ( temp_output_261_0 + saturate( ( temp_output_147_0 * _GlowIntensity ) ) ) ) * cutout256 ) ) * staticSwitch159 ) * _AlphaBoldness ) );
				float3 temp_cast_5 = (_ValueStep).xxx;
				float3 temp_cast_6 = (( _ValueStep + _ValueStepAdd )).xxx;
				float3 smoothstepResult170 = smoothstep( temp_cast_5 , temp_cast_6 , temp_output_167_0);
				#ifdef _STEP_ON
				float3 staticSwitch183 = saturate( smoothstepResult170 );
				#else
				float3 staticSwitch183 = temp_output_167_0;
				#endif
				float4 screenPos = input.ase_texcoord2;
				float4 ase_positionSSNorm = screenPos / screenPos.w;
				ase_positionSSNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_positionSSNorm.z : ase_positionSSNorm.z * 0.5 + 0.5;
				float screenDepth213 = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH( ase_positionSSNorm.xy ),_ZBufferParams);
				float distanceDepth213 = ( screenDepth213 - LinearEyeDepth( ase_positionSSNorm.z,_ZBufferParams ) ) / ( _DepthFadeIntensity );
				float temp_output_236_0 = saturate( distanceDepth213 );
				#ifdef _USEDEPTHFADE_ON
				float3 staticSwitch239 = ( staticSwitch183 * temp_output_236_0 );
				#else
				float3 staticSwitch239 = staticSwitch183;
				#endif
				

				surfaceDescription.Alpha = staticSwitch239.x;
				surfaceDescription.AlphaClipThreshold = 0.5;

				#ifdef _ALPHATEST_ON
					clip(surfaceDescription.Alpha - surfaceDescription.AlphaClipThreshold);
				#endif

				half4 outColor = 0;
				outColor = unity_SelectionID;

				return outColor;
			}

			ENDHLSL
		}

		
		Pass
		{
			
			Name "DepthNormals"
			Tags { "LightMode"="DepthNormalsOnly" }

			ZTest LEqual
			ZWrite On

			HLSLPROGRAM

        	#pragma multi_compile_instancing
        	#pragma multi_compile _ LOD_FADE_CROSSFADE
        	#define _SURFACE_TYPE_TRANSPARENT 1
        	#define ASE_VERSION 19905
        	#define ASE_SRP_VERSION 170100
        	#define REQUIRE_DEPTH_TEXTURE 1


        	#pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT

			#pragma vertex vert
			#pragma fragment frag

			#define ATTRIBUTES_NEED_NORMAL
			#define ATTRIBUTES_NEED_TANGENT
			#define VARYINGS_NEED_NORMAL_WS

			#define SHADERPASS SHADERPASS_DEPTHNORMALSONLY

			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

            #if defined(LOD_FADE_CROSSFADE)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #endif

			#define ASE_NEEDS_TEXTURE_COORDINATES0
			#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES0
			#define ASE_NEEDS_TEXTURE_COORDINATES1
			#define ASE_NEEDS_FRAG_COLOR
			#define ASE_NEEDS_FRAG_SCREEN_POSITION_NORMALIZED
			#pragma shader_feature_local _USEDEPTHFADE_ON
			#pragma shader_feature_local _STEP_ON
			#pragma shader_feature_local _CUSTOMPANSWITCH_ON
			#pragma shader_feature_local _MESHVERTEXCOLOR_ON


			#if defined(ASE_EARLY_Z_DEPTH_OPTIMIZE) && (SHADER_TARGET >= 45)
				#define ASE_SV_DEPTH SV_DepthLessEqual
				#define ASE_SV_POSITION_QUALIFIERS linear noperspective centroid
			#else
				#define ASE_SV_DEPTH SV_Depth
				#define ASE_SV_POSITION_QUALIFIERS
			#endif

			struct Attributes
			{
				float4 positionOS : POSITION;
				half3 normalOS : NORMAL;
				float4 ase_color : COLOR;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_texcoord1 : TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				ASE_SV_POSITION_QUALIFIERS float4 positionCS : SV_POSITION;
				half3 normalWS : TEXCOORD0;
				float4 ase_color : COLOR;
				float4 ase_texcoord1 : TEXCOORD1;
				float4 ase_texcoord2 : TEXCOORD2;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float4 _TextureChannel;
			float4 _Texture_ST;
			float4 _CoreColor;
			float4 _DissolveMask_ST;
			float4 _DissolveMaskChannel;
			float4 _DistortionMask_ST;
			float4 _DistortionMaskChannel;
			float2 _DissolveMaskPanSpeed;
			float2 _TexturePanSpeed;
			float2 _DistortionMaskPanSpeed;
			float2 _CutoutOffset;
			float _CoreIntensity;
			float _CutoutErosion;
			float _CutoutErosionSmoothness;
			float _CutoutRotation;
			float _EmissionIntensity;
			float _GlowIntensity;
			float _AlphaBoldness;
			float _ValueStep;
			float _DifferentCoreColor;
			float _Cull;
			float _DissolveOffset;
			float _TextureRotation;
			float _ValueStepAdd;
			float _DissolveMaskInvert;
			float _DistortionIntensity;
			float _DistortionMaskRotation;
			float _DissolveMaskRotation;
			float _ZTest;
			float _ZWrite;
			float _Dst;
			float _Src;
			float _CorePower;
			float _DepthFadeIntensity;
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			sampler2D _DissolveMask;
			sampler2D _DistortionMask;
			sampler2D _Texture;
			sampler2D _Cutout;


			
			struct SurfaceDescription
			{
				float Alpha;
				float AlphaClipThreshold;
			};

			PackedVaryings VertexFunction( Attributes input  )
			{
				PackedVaryings output;
				ZERO_INITIALIZE(PackedVaryings, output);

				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				output.ase_color = input.ase_color;
				output.ase_texcoord1 = input.ase_texcoord;
				output.ase_texcoord2.xy = input.ase_texcoord1.xy;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				output.ase_texcoord2.zw = 0;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = input.positionOS.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = defaultVertexValue;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					input.positionOS.xyz = vertexValue;
				#else
					input.positionOS.xyz += vertexValue;
				#endif

				input.normalOS = input.normalOS;

				VertexPositionInputs vertexInput = GetVertexPositionInputs( input.positionOS.xyz );
				VertexNormalInputs normalInput = GetVertexNormalInputs( input.normalOS );

				output.positionCS = vertexInput.positionCS;
				output.normalWS = normalInput.normalWS;
				return output;
			}

			#if defined(ASE_TESSELLATION)
			struct VertexControl
			{
				float4 positionOS : INTERNALTESSPOS;
				half3 normalOS : NORMAL;
				float4 ase_color : COLOR;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_texcoord1 : TEXCOORD1;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( Attributes input )
			{
				VertexControl output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				output.positionOS = input.positionOS;
				output.normalOS = input.normalOS;
				output.ase_color = input.ase_color;
				output.ase_texcoord = input.ase_texcoord;
				output.ase_texcoord1 = input.ase_texcoord1;
				return output;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> input)
			{
				TessellationFactors output;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				output.edge[0] = tf.x; output.edge[1] = tf.y; output.edge[2] = tf.z; output.inside = tf.w;
				return output;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
				return patch[id];
			}

			[domain("tri")]
			PackedVaryings DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				Attributes output = (Attributes) 0;
				output.positionOS = patch[0].positionOS * bary.x + patch[1].positionOS * bary.y + patch[2].positionOS * bary.z;
				output.normalOS = patch[0].normalOS * bary.x + patch[1].normalOS * bary.y + patch[2].normalOS * bary.z;
				output.ase_color = patch[0].ase_color * bary.x + patch[1].ase_color * bary.y + patch[2].ase_color * bary.z;
				output.ase_texcoord = patch[0].ase_texcoord * bary.x + patch[1].ase_texcoord * bary.y + patch[2].ase_texcoord * bary.z;
				output.ase_texcoord1 = patch[0].ase_texcoord1 * bary.x + patch[1].ase_texcoord1 * bary.y + patch[2].ase_texcoord1 * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = output.positionOS.xyz - patch[i].normalOS * (dot(output.positionOS.xyz, patch[i].normalOS) - dot(patch[i].positionOS.xyz, patch[i].normalOS));
				float phongStrength = _TessPhongStrength;
				output.positionOS.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * output.positionOS.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], output);
				return VertexFunction(output);
			}
			#else
			PackedVaryings vert ( Attributes input )
			{
				return VertexFunction( input );
			}
			#endif

			void frag(PackedVaryings input
						, out half4 outNormalWS : SV_Target0
						#if defined( ASE_DEPTH_WRITE_ON )
						,out float outputDepth : ASE_SV_DEPTH
						#endif
						#ifdef _WRITE_RENDERING_LAYERS
						, out float4 outRenderingLayers : SV_Target1
						#endif
						 )
			{
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( input );

				half3 NormalWS = normalize( input.normalWS );
				float4 ScreenPosNorm = float4( GetNormalizedScreenSpaceUV( input.positionCS ), input.positionCS.zw );
				float4 ClipPos = ComputeClipSpacePosition( ScreenPosNorm.xy, input.positionCS.z ) * input.positionCS.w;
				float4 ScreenPos = ComputeScreenPos( ClipPos );

				float2 uv_DissolveMask = input.ase_texcoord1.xy * _DissolveMask_ST.xy + _DissolveMask_ST.zw;
				float cos112 = cos( radians( _DissolveMaskRotation ) );
				float sin112 = sin( radians( _DissolveMaskRotation ) );
				float2 rotator112 = mul( uv_DissolveMask - float2( 0.5,0.5 ) , float2x2( cos112 , -sin112 , sin112 , cos112 )) + float2( 0.5,0.5 );
				float4 uvs4_DissolveMask = input.ase_texcoord1;
				uvs4_DissolveMask.xy = input.ase_texcoord1.xy * _DissolveMask_ST.xy + _DissolveMask_ST.zw;
				float2 temp_cast_0 = (0.0).xx;
				float2 texCoord83 = input.ase_texcoord2.xy * float2( 1,1 ) + float2( 0,0 );
				#ifdef _CUSTOMPANSWITCH_ON
				float2 staticSwitch85 = texCoord83;
				#else
				float2 staticSwitch85 = temp_cast_0;
				#endif
				float2 CustomUV89 = staticSwitch85;
				float2 uv_DistortionMask = input.ase_texcoord1.xy * _DistortionMask_ST.xy + _DistortionMask_ST.zw;
				float cos95 = cos( radians( _DistortionMaskRotation ) );
				float sin95 = sin( radians( _DistortionMaskRotation ) );
				float2 rotator95 = mul( uv_DistortionMask - float2( 0.5,0.5 ) , float2x2( cos95 , -sin95 , sin95 , cos95 )) + float2( 0.5,0.5 );
				float4 uvs4_DistortionMask = input.ase_texcoord1;
				uvs4_DistortionMask.xy = input.ase_texcoord1.xy * _DistortionMask_ST.xy + _DistortionMask_ST.zw;
				float dotResult100 = dot( tex2D( _DistortionMask, ( rotator95 + uvs4_DistortionMask.w + CustomUV89 + ( _TimeParameters.x * _DistortionMaskPanSpeed ) ) ) , _DistortionMaskChannel );
				float Disto107 = ( saturate( dotResult100 ) * _DistortionIntensity );
				float dotResult122 = dot( tex2D( _DissolveMask, ( rotator112 + uvs4_DissolveMask.w + CustomUV89 + ( _TimeParameters.x * _DissolveMaskPanSpeed ) + Disto107 ) ) , _DissolveMaskChannel );
				float temp_output_126_0 = saturate( dotResult122 );
				float lerpResult138 = lerp( temp_output_126_0 , saturate( ( 1.0 - temp_output_126_0 ) ) , _DissolveMaskInvert);
				float4 texCoord141 = input.ase_texcoord1;
				texCoord141.xy = input.ase_texcoord1.xy * float2( 1,1 ) + float2( 0,0 );
				float2 uv_Texture = input.ase_texcoord1.xy * _Texture_ST.xy + _Texture_ST.zw;
				float cos129 = cos( radians( _TextureRotation ) );
				float sin129 = sin( radians( _TextureRotation ) );
				float2 rotator129 = mul( uv_Texture - float2( 0.5,0.5 ) , float2x2( cos129 , -sin129 , sin129 , cos129 )) + float2( 0.5,0.5 );
				float dotResult140 = dot( tex2D( _Texture, ( rotator129 + ( _TimeParameters.x * _TexturePanSpeed ) + CustomUV89 + Disto107 ) ) , _TextureChannel );
				float temp_output_147_0 = ( ( saturate( lerpResult138 ) + texCoord141.z + _DissolveOffset ) * saturate( dotResult140 ) );
				float temp_output_261_0 = saturate( ( pow( temp_output_147_0 , _CorePower ) * _CoreIntensity ) );
				float2 texCoord248 = input.ase_texcoord1.xy * float2( 1,1 ) + float2( 0,0 );
				float2 temp_output_266_0 = ( texCoord248 + _CutoutOffset );
				float cos258 = cos( radians( _CutoutRotation ) );
				float sin258 = sin( radians( _CutoutRotation ) );
				float2 rotator258 = mul( temp_output_266_0 - float2( 0.5,0.5 ) , float2x2( cos258 , -sin258 , sin258 , cos258 )) + float2( 0.5,0.5 );
				float smoothstepResult252 = smoothstep( _CutoutErosion , ( _CutoutErosion + _CutoutErosionSmoothness ) , tex2D( _Cutout, rotator258 ).g);
				float cutout256 = smoothstepResult252;
				float3 temp_cast_4 = (1.0).xxx;
				float3 temp_output_157_0 = (input.ase_color).rgb;
				#ifdef _MESHVERTEXCOLOR_ON
				float3 staticSwitch159 = temp_output_157_0;
				#else
				float3 staticSwitch159 = temp_cast_4;
				#endif
				float3 temp_output_167_0 = saturate( ( ( input.ase_color.a * saturate( ( saturate( ( temp_output_261_0 + saturate( ( temp_output_147_0 * _GlowIntensity ) ) ) ) * cutout256 ) ) * staticSwitch159 ) * _AlphaBoldness ) );
				float3 temp_cast_5 = (_ValueStep).xxx;
				float3 temp_cast_6 = (( _ValueStep + _ValueStepAdd )).xxx;
				float3 smoothstepResult170 = smoothstep( temp_cast_5 , temp_cast_6 , temp_output_167_0);
				#ifdef _STEP_ON
				float3 staticSwitch183 = saturate( smoothstepResult170 );
				#else
				float3 staticSwitch183 = temp_output_167_0;
				#endif
				float screenDepth213 = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH( ScreenPosNorm.xy ),_ZBufferParams);
				float distanceDepth213 = ( screenDepth213 - LinearEyeDepth( ScreenPosNorm.z,_ZBufferParams ) ) / ( _DepthFadeIntensity );
				float temp_output_236_0 = saturate( distanceDepth213 );
				#ifdef _USEDEPTHFADE_ON
				float3 staticSwitch239 = ( staticSwitch183 * temp_output_236_0 );
				#else
				float3 staticSwitch239 = staticSwitch183;
				#endif
				

				float Alpha = staticSwitch239.x;
				float AlphaClipThreshold = 0.5;

				#if defined( ASE_DEPTH_WRITE_ON )
					float DeviceDepth = input.positionCS.z;
				#endif

				#ifdef _ALPHATEST_ON
					clip(Alpha - AlphaClipThreshold);
				#endif

				#if defined(LOD_FADE_CROSSFADE)
					LODFadeCrossFade( input.positionCS );
				#endif

				#if defined( ASE_DEPTH_WRITE_ON )
					outputDepth = DeviceDepth;
				#endif

				#if defined(_GBUFFER_NORMALS_OCT)
					float2 octNormalWS = PackNormalOctQuadEncode(NormalWS);
					float2 remappedOctNormalWS = saturate(octNormalWS * 0.5 + 0.5);
					half3 packedNormalWS = PackFloat2To888(remappedOctNormalWS);
					outNormalWS = half4(packedNormalWS, 0.0);
				#else
					outNormalWS = half4(NormalizeNormalPerPixel( NormalWS ), 0.0);
				#endif

				#ifdef _WRITE_RENDERING_LAYERS
					uint renderingLayers = GetMeshRenderingLayer();
					outRenderingLayers = float4(EncodeMeshRenderingLayer(renderingLayers), 0, 0, 0);
				#endif
			}
			ENDHLSL
		}

		
		Pass
		{
			
			Name "MotionVectors"
			Tags { "LightMode"="MotionVectors" }

			ColorMask RG

			HLSLPROGRAM

			#pragma multi_compile _ LOD_FADE_CROSSFADE
			#define _SURFACE_TYPE_TRANSPARENT 1
			#define ASE_VERSION 19905
			#define ASE_SRP_VERSION 170100
			#define REQUIRE_DEPTH_TEXTURE 1


			#pragma vertex vert
			#pragma fragment frag

            #define SHADERPASS SHADERPASS_MOTION_VECTORS

            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
		    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
		    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
		    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
		    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
		    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
		    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
		    #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#if defined(LOD_FADE_CROSSFADE)
				#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
			#endif

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MotionVectorsCommon.hlsl"

			#define ASE_NEEDS_TEXTURE_COORDINATES0
			#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES0
			#define ASE_NEEDS_TEXTURE_COORDINATES1
			#define ASE_NEEDS_FRAG_COLOR
			#define ASE_NEEDS_FRAG_SCREEN_POSITION_NORMALIZED
			#pragma shader_feature_local _USEDEPTHFADE_ON
			#pragma shader_feature_local _STEP_ON
			#pragma shader_feature_local _CUSTOMPANSWITCH_ON
			#pragma shader_feature_local _MESHVERTEXCOLOR_ON


			#if defined(ASE_EARLY_Z_DEPTH_OPTIMIZE) && (SHADER_TARGET >= 45)
				#define ASE_SV_DEPTH SV_DepthLessEqual
				#define ASE_SV_POSITION_QUALIFIERS linear noperspective centroid
			#else
				#define ASE_SV_DEPTH SV_Depth
				#define ASE_SV_POSITION_QUALIFIERS
			#endif

			struct Attributes
			{
				float4 positionOS : POSITION;
				float3 positionOld : TEXCOORD4;
				#if _ADD_PRECOMPUTED_VELOCITY
					float3 alembicMotionVector : TEXCOORD5;
				#endif
				half3 normalOS : NORMAL;
				half4 tangentOS : TANGENT;
				float4 ase_color : COLOR;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_texcoord1 : TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				float4 positionCS : SV_POSITION;
				float4 positionCSNoJitter : TEXCOORD0;
				float4 previousPositionCSNoJitter : TEXCOORD1;
				float3 positionWS : TEXCOORD2;
				float4 ase_color : COLOR;
				float4 ase_texcoord3 : TEXCOORD3;
				float4 ase_texcoord4 : TEXCOORD4;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float4 _TextureChannel;
			float4 _Texture_ST;
			float4 _CoreColor;
			float4 _DissolveMask_ST;
			float4 _DissolveMaskChannel;
			float4 _DistortionMask_ST;
			float4 _DistortionMaskChannel;
			float2 _DissolveMaskPanSpeed;
			float2 _TexturePanSpeed;
			float2 _DistortionMaskPanSpeed;
			float2 _CutoutOffset;
			float _CoreIntensity;
			float _CutoutErosion;
			float _CutoutErosionSmoothness;
			float _CutoutRotation;
			float _EmissionIntensity;
			float _GlowIntensity;
			float _AlphaBoldness;
			float _ValueStep;
			float _DifferentCoreColor;
			float _Cull;
			float _DissolveOffset;
			float _TextureRotation;
			float _ValueStepAdd;
			float _DissolveMaskInvert;
			float _DistortionIntensity;
			float _DistortionMaskRotation;
			float _DissolveMaskRotation;
			float _ZTest;
			float _ZWrite;
			float _Dst;
			float _Src;
			float _CorePower;
			float _DepthFadeIntensity;
			#ifdef ASE_TRANSMISSION
				float _TransmissionShadow;
			#endif
			#ifdef ASE_TRANSLUCENCY
				float _TransStrength;
				float _TransNormal;
				float _TransScattering;
				float _TransDirect;
				float _TransAmbient;
				float _TransShadow;
			#endif
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			#ifdef SCENEPICKINGPASS
				float4 _SelectionID;
			#endif

			#ifdef SCENESELECTIONPASS
				int _ObjectId;
				int _PassValue;
			#endif

			sampler2D _DissolveMask;
			sampler2D _DistortionMask;
			sampler2D _Texture;
			sampler2D _Cutout;


			
			PackedVaryings VertexFunction( Attributes input  )
			{
				PackedVaryings output = (PackedVaryings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				output.ase_color = input.ase_color;
				output.ase_texcoord3 = input.ase_texcoord;
				output.ase_texcoord4.xy = input.ase_texcoord1.xy;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				output.ase_texcoord4.zw = 0;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = input.positionOS.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = defaultVertexValue;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					input.positionOS.xyz = vertexValue;
				#else
					input.positionOS.xyz += vertexValue;
				#endif

				VertexPositionInputs vertexInput = GetVertexPositionInputs( input.positionOS.xyz );

				#if defined(APPLICATION_SPACE_WARP_MOTION)
					output.positionCSNoJitter = mul(_NonJitteredViewProjMatrix, mul(UNITY_MATRIX_M, input.positionOS));
					output.positionCS = output.positionCSNoJitter;
				#else
					output.positionCS = vertexInput.positionCS;
					output.positionCSNoJitter = mul(_NonJitteredViewProjMatrix, mul(UNITY_MATRIX_M, input.positionOS));
				#endif

				float4 prevPos = ( unity_MotionVectorsParams.x == 1 ) ? float4( input.positionOld, 1 ) : input.positionOS;

				#if _ADD_PRECOMPUTED_VELOCITY
					prevPos = prevPos - float4(input.alembicMotionVector, 0);
				#endif

				output.previousPositionCSNoJitter = mul( _PrevViewProjMatrix, mul( UNITY_PREV_MATRIX_M, prevPos ) );
				output.positionWS = vertexInput.positionWS;
				return output;
			}

			PackedVaryings vert ( Attributes input )
			{
				return VertexFunction( input );
			}

			half4 frag(	PackedVaryings input
				#if defined( ASE_DEPTH_WRITE_ON )
				,out float outputDepth : ASE_SV_DEPTH
				#endif
				 ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( input );

				float3 PositionWS = input.positionWS;
				float3 PositionRWS = GetCameraRelativePositionWS( PositionWS );
				float4 ScreenPosNorm = float4( GetNormalizedScreenSpaceUV( input.positionCS ), input.positionCS.zw );
				float4 ClipPos = ComputeClipSpacePosition( ScreenPosNorm.xy, input.positionCS.z ) * input.positionCS.w;

				float2 uv_DissolveMask = input.ase_texcoord3.xy * _DissolveMask_ST.xy + _DissolveMask_ST.zw;
				float cos112 = cos( radians( _DissolveMaskRotation ) );
				float sin112 = sin( radians( _DissolveMaskRotation ) );
				float2 rotator112 = mul( uv_DissolveMask - float2( 0.5,0.5 ) , float2x2( cos112 , -sin112 , sin112 , cos112 )) + float2( 0.5,0.5 );
				float4 uvs4_DissolveMask = input.ase_texcoord3;
				uvs4_DissolveMask.xy = input.ase_texcoord3.xy * _DissolveMask_ST.xy + _DissolveMask_ST.zw;
				float2 temp_cast_0 = (0.0).xx;
				float2 texCoord83 = input.ase_texcoord4.xy * float2( 1,1 ) + float2( 0,0 );
				#ifdef _CUSTOMPANSWITCH_ON
				float2 staticSwitch85 = texCoord83;
				#else
				float2 staticSwitch85 = temp_cast_0;
				#endif
				float2 CustomUV89 = staticSwitch85;
				float2 uv_DistortionMask = input.ase_texcoord3.xy * _DistortionMask_ST.xy + _DistortionMask_ST.zw;
				float cos95 = cos( radians( _DistortionMaskRotation ) );
				float sin95 = sin( radians( _DistortionMaskRotation ) );
				float2 rotator95 = mul( uv_DistortionMask - float2( 0.5,0.5 ) , float2x2( cos95 , -sin95 , sin95 , cos95 )) + float2( 0.5,0.5 );
				float4 uvs4_DistortionMask = input.ase_texcoord3;
				uvs4_DistortionMask.xy = input.ase_texcoord3.xy * _DistortionMask_ST.xy + _DistortionMask_ST.zw;
				float dotResult100 = dot( tex2D( _DistortionMask, ( rotator95 + uvs4_DistortionMask.w + CustomUV89 + ( _TimeParameters.x * _DistortionMaskPanSpeed ) ) ) , _DistortionMaskChannel );
				float Disto107 = ( saturate( dotResult100 ) * _DistortionIntensity );
				float dotResult122 = dot( tex2D( _DissolveMask, ( rotator112 + uvs4_DissolveMask.w + CustomUV89 + ( _TimeParameters.x * _DissolveMaskPanSpeed ) + Disto107 ) ) , _DissolveMaskChannel );
				float temp_output_126_0 = saturate( dotResult122 );
				float lerpResult138 = lerp( temp_output_126_0 , saturate( ( 1.0 - temp_output_126_0 ) ) , _DissolveMaskInvert);
				float4 texCoord141 = input.ase_texcoord3;
				texCoord141.xy = input.ase_texcoord3.xy * float2( 1,1 ) + float2( 0,0 );
				float2 uv_Texture = input.ase_texcoord3.xy * _Texture_ST.xy + _Texture_ST.zw;
				float cos129 = cos( radians( _TextureRotation ) );
				float sin129 = sin( radians( _TextureRotation ) );
				float2 rotator129 = mul( uv_Texture - float2( 0.5,0.5 ) , float2x2( cos129 , -sin129 , sin129 , cos129 )) + float2( 0.5,0.5 );
				float dotResult140 = dot( tex2D( _Texture, ( rotator129 + ( _TimeParameters.x * _TexturePanSpeed ) + CustomUV89 + Disto107 ) ) , _TextureChannel );
				float temp_output_147_0 = ( ( saturate( lerpResult138 ) + texCoord141.z + _DissolveOffset ) * saturate( dotResult140 ) );
				float temp_output_261_0 = saturate( ( pow( temp_output_147_0 , _CorePower ) * _CoreIntensity ) );
				float2 texCoord248 = input.ase_texcoord3.xy * float2( 1,1 ) + float2( 0,0 );
				float2 temp_output_266_0 = ( texCoord248 + _CutoutOffset );
				float cos258 = cos( radians( _CutoutRotation ) );
				float sin258 = sin( radians( _CutoutRotation ) );
				float2 rotator258 = mul( temp_output_266_0 - float2( 0.5,0.5 ) , float2x2( cos258 , -sin258 , sin258 , cos258 )) + float2( 0.5,0.5 );
				float smoothstepResult252 = smoothstep( _CutoutErosion , ( _CutoutErosion + _CutoutErosionSmoothness ) , tex2D( _Cutout, rotator258 ).g);
				float cutout256 = smoothstepResult252;
				float3 temp_cast_4 = (1.0).xxx;
				float3 temp_output_157_0 = (input.ase_color).rgb;
				#ifdef _MESHVERTEXCOLOR_ON
				float3 staticSwitch159 = temp_output_157_0;
				#else
				float3 staticSwitch159 = temp_cast_4;
				#endif
				float3 temp_output_167_0 = saturate( ( ( input.ase_color.a * saturate( ( saturate( ( temp_output_261_0 + saturate( ( temp_output_147_0 * _GlowIntensity ) ) ) ) * cutout256 ) ) * staticSwitch159 ) * _AlphaBoldness ) );
				float3 temp_cast_5 = (_ValueStep).xxx;
				float3 temp_cast_6 = (( _ValueStep + _ValueStepAdd )).xxx;
				float3 smoothstepResult170 = smoothstep( temp_cast_5 , temp_cast_6 , temp_output_167_0);
				#ifdef _STEP_ON
				float3 staticSwitch183 = saturate( smoothstepResult170 );
				#else
				float3 staticSwitch183 = temp_output_167_0;
				#endif
				float screenDepth213 = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH( ScreenPosNorm.xy ),_ZBufferParams);
				float distanceDepth213 = ( screenDepth213 - LinearEyeDepth( ScreenPosNorm.z,_ZBufferParams ) ) / ( _DepthFadeIntensity );
				float temp_output_236_0 = saturate( distanceDepth213 );
				#ifdef _USEDEPTHFADE_ON
				float3 staticSwitch239 = ( staticSwitch183 * temp_output_236_0 );
				#else
				float3 staticSwitch239 = staticSwitch183;
				#endif
				

				float Alpha = staticSwitch239.x;
				float AlphaClipThreshold = 0.5;

				#if defined( ASE_DEPTH_WRITE_ON )
					float DeviceDepth = input.positionCS.z;
				#endif

				#ifdef _ALPHATEST_ON
					clip(Alpha - AlphaClipThreshold);
				#endif

				#if defined( ASE_CHANGES_WORLD_POS )
					float3 positionOS = mul( GetWorldToObjectMatrix(),  float4( PositionWS, 1.0 ) ).xyz;
					float3 previousPositionWS = mul( GetPrevObjectToWorldMatrix(),  float4( positionOS, 1.0 ) ).xyz;
					input.positionCSNoJitter = mul( _NonJitteredViewProjMatrix, float4( PositionWS, 1.0 ) );
					input.previousPositionCSNoJitter = mul( _PrevViewProjMatrix, float4( previousPositionWS, 1.0 ) );
				#endif

				#if defined( LOD_FADE_CROSSFADE )
					LODFadeCrossFade( input.positionCS );
				#endif

				#if defined( ASE_DEPTH_WRITE_ON )
					outputDepth = DeviceDepth;
				#endif

				#if defined(APPLICATION_SPACE_WARP_MOTION)
					return float4( CalcAswNdcMotionVectorFromCsPositions( input.positionCSNoJitter, input.previousPositionCSNoJitter ), 1 );
				#else
					return float4( CalcNdcMotionVectorFromCsPositions( input.positionCSNoJitter, input.previousPositionCSNoJitter ), 0, 0 );
				#endif
			}
			ENDHLSL
		}

		
		Pass
		{
			
			Name "GBuffer"
			Tags { "LightMode"="UniversalGBuffer" }

			Blend [_Src] [_Dst], One OneMinusSrcAlpha
			ZWrite Off
			ZTest [_ZTest]
			Offset 0 , 0
			ColorMask RGBA

			

			HLSLPROGRAM

			#pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
			#define _SURFACE_TYPE_TRANSPARENT 1
			#define ASE_VERSION 19905
			#define ASE_SRP_VERSION 170100
			#define REQUIRE_DEPTH_TEXTURE 1


			// Deferred Rendering Path does not support the OpenGL-based graphics API:
			// Desktop OpenGL, OpenGL ES 3.0, WebGL 2.0.
			#pragma exclude_renderers glcore gles3 

			#pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
			#pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
			#pragma multi_compile_fragment _ _RENDER_PASS_ENABLED

			#pragma vertex vert
			#pragma fragment frag

			#define SHADERPASS SHADERPASS_GBUFFER

			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"

			#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#if defined(LOD_FADE_CROSSFADE)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #endif

			#if defined(UNITY_INSTANCING_ENABLED) && defined(_TERRAIN_INSTANCED_PERPIXEL_NORMAL)
				#define ENABLE_TERRAIN_PERPIXEL_NORMAL
			#endif

			#define ASE_NEEDS_TEXTURE_COORDINATES0
			#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES0
			#define ASE_NEEDS_TEXTURE_COORDINATES1
			#define ASE_NEEDS_FRAG_COLOR
			#define ASE_NEEDS_FRAG_SCREEN_POSITION_NORMALIZED
			#pragma shader_feature_local _USEDEPTHFADE_ON
			#pragma shader_feature_local _CUSTOMPANSWITCH_ON
			#pragma shader_feature_local _STEP_ON
			#pragma shader_feature_local _MESHVERTEXCOLOR_ON


			#if defined(ASE_EARLY_Z_DEPTH_OPTIMIZE) && (SHADER_TARGET >= 45)
				#define ASE_SV_DEPTH SV_DepthLessEqual
				#define ASE_SV_POSITION_QUALIFIERS linear noperspective centroid
			#else
				#define ASE_SV_DEPTH SV_Depth
				#define ASE_SV_POSITION_QUALIFIERS
			#endif

			struct Attributes
			{
				float4 positionOS : POSITION;
				half3 normalOS : NORMAL;
				float4 ase_color : COLOR;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_texcoord1 : TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				ASE_SV_POSITION_QUALIFIERS float4 positionCS : SV_POSITION;
				float3 positionWS : TEXCOORD0;
				half3 normalWS : TEXCOORD1;
				float4 ase_color : COLOR;
				float4 ase_texcoord2 : TEXCOORD2;
				float4 ase_texcoord3 : TEXCOORD3;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float4 _TextureChannel;
			float4 _Texture_ST;
			float4 _CoreColor;
			float4 _DissolveMask_ST;
			float4 _DissolveMaskChannel;
			float4 _DistortionMask_ST;
			float4 _DistortionMaskChannel;
			float2 _DissolveMaskPanSpeed;
			float2 _TexturePanSpeed;
			float2 _DistortionMaskPanSpeed;
			float2 _CutoutOffset;
			float _CoreIntensity;
			float _CutoutErosion;
			float _CutoutErosionSmoothness;
			float _CutoutRotation;
			float _EmissionIntensity;
			float _GlowIntensity;
			float _AlphaBoldness;
			float _ValueStep;
			float _DifferentCoreColor;
			float _Cull;
			float _DissolveOffset;
			float _TextureRotation;
			float _ValueStepAdd;
			float _DissolveMaskInvert;
			float _DistortionIntensity;
			float _DistortionMaskRotation;
			float _DissolveMaskRotation;
			float _ZTest;
			float _ZWrite;
			float _Dst;
			float _Src;
			float _CorePower;
			float _DepthFadeIntensity;
			#ifdef ASE_TRANSMISSION
				float _TransmissionShadow;
			#endif
			#ifdef ASE_TRANSLUCENCY
				float _TransStrength;
				float _TransNormal;
				float _TransScattering;
				float _TransDirect;
				float _TransAmbient;
				float _TransShadow;
			#endif
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			#ifdef SCENEPICKINGPASS
				float4 _SelectionID;
			#endif

			#ifdef SCENESELECTIONPASS
				int _ObjectId;
				int _PassValue;
			#endif

			sampler2D _DissolveMask;
			sampler2D _DistortionMask;
			sampler2D _Texture;
			sampler2D _Cutout;


			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GBufferOutput.hlsl"

			
			PackedVaryings VertexFunction( Attributes input  )
			{
				PackedVaryings output = (PackedVaryings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				output.ase_color = input.ase_color;
				output.ase_texcoord2 = input.ase_texcoord;
				output.ase_texcoord3.xy = input.ase_texcoord1.xy;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				output.ase_texcoord3.zw = 0;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = input.positionOS.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = defaultVertexValue;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					input.positionOS.xyz = vertexValue;
				#else
					input.positionOS.xyz += vertexValue;
				#endif

				VertexPositionInputs vertexInput = GetVertexPositionInputs( input.positionOS.xyz );
				VertexNormalInputs normalInput = GetVertexNormalInputs( input.normalOS );

				output.positionCS = vertexInput.positionCS;
				output.positionWS = vertexInput.positionWS;
				output.normalWS = normalInput.normalWS;
				return output;
			}

			#if defined(ASE_TESSELLATION)
			struct VertexControl
			{
				float4 positionOS : INTERNALTESSPOS;
				half3 normalOS : NORMAL;
				float4 ase_color : COLOR;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_texcoord1 : TEXCOORD1;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( Attributes input )
			{
				VertexControl output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				output.positionOS = input.positionOS;
				output.normalOS = input.normalOS;
				output.ase_color = input.ase_color;
				output.ase_texcoord = input.ase_texcoord;
				output.ase_texcoord1 = input.ase_texcoord1;
				return output;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> input)
			{
				TessellationFactors output;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				output.edge[0] = tf.x; output.edge[1] = tf.y; output.edge[2] = tf.z; output.inside = tf.w;
				return output;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
				return patch[id];
			}

			[domain("tri")]
			PackedVaryings DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				Attributes output = (Attributes) 0;
				output.positionOS = patch[0].positionOS * bary.x + patch[1].positionOS * bary.y + patch[2].positionOS * bary.z;
				output.normalOS = patch[0].normalOS * bary.x + patch[1].normalOS * bary.y + patch[2].normalOS * bary.z;
				output.ase_color = patch[0].ase_color * bary.x + patch[1].ase_color * bary.y + patch[2].ase_color * bary.z;
				output.ase_texcoord = patch[0].ase_texcoord * bary.x + patch[1].ase_texcoord * bary.y + patch[2].ase_texcoord * bary.z;
				output.ase_texcoord1 = patch[0].ase_texcoord1 * bary.x + patch[1].ase_texcoord1 * bary.y + patch[2].ase_texcoord1 * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = output.positionOS.xyz - patch[i].normalOS * (dot(output.positionOS.xyz, patch[i].normalOS) - dot(patch[i].positionOS.xyz, patch[i].normalOS));
				float phongStrength = _TessPhongStrength;
				output.positionOS.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * output.positionOS.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], output);
				return VertexFunction(output);
			}
			#else
			PackedVaryings vert ( Attributes input )
			{
				return VertexFunction( input );
			}
			#endif

			GBufferFragOutput frag ( PackedVaryings input
								#if defined( ASE_DEPTH_WRITE_ON )
								,out float outputDepth : ASE_SV_DEPTH
								#endif
								 )
			{
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

				#if defined(LOD_FADE_CROSSFADE)
					LODFadeCrossFade( input.positionCS );
				#endif

				float3 PositionWS = input.positionWS;
				float3 ViewDirWS = GetWorldSpaceNormalizeViewDir( PositionWS );
				float4 ScreenPosNorm = float4( GetNormalizedScreenSpaceUV( input.positionCS ), input.positionCS.zw );
				float4 ClipPos = ComputeClipSpacePosition( ScreenPosNorm.xy, input.positionCS.z ) * input.positionCS.w;
				float4 ScreenPos = ComputeScreenPos( ClipPos );
				half3 NormalWS = normalize( input.normalWS );

				float3 temp_output_157_0 = (input.ase_color).rgb;
				float2 uv_DissolveMask = input.ase_texcoord2.xy * _DissolveMask_ST.xy + _DissolveMask_ST.zw;
				float cos112 = cos( radians( _DissolveMaskRotation ) );
				float sin112 = sin( radians( _DissolveMaskRotation ) );
				float2 rotator112 = mul( uv_DissolveMask - float2( 0.5,0.5 ) , float2x2( cos112 , -sin112 , sin112 , cos112 )) + float2( 0.5,0.5 );
				float4 uvs4_DissolveMask = input.ase_texcoord2;
				uvs4_DissolveMask.xy = input.ase_texcoord2.xy * _DissolveMask_ST.xy + _DissolveMask_ST.zw;
				float2 temp_cast_2 = (0.0).xx;
				float2 texCoord83 = input.ase_texcoord3.xy * float2( 1,1 ) + float2( 0,0 );
				#ifdef _CUSTOMPANSWITCH_ON
				float2 staticSwitch85 = texCoord83;
				#else
				float2 staticSwitch85 = temp_cast_2;
				#endif
				float2 CustomUV89 = staticSwitch85;
				float2 uv_DistortionMask = input.ase_texcoord2.xy * _DistortionMask_ST.xy + _DistortionMask_ST.zw;
				float cos95 = cos( radians( _DistortionMaskRotation ) );
				float sin95 = sin( radians( _DistortionMaskRotation ) );
				float2 rotator95 = mul( uv_DistortionMask - float2( 0.5,0.5 ) , float2x2( cos95 , -sin95 , sin95 , cos95 )) + float2( 0.5,0.5 );
				float4 uvs4_DistortionMask = input.ase_texcoord2;
				uvs4_DistortionMask.xy = input.ase_texcoord2.xy * _DistortionMask_ST.xy + _DistortionMask_ST.zw;
				float dotResult100 = dot( tex2D( _DistortionMask, ( rotator95 + uvs4_DistortionMask.w + CustomUV89 + ( _TimeParameters.x * _DistortionMaskPanSpeed ) ) ) , _DistortionMaskChannel );
				float Disto107 = ( saturate( dotResult100 ) * _DistortionIntensity );
				float dotResult122 = dot( tex2D( _DissolveMask, ( rotator112 + uvs4_DissolveMask.w + CustomUV89 + ( _TimeParameters.x * _DissolveMaskPanSpeed ) + Disto107 ) ) , _DissolveMaskChannel );
				float temp_output_126_0 = saturate( dotResult122 );
				float lerpResult138 = lerp( temp_output_126_0 , saturate( ( 1.0 - temp_output_126_0 ) ) , _DissolveMaskInvert);
				float4 texCoord141 = input.ase_texcoord2;
				texCoord141.xy = input.ase_texcoord2.xy * float2( 1,1 ) + float2( 0,0 );
				float2 uv_Texture = input.ase_texcoord2.xy * _Texture_ST.xy + _Texture_ST.zw;
				float cos129 = cos( radians( _TextureRotation ) );
				float sin129 = sin( radians( _TextureRotation ) );
				float2 rotator129 = mul( uv_Texture - float2( 0.5,0.5 ) , float2x2( cos129 , -sin129 , sin129 , cos129 )) + float2( 0.5,0.5 );
				float dotResult140 = dot( tex2D( _Texture, ( rotator129 + ( _TimeParameters.x * _TexturePanSpeed ) + CustomUV89 + Disto107 ) ) , _TextureChannel );
				float temp_output_147_0 = ( ( saturate( lerpResult138 ) + texCoord141.z + _DissolveOffset ) * saturate( dotResult140 ) );
				float temp_output_261_0 = saturate( ( pow( temp_output_147_0 , _CorePower ) * _CoreIntensity ) );
				float2 texCoord248 = input.ase_texcoord2.xy * float2( 1,1 ) + float2( 0,0 );
				float2 temp_output_266_0 = ( texCoord248 + _CutoutOffset );
				float cos258 = cos( radians( _CutoutRotation ) );
				float sin258 = sin( radians( _CutoutRotation ) );
				float2 rotator258 = mul( temp_output_266_0 - float2( 0.5,0.5 ) , float2x2( cos258 , -sin258 , sin258 , cos258 )) + float2( 0.5,0.5 );
				float smoothstepResult252 = smoothstep( _CutoutErosion , ( _CutoutErosion + _CutoutErosionSmoothness ) , tex2D( _Cutout, rotator258 ).g);
				float cutout256 = smoothstepResult252;
				float4 lerpResult188 = lerp( float4( temp_output_157_0 , 0.0 ) , _CoreColor , saturate( ( temp_output_261_0 * cutout256 ) ));
				float4 lerpResult217 = lerp( float4( temp_output_157_0 , 0.0 ) , saturate( lerpResult188 ) , _DifferentCoreColor);
				float3 temp_cast_6 = (1.0).xxx;
				#ifdef _MESHVERTEXCOLOR_ON
				float3 staticSwitch159 = temp_output_157_0;
				#else
				float3 staticSwitch159 = temp_cast_6;
				#endif
				float3 temp_output_167_0 = saturate( ( ( input.ase_color.a * saturate( ( saturate( ( temp_output_261_0 + saturate( ( temp_output_147_0 * _GlowIntensity ) ) ) ) * cutout256 ) ) * staticSwitch159 ) * _AlphaBoldness ) );
				float3 temp_cast_7 = (_ValueStep).xxx;
				float3 temp_cast_8 = (( _ValueStep + _ValueStepAdd )).xxx;
				float3 smoothstepResult170 = smoothstep( temp_cast_7 , temp_cast_8 , temp_output_167_0);
				#ifdef _STEP_ON
				float3 staticSwitch183 = saturate( smoothstepResult170 );
				#else
				float3 staticSwitch183 = temp_output_167_0;
				#endif
				float4 temp_output_234_0 = ( ( saturate( lerpResult217 ) * _EmissionIntensity ) * float4( staticSwitch183 , 0.0 ) );
				float screenDepth213 = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH( ScreenPosNorm.xy ),_ZBufferParams);
				float distanceDepth213 = ( screenDepth213 - LinearEyeDepth( ScreenPosNorm.z,_ZBufferParams ) ) / ( _DepthFadeIntensity );
				float temp_output_236_0 = saturate( distanceDepth213 );
				#ifdef _USEDEPTHFADE_ON
				float4 staticSwitch238 = ( temp_output_234_0 * temp_output_236_0 );
				#else
				float4 staticSwitch238 = temp_output_234_0;
				#endif
				
				#ifdef _USEDEPTHFADE_ON
				float3 staticSwitch239 = ( staticSwitch183 * temp_output_236_0 );
				#else
				float3 staticSwitch239 = staticSwitch183;
				#endif
				

				float3 Color = staticSwitch238.rgb;
				float Alpha = staticSwitch239.x;
				float AlphaClipThreshold = 0.5;
				float AlphaClipThresholdShadow = 0.5;

				#if defined( ASE_DEPTH_WRITE_ON )
					float DeviceDepth = input.positionCS.z;
				#endif

				#ifdef _ALPHATEST_ON
					clip(Alpha - AlphaClipThreshold);
				#endif

				InputData inputData = (InputData)0;
				inputData.positionWS = PositionWS;
				inputData.positionCS = float4( input.positionCS.xy, ClipPos.zw / ClipPos.w );
				inputData.normalizedScreenSpaceUV = ScreenPosNorm.xy;
				inputData.normalWS = NormalWS;
				inputData.viewDirectionWS = ViewDirWS;

				#if defined(_DBUFFER)
					ApplyDecalToBaseColor(input.positionCS, Color);
				#endif

				#if defined( ASE_DEPTH_WRITE_ON )
					outputDepth = DeviceDepth;
				#endif

				SurfaceData surfaceData = (SurfaceData)0;
				surfaceData.albedo = Color;
				surfaceData.alpha = Alpha;

			#if defined( _SCREEN_SPACE_OCCLUSION ) // GBuffer never has transparents
				float2 normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV( input.positionCS );
				AmbientOcclusionFactor aoFactor = GetScreenSpaceAmbientOcclusion( normalizedScreenSpaceUV );
				surfaceData.occlusion = aoFactor.directAmbientOcclusion;
			#else
				surfaceData.occlusion = 1;
			#endif

				return PackGBuffersSurfaceData( surfaceData, inputData, float3( 0, 0, 0 ) );
			}

			ENDHLSL
		}
		
	}
	
	CustomEditor "UnityEditor.ShaderGraphUnlitGUI"
	FallBack "Hidden/Shader Graph/FallbackError"
	
	Fallback Off
}
/*ASEBEGIN
Version=19905
Node;AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;82;-6112,-1536;Inherit;False;3687.623;895.329;Distortion;18;107;106;102;103;100;98;99;97;96;95;94;93;92;91;90;88;87;86;Distortion;0,0,0,1;0;0
Node;AmplifyShaderEditor.TexturePropertyNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;87;-6016,-1408;Inherit;True;Property;_DistortionMask;Distortion Mask;4;0;Create;True;0;0;0;False;3;Space(33);Header(Distortion);Space(13);False;None;None;False;white;Auto;Texture2D;False;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;86;-5376,-1152;Inherit;False;Property;_DistortionMaskRotation;Distortion Mask Rotation;6;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;91;-4608,-896;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;92;-5376,-1280;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;88;-4608,-816;Inherit;False;Property;_DistortionMaskPanSpeed;Distortion Mask Pan Speed;7;0;Create;True;0;0;0;False;0;False;0,0;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.RadiansOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;90;-5120,-1152;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;94;-4352,-896;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;96;-4608,-1024;Inherit;False;89;CustomUV;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;93;-5376,-1024;Inherit;False;0;-1;4;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RotatorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;95;-5120,-1280;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;97;-4224,-1280;Inherit;False;4;4;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT2;0,0;False;3;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;98;-3968,-1408;Inherit;True;Property;_TextureSample5;Texture Sample 5;1;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;False;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.Vector4Node, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;99;-3584,-1280;Inherit;False;Property;_DistortionMaskChannel;Distortion Mask Channel;5;0;Create;True;0;0;0;False;0;False;0,1,0,0;0,1,0,0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;81;-5920,-512;Inherit;False;1052.771;508.8153;PanInCustomUV;4;89;85;84;83;PanInCustomUV;0,0,0,1;0;0
Node;AmplifyShaderEditor.DotProductOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;100;-3584,-1408;Inherit;False;2;0;COLOR;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;101;-7248,592;Inherit;False;2848.594;729.824;Dissolve Mask;24;145;143;142;141;138;136;135;133;126;122;120;119;117;116;115;114;113;112;111;110;109;108;105;104;Dissolve Mask;0,0,0,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;84;-5888,-384;Inherit;False;Constant;_Value0;Value0;26;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;83;-5888,-256;Inherit;False;1;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;102;-3456,-1408;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;103;-3072,-1280;Inherit;False;Property;_DistortionIntensity;Distortion Intensity;8;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;85;-5504,-384;Inherit;False;Property;_CustomPanSwitch;CustomPanSwitch;23;0;Create;True;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;True;All;9;1;FLOAT2;0,0;False;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;6;FLOAT2;0,0;False;7;FLOAT2;0,0;False;8;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;104;-6928,832;Inherit;False;Property;_DissolveMaskRotation;Dissolve Mask Rotation;11;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;106;-3072,-1408;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TexturePropertyNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;105;-7200,640;Inherit;True;Property;_DissolveMask;Dissolve Mask;9;0;Create;True;0;0;0;False;3;Space(33);Header(Dissolve);Space(13);False;None;None;False;white;Auto;Texture2D;False;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;89;-5120,-384;Inherit;False;CustomUV;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;108;-6608,1152;Inherit;False;Property;_DissolveMaskPanSpeed;Dissolve Mask Pan Speed;12;0;Create;True;0;0;0;False;0;False;0,0;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.TextureCoordinatesNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;109;-6800,704;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RadiansOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;110;-6672,864;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;111;-6592,1072;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;107;-2816,-1408;Inherit;False;Disto;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;118;-5984,1392;Inherit;False;1885.642;658.4678;Main Texture;15;146;140;139;137;134;132;131;130;129;128;127;125;124;121;123;Main Texture;0,0,0,1;0;0
Node;AmplifyShaderEditor.RotatorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;112;-6496,720;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;114;-6928,944;Inherit;False;0;-1;4;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;116;-6352,1072;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;115;-6464,992;Inherit;False;107;Disto;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;113;-6496,896;Inherit;False;89;CustomUV;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;117;-6256,720;Inherit;False;5;5;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT2;0,0;False;3;FLOAT2;0,0;False;4;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;123;-5824,1648;Inherit;False;Property;_TextureRotation;Texture Rotation;2;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TexturePropertyNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;121;-5936,1440;Inherit;True;Property;_Texture;Texture;0;0;Create;True;0;0;0;False;3;Space(33);Header(Main Texture);Space(13);False;None;None;False;white;Auto;Texture2D;False;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.Vector4Node, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;119;-6000,848;Inherit;False;Property;_DissolveMaskChannel;Dissolve Mask Channel;10;0;Create;True;0;0;0;False;0;False;0,1,0,0;0,1,0,0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;120;-6064,640;Inherit;True;Property;_TextureSample3;Texture Sample 3;1;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;False;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SimpleTimeNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;124;-5376,1648;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;125;-5664,1520;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;127;-5392,1712;Inherit;False;Property;_TexturePanSpeed;Texture Pan Speed;3;0;Create;True;0;0;0;False;0;False;0,0;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.RadiansOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;128;-5584,1648;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DotProductOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;122;-5728,704;Inherit;False;2;0;COLOR;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RotatorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;129;-5408,1520;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;130;-5136,1872;Inherit;False;107;Disto;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;131;-5168,1648;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;132;-5152,1792;Inherit;False;89;CustomUV;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.OneMinusNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;133;-5376,800;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;257;-6706,2510;Inherit;False;1841.428;1076.581;Cutout;17;258;259;256;252;254;253;255;250;251;246;249;245;247;248;244;266;267;Cutout;0,0,0,1;0;0
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;126;-5504,704;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;134;-4944,1520;Inherit;False;4;4;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT2;0,0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;135;-5328,896;Inherit;False;Property;_DissolveMaskInvert;Dissolve Mask Invert;13;0;Create;True;0;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;136;-5200,800;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector4Node, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;137;-4688,1680;Inherit;False;Property;_TextureChannel;Texture Channel;1;0;Create;True;0;0;0;False;0;False;0,1,0,0;0,1,0,0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;138;-5008,704;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;139;-4784,1456;Inherit;True;Property;_TextureSample1;Texture Sample 1;1;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;False;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;246;-6656,2944;Inherit;False;Property;_CutoutRotation;Cutout Rotation;33;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;248;-6656,2560;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;267;-6400,2768;Inherit;False;Property;_CutoutOffset;Cutout Offset;34;0;Create;True;0;0;0;False;0;False;0,0;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.DotProductOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;140;-4464,1504;Inherit;False;2;0;COLOR;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;141;-4816,896;Inherit;False;0;-1;4;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;143;-4800,784;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;142;-4736,1088;Inherit;False;Property;_DissolveOffset;Dissolve Offset;14;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RadiansOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;259;-6272,3328;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;266;-6400,2560;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;144;-3968,752;Inherit;False;1533.878;534.1096;Adjustments;14;223;158;155;153;152;151;150;149;148;260;261;262;263;264;Adjustments;0,0,0,1;0;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;145;-4560,784;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;146;-4304,1504;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;254;-5888,2944;Inherit;False;Property;_CutoutErosionSmoothness;Cutout Erosion Smoothness;32;0;Create;True;0;0;0;False;0;False;0.05;0.05;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RotatorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;258;-6272,3200;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;147;-4144,800;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;148;-3856,896;Inherit;False;Property;_CorePower;Core Power;18;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;150;-3904,1120;Inherit;False;Property;_GlowIntensity;Glow Intensity;20;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;253;-5888,2816;Inherit;False;Property;_CutoutErosion;Cutout Erosion;31;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;255;-5504,2816;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;244;-5888,2560;Inherit;True;Property;_Cutout;Cutout;30;0;Create;True;0;0;0;False;3;Space(33);Header(Cutout);Space(13);False;-1;57a59131c90e9a248aa75dede8b96462;57a59131c90e9a248aa75dede8b96462;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;False;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.PowerNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;149;-3712,800;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;151;-3600,912;Inherit;False;Property;_CoreIntensity;Core Intensity;19;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;152;-3712,1040;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;252;-5504,2560;Inherit;True;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;153;-3424,816;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;263;-3456,1040;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;256;-5120,2560;Inherit;False;cutout;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;261;-3200,816;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;155;-3216,1024;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;265;-2974.576,934.7301;Inherit;False;256;cutout;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;262;-3072,1024;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;260;-2928,816;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;156;-2176,768;Inherit;False;Constant;_Value1;Value1;28;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;264;-2928,1024;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;201;-1920,0;Inherit;False;Property;_CoreColor;Core Color;16;0;Create;True;0;0;0;False;0;False;1,1,1,0;1,1,1,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;223;-2720,816;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.VertexColorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;154;-2688,-256;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;159;-1920,768;Inherit;False;Property;_MeshVertexColor;MeshVertexColor;24;0;Create;True;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;True;All;9;1;FLOAT3;0,0,0;False;0;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT3;0,0,0;False;5;FLOAT3;0,0,0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;158;-2720,1024;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;164;-2176,1152;Inherit;False;Property;_ValueStepAdd;Value Step Add;27;0;Create;True;0;0;0;False;0;False;0.1;0.1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;188;-1920,-128;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;242;-816,1344;Inherit;False;324;418.95;Depth Fade;3;219;213;236;Depth Fade;0,0,0,1;0;0
Node;AmplifyShaderEditor.ComponentMaskNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;157;-2432,-256;Inherit;False;True;True;True;False;1;0;COLOR;0,0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;160;-1920,512;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;161;-1536,640;Inherit;False;Property;_AlphaBoldness;Alpha Boldness;21;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;165;-2176,1024;Inherit;False;Property;_ValueStep;Value Step;26;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;166;-1152,1152;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.2;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;211;-1664,-128;Inherit;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;212;-1280,-128;Inherit;False;Property;_DifferentCoreColor;Different Core Color;17;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;162;-1536,512;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;219;-768,1664;Inherit;False;Property;_DepthFadeIntensity;Depth Fade Intensity;29;0;Create;True;0;0;0;False;0;False;0;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;170;-1152,1024;Inherit;False;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;1,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;217;-1280,-256;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;167;-1280,512;Inherit;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.DepthFade, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;213;-768,1536;Inherit;False;True;False;False;2;1;FLOAT3;0,0,0;False;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;174;-896,1024;Inherit;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;233;-1024,-256;Inherit;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;193;-896,-128;Inherit;False;Property;_EmissionIntensity;Emission Intensity;15;0;Create;True;0;0;0;False;3;Space(33);Header(Properties);Space(13);False;1;2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;236;-768,1408;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;183;-896,512;Inherit;False;Property;_Step;Step;25;0;Create;True;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;True;All;9;1;FLOAT3;0,0,0;False;0;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT3;0,0,0;False;5;FLOAT3;0,0,0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;224;-896,-256;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;163;-2048,1920;Inherit;False;1535.601;892.1229;Push Particle toward camera direction (no more glow clipping in the ground) | 0=Disabled;9;240;168;169;171;177;173;175;221;172;;0,0,0,1;0;0
Node;AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;15;462,-50;Inherit;False;1252;163.3674;Ge Lush was here! <3;5;10;11;12;13;14;Ge Lush was here! <3;0.4902092,0.3301886,1,1;0;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;235;-448,416;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;234;-640,-256;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT3;0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;228;-384,-128;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;10;512,0;Inherit;False;Property;_Cull;Cull;35;0;Create;True;0;0;0;True;3;Space(33);Header(AR);Space(13);False;2;2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;11;768,0;Inherit;False;Property;_Src;Src;36;0;Create;True;0;0;0;True;0;False;5;5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;12;1024,0.7006989;Inherit;False;Property;_Dst;Dst;37;0;Create;True;0;0;0;True;0;False;10;10;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;13;1280,0;Inherit;False;Property;_ZWrite;ZWrite;38;0;Create;True;0;0;0;True;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;239;-384,128;Inherit;False;Property;_UseDepthFade1;Use Depth Fade;28;0;Create;True;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Reference;238;True;True;All;9;1;FLOAT3;0,0,0;False;0;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT3;0,0,0;False;5;FLOAT3;0,0,0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.WorldPosInputsNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;172;-1920,2048;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;221;-1280,2048;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;175;-1664,2048;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.WorldSpaceCameraPos, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;173;-1920,2192;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;177;-1280,2176;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.01;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;171;-1280,2304;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;169;-1920,2432;Inherit;False;Property;_CameraDirPush;CameraDirPush;22;0;Create;True;0;0;0;False;0;False;0;-50;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;168;-1920,2560;Inherit;False;3;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;240;-1024,2048;Inherit;False;cameraPush;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;241;0,256;Inherit;False;240;cameraPush;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;14;1536,0;Inherit;False;Property;_ZTest;ZTest;39;0;Create;True;0;0;0;True;0;False;2;2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;238;-384,-256;Inherit;False;Property;_UseDepthFade;Use Depth Fade;28;0;Create;True;0;0;0;False;3;Space(33);Header(Depth Fade);Space(13);False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;True;All;9;1;COLOR;0,0,0,0;False;0;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;4;COLOR;0,0,0,0;False;5;COLOR;0,0,0,0;False;6;COLOR;0,0,0,0;False;7;COLOR;0,0,0,0;False;8;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;247;-6656,2688;Inherit;False;Constant;_Vector0;Vector 0;44;0;Create;True;0;0;0;False;0;False;0.5,0.5;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.RotatorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;245;-6272,2560;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleDivideOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;251;-6400,2944;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;360;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;250;-6272,2944;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PiNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;249;-6272,3072;Inherit;False;1;0;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;0;0,0;Float;False;False;-1;2;UnityEditor.ShaderGraphUnlitGUI;0;1;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;ExtraPrePass;0;0;ExtraPrePass;5;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;False;False;False;True;4;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;UniversalMaterialType=Unlit;True;5;True;14;all;0;False;True;1;1;False;;0;False;;0;1;False;;0;False;;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;True;True;True;True;0;False;;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;True;1;False;;True;3;False;;True;True;0;False;;0;False;;True;0;False;False;0;;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;1;0,0;Float;False;True;-1;2;UnityEditor.ShaderGraphUnlitGUI;0;16;Vefects/SH_VFX_Stylized_Slash_01;2992e84f91cbeb14eab234972e07ea9d;True;Forward;0;1;Forward;9;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;True;True;0;True;_Cull;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;False;False;False;True;4;RenderPipeline=UniversalPipeline;RenderType=Transparent=RenderType;Queue=Transparent=Queue=0;UniversalMaterialType=Unlit;True;5;True;14;all;0;True;True;2;5;True;_Src;10;True;_Dst;1;1;False;;10;False;;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;True;True;True;True;0;False;;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;True;2;False;_ZWrite;True;3;True;_ZTest;True;True;0;False;;0;False;;True;1;LightMode=UniversalForwardOnly;False;False;0;;0;0;Standard;30;Surface;1;638463264845950016;  Keep Alpha;0;0;  Blend;0;0;Two Sided;1;0;Alpha Clipping;0;638774900561332734;  Use Shadow Threshold;0;0;Forward Only;0;0;Cast Shadows;0;638463266204850631;Receive Shadows;1;0;Receive SSAO;1;0;Motion Vectors;1;0;  Add Precomputed Velocity;0;0;  XR Motion Vectors;0;0;GPU Instancing;1;0;LOD CrossFade;1;0;Built-in Fog;0;638995906852995947;Meta Pass;0;0;Extra Pre Pass;0;0;Tessellation;0;0;  Phong;0;0;  Strength;0.5,False,;0;  Type;0;0;  Tess;16,False,;0;  Min;10,False,;0;  Max;25,False,;0;  Edge Length;16,False,;0;  Max Displacement;25,False,;0;Write Depth;0;0;  Early Z;0;0;Vertex Position;1;638485006437223309;0;13;False;True;False;True;False;False;True;True;True;False;True;False;True;False;;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;2;0,0;Float;False;False;-1;2;UnityEditor.ShaderGraphUnlitGUI;0;1;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;ShadowCaster;0;2;ShadowCaster;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;False;False;False;True;4;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;UniversalMaterialType=Unlit;True;5;True;14;all;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;False;False;True;False;False;False;False;0;False;;False;False;False;False;False;False;False;False;False;True;1;False;;True;3;False;;False;True;1;LightMode=ShadowCaster;False;False;0;;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;3;0,0;Float;False;False;-1;2;UnityEditor.ShaderGraphUnlitGUI;0;1;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;DepthOnly;0;3;DepthOnly;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;False;False;False;True;4;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;UniversalMaterialType=Unlit;True;5;True;14;all;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;False;False;True;False;False;False;False;0;False;;False;False;False;False;False;False;False;False;False;True;1;False;;False;False;True;1;LightMode=DepthOnly;False;False;0;;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;4;0,0;Float;False;False;-1;2;UnityEditor.ShaderGraphUnlitGUI;0;1;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;Meta;0;4;Meta;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;False;False;False;True;4;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;UniversalMaterialType=Unlit;True;5;True;14;all;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;2;False;;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;LightMode=Meta;False;False;0;;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;5;0,0;Float;False;False;-1;2;UnityEditor.ShaderGraphUnlitGUI;0;1;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;Universal2D;0;5;Universal2D;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;False;False;False;True;4;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;UniversalMaterialType=Unlit;True;5;True;14;all;0;False;True;1;1;False;;0;False;;0;1;False;;0;False;;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;True;True;True;True;0;False;;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;True;1;False;;True;3;False;;True;True;0;False;;0;False;;True;1;LightMode=Universal2D;False;False;0;;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;6;0,0;Float;False;False;-1;2;UnityEditor.ShaderGraphUnlitGUI;0;1;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;SceneSelectionPass;0;6;SceneSelectionPass;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;False;False;False;True;4;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;UniversalMaterialType=Unlit;True;5;True;14;all;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;2;False;;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;LightMode=SceneSelectionPass;False;False;0;;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;7;0,0;Float;False;False;-1;2;UnityEditor.ShaderGraphUnlitGUI;0;1;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;ScenePickingPass;0;7;ScenePickingPass;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;False;False;False;True;4;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;UniversalMaterialType=Unlit;True;5;True;14;all;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;LightMode=Picking;False;False;0;;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;8;0,0;Float;False;False;-1;2;UnityEditor.ShaderGraphUnlitGUI;0;1;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;DepthNormals;0;8;DepthNormals;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;False;False;False;True;4;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;UniversalMaterialType=Unlit;True;5;True;14;all;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;False;;True;3;False;;False;True;1;LightMode=DepthNormalsOnly;False;False;0;;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;9;0,0;Float;False;False;-1;2;UnityEditor.ShaderGraphUnlitGUI;0;1;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;DepthNormalsOnly;0;9;DepthNormalsOnly;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;False;False;False;True;4;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;UniversalMaterialType=Unlit;True;5;True;14;all;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;False;;True;3;False;;False;True;1;LightMode=DepthNormalsOnly;False;True;11;d3d11;metal;vulkan;xboxone;xboxseries;playstation;ps4;ps5;switch;switch2;webgpu;0;;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;268;0,100;Float;False;False;-1;3;UnityEditor.ShaderGraphUnlitGUI;0;1;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;MotionVectors;0;10;MotionVectors;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;False;False;False;True;4;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;UniversalMaterialType=Unlit;True;5;True;14;all;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;True;True;False;False;0;False;;False;False;False;False;False;False;False;False;False;False;False;False;True;1;LightMode=MotionVectors;False;False;0;;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;269;0,100;Float;False;False;-1;3;UnityEditor.ShaderGraphUnlitGUI;0;1;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;XRMotionVectors;0;11;XRMotionVectors;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;False;False;False;True;4;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;UniversalMaterialType=Unlit;True;5;True;14;all;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;True;True;True;True;0;False;;False;False;False;False;False;False;False;True;True;1;False;;255;False;;1;False;;7;False;;3;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;False;False;False;True;1;LightMode=XRMotionVectors;False;False;0;;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;270;0,100;Float;False;False;-1;3;UnityEditor.ShaderGraphUnlitGUI;0;1;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;GBuffer;0;12;GBuffer;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;False;False;False;True;4;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;UniversalMaterialType=Unlit;True;5;True;14;all;0;False;True;2;5;True;_Src;10;True;_Dst;1;1;False;;10;False;;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;True;True;True;True;0;False;;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;True;2;False;;True;3;True;;True;True;0;False;;0;False;;True;1;LightMode=UniversalGBuffer;False;True;12;d3d11;gles;metal;vulkan;xboxone;xboxseries;playstation;ps4;ps5;switch;switch2;webgpu;0;;0;0;Standard;0;False;0
WireConnection;92;2;87;0
WireConnection;90;0;86;0
WireConnection;94;0;91;0
WireConnection;94;1;88;0
WireConnection;93;2;87;0
WireConnection;95;0;92;0
WireConnection;95;2;90;0
WireConnection;97;0;95;0
WireConnection;97;1;93;4
WireConnection;97;2;96;0
WireConnection;97;3;94;0
WireConnection;98;0;87;0
WireConnection;98;1;97;0
WireConnection;100;0;98;0
WireConnection;100;1;99;0
WireConnection;102;0;100;0
WireConnection;85;1;84;0
WireConnection;85;0;83;0
WireConnection;106;0;102;0
WireConnection;106;1;103;0
WireConnection;89;0;85;0
WireConnection;109;2;105;0
WireConnection;110;0;104;0
WireConnection;107;0;106;0
WireConnection;112;0;109;0
WireConnection;112;2;110;0
WireConnection;114;2;105;0
WireConnection;116;0;111;0
WireConnection;116;1;108;0
WireConnection;117;0;112;0
WireConnection;117;1;114;4
WireConnection;117;2;113;0
WireConnection;117;3;116;0
WireConnection;117;4;115;0
WireConnection;120;0;105;0
WireConnection;120;1;117;0
WireConnection;125;2;121;0
WireConnection;128;0;123;0
WireConnection;122;0;120;0
WireConnection;122;1;119;0
WireConnection;129;0;125;0
WireConnection;129;2;128;0
WireConnection;131;0;124;0
WireConnection;131;1;127;0
WireConnection;133;0;126;0
WireConnection;126;0;122;0
WireConnection;134;0;129;0
WireConnection;134;1;131;0
WireConnection;134;2;132;0
WireConnection;134;3;130;0
WireConnection;136;0;133;0
WireConnection;138;0;126;0
WireConnection;138;1;136;0
WireConnection;138;2;135;0
WireConnection;139;0;121;0
WireConnection;139;1;134;0
WireConnection;140;0;139;0
WireConnection;140;1;137;0
WireConnection;143;0;138;0
WireConnection;259;0;246;0
WireConnection;266;0;248;0
WireConnection;266;1;267;0
WireConnection;145;0;143;0
WireConnection;145;1;141;3
WireConnection;145;2;142;0
WireConnection;146;0;140;0
WireConnection;258;0;266;0
WireConnection;258;2;259;0
WireConnection;147;0;145;0
WireConnection;147;1;146;0
WireConnection;255;0;253;0
WireConnection;255;1;254;0
WireConnection;244;1;258;0
WireConnection;149;0;147;0
WireConnection;149;1;148;0
WireConnection;152;0;147;0
WireConnection;152;1;150;0
WireConnection;252;0;244;2
WireConnection;252;1;253;0
WireConnection;252;2;255;0
WireConnection;153;0;149;0
WireConnection;153;1;151;0
WireConnection;263;0;152;0
WireConnection;256;0;252;0
WireConnection;261;0;153;0
WireConnection;155;0;261;0
WireConnection;155;1;263;0
WireConnection;262;0;155;0
WireConnection;260;0;261;0
WireConnection;260;1;265;0
WireConnection;264;0;262;0
WireConnection;264;1;265;0
WireConnection;223;0;260;0
WireConnection;159;1;156;0
WireConnection;159;0;157;0
WireConnection;158;0;264;0
WireConnection;188;0;157;0
WireConnection;188;1;201;0
WireConnection;188;2;223;0
WireConnection;157;0;154;0
WireConnection;160;0;154;4
WireConnection;160;1;158;0
WireConnection;160;2;159;0
WireConnection;166;0;165;0
WireConnection;166;1;164;0
WireConnection;211;0;188;0
WireConnection;162;0;160;0
WireConnection;162;1;161;0
WireConnection;170;0;167;0
WireConnection;170;1;165;0
WireConnection;170;2;166;0
WireConnection;217;0;157;0
WireConnection;217;1;211;0
WireConnection;217;2;212;0
WireConnection;167;0;162;0
WireConnection;213;0;219;0
WireConnection;174;0;170;0
WireConnection;233;0;217;0
WireConnection;236;0;213;0
WireConnection;183;1;167;0
WireConnection;183;0;174;0
WireConnection;224;0;233;0
WireConnection;224;1;193;0
WireConnection;235;0;183;0
WireConnection;235;1;236;0
WireConnection;234;0;224;0
WireConnection;234;1;183;0
WireConnection;228;0;234;0
WireConnection;228;1;236;0
WireConnection;239;1;183;0
WireConnection;239;0;235;0
WireConnection;221;0;175;0
WireConnection;221;1;177;0
WireConnection;175;0;172;0
WireConnection;175;1;173;0
WireConnection;177;0;171;0
WireConnection;171;0;169;0
WireConnection;171;1;168;2
WireConnection;240;0;221;0
WireConnection;238;1;234;0
WireConnection;238;0;228;0
WireConnection;245;0;266;0
WireConnection;245;1;247;0
WireConnection;245;2;250;0
WireConnection;251;0;246;0
WireConnection;250;0;251;0
WireConnection;250;1;249;0
WireConnection;1;2;238;0
WireConnection;1;3;239;0
ASEEND*/
//CHKSM=0C6539B7C7118307AEAF004E0C29E565780729E3