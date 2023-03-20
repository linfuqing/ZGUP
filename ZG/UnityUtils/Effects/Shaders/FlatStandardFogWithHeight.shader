// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "ZG/FlatStandardFogWithHeight"
{
	Properties
	{
		[Toggle(ENABLE_FLAT)] _EnableFlat("Enable Flat", Float) = 1
		//[Toggle(ENABLE_TEX_AND_COLOR)] _EnableColor("Enable Texture And Color", Float) = 0
		[Toggle(ENABLE_TEX)] _EnableAlbedo("Enable Tex", Float) = 1
		//[Toggle(ENABLE_COLOR)] _EnableColor("Enable Color", Float) = 0
		[Toggle(ENABLE_EMISSION)] _EnableEmission("Enable Emission", Float) = 0

		_MainTex("Albedo (RGB)", 2D) = "white" {}
		[HDR]_Color("Color", Color) = (0, 0, 0, 0)
		[HDR]_Emission("Emission", Color) = (0, 0, 0, 0)
		_Glossiness("Smoothness", Range(0,1)) = 0.5
		_Metallic("Metallic", Range(0,1)) = 0.0
		_Factor("Factor = MaxHeight / (MaxHeight - MinHeight)", float) = 2
		_Height("Height = (MaxHeight - MinHeight)", float) = 15
		_Offset("Offset", float) = 0
		_FogEnd("Fog End", float) = 600
		_FogStart("Fog Start", float) = 500
		_AlphaStart("Alpha Start", float) = 200
		_MaxDistance("Max Distance", float) = 78
	}

		SubShader
		{
			Tags{ "RenderType" = "Opaque" /*"Queue" = "Transparent-1" */"DisableBatching" = "True" }
			LOD 200

			CGPROGRAM
			// Physically based Standard lighting model, and enable shadows on all light types
			#pragma surface surf Standard keepalpha vertex:vert finalcolor:col finalgbuffer:col_deferred fullforwardshadows

			#pragma multi_compile_fog

			// Use shader model 3.0 target, to get nicer looking lighting
			//#pragma target 3.0
			#pragma shader_feature __ ENABLE_FLAT
			#pragma shader_feature __ ENABLE_TEX//ENABLE_TEX_AND_COLOR ENABLE_TEXTURE
			#pragma shader_feature __ ENABLE_EMISSION

			struct Input {
				float fog;
				float alpha;

	#if ENABLE_FLAT
				float ambient;
	#endif

	#if ENABLE_TEX
				float2 uv_MainTex;
	#endif
			};

			sampler2D _MainTex;

			float g_FogFactor;

	#if ENABLE_TEX_AND_COLOR || !ENABLE_TEX
			fixed4 _Color;
	#endif

	#if ENABLE_EMISSION
			fixed4 _Emission;
	#endif

			half _Glossiness;

			half _Metallic;
			half _Factor;
			half _Height;
			half _Offset;
			half _FogEnd;
			half _FogStart;
			half _AlphaStart;
			half _MaxDistance;

			// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
			// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
			// #pragma instancing_options assumeuniformscaling
			UNITY_INSTANCING_BUFFER_START(Props)
			UNITY_INSTANCING_BUFFER_END(Props)

			void col(Input IN, SurfaceOutputStandard o, inout fixed4 color)
			{
#ifdef UNITY_PASS_FORWARDADD
				float4 fogColor = float4(0, 0, 0, 0);
				UNITY_FOG_LERP_COLOR(color, fogColor, IN.fog);
#else
				UNITY_FOG_LERP_COLOR(color, unity_FogColor, IN.fog);
#endif

				color.a = IN.alpha;

				/*fixed4 fogColor;
#ifdef UNITY_PASS_FORWARDADD
				fogColor = 0;
#else
				fogColor = unity_FogColor;
#endif

				color = fixed4(lerp(fogColor, color, IN.fog).xyz, IN.alpha);*/
			}

			void col_deferred(Input IN, SurfaceOutputStandard o, inout half4 diffuse, inout half4 specSmoothness, inout half4 normal, inout half4 emission)
			{
				diffuse.a = IN.fog;
				normal.a = 1.0f;
			}

			void vert(inout appdata_full v, out Input o)
			{
				UNITY_INITIALIZE_OUTPUT(Input, o);
				float4 vertex = mul(unity_ObjectToWorld, v.vertex);
				float3 viewDir = UnityWorldSpaceViewDir(vertex);
				float rdistance = dot(viewDir, viewDir);
				rdistance = rsqrt(rdistance);

	#if ENABLE_FLAT
				o.ambient = max(dot(mul((float3x3)unity_ObjectToWorld, v.normal), viewDir) * rdistance, 0.0f);
	#if ENABLE_EMISSION
				o.ambient = 1.0f - o.ambient;
	#else
				o.ambient = o.ambient * 0.5f + 0.5f;
	#endif
	#endif

				float distance = 1.0f / rdistance;
				UNITY_CALC_FOG_FACTOR_RAW(distance - _ProjectionParams.y);
				//float fog = unityFogFactor;// saturate(unity_FogParams.w + distance * unity_FogParams.z);

				float height = vertex.y - mul(unity_ObjectToWorld, float4(0.0f, 0.0f, 0.0f, 1.0f)).y + _Offset;
				height = saturate(_Factor - height / _Height);

				o.fog = lerp(max(g_FogFactor, unityFogFactor), unityFogFactor, height);
				o.fog = lerp(unityFogFactor, o.fog, saturate((_FogEnd - distance) / (_FogEnd - _FogStart)));

				/*o.fog = lerp(1.0f, fog, height);

				o.fog = lerp(fog, o.fog, saturate((_FogEnd - distance) / (_FogEnd - _FogStart)));
				o.fog = lerp(o.fog, fog, g_FogFactor);*/

	#ifdef UNITY_PASS_DEFERRED
				o.fog = 1.0f - o.fog;
	#endif
				o.alpha = smoothstep(_FogEnd, _AlphaStart, distance);// lerp(o.fog, 1.0f, saturate((_FogStart - distance) / (_FogStart - _AlphaStart)));

				/*viewDir *= min(distance, lerp(distance, _MaxDistance, sign(o.alpha))) * rdistance;
				vertex.xyz = _WorldSpaceCameraPos - viewDir;
				v.vertex = mul(unity_WorldToObject, vertex);*/
			}

			void surf(Input IN, inout SurfaceOutputStandard o)
			{
				fixed4 c;
	#if ENABLE_TEX_AND_COLOR
				// Albedo comes from a texture tinted by color
				c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
	#elif ENABLE_TEX
				c = tex2D(_MainTex, IN.uv_MainTex);
	#else
				c = _Color;
	#endif

	#if ENABLE_EMISSION
	#if ENABLE_FLAT
				o.Emission = _Emission * IN.ambient;

				c.rgb = c.rgb * (1.5f - IN.ambient);
	#else
				o.Emission = _Emission;
	#endif
	#elif ENABLE_FLAT
				c.rgb = c.rgb * IN.ambient;
	#endif
				o.Albedo = c.rgb;

				// Metallic and smoothness come from slider variables
				o.Metallic = _Metallic;
				o.Smoothness = _Glossiness;
				o.Alpha = IN.alpha;// IN.fog;
			}
			ENDCG
		}
			FallBack "Diffuse"
}
