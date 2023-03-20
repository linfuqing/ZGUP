Shader "ZG/BlurPostProcess"
{
	HLSLINCLUDE

#include "Packages/com.unity.postprocessing/PostProcessing/Shaders/StdLib.hlsl"

#define PI 3.14159265359
#define E 2.71828182846

	TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);

	float _BlurSize;
	float _Samples;
	float _StandardDeviation;

	float4 FragVerticalBlur(VaryingsDefault i) : SV_Target
	{
		//init color variable
		float4 col = 0;
		float sum = _Samples;
		//iterate over blur samples
		for (float index = 0; index < _Samples; index++)
		{
			//get the offset of the sample
			float offset = (index / (_Samples - 1) - 0.5) * _BlurSize;
			//get uv coordinate of sample
			float2 uv = i.texcoord + float2(0, offset);

			//simply add the color if we don't have a gaussian blur (box)
			col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
		}
		//divide the sum of values by the amount of samples
		col = col / sum;
		return col;
	}

	float4 FragHorizontalGaussianBlur(VaryingsDefault i) : SV_Target
	{
		//init color variable
		float4 col = 0;
		float sum = 0;
		//iterate over blur samples
		for (float index = 0; index < _Samples; index++)
		{
			//get the offset of the sample
			float offset = (index / (_Samples - 1) - 0.5) * _BlurSize;
			//get uv coordinate of sample
			float2 uv = i.texcoord + float2(0, offset);

			//calculate the result of the gaussian function
			float stDevSquared = _StandardDeviation * _StandardDeviation;
			float gauss = (1 / sqrt(2 * PI*stDevSquared)) * pow(E, -((offset*offset) / (2 * stDevSquared)));
			//add result to sum
			sum += gauss;
			//multiply color with influence from gaussian function and add it to sum color
			col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv) * gauss;
		}
		//divide the sum of values by the amount of samples
		col = col / sum;
		return col;
	}

	float4 FragHorizontalBlur(VaryingsDefault i) : SV_Target
	{
		float invAspect = _ScreenParams.y / _ScreenParams.x;
		//init color variable
		float4 col = 0;
		float sum = _Samples;
		//iterate over blur samples
		for (float index = 0; index < _Samples; index++)
		{
			//get the offset of the sample
			float offset = (index / (_Samples - 1) - 0.5) * _BlurSize * invAspect;
			//get uv coordinate of sample
			float2 uv = i.texcoord + float2(offset, 0);

			col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
		}
		//divide the sum of values by the amount of samples
		col = col / sum;
		return col;
	}

	float4 FragVerticalGaussianBlur(VaryingsDefault i) : SV_Target
	{ 
		float invAspect = _ScreenParams.y / _ScreenParams.x;
		//init color variable
		float4 col = 0;
		float sum = 0;
		//iterate over blur samples
		for (float index = 0; index < _Samples; index++)
		{
			//get the offset of the sample
			float offset = (index / (_Samples - 1) - 0.5) * _BlurSize * invAspect;
			//get uv coordinate of sample
			float2 uv = i.texcoord + float2(offset, 0);
			//calculate the result of the gaussian function
			float stDevSquared = _StandardDeviation * _StandardDeviation;
			float gauss = (1 / sqrt(2 * PI*stDevSquared)) * pow(E, -((offset*offset) / (2 * stDevSquared)));
			//add result to sum
			sum += gauss;
			//multiply color with influence from gaussian function and add it to sum color
			col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv) * gauss;
		}
		//divide the sum of values by the amount of samples
		col = col / sum;
		return col;
	}

	ENDHLSL

	SubShader
	{
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			HLSLPROGRAM

			#pragma vertex VertDefault
			#pragma fragment FragVerticalBlur
			//#pragma fragmentoption ARB_precision_hint_fastest

			ENDHLSL
		}

		Pass
		{
			HLSLPROGRAM

			#pragma vertex VertDefault
			#pragma fragment FragVerticalGaussianBlur
			//#pragma fragmentoption ARB_precision_hint_fastest

			ENDHLSL
		}

		Pass
		{
			HLSLPROGRAM

			#pragma vertex VertDefault
			#pragma fragment FragHorizontalBlur
			//#pragma fragmentoption ARB_precision_hint_fastest

			ENDHLSL
		}

		Pass
		{
			HLSLPROGRAM

			#pragma vertex VertDefault
			#pragma fragment FragHorizontalGaussianBlur
			//#pragma fragmentoption ARB_precision_hint_fastest

			ENDHLSL
		}
	}
}
