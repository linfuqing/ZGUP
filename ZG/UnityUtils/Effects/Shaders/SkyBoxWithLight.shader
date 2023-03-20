// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'

Shader "ZG/SkyBoxWithLight" 
{
	Properties {
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
	}
	SubShader {
		Tags { "Queue" = "Background+1" "RenderType" = "Transparent" "ForceNoShadowCasting" = "True" "DisableBatching" = "True" }

		//LOD 200

		ZWrite Off

		//Blend SrcAlpha OneMinusSrcAlpha

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard keepalpha noshadow nolightmap noambient nodirlightmap nofog nolppv noshadowmask

		// Use shader model 3.0 target, to get nicer looking lighting
		//#pragma target 3.0
		struct Input 
		{
			float4 color : COLOR;
		};

		half _Glossiness;
		half _Metallic;

		/*void vert(inout appdata_full v, out Input o) {
			UNITY_INITIALIZE_OUTPUT(Input, o);

#ifdef USING_DIRECTIONAL_LIGHT
			float3 worldPos = mul(unity_ObjectToWorld, v.vertex);
			//viewPos.z = _ProjectionParams.z - _ProjectionParams.y;
			worldPos = normalize(worldPos - _WorldSpaceCameraPos) * _ProjectionParams.z  + _WorldSpaceCameraPos;
			v.vertex = mul(unity_WorldToObject, float4(worldPos, 1.0f));
#endif
		}*/

		void surf (Input IN, inout SurfaceOutputStandard o) {
			// Albedo comes from a texture tinted by color
			o.Albedo = 1.0;
			// Metallic and smoothness come from slider variables
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Emission = unity_FogColor.rgb;
			o.Alpha = unity_FogColor.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
