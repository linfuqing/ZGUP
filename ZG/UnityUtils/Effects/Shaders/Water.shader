// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "ZG/Water"
{
    Properties
    {
		_Color("Color", Color) = (0,0.8235294,0.7894523,1)
		_ColorDepth("Color Depth", Color) = (0,0.5656692,0.8455882,1)
		//_ColorFar("Color Far", Color) = (0.02205884,0.1502028,1,1)
		
		_Metal("Metal", Range(0,1)) = 0.5
		_Smoothness("Smoothness", Range(0,1)) = 0.95
		_RefractionStrength("Refraction Strength", float) = 1.0

		_WaterClarity("Water Clarity",Range(0.1, 1)) = 0.1
		_WaterClarityAttenuation("Water Clarity Attenuation",Range(0.1,3)) = 1.0

		_DepthFogDensity("Fog Density", Range(0.0, 1)) = 0.1

		_FresnelAttenuation("Fresnel Attenuation", float) = 1.55
		_FresnelFactor("Fresnel Factor", Range(0,1)) = 0.02

		[Header(Toon)]
		[Toggle] _Toon("Enable", Float) = 1
		_FresnelToon("Fresnel", Vector) = (1.71, 0.2, 2, 1)
		_DiffuseToon("Diffuse", Vector) = (1.71, 0.2, 2, 1)
		_SpecularToon("Specular", Vector) = (3.33, 0.0, 5, 4)

		[Header(Caustics)]
		// Approximate rays being focused/defocused on underwater surfaces
		[KeywordEnum(None, Normal, Colour)] _Caustics("Type", Float) = 1
		// Scaling / intensity
		_CausticsStrength("Strength", Range(0.0, 10.0)) = 1.6
		// The depth at which the caustics are in focus
		_CausticsFocalDepth("Focal Depth", Range(0.0, 25.0)) = 2.0
		// The range of depths over which the caustics are in focus
		_CausticsInvDepthOfField("Inv Depth Of Field", Range(0.1, 100.0)) = 3
		// How much the caustics texture is distorted
		_CausticsDistortionStrength("Distortion Strength", Range(0.0, 0.25)) = 0.16
		// The scale of the distortion pattern used to distort the caustics
		_CausticsDistortionScale("Distortion Scale", Range(0.02, 100.0)) = 0.04

		_CausticsST1("ST1", Vector) = (0.2, 1.3, 0.044, 0.069)
		_CausticsST2("ST2", Vector) = (-0.274, 1.77, 0.048, 0.017)

		//_CausticsVisuals("Visuals", Vector) = (0.61, 0.00351, 0.1091, 2.33)
		_CausticsTex("Tex", 2D) = "white" {}

		[Header(Foam)]
		[Toggle] _Foam("Enable", Float) = 1
		_FoamNoise("Noise", 2D) = "black" { }
		_FoamMaxDistance("Max Distance", float) = 0.4
		_FoamMinDistance("Min Distance", float) = 0.04
		[PowerSlider(2)] _FoamPower("Range Power", Range(0.01, 10)) = 0.75
		_FoamNoiseSpeed("Noise Speed", Range(0.01, 10)) = 1.0
		_FoamColor("Color", Color) = (0.5, 0.5, 0.5, 1)

		[Header(Bump)]
		_BumpScale("Scale", Vector) = (0.5, 0.5, 0.5, 1.0)
		_BumpSpeed("Speed", Vector) = (0.05, 0.005, 0.01, 1.0)
		_BumpMap("Tex", 2D) = "white" {}
    }

    SubShader
    {
		GrabPass{ "_GrabTexture" }

        Tags { "RenderType" = "Opaque" "Queue" = "Transparent-1" }
		//ZWrite Off
		//ZTest LEqual
		//Blend Off

        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Custom vertex:vert noshadow
		#pragma shader_feature _TOON_ON
		#pragma shader_feature _FOAM_ON
		#pragma multi_compile _CAUSTICS_NONE _CAUSTICS_NORMAL _CAUSTICS_COLOUR

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

		#include "UnityPBSLighting.cginc"
		//#include "Lighting.cginc"
#define CAUSTICS_ON (_CAUSTICS_NORMAL | _CAUSTICS_COLOUR)

        struct Input
        {
			float4 grabScreenPos;
            float3 worldPos;
			float3 worldNormal;
			float3 viewDir;
			INTERNAL_DATA
        };
		
		struct SurfaceOutputCustom
		{
			fixed3 Albedo;
			fixed3 Normal;
			fixed3 Emission;

			fixed3 Fraction;

			float SceneDepth;

#if CAUSTICS_ON
			float Height;
			fixed Fog;
#endif

			fixed Alpha;
		};

		fixed4 _Color;
		fixed4 _ColorDepth;

		half _Metal;
		half _Smoothness;

		float _RefractionStrength;

		float _WaterClarity;
		half _WaterClarityAttenuation;

		half _DepthFogDensity;

		half _FresnelAttenuation;
		half _FresnelFactor;

#if _TOON_ON
		float4 _FresnelToon;
		float4 _DiffuseToon;
		float4 _SpecularToon;
#endif

#if CAUSTICS_ON
		half _CausticsStrength;
		half _CausticsFocalDepth;
		half _CausticsInvDepthOfField;

#if _CAUSTICS_NORMAL
		half _CausticsDistortionScale;
		half _CausticsDistortionStrength;
#endif

		float4 _CausticsST1;
		float4 _CausticsST2;

		sampler2D _CausticsTex;
#endif


#if _FOAM_ON
		half _FoamMaxDistance;
		half _FoamMinDistance;
		fixed _FoamPower;
		half _FoamNoiseSpeed;
		fixed4 _FoamColor;
		sampler2D _FoamNoise;
#endif

		float3 _BumpScale;
		float3 _BumpSpeed;
		sampler2D _BumpMap;

		sampler2D _GrabTexture; 
		float4 _GrabTexture_TexelSize;

		sampler2D _CameraDepthTexture;
		float4 _CameraDepthTexture_TexelSize;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

#if _TOON_ON
		half Toon(half facing, half4 params)
		{
			return saturate(params.x * (params.y + floor(facing * params.z) / params.w));
		}
#endif

		float3 ReconstructionWorldPos(float3 viewDir, float sceneDepth)
		{
			float3 camForward = mul((float3x3)unity_CameraToWorld, float3(0.0, 0.0, 1.));
			return _WorldSpaceCameraPos - viewDir * sceneDepth / -dot(camForward, viewDir);
		}

		half FastFresnel(float r, float facing, float shininess)
		{
			return r + (1.0 - r) * pow(1.0 - facing, shininess);
		}

		half Highlights(half perceptualRoughness, half3 lightDir, half3 normal, half3 viewDir, out half roughness)
		{
			roughness = PerceptualRoughnessToRoughness(perceptualRoughness);

			float3 halfDir = normalize(lightDir + viewDir);
			float NoH = saturate(dot(normal, halfDir));
			float LoH = saturate(dot(lightDir, halfDir));
			// GGX Distribution multiplied by combined approximation of Visibility and Fresnel
			// See "Optimizing PBR for Mobile" from Siggraph 2015 moving mobile graphics course
			// https://community.arm.com/events/1155
			float d = NoH * NoH * (roughness - 1.h) + 1.0001h;
			float LoH2 = LoH * LoH;
			float specularTerm = roughness / ((d * d) * max(0.1h, LoH2) * (perceptualRoughness + 0.5h) * 4);
			// on mobiles (where float actually means something) denominator have risk of overflow
			// clamp below was added specifically to "fix" that, but dx compiler (we convert bytecode to metal/gles)
			// sees that specularTerm have only non-negative terms, so it skips max(0,..) in clamp (leaving only min(100,...))
#if defined(SHADER_API_MOBILE)
			specularTerm = specularTerm - HALF_MIN;
			specularTerm = clamp(specularTerm, 0.0, 5.0); // Prevent FP16 overflow on mobiles
#endif
			return specularTerm;
		}

		float3 GetNormal(float time, float2 uv, float3 scale, float3 speed)
		{
			float2 uv1 = uv * scale.x + time * speed.x * float2(1, 1);
			float3 normal = UnpackNormal(tex2D(_BumpMap, uv1));
			float2 uv2 = uv * scale.y + time * speed.y * float2(1, -1);
			normal += UnpackNormal(tex2D(_BumpMap, uv2));
			float2 uv3 = uv * scale.z + time * speed.z * float2(-1, 1);
			normal += UnpackNormal(tex2D(_BumpMap, uv3));

			return normalize(normal);
		}

		float2 AlignWithGrabTexel(float2 uv)
		{
			return (floor(uv * _CameraDepthTexture_TexelSize.zw) + 0.5) * abs(_CameraDepthTexture_TexelSize.xy);
		}

		float4 CalculateRefractiveColor(float4 grabPos, float3 worldNormal, out float sceneDepth/*, out float viewWaterDepth*/)
		{
			//USING DEPTH TEXTURE(.W) BUT NOT ACTUAL RAYLENGTH IN WATER, NEED TO FIX
			float sceneDepthNoDistortion = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, grabPos));

			/*float surfaceDepth = grabPos.w;
			float viewWaterDepthNoDistortion = sceneDepth -surfaceDepth;*/

			float4 distortedUV = grabPos;
			float2 uvOffset = worldNormal.xz * _RefractionStrength;

			//Distortion near water surface should be attenuated
			uvOffset *= saturate(sceneDepthNoDistortion);

			distortedUV.xy = AlignWithGrabTexel(distortedUV.xy + uvOffset);

			//Resample depth to avoid false distortion above water
			sceneDepth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, distortedUV));

			/*surfaceDepth = grabPos.w;
			viewWaterDepth = sceneDepth -surfaceDepth;*/

			float tmp = step(0, sceneDepth);
			distortedUV.xy = lerp(AlignWithGrabTexel(grabPos.xy), distortedUV.xy, tmp);
			sceneDepth = lerp(sceneDepthNoDistortion, sceneDepth, tmp);

			return tex2Dproj(_GrabTexture, distortedUV);
		}

