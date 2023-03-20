#ifndef CLIP_CGINC  
#define CLIP_CGINC

#if UNITY_PASS_SHADOWCASTER 

#define CLIP_PARAMS(clipParams)
#define CLIP(screenPos, clipParams, alpha) AlphaClip(screenPos, alpha)

#elif CLIP_MIX

#define CLIP_PARAMS(clipParams) float4 clipParams;
#define CLIP(screenPos, clipParams, alpha) MixedClip(screenPos, (clipParams).x, (clipParams).y, (clipParams).z, alpha)

#elif CLIP_GLOBAL

#define CLIP_PARAMS(name)
#define CLIP(screenPos, clipParams, alpha) MixedClip(screenPos, g_ClipInvDist, g_ClipNearDivDist, g_ClipFarDivDist, alpha)

uniform float g_ClipInvDist;
uniform float g_ClipNearDivDist;
uniform float g_ClipFarDivDist;
 
#else

#define CLIP_PARAMS(clipParams)
#define CLIP(screenPos, clipParams, alpha) AlphaClip(screenPos, alpha)

#endif

float3 CalculatePixelPos(float4 screenPos)
{
	float3 pixelPos = screenPos.xyz / screenPos.w;
	pixelPos.xy *= _ScreenParams.xy;
	return pixelPos;
}

float CalculateAlphaClipThreshold(float2 pixelPos, float alpha)
{
	const float4x4 thresholdMatrix =
	{
		1.0 / 17.0,  9.0 / 17.0,  3.0 / 17.0, 11.0 / 17.0,
		13.0 / 17.0,  5.0 / 17.0, 15.0 / 17.0,  7.0 / 17.0,
		4.0 / 17.0, 12.0 / 17.0,  2.0 / 17.0, 10.0 / 17.0,
		16.0 / 17.0,  8.0 / 17.0, 14.0 / 17.0,  6.0 / 17.0
	};

	return alpha - thresholdMatrix[fmod(pixelPos.x, 4)][fmod(pixelPos.y, 4)];
}

float CalculateNearFarClipThreshold(float screenDepth, float invDist, float nearDivDist, float farDivDist)
{
#if (defined(SHADER_API_GLES) || defined(SHADER_API_GLES3)) && defined(SHADER_API_MOBILE)
	float linearEyeDepth = LinearEyeDepth((screenDepth + 1.0f) * 0.5f);
#else
	float linearEyeDepth = LinearEyeDepth(screenDepth);
#endif

	float depthDivDist = linearEyeDepth * invDist;
	return saturate(depthDivDist - nearDivDist) * saturate(farDivDist - depthDivDist);
}

float CalculateMixedClipThreshold(float4 screenPos, float invDist, float nearDivDist, float farDivDist, float alpha)
{
	float3 pixelPos = CalculatePixelPos(screenPos);

	alpha *= CalculateNearFarClipThreshold(pixelPos.z, invDist, nearDivDist, farDivDist);
	return CalculateAlphaClipThreshold(pixelPos.xy, alpha);
}

void AlphaClip(float4 screenPos, float alpha)
{
	float threshold = CalculateAlphaClipThreshold(screenPos.xy / screenPos.w * _ScreenParams.xy, alpha);

	clip(threshold);
}

void MixedClip(float4 screenPos, float invDist, float nearDivDist, float farDivDist, float alpha)
{
	float threshold = CalculateMixedClipThreshold(screenPos, invDist, nearDivDist, farDivDist, alpha);

	clip(threshold);
}

#endif