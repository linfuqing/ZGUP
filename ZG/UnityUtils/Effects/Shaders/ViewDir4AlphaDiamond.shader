Shader "ZG/ViewDir4AlphaDiamond"
{
    Properties
    {
        _Base("Base", Range(0,1)) = 0.0
        _Target("Target", Vector) = (0.0, 0.0, 0.0, 1.0)

        _Color("Color",Color) = (1,1,1,1)
        _ReflectionStrength("Reflection Strength",Range(0.0,2.0)) = 1.0
        _EnvironmentLight("Environment Light", Range(0.0,2.0)) = 1.0
        _Emission("Emission", Range(0.0,2.0)) = 0.0
        [NoScaleOffset] _RefractTex("Refraction Texture", Cube) = "" {}
    }

        SubShader
    {
        Tags { "Queue" = "Transparent" }
        LOD 100

        Pass
        {
            Cull  Front
            ZWrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            #include "Clip.cginc"

            struct v2f
            {
                float4 pos:SV_POSITION;

                float4 screenPos:TEXCOORD0;

                float4 uv:TEXCOORD1;

                UNITY_FOG_COORDS(2)
            };

            fixed4 _Color;
            samplerCUBE _RefractTex;
            half _EnvironmentLight;
            half _Emission;

            half _Base;
            float4 _Target;

            v2f vert(appdata_base  v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);

                o.screenPos = ComputeScreenPos(o.pos);

                float3 viewDir = normalize(ObjSpaceViewDir(v.vertex));
                o.uv.xyz = -reflect(viewDir,v.normal);
                o.uv.xyz = mul(unity_ObjectToWorld,float4(o.uv.xyz,0));
                o.uv.w = saturate(_Base + 1.0 - max(0.0, dot(normalize(_Target - mul(unity_ObjectToWorld, v.vertex)), normalize(_Target - _WorldSpaceCameraPos))));

                UNITY_TRANSFER_FOG(o, o.pos);

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float alpha = _Color.a * i.uv.w;

                AlphaClip(i.screenPos, alpha);

                half3 refraction = texCUBE(_RefractTex,i.uv.xyz).rgb * _Color.rgb;
                half4 reflection = UNITY_SAMPLE_TEXCUBE(unity_SpecCube0,i.uv.xyz);
                reflection.rgb = DecodeHDR(reflection,unity_SpecCube0_HDR);
                half3 multiplier = reflection.rgb * _EnvironmentLight + _Emission;
                half4 color = half4(refraction.rgb * multiplier.rgb,1.0f);

                UNITY_APPLY_FOG(i.fogCoord, color);

                return color;
            }
            ENDCG
        }

        Pass
        {
            ZWrite On
            Blend One One

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            #include "Clip.cginc"

            struct v2f
            {
                float4 pos:SV_POSITION;

                float4 screenPos:TEXCOORD0;

                float4 uv:TEXCOORD1;

                half fresnel:TEXCOORD2;

                UNITY_FOG_COORDS(3)
            };

            fixed4 _Color;
            samplerCUBE _RefractTex;
            half _ReflectionStrength;
            half _EnvironmentLight;
            half _Emission;

            half _Base;
            float4 _Target;

            v2f vert(appdata_base  v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);

                o.screenPos = ComputeScreenPos(o.pos);

                float3 viewDir = normalize(ObjSpaceViewDir(v.vertex));
                o.uv.xyz = -reflect(viewDir,v.normal);
                o.uv.xyz = mul(unity_ObjectToWorld,float4(o.uv.xyz,0));
                o.uv.w = saturate(_Base + 1.0 - max(0.0, dot(normalize(_Target - mul(unity_ObjectToWorld, v.vertex)), normalize(_Target - _WorldSpaceCameraPos))));
                o.fresnel = 1 - saturate(dot(v.normal,viewDir));

                UNITY_TRANSFER_FOG(o, o.pos);

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                /* unity_SpecCube0保存了环境的立体贴图（天空盒和反射探针形成的立体贴图）
                 UNITY_SAMPLE_TEXCUBE 用于采样立体贴图
                 DecodeHDR将高动态贴图转为正常RGB，如果导入的不是HDR贴图，就没有变化*/

                float alpha = _Color.a * i.uv.w;

                AlphaClip(i.screenPos, alpha);

                 half3 refraction = texCUBE(_RefractTex,i.uv).rgb * _Color.rgb;
                 half4 reflection = UNITY_SAMPLE_TEXCUBE(unity_SpecCube0,i.uv);
                 reflection.rgb = DecodeHDR(reflection,unity_SpecCube0_HDR);
                 half3 reflection2 = reflection * _ReflectionStrength * i.fresnel;
                 half3 multiplier = reflection.rgb * _EnvironmentLight + _Emission;
                 half4 color = half4(reflection2 + refraction.rgb * multiplier.rgb,1.0f);

                 UNITY_APPLY_FOG(i.fogCoord, color);

                 return color;
             }
             ENDCG
         }

         UsePass "VertexLit/SHADOWCASTER"
    }
}