#if CAUSTICS_ON
		float2 Panner(float2 uv, float2 offset, float tiling)
		{
			return  _Time.y * offset + uv * tiling;
		}

#if _CAUSTICS_NORMAL
		half3 TexCaustics(float2 uv, float mipLod)
		{
			half2 normal = _CausticsDistortionStrength * UnpackNormal(tex2D(_BumpMap, uv * _CausticsDistortionScale)).xz;

			float4 uv1 = float4((normal * _CausticsST1.y + Panner(uv, _CausticsST1.zw, _CausticsST1.x)), 0.0f, mipLod);
			float4 uv2 = float4((normal * _CausticsST2.y + Panner(uv, _CausticsST2.zw, _CausticsST2.x)), 0.0f, mipLod);

			return tex2Dlod(_CausticsTex, uv1).xyz + tex2Dlod(_CausticsTex, uv2).xyz;
		}
#endif

#if _CAUSTICS_COLOUR
		fixed3 RGBSplit(float split, float2 uv, float mipLod)
		{
			float4 uvr = float4(uv + float2(split, split), 0, mipLod);
			float4 uvg = float4(uv + float2(split, -split), 0, mipLod);
			float4 uvb = float4(uv + float2(-split, -split), 0, mipLod);

			fixed r = tex2Dlod(_CausticsTex, uvr).r;
			fixed g = tex2Dlod(_CausticsTex, uvg).g;
			fixed b = tex2Dlod(_CausticsTex, uvb).b;

			return float3(r, g, b);
		}

		half3 TexCaustics(float2 uv, float mipLod)
		{
			float2 uv1 = Panner(uv, _CausticsST1.zw, _CausticsST1.x);
			float2 uv2 = Panner(uv, _CausticsST2.zw, _CausticsST2.x);

			fixed3 texture1 = RGBSplit(_CausticsST1.y, uv1, mipLod);
			fixed3 texture2 = RGBSplit(_CausticsST2.y, uv2, mipLod);

			fixed3 textureCombined = min(texture1, texture2);

			return textureCombined;
		}
