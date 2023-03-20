// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'

Shader "ZG/FlatGrass"
{
	Properties{
		[Texture(ENABLE_TEXTURE_AND_COLOR)]_MainTex("Base (RGB) Trans (A)", 2D) = "white" {}
		//[Toggle(ENABLE_TEXTURE_AND_COLOR)] _EnableColor("Enable Texture And Color", Float) = 0
		[Clip] _ClipParams("Clip", Vector) = (0.2, 0.2, 12.0, 0)
		//[Toggle(ENABLE_COLOR)] _EnableColor("Enable Color", Float) = 0

		_Glossiness("Smoothness", Range(0,1)) = 0.5
		_Metallic("Metallic", Range(0,1)) = 0.0

		_Strength("Strength", float) = 0.5

		_MinHeight("Min Height", Range(0,1)) = 0.3

		_Vertical("Vertical Scale(x), Length(y), Far start(z) And Far end(w)", Vector) = (10, 1, 50, 60)
		_Horizontal("Horizontal Rate(x), Scale(y), Bending(z) And Power(w)", Vector) = (0.5, 0.02, 0.5, 2.5)

		_WindDirection("Wind Diretion(xy), Wind Speed Scale(z)  Random Scale(w)", Vector) = (1, 0, 3, 50)
		_WindDirectionParams("Wind Direction Speed(x), Min Strength(y), Max Strength(z) And Power(w)", Vector) = (0.3, 1.5, 2, 1.5)

		_WindNoise("Wind Noise Speed(x), Min Strength(y), Max Strength(z) And Power(w)", Vector) = (0.5, 0.3, 0.5, 1.5)
		_WindNoiseST("Wind Noise ST", Vector) = (1, 1, 634, 3634)

		_WindNoise2("Wind Noise 2th Speed(x), Min Strength(y), Max Strength(z) And Power(w)", Vector) = (1, 0.3, 0.5, 1.5)
		_WindNoiseST2("Wind Noise 2th ST", Vector) = (0.1, 0.5, 634, 3634)

		_HeightST("Height Noise ST", Vector) = (1, 1, 2342, 23623)

		[HDR]_Color("Color", Color) = (1,1,1,1)
	}

	SubShader{
		Tags{ "RenderType" = "Opaque" "DisableBatching" = "True" }
		LOD 200

		CGPROGRAM
		#pragma surface GrassSurf Standard vertex:Vert nolightmap nodynlightmap nodirlightmap nometa nolppv addshadow
		#pragma shader_feature __ ENABLE_TEXTURE_AND_COLOR//ENABLE_TEXTURE_AND_COLOR ENABLE_TEXTURE
		#pragma shader_feature __ CLIP_MIX CLIP_GLOBAL

		#define IN_SURFACE_SHADER

		#include "GrassUtility.cginc"

		void Vert(inout appdata_full v)
		{
			//UNITY_INITIALIZE_OUTPUT(v, o);

			float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz, worldNormal;
			float3 worldDistance = GrassCalculateWorldDistance(v.color.a, worldPos, v.normal, worldNormal);

			v.vertex.xyz += mul((float3x3)unity_WorldToObject, worldDistance);

			v.normal = UnityWorldToObjectDir(worldNormal);

#if ENABLE_TEXTURE_AND_COLOR || !ENABLE_TEXTURE
			v.color.a = _Color.a;
			v.color.rgb *= _Color.rgb;
#endif
		}

		ENDCG
	}

	FallBack "Diffuse"
}