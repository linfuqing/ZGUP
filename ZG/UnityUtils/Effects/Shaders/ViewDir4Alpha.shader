// Upgrade NOTE: upgraded instancing buffer 'Props' to new syntax.

Shader "ZG/ViewDir4Alpha" 
{
	Properties 
	{
		_Glossiness("Smoothness", Range(0,1)) = 0.5
		_Metallic("Metallic", Range(0,1)) = 0.0
		_Base("Base", Range(0,1)) = 0.0
		_Target("Target", Vector) = (0.0, 0.0, 0.0, 1.0)
		[HDR]_Color("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
	}

	SubShader 
	{
		//Tags{ "RenderType" = "Transparent" "Queue" = "Transparent" }
		Tags { "RenderType" = "Opaque" }
		LOD 200
		
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows vertex:vert// alpha:blend

		#pragma shader_feature __ ENABLE_TEXTURE

		#include "Clip.cginc"
		// Use shader model 3.0 target, to get nicer looking lighting
		//#pragma target 3.0

		half _Glossiness;
		half _Metallic;
		half _Base;
		float4 _Target;

#if ENABLE_TEXTURE_AND_COLOR || !ENABLE_TEXTURE
		fixed4 _Color;
#endif

#if ENABLE_TEXTURE || ENABLE_TEXTURE_AND_COLOR
		sampler2D _MainTex;
#endif

		struct Input
		{
			float4 texcoord;
			//float4 uv_MainTex;
			//float2 alpha;
			float4 screenPos;
		};

		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_BUFFER_START(Props)
			// put more per-instance properties here
		UNITY_INSTANCING_BUFFER_END(Props)

		void vert(inout appdata_full v, out Input o) 
		{
			UNITY_INITIALIZE_OUTPUT(Input, o);
			o.texcoord.xy = v.texcoord.xy;
			o.texcoord.z = max(dot(v.normal, normalize(ObjSpaceViewDir(v.vertex))), 0.0f) + 0.5f;
			o.texcoord.w = saturate(_Base + 1.0 - max(0.0, dot(normalize(_Target - mul(unity_ObjectToWorld, v.vertex)), normalize(_Target - _WorldSpaceCameraPos))));
		}

		void surf (Input IN, inout SurfaceOutputStandard o) 
		{
			fixed4 c;
#if ENABLE_TEXTURE_AND_COLOR
			// Albedo comes from a texture tinted by color
			c = tex2D(_MainTex, IN.texcoord.xy) * _Color;
#elif ENABLE_TEXTURE
			c = tex2D(_MainTex, IN.texcoord.xy);
#else 
			c = _Color;
#endif

			c.a *= IN.texcoord.w;

			AlphaClip(IN.screenPos, c.a);

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