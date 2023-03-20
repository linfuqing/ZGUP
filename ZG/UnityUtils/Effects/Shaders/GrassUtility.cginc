// Upgrade NOTE: replaced 'defined FOG_COMBINED_WITH_WORLD_POS' with 'defined (FOG_COMBINED_WITH_WORLD_POS)'

// Upgrade NOTE: replaced 'defined FOG_COMBINED_WITH_WORLD_POS' with 'defined (FOG_COMBINED_WITH_WORLD_POS)'

#ifndef GRASS_UTILITY_CGINC  
#define GRASS_UTILITY_CGINC

#include "UnityCG.cginc"
#include "AutoLight.cginc"
#include "UnityPBSLighting.cginc"

#if CLIP_MIX || CLIP_GLOBAL
#include "Clip.cginc"

CLIP_PARAMS(_ClipParams)
#endif

half _Glossiness;
half _Metallic;

half _Strength;
half _MinHeight;

float4 _Vertical;
float4 _Horizontal;

float4 _WindDirection;
float4 _WindDirectionParams;

float4 _WindNoise;
float4 _WindNoiseST;

float4 _WindNoise2;
float4 _WindNoiseST2;

float4 _HeightST;

#if ENABLE_TEXTURE_AND_COLOR || !ENABLE_TEXTURE
fixed4 _Color;
#endif

#if ENABLE_TEXTURE || ENABLE_TEXTURE_AND_COLOR
sampler2D _MainTex;
float4 _MainTex_ST;
#endif

uniform float g_Windspeed;
uniform int g_GrassObstacleCount = 0;
uniform float4 g_GrassObstacles[32];

/*struct appdata
{
	float3 normal : Normal;

	float4 vertex : POSITION;

#if ENABLE_TEXTURE_AND_COLOR || !ENABLE_TEXTURE
	float4 color:COLOR;
#endif

#if ENABLE_TEXTURE_AND_COLOR || ENABLE_TEXTURE
	float2 uv : TEXCOORD0;
#endif
};*/

struct Input
{
#if ENABLE_TEXTURE_AND_COLOR || ENABLE_TEXTURE
	float2 uv_MainTex;
#endif

#if CLIP_MIX || CLIP_GLOBAL
	float4 screenPos;
#endif

#if ENABLE_TEXTURE_AND_COLOR || !ENABLE_TEXTURE
	float4 color:COLOR;
#endif
};

#ifdef UNITY_HALF_PRECISION_FRAGMENT_SHADER_REGISTERS
#define FOG_COMBINED_WITH_WORLD_POS
#endif

struct grass_v2f
{
#if ENABLE_TEXTURE_AND_COLOR || ENABLE_TEXTURE
	float2 uv : TEXCOORD0;
#endif

	float3 worldNormal : TEXCOORD1;

	float3 worldPos : TEXCOORD2;

#if CLIP_MIX || CLIP_GLOBAL
	float4 screenPos : TEXCOORD3;
#endif

#if defined(UNITY_HALF_PRECISION_FRAGMENT_SHADER_REGISTERS) || !defined(IN_SURFACE_SHADER)
	LIGHTING_COORDS(4, 5)
#else
	UNITY_SHADOW_COORDS(4)
	UNITY_FOG_COORDS(5)
#endif

#if ENABLE_TEXTURE_AND_COLOR || !ENABLE_TEXTURE
	float4 color:COLOR;
#endif

	float4 pos : SV_POSITION;

	//UNITY_VERTEX_INPUT_INSTANCE_ID
	//UNITY_VERTEX_OUTPUT_STEREO
};

float Random(float2 st)
{
	return frac(sin(dot(st, float2(12.9898f, 78.233f))) * 43758.5453123f);
}

float Noise(float2 x)
{
	float2 i = floor(x);
	float2 f = frac(x);

	// Four corners in 2D of a tile
	float a = Random(i);
	float b = Random(i + float2(1.0, 0.0));
	float c = Random(i + float2(0.0, 1.0));
	float d = Random(i + float2(1.0, 1.0));

	// Simple 2D lerp using smoothstep envelope between the values.
	// return vec3(mix(mix(a, b, smoothstep(0.0, 1.0, f.x)),
	//			mix(c, d, smoothstep(0.0, 1.0, f.x)),
	//			smoothstep(0.0, 1.0, f.y)));

	// Same code, with the clamps in smoothstep and common subexpressions
	// optimized away.
	float2 u = f * f * (3.0 - 2.0 * f);
	return lerp(a, b, u.x) + (c - a) * u.y * (1.0 - u.x) + (d - b) * u.x * u.y;
}

