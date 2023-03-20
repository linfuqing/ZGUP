Shader "ZG/BlurOutlinePostProcess"
{
	HLSLINCLUDE

#include "Packages/com.unity.postprocessing/PostProcessing/Shaders/StdLib.hlsl"

	struct VaryingsBlur
	{
		float2 uv : TEXCOORD0;
		float4 uv01 : TEXCOORD1;
		float4 uv23 : TEXCOORD2;
		float4 uv45 : TEXCOORD3;
		float4 vertex : SV_POSITION;
	};

	TEXTURE2D_SAMPLER2D(_BlurTex, sampler_BlurTex);
	TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
	float4 _MainTex_TexelSize;

	float _BlurOffsetX;
	float _BlurOffsetY;

	float _Strength;

	float4 FragTex(VaryingsDefault i) : SV_Target
	{
		return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord);
	}

	VaryingsBlur VertBlur(AttributesDefault v)
	{
		VaryingsDefault i = VertDefault(v);
		VaryingsBlur o;

		float2 offsets = float2(_MainTex_TexelSize.x * _BlurOffsetX, _MainTex_TexelSize.y * _BlurOffsetY);

		o.uv = i.texcoord;
		o.uv01 = i.texcoord.xyxy + offsets.xyxy * float4(1, 1, -1, -1);
		o.uv23 = i.texcoord.xyxy + offsets.xyxy * float4(1, 1, -1, -1) * 2.0;
		o.uv45 = i.texcoord.xyxy + offsets.xyxy * float4(1, 1, -1, -1) * 3.0;

		o.vertex = i.vertex;

		return o;
	}

	float4 FragBlur(VaryingsBlur i) : SV_Target
	{
		float4 color = float4(0.0, 0.0, 0.0, 0.0);

		color += 0.40 * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
		color += 0.15 * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv01.xy);
		color += 0.15 * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv01.zw);
		color += 0.10 * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv23.xy);
		color += 0.10 * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv23.zw);
		color += 0.05 * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv45.xy);
		color += 0.05 * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv45.zw);

		return color;
	}

	float4 FragOutline(VaryingsDefault i) : SV_Target
	{
		return SAMPLE_TEXTURE2D(_BlurTex, sampler_BlurTex, i.texcoord) - SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord);
	}

	float4 FragFinal(VaryingsDefault i) : SV_Target
	{
		return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord) + SAMPLE_TEXTURE2D(_BlurTex, sampler_BlurTex, i.texcoord) * _Strength;
	}

	ENDHLSL

	SubShader
	{
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			HLSLPROGRAM

			#pragma vertex VertDefault
			#pragma fragment FragTex
			//#pragma fragmentoption ARB_precision_hint_fastest

			ENDHLSL
		}

		Pass
		{
			HLSLPROGRAM

			#pragma vertex VertBlur
			#pragma fragment FragBlur
			//#pragma fragmentoption ARB_precision_hint_fastest  

			ENDHLSL
		}

		Pass
		{
			HLSLPROGRAM

			#pragma vertex VertDefault
			#pragma fragment FragOutline
			//#pragma fragmentoption ARB_precision_hint_fastest

			ENDHLSL
		}

		Pass
		{
			HLSLPROGRAM

			#pragma vertex VertDefault
			#pragma fragment FragFinal
			//#pragma fragmentoption ARB_precision_hint_fastest

			ENDHLSL
		}
	}
}
