Shader "ZG/Background"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    { 
		Tags{ "Queue" = "Transparent-1" "RenderType" = "Background" "ForceNoShadowCasting" = "True" "DisableBatching" = "True" }

        // No culling or depth
        Cull Off ZWrite Off// ZTest Less //Always
		//Offset -1, -1
		Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
				float2 uv : TEXCOORD0;
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

			sampler2D _MainTex;
			//Texture2D _MainTex;
			//SamplerState sampler_point_clamp;

            v2f vert (appdata v)
            {
                v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);//v.vertex;// UnityObjectToClipPos(v.vertex);

				///精度可导致白边
#ifdef UNITY_REVERSED_Z
				o.vertex.z = o.vertex.w * 0.00001f;
#else
				o.vertex.z = o.vertex.w * 0.99999f;
#endif
				//o.vertex.z = UNITY_NEAR_CLIP_VALUE;
/*#if defined(UNITY_REVERSED_Z)
				o.vertex.z = 1.0f - o.vertex.z;
#endif*/

                o.uv = v.uv;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return tex2D(_MainTex, i.uv);//_MainTex.Sample(sampler_point_clamp, i.uv);//tex2D(_MainTex, i.uv);
            }
            ENDCG
        }

        Cull Off ZWrite Off// ZTest Always
		Blend SrcAlpha OneMinusSrcAlpha
		Pass
		{
			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata
			{
				float2 uv : TEXCOORD0;
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 screenPos : TEXCOORD1;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			sampler2D _CameraDepthTexture;

#define SKYBOX_THREASHOLD_VALUE 0.99f

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);

				o.uv = v.uv;

				o.screenPos = ComputeScreenPos(o.vertex);

				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				float depth = Linear01Depth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, i.screenPos));
				if(depth < SKYBOX_THREASHOLD_VALUE)
					discard;

				return tex2D(_MainTex, i.uv);
			}
			ENDCG
		}

		/*Pass
		{
			Tags{ "LightMode" = "Deferred" }

			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata
			{
				float2 uv : TEXCOORD0;
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			struct fo 
			{
				float4 diffuse:SV_TARGET0;
				float4 specSmoothness:SV_TARGET1;
				float4 normal:SV_TARGET2;
				float4 emission:SV_TARGET3;
				float4 depth:SV_TARGET4;
			};

			sampler2D _MainTex;
			sampler2D _SpecTex;
			sampler2D _NormalTex;
			sampler2D _EmissionTex;
			sampler2D _DepthTex;

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);//v.vertex;// UnityObjectToClipPos(v.vertex);

				///精度可导致白边
#ifdef UNITY_REVERSED_Z
				o.vertex.z = o.vertex.w * 0.00001f;
#else
				o.vertex.z = o.vertex.w * 0.99999f;
#endif
				o.uv = v.uv;

				return o;
			}

			fo frag(v2f i) : SV_Target
			{
				fo fo;
				fo.diffuse = tex2D(_MainTex, i.uv);
				fo.specSmoothness = tex2D(_SpecTex, i.uv);
				fo.normal = tex2D(_NormalTex, i.uv);
				fo.emission = tex2D(_EmissionTex, i.uv);
				fo.depth = tex2D(_DepthTex, i.uv);

				return fo;
			}
			ENDCG
		}*/
    }
}