float3 GrassCalculateWorldDistance(float heightRate, float3 worldPos, float3 normal, out float3 worldNormal)
{
	float3 viewDir = UnityWorldSpaceViewDir(worldPos);
	float viewDirLength = length(viewDir),
		heightNoise = max(Noise(worldPos.xz * _HeightST.xy + _HeightST.zw), _MinHeight) * smoothstep(_Vertical.w, _Vertical.z, viewDirLength),
		randomValue = Random(round(worldPos.xz * _Vertical.x)),
		randomAngle = randomValue * UNITY_PI * 2.0f, 
		height = heightNoise * heightRate;

	float horizontal = smoothstep(1.0f, _Horizontal.x, heightRate) * smoothstep(0.0f, _Horizontal.x, heightRate);
	float3 distance = mul(UNITY_MATRIX_T_MV, normal) * (horizontal * _Horizontal.y * heightNoise);
	//v.vertex.xyz += mul((float3x3)unity_WorldToObject, float3(0.0f, _Vertical.y, 0.0f)) * height;

	float3 randomDistance = float3(cos(randomAngle), 0.0f, sin(randomAngle));
	randomDistance *= _Horizontal.z * pow(height, _Horizontal.w);

	float windSpeed = max(g_Windspeed, _WindDirectionParams.x), randomTime = randomValue * _WindDirection.w;
	float windStrength = windSpeed * lerp(_WindDirectionParams.y, _WindDirectionParams.z, sin(windSpeed * _WindDirection.z * _Time.y + randomTime));
	float3 windDistance;
	windDistance.y = 0.0f;
	windDistance.xz = _WindDirection.xy * (windStrength * pow(height, _WindDirectionParams.w));

	randomTime = sin(_Time.y + randomTime);
	float windAngle = Noise(worldPos.xz * _WindNoiseST.xy + _WindNoiseST.zw + max(g_Windspeed, _WindNoise.x) * _Time.y * _WindDirection.xy) * (UNITY_PI * 2.0f);
	windDistance += float3(cos(windAngle), 0.0f, sin(windAngle)) * (lerp(_WindNoise.y, _WindNoise.z, randomTime) * pow(height, _WindNoise.w));

	windAngle = Noise(worldPos.xz * _WindNoiseST2.xy + _WindNoiseST2.zw + max(g_Windspeed, _WindNoise2.x) * _Time.y * _WindDirection.xy) * (UNITY_PI * 2.0f);
	windDistance += float3(cos(windAngle), 0.0f, sin(windAngle)) * (lerp(_WindNoise2.y, _WindNoise2.z, randomTime) * pow(height, _WindNoise2.w));

	float3 worldDistance = randomDistance + windDistance;
	height *= _Vertical.y;
	worldDistance.y += height;

	worldPos += worldDistance;
	for (int i = 0; i < g_GrassObstacleCount; ++i)
	{
		float4 obstacle = g_GrassObstacles[i];
		float3 obstacleDistance = worldPos - obstacle.xyz;
		//obstacleDistance.y = 0.0f;
		float obstacleLength = length(obstacleDistance), strength = (saturate(obstacle.w / obstacleLength - 1.0f)) * heightRate * _Strength;

		worldDistance += obstacleDistance * strength;
		//worldDistance += obstacleDistance * (saturate((1 - obstacleLength + obstacle.w) * heightRate * _Strength) / obstacleLength);
	}

	worldNormal = heightRate > 0.0f ? normalize(worldDistance) : float3(0.0f, 1.0f, 0.0f);

	worldDistance = worldNormal * height + mul((float3x3)unity_ObjectToWorld, distance);

	return worldDistance;
}

void GrassSurf(Input i, inout SurfaceOutputStandard o)
{
	fixed4 c;
#if ENABLE_TEXTURE_AND_COLOR
	// Albedo comes from a texture tinted by color
	c = tex2D(_MainTex, i.uv_MainTex) * i.color;
#elif ENABLE_TEXTURE
	c = tex2D(_MainTex, i.uv_MainTex);
#else 
	c = i.color;
#endif

#if CLIP_MIX || CLIP_GLOBAL
	CLIP(i.screenPos, _ClipParams, c.a);
#endif

	o.Albedo = c.rgb;
	// Metallic and smoothness come from slider variables
	o.Metallic = _Metallic;
	o.Smoothness = _Glossiness;
	o.Alpha = c.a;// saturate(_Base + 1.0 - max(0.0, dot(IN.viewDir, _Forward.xyz)));
	//o.Occlusion = 0.0f;
}

