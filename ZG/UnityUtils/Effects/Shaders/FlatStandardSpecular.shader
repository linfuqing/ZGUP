Shader "ZG/FlatStandardSpecular" {
	Properties {
		[Toggle(ENABLE_TEXTURE)] _EnableTexture("Enable Texture", Float) = 1
		//[Toggle(ENABLE_TEXTURE_AND_COLOR)] _EnableColor("Enable Texture And Color", Float) = 0
		[Toggle(ENABLE_EMISSION)] _EnableEmission("Enable Emission", Float) = 0
		[Clip] _ClipParams("Clip", Vector) = (0.2, 0.2, 12.0, 0)

		_MainTex("Albedo (RGB)", 2D) = "white" {}

		[HDR]_Emission("Emission", Color) = (0, 0, 0, 0)

		[HDR]_Specular("Specular", Color) = (1, 1, 1, 1)

		[HDR]_Color ("Color", Color) = (1,1,1,1)
		_Smoothness("Smoothness", Range(0,1)) = 0.5
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf StandardSpecular fullforwardshadows
		#pragma shader_feature __ ENABLE_TEXTURE//ENABLE_TEXTURE_AND_COLOR ENABLE_TEXTURE
		//#pragma shader_feature __ ENABLE_COLOR
		#pragma shader_feature __ ENABLE_EMISSION
		#pragma shader_feature __ CLIP_MIX CLIP_GLOBAL
		// Use shader model 3.0 target, to get nicer looking lighting
		//#pragma target 3.0

#if CLIP_MIX || CLIP_GLOBAL
		#include "Clip.cginc"

		CLIP_PARAMS(_ClipParams)
#endif

		half _Smoothness;
		fixed4 _Specular;

#if ENABLE_EMISSION
		fixed4 _Emission;
#endif

#if ENABLE_TEXTURE_AND_COLOR || !ENABLE_TEXTURE
		fixed4 _Color;
#endif

		sampler2D _MainTex;

		struct Input {
			float2 uv_MainTex;
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
			// put more per-instance properties here
		UNITY_INSTANCING_BUFFER_END(Props)

		void surf (Input IN, inout SurfaceOutputStandardSpecular o) 
		{
			fixed4 c;
#if ENABLE_TEXTURE_AND_COLOR
			// Albedo comes from a texture tinted by color
			c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
#elif ENABLE_TEXTURE
			c = tex2D(_MainTex, IN.uv_MainTex);
#else 
			c = _Color;
#endif

#if CLIP_MIX || CLIP_GLOBAL
			CLIP(IN.screenPos, _ClipParams, c.a);
#endif

			float factor = max(dot(IN.worldNormal, IN.viewDir), 0.0f);
			o.Albedo = c.rgb * (factor * 0.5f + 0.5f);

#if ENABLE_EMISSION
			o.Emission = _Emission * (1.0f - factor);
#endif
			o.Specular = _Specular;
			o.Smoothness = _Smoothness;
			o.Alpha = c.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
