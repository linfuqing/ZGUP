// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'

Shader "ZG/FlatTree" 
{
	Properties{
		[Clip] _ClipParams("Clip", Vector) = (0.2, 0.2, 12.0, 0)

		_Windspeed("Windspeed", float) = 0.5
		_ShakeBending("Shake Bending", float) = 3.0
		_ShakePower("Shake Power", float) = 2.0

		_MinHeight("Min Height", float) = 0.0
		_MaxHeight("Max Height", float) = 1.0

		_Glossiness("Smoothness", Range(0,1)) = 0.5
		_Metallic("Metallic", Range(0,1)) = 0.0

		_WaveXSize("Wave X Size", Vector) = (0.048, 0.06, 0.24, 0.096)
		_WaveZSize("Wave Z Size", Vector) = (0.024, .08, 0.08, 0.2)
		_WaveSpeed("Wave Speed", Vector) = (1.2, 2, 1.6, 4.8)

		_WaveXMove("Wave X Move", Vector) = (0.024, 0.04, -0.12, 0.096)
		_WaveZMove("Wave Z Move", Vector) = (0.006, .02, -0.02, 0.1)

		_MainTex("Base (RGB) Trans (A)", 2D) = "white" {}
	}

	SubShader{
		Tags{ "RenderType" = "Opaque" "DisableBatching" = "True" }
		LOD 200

		CGPROGRAM
		#pragma surface surf Standard vertex:vert addshadow
		#pragma shader_feature __ CLIP_MIX CLIP_GLOBAL

#if CLIP_MIX || CLIP_GLOBAL
		#include "Clip.cginc"

		CLIP_PARAMS(_ClipParams)
#endif

		uniform float g_Windspeed;

		float _Windspeed;
		float _ShakeBending;
		float _ShakePower;

		float _MinHeight;
		float _MaxHeight;

		float _ClipDistance;
		float _ClipNear;
		float _ClipFar;

		half _Glossiness;
		half _Metallic;

		float4 _WaveXSize;
		float4 _WaveZSize;
		float4 _WaveSpeed;
		float4 _WaveXMove;
		float4 _WaveZMove;

		sampler2D _MainTex;

		struct Input 
		{
			float3 texcoord;

#if CLIP_MIX || CLIP_GLOBAL
			float4 screenPos;
#endif
			//float ambient;
			//float2 uv_MainTex;
		};

		/*void FastSinCos(float4 val, out float4 s, out float4 c) {
			val = val * 6.408849 - 3.1415927;
			float4 r5 = val * val;
			float4 r6 = r5 * r5;
			float4 r7 = r6 * r5;
			float4 r8 = r6 * r5;
			float4 r1 = r5 * val;
			float4 r2 = r1 * r5;
			float4 r3 = r2 * r5;
			float4 sin7 = { 1, -0.16161616, 0.0083333, -0.00019841 };
			float4 cos8 = { -0.5, 0.041666666, -0.0013888889, 0.000024801587 };
			s = val + r1 * sin7.y + r2 * sin7.z + r3 * sin7.w;
			c = 1 + r5 * cos8.x + r6 * cos8.y + r7 * cos8.z + r8 * cos8.w;
		}*/

		float4 FastSin(float4 val) {
			val = val * 6.408849 - 3.1415927;
			float4 r5 = val * val;
			float4 r6 = r5 * r5;
			float4 r7 = r6 * r5;
			float4 r8 = r6 * r5;
			float4 r1 = r5 * val;
			float4 r2 = r1 * r5;
			float4 r3 = r2 * r5;
			float4 sin7 = { 1, -0.16161616, 0.0083333, -0.00019841 };
			float4 cos8 = { -0.5, 0.041666666, -0.0013888889, 0.000024801587 };
			return val + r1 * sin7.y + r2 * sin7.z + r3 * sin7.w;
			//c = 1 + r5 * cos8.x + r6 * cos8.y + r7 * cos8.z + r8 * cos8.w;
		}

		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_BUFFER_START(Props)
		// put more per-instance properties here
		UNITY_INSTANCING_BUFFER_END(Props)

		void vert(inout appdata_full v, out Input o)
		{
			UNITY_INITIALIZE_OUTPUT(Input, o);

			//const float4 _waveXSize = float4(0.048, 0.06, 0.24, 0.096);
			//const float4 _waveZSize = float4 (0.024, .08, 0.08, 0.2);
			//const float4 waveSpeed = float4 (1.2, 2, 1.6, 4.8);

			//float4 _waveXmove = float4(0.024, 0.04, -0.12, 0.096);
			//float4 _waveZmove = float4 (0.006, .02, -0.02, 0.1);

			float4 waves;
			waves = v.vertex.x * _WaveXSize;
			waves += v.vertex.z * _WaveZSize;

			waves -= _Time.x * _WaveSpeed * max(g_Windspeed, _Windspeed);

			//float4 s , c;
			waves = frac(waves);
			float4 s = FastSin(waves);

			float height = (v.vertex.y - _MinHeight) / (_MaxHeight - _MinHeight);
			float waveAmount = saturate(height) * _ShakeBending;
			s *= waveAmount;

			s *= normalize(_WaveSpeed);

			//float fade = dot(s, 1.3);
			s = sign(s) * pow(abs(s), _ShakePower);
			float3 waveMove = float3 (0,0,0);
			waveMove.x = dot(s, _WaveXMove);
			waveMove.z = dot(s, _WaveZMove);
			v.vertex.xyz -= mul((float3x3)unity_WorldToObject, waveMove).xyz;

			o.texcoord.xy = v.texcoord.xy;
			o.texcoord.z = max(dot(v.normal, normalize(ObjSpaceViewDir(v.vertex))), 0.0f) * 0.5f + 0.5f;
		}

		void surf(Input IN, inout SurfaceOutputStandard o)
		{
			// Albedo comes from a texture tinted by color
			fixed4 c = tex2D(_MainTex, IN.texcoord.xy);

#if CLIP_MIX || CLIP_GLOBAL
			CLIP(IN.screenPos, _ClipParams, c.a);
#endif

			o.Albedo = c.rgb * IN.texcoord.z;
			// Metallic and smoothness come from slider variables
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = c.a;// saturate(_Base + 1.0 - max(0.0, dot(IN.viewDir, _Forward.xyz)));
		}
		ENDCG
	}

	FallBack "Diffuse"
}