#endif
		half3 ApplyCaustics(half3 viewDir, /*half3 viewDir, */half3 lightDir, float fog, float waterHeight, float sceneDepth/*, float viewWaterDepth*/)
		{
			// could sample from the screen space shadow texture to attenuate this..
			// underwater caustics - dedicated to P
			float3 scenePos = ReconstructionWorldPos(viewDir, sceneDepth);
			scenePos.y = waterHeight - scenePos.y;

			// Compute mip index manually, with bias based on sea floor depth. We compute it manually because if it is computed automatically it produces ugly patches
			// where samples are stretched/dilated. The bias is to give a focusing effect to caustics - they are sharpest at a particular depth. This doesn't work amazingly
			// well and could be replaced.
			float mipLod = log2(max(sceneDepth, 1.0)) + abs(scenePos.y/*viewWaterDepth*/ - _CausticsFocalDepth) * _CausticsInvDepthOfField;
			// project along light dir, but multiply by a fudge factor reduce the angle bit - compensates for fact that in real life
			// caustics come from many directions and don't exhibit such a strong directonality
			float2 surfacePosXZ = scenePos.xz + lightDir.xz * scenePos.y;// / (4.0 * lightDir.y);

			// Scale caustics strength by primary light, depth fog density and scene depth.
			half3 strength = lerp(_CausticsStrength * _LightColor0, 0.0, fog);

			//return causticsStrength * tex2Dlod(_MainTex, uv).xyz;
			return strength * TexCaustics(surfacePosXZ, mipLod);
		}
