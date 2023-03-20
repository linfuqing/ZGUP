Shader "ZG/GrassWithoutShadow"
{
    Properties
    { 
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

    SubShader
    {
        Tags { "RenderType"="Opaque" "DisableBatching" = "True" }
        LOD 100

        Pass
        {
			Tags{"LightMode" = "ForwardBase"}

            CGPROGRAM
            #pragma vertex GrassVert
            #pragma fragment GrassFrag

            #pragma multi_compile_fwdbase
			#pragma multi_compile_fog
			#pragma multi_compile_instancing
			#pragma shader_feature __ ENABLE_TEXTURE_AND_COLOR//ENABLE_TEXTURE_AND_COLOR ENABLE_TEXTURE
			#pragma shader_feature __ CLIP_MIX CLIP_GLOBAL

			#include "GrassUtility.cginc"

            ENDCG
        }

		Pass
		{
			Tags{ "LightMode" = "ForwardAdd" "DisableBatching" = "True" }

			Blend One One

			CGPROGRAM
			#pragma vertex GrassVert
			#pragma fragment GrassFrag

			#pragma multi_compile_fwdadd_fullshadows
			#pragma multi_compile_fog
			#pragma multi_compile_instancing
			#pragma shader_feature __ CLIP_MIX CLIP_GLOBAL

			#include "GrassUtility.cginc"

			ENDCG
		}
    }
}
