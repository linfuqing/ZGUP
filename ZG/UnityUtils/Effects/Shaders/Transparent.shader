﻿Shader "ZG/Transparent"
{
    Properties
    { 
		[Toggle(ENABLE_COLOR)] _EnalbeColor("Enable Color", Float) = 0

		_Color("Color", Color) = (1,1,1,1)
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
		Tags{ "Queue" = "Transparent" "RenderType" = "Transparent" "ForceNoShadowCasting" = "True" "DisableBatching" = "True" }

        LOD 100
		Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

			#pragma shader_feature __ ENABLE_COLOR

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

#ifdef ENABLE_COLOR
			fixed4 _Color;
#endif

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
				fixed4 color = tex2D(_MainTex, i.uv);

#ifdef ENABLE_COLOR
				color *= _Color;
#endif
				return color;
            }
            ENDCG
        }
    }
}