#endif

#if _FOAM_ON
		float3 ApplyFoam(float facing, float viewWaterDepth, float2 uv, float3 color)
		{
			float distance = lerp(_FoamMaxDistance, _FoamMinDistance, facing);
			float range = pow(saturate(distance / viewWaterDepth), _FoamPower);
			float noise = tex2D(_FoamNoise, uv + _Time.x * _FoamNoiseSpeed).r;
			//foamNoise = pow(foamNoise, _FoamNoisePower);
			return lerp(color, _FoamColor.rgb, step(noise, range) * _FoamColor.a);
		}
#endif

		half4 LightingCustom(SurfaceOutputCustom s, half3 viewDir, UnityGI gi)
		{
			/*SurfaceOutputStandard o;
			o.Albedo = s.Albedo;
			o.Normal = s.Normal;
			o.Emission = s.Emission;
			o.Metallic = 0.0f;
			o.Smoothness = _Smoothness;
			o.Occlusion = 1.0f;
			o.Alpha = 1.0f;

			return LightingStandard(o, viewDir, gi);*/
			half diffuseTerm = max(0.0, dot(gi.light.dir, s.Normal));
#if _TOON_ON
			diffuseTerm = Toon(diffuseTerm, _DiffuseToon);
#endif

			half perceptualRoughness = SmoothnessToPerceptualRoughness(_Smoothness);
			half roughness;
			half specularTerm = Highlights(perceptualRoughness, gi.light.dir, s.Normal, viewDir, roughness);

#if _TOON_ON
			specularTerm = Toon(specularTerm, _SpecularToon);
#endif

#ifdef UNITY_COLORSPACE_GAMMA
			specularTerm = sqrt(max(1e-4h, specularTerm));
#endif

#if defined(_SPECULARHIGHLIGHTS_OFF)
			specularTerm = 0.0;
#endif

			// surfaceReduction = Int D(NdotH) * NdotH * Id(NdotL>0) dH = 1/(roughness^2+1)
			half surfaceReduction;
#ifdef UNITY_COLORSPACE_GAMMA
			surfaceReduction = 1.0 - 0.28 * roughness * perceptualRoughness;      // 1-0.28*x^3 as approximation for (1/(x^4+1))^(1/2.2) on the domain [0;1]
#else
			surfaceReduction = 1.0 / (roughness * roughness + 1.0);           // fade \in [0.5;1]
#endif

			half4 color;
			color.rgb = lerp(s.Albedo * (gi.indirect.diffuse + gi.light.color * diffuseTerm), 
			lerp(s.Fraction,
				specularTerm * gi.light.color + surfaceReduction * gi.indirect.specular, s.Alpha), _Metal);

			//c.rgb = lerp(s.Albedo * diff, spec, s.Alpha) * _LightColor0.rgb * atten;

#if CAUSTICS_ON
			color.rgb += ApplyCaustics(viewDir, gi.light.dir, s.Fog, s.Height, s.SceneDepth);
#endif
			color.a = 1.0f;

			return color;
		}

		/*half4 LightingCustom_Deferred(SurfaceOutputCustom s, half3 viewDir, UnityGI gi, out half4 outDiffuseOcclusion, out half4 outSpecSmoothness, out half4 outNormal)
		{
			outDiffuseOcclusion = half4(s.Albedo, 1.0f);
			outSpecSmoothness = half4(0.0f, 0.0f, 0.0f, _Smoothness);
			outNormal = half4(s.Normal, 0.0f);

			return LightingCustom(s, viewDir, gi);
		}*/

		void LightingCustom_GI(SurfaceOutputCustom s, UnityGIInput data, inout UnityGI gi)
		{
			SurfaceOutputStandard o;
			o.Albedo = s.Albedo;
			o.Normal = s.Normal;
			o.Emission = s.Emission;
			o.Metallic = 0.0f;
			o.Smoothness = _Smoothness;
			o.Occlusion = 1.0f;
			o.Alpha = 1.0f;

			LightingStandard_GI(o, data, gi);
		}

		void vert(inout appdata_full v, out Input o)
		{
			UNITY_INITIALIZE_OUTPUT(Input, o);

			o.grabScreenPos = ComputeGrabScreenPos(UnityObjectToClipPos(v.vertex));
		}

        void surf (Input IN, inout SurfaceOutputCustom o)
        {
			float2 uv = IN.worldPos.xz;

			o.Normal = GetNormal(_Time.y, uv, _BumpScale, _BumpSpeed);

			float3 worldNormal = WorldNormalVector(IN, o.Normal);
			/*float3 worldSpaceViewDir = UnityWorldSpaceViewDir(IN.worldPos);
			float viewDistance = length(worldSpaceViewDir);*/
			o.Fraction = CalculateRefractiveColor(IN.grabScreenPos, worldNormal, o.SceneDepth);

			/*o.Fog = saturate(viewDistance / _RefractionDistance);

			float viewWaterDepthFactor = pow(saturate(o.ViewWaterDepth / _WaterClarity), _WaterClarityAttenuation);

			o.Fog = lerp(viewWaterDepthFactor, 1.0f, o.Fog);*/

			float viewWaterDepth = o.SceneDepth - IN.grabScreenPos.w;

			float viewWaterDepthFactor = pow(saturate(viewWaterDepth * _WaterClarity), _WaterClarityAttenuation);

			fixed4 col = lerp(_Color, _ColorDepth, viewWaterDepthFactor);

			fixed fog = saturate(1.0 - exp(_DepthFogDensity * -o.SceneDepth));

			o.Fraction *= 1.0f - fog;
			o.Albedo = col * fog;

			//float3 worldViewDir = worldSpaceViewDir / viewDistance;

			fixed facing = max(dot(IN.viewDir, o.Normal), 0.0f);

#if _FOAM_ON
			o.Albedo = ApplyFoam(facing, viewWaterDepth, uv, o.Albedo);
#endif

#if CAUSTICS_ON
			o.Height = IN.worldPos.y;
			o.Fog = fog;
#endif

#if _TOON_ON
			facing = Toon(facing, _FresnelToon);
#endif

			o.Alpha = FastFresnel(_FresnelFactor, facing, _FresnelAttenuation);
        }
        ENDCG
    }

    FallBack "Diffuse"
}
