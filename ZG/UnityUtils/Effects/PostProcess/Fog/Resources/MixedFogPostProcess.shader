Shader "ZG/MixedFogPostProcess"
{
	HLSLINCLUDE

#pragma multi_compile __ FOG_LINEAR FOG_EXP FOG_EXP2

#include "Packages/com.unity.postprocessing/PostProcessing/Shaders/StdLib.hlsl"
#include "Packages/com.unity.postprocessing/PostProcessing/Shaders/Builtins/Fog.hlsl"

	TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
	TEXTURE2D_SAMPLER2D(_CameraDepthTexture, sampler_CameraDepthTexture);
	TEXTURE2D_SAMPLER2D(_CameraGBufferTexture0, sampler_CameraGBufferTexture0);
	TEXTURE2D_SAMPLER2D(_CameraGBufferTexture2, sampler_CameraGBufferTexture2);

#define SKYBOX_THREASHOLD_VALUE 0.9999

	float _AlphaStart;
	float _AlphaEnd;

	float4 Frag(VaryingsDefault i) : SV_Target
	{
		half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoordStereo);

		float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, i.texcoordStereo);
		depth = Linear01Depth(depth);
		float dist = ComputeFogDistance(depth);
		half fog = 1.0 - ComputeFog(dist);
		half3 fogColor = lerp(color.rgb, _FogColor.rgb, fog);
		fog = SAMPLE_TEXTURE2D(_CameraGBufferTexture0, sampler_CameraGBufferTexture0, i.texcoordStereo).a;
		float mix = SAMPLE_TEXTURE2D(_CameraGBufferTexture2, sampler_CameraGBufferTexture2, i.texcoordStereo).a;
		fog = lerp(1.0f, fog, mix);

		float alpha = smoothstep(_AlphaStart, _AlphaEnd, dist);

		return float4(lerp(color.rgb, fogColor.rgb, fog), (1.0f - alpha/* * fog*/) * mix);
	}

	float4 FragExcludeSkybox(VaryingsDefault i) : SV_Target
	{
		half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoordStereo);

		float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, i.texcoordStereo);
		depth = Linear01Depth(depth);
		float skybox = depth < SKYBOX_THREASHOLD_VALUE;
		float dist = ComputeFogDistance(depth);
		half fog = 1.0 - ComputeFog(dist);
		half3 fogColor = lerp(color.rgb, _FogColor.rgb, fog * skybox);
		fog = SAMPLE_TEXTURE2D(_CameraGBufferTexture0, sampler_CameraGBufferTexture0, i.texcoordStereo).a;
		float mix = SAMPLE_TEXTURE2D(_CameraGBufferTexture2, sampler_CameraGBufferTexture2, i.texcoordStereo).a;
		fog = lerp(1.0f, fog, mix);

		float alpha = smoothstep(_AlphaStart, _AlphaEnd, dist);

		return float4(lerp(color.rgb, fogColor.rgb, fog), (1.0f - alpha * fog) * mix);
	}

	ENDHLSL

	SubShader
	{
		Cull Off ZWrite Off ZTest Always
		Pass
		{
			HLSLPROGRAM

			#pragma vertex VertDefault
			#pragma fragment Frag

			ENDHLSL
		}

		Pass
		{
			HLSLPROGRAM

			#pragma vertex VertDefault
			#pragma fragment FragExcludeSkybox

			ENDHLSL
		}
	}
}