grass_v2f GrassVert(appdata_full v)
{
	grass_v2f o;
	UNITY_INITIALIZE_OUTPUT(grass_v2f, o);
	UNITY_SETUP_INSTANCE_ID(v);
	//UNITY_TRANSFER_INSTANCE_ID(v, o);
	//UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

	float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
	float3 worldDistance = GrassCalculateWorldDistance(v.color.a, worldPos, v.normal, o.worldNormal);
#ifdef UNITY_NO_SCREENSPACE_SHADOWS
	v.vertex.xyz += mul((float3x3)unity_WorldToObject, worldDistance);
#else
	float3 distance = float3(worldDistance.x, 0.0f, worldDistance.z);// , viewDir = normalize(WorldSpaceViewDir(float4(worldPos, 1.0f)));
	//distance -= viewDir * (dot(distance, viewDir) - _ShadowBias);
	v.vertex.xyz += mul((float3x3)unity_WorldToObject, distance);

	o.pos = UnityObjectToClipPos(v.vertex);
#endif

#if defined(UNITY_HALF_PRECISION_FRAGMENT_SHADER_REGISTERS) || !defined(IN_SURFACE_SHADER)
	UNITY_TRANSFER_LIGHTING(o, v.texcoord1.xy);
#endif

	o.worldPos = worldPos + worldDistance;
	o.pos = UnityWorldToClipPos(o.worldPos);

#if CLIP_MIX || CLIP_GLOBAL
	o.screenPos = ComputeScreenPos(o.pos);
#endif

#if ENABLE_TEXTURE_AND_COLOR || !ENABLE_TEXTURE
	o.color.a = _Color.a;
	o.color.rgb = v.color.rgb * _Color.rgb;
#endif

#if ENABLE_TEXTURE_AND_COLOR || ENABLE_TEXTURE
	o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
#endif

#ifdef FOG_COMBINED_WITH_TSPACE
	UNITY_TRANSFER_FOG_COMBINED_WITH_TSPACE(o, o.pos); // pass fog coordinates to pixel shader
#elif defined (FOG_COMBINED_WITH_WORLD_POS)
	UNITY_TRANSFER_FOG_COMBINED_WITH_WORLD_POS(o, o.pos); // pass fog coordinates to pixel shader
#else
	UNITY_TRANSFER_FOG(o, o.pos); // pass fog coordinates to pixel shader
#endif

	return o;
}

fixed4 GrassFrag(grass_v2f i) : SV_Target
{
	//UNITY_SETUP_INSTANCE_ID(i);
#ifdef FOG_COMBINED_WITH_TSPACE
	UNITY_EXTRACT_FOG_FROM_TSPACE(i);
#elif defined (FOG_COMBINED_WITH_WORLD_POS)
	UNITY_EXTRACT_FOG_FROM_WORLD_POS(i);
#else
	UNITY_EXTRACT_FOG(i);
#endif

	Input v;
#if ENABLE_TEXTURE_AND_COLOR || ENABLE_TEXTURE
	v.uv_MainTex = i.uv;
#endif

#if CLIP_MIX || CLIP_GLOBAL
	v.screenPos = i.screenPos;
#endif

#if ENABLE_TEXTURE_AND_COLOR || !ENABLE_TEXTURE
	v.color = i.color;
#endif

	SurfaceOutputStandard o;
	o.Albedo = 0.0;
	o.Emission = 0.0;
	o.Alpha = 0.0;
	o.Occlusion = 1.0;
	o.Normal = i.worldNormal;

	// call surface function
	GrassSurf(v, o);

#ifndef USING_DIRECTIONAL_LIGHT
	fixed3 lightDir = normalize(UnityWorldSpaceLightDir(i.worldPos));
#else
	fixed3 lightDir = _WorldSpaceLightPos0.xyz;
#endif

	float3 worldViewDir = normalize(UnityWorldSpaceViewDir(i.worldPos));

	// compute lighting & shadowing factor
	UNITY_LIGHT_ATTENUATION(atten, i, i.worldPos)
	fixed4 c = 0;

	// Setup lighting environment
	UnityGI gi;
	UNITY_INITIALIZE_OUTPUT(UnityGI, gi);
	gi.indirect.diffuse = 0;
	gi.indirect.specular = 0;
	gi.light.color = _LightColor0.rgb;
	gi.light.dir = lightDir;

	// Call GI (lightmaps/SH/reflections) lighting function
	UnityGIInput giInput;
	UNITY_INITIALIZE_OUTPUT(UnityGIInput, giInput);
	giInput.light = gi.light;
	giInput.worldPos = i.worldPos;
	giInput.worldViewDir = worldViewDir;
	giInput.atten = atten;

	giInput.lightmapUV = 0.0;

	giInput.ambient.rgb = 0.0;

	giInput.probeHDR[0] = unity_SpecCube0_HDR;
	giInput.probeHDR[1] = unity_SpecCube1_HDR;
#if defined(UNITY_SPECCUBE_BLENDING) || defined(UNITY_SPECCUBE_BOX_PROJECTION)
	giInput.boxMin[0] = unity_SpecCube0_BoxMin; // .w holds lerp value for blending
#endif

#ifdef UNITY_SPECCUBE_BOX_PROJECTION
	giInput.boxMax[0] = unity_SpecCube0_BoxMax;
	giInput.probePosition[0] = unity_SpecCube0_ProbePosition;
	giInput.boxMax[1] = unity_SpecCube1_BoxMax;
	giInput.boxMin[1] = unity_SpecCube1_BoxMin;
	giInput.probePosition[1] = unity_SpecCube1_ProbePosition;
#endif
	LightingStandard_GI(o, giInput, gi);

	// realtime lighting: call lighting function
	c += LightingStandard(o, worldViewDir, gi);

	// apply fog
	UNITY_APPLY_FOG(i.fogCoord, c);
	UNITY_OPAQUE_ALPHA(c.a);
	return c;
}

#endif