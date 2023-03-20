Shader "ZG/FlatStandard" {
	Properties {
		[Texture(ENABLE_TEXTURE)] _MainTex("Albedo (RGB)", 2D) = "white" {}
		//[Toggle(ENABLE_TEXTURE_AND_COLOR)] _EnableColor("Enable Texture And Color", Float) = 0
		[Toggle(ENABLE_EMISSION)] _EnableEmission("Enable Emission", Float) = 0
		//[Toggle(ENABLE_CLIP)] _EnableClip("Enable Clip", Float) = 0
		[Clip] _ClipParams("Clip", Vector) = (0.2, 0.2, 12.0, 0)

		[HDR]_Emission ("Emission", Color) = (0, 0, 0, 0)
		[HDR]_Color ("Color", Color) = (1,1,1,1)
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
		/*_ClipDistance("Clip Distance", Float) = 3.0
		_ClipNear("Clip Near", Float) = 1.0
		_ClipFar("Clip Far", Float) = 70.0*/
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
			 
		CGPROGRAM

		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows
		#pragma shader_feature __ ENABLE_TEXTURE//ENABLE_TEXTURE_AND_COLOR ENABLE_TEXTURE
		//#pragma shader_feature __ ENABLE_COLOR
		#pragma shader_feature __ ENABLE_EMISSION
		#pragma shader_feature __ CLIP_MIX CLIP_GLOBAL

#if CLIP_MIX || CLIP_GLOBAL
		#include "Clip.cginc"

		CLIP_PARAMS(_ClipParams)
#endif

		// Use shader model 3.0 target, to get nicer looking lighting
		//#pragma target 3.0

		half _Glossiness;
		half _Metallic;

#if ENABLE_EMISSION
		fixed4 _Emission;
#endif

#if ENABLE_TEXTURE_AND_COLOR || !ENABLE_TEXTURE
		fixed4 _Color;
#endif

#if ENABLE_TEXTURE || ENABLE_TEXTURE_AND_COLOR
		sampler2D _MainTex;
#endif

		//float4x4 _RowAccess = { 1,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,0,1 };

		struct Input {
#if ENABLE_TEXTURE || ENABLE_TEXTURE_AND_COLOR
			float2 uv_MainTex;
#endif
			float3 worldNormal;
			float3 viewDir;

#if CLIP_MIX || CLIP_GLOBAL
			float4 screenPos;
#endif
		};

		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_BUFFER_START(Props)
		UNITY_INSTANCING_BUFFER_END(Props)

		void surf (Input IN, inout SurfaceOutputStandard o) 
		{
			fixed4 c;
#if ENABLE_TEXTURE_AND_COLOR
			// Albedo comes from a texture tinted by color
			c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
#elif ENABLE_TEXTURE
			c = tex2D(_MainTex, IN.uv_MainTex);
#else 
			c = _Color;
#endif

#if CLIP_MIX || CLIP_GLOBAL
			CLIP(IN.screenPos, _ClipParams, c.a);
			/*float3 screenPos = IN.screenPos.xyz / IN.screenPos.w;
			screenPos.xy *= _ScreenParams.xy;

			float4x4 thresholdMatrix =
			{
				1.0 / 17.0,  9.0 / 17.0,  3.0 / 17.0, 11.0 / 17.0,
				13.0 / 17.0,  5.0 / 17.0, 15.0 / 17.0,  7.0 / 17.0,
				4.0 / 17.0, 12.0 / 17.0,  2.0 / 17.0, 10.0 / 17.0,
				16.0 / 17.0,  8.0 / 17.0, 14.0 / 17.0,  6.0 / 17.0
			};

#if (defined(SHADER_API_GLES) || defined(SHADER_API_GLES3)) && defined(SHADER_API_MOBILE)
			float linearEyeDepth = LinearEyeDepth((screenPos.z + 1.0f) * 0.5f);
#else
			float linearEyeDepth = LinearEyeDepth(screenPos.z);
#endif

			float threshold = saturate((linearEyeDepth - _ClipNear) / _ClipDistance);
			threshold = lerp(0.0f, threshold, (_ClipFar - linearEyeDepth) / _ClipDistance);
			threshold = lerp(0.0f, threshold, c.a);
			clip(threshold - thresholdMatrix[fmod(screenPos.x, 4)][fmod(screenPos.y, 4)]);*/
#endif

			float factor = max(dot(IN.worldNormal, IN.viewDir), 0.0f);
			o.Albedo = c.rgb * (factor * 0.5f + 0.5f);

#if ENABLE_EMISSION
			o.Emission = _Emission * (1.0f - factor);
#endif
			// Metallic and smoothness come from slider variables
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = c.a;
		}
		ENDCG
	}

	CustomEditor "ZG.ShaderRenderTypeGUI"

	FallBack "Diffuse"
}
