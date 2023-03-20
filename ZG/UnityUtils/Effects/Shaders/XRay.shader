// Upgrade NOTE: replaced 'UNITY_INSTANCE_ID' with 'UNITY_VERTEX_INPUT_INSTANCE_ID'
// Upgrade NOTE: upgraded instancing buffer 'name' to new syntax.

Shader "ZG/Xray"
{
	Properties
	{
		_RimColor("RimColor", Color) = (0, 0, 1, 1)
		_RimIntensity("Intensity", Range(0, 2)) = 1
	}
		SubShader
	{
		Tags{ "Queue" = "Transparent" "RenderType" = "Opaque" }

		LOD 200
		Pass
		{
			//Blend SrcAlpha One//打开混合模式
			Blend SrcAlpha  One
			ZWrite Off
			Lighting Off
			Cull Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float3 normal:NORMAL;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				fixed4 color : COLOR;
				//UNITY_INSTANCE_ID
			};

			fixed4 _RimColor;
			float _RimIntensity;

			v2f vert(appdata v)
			{
				v2f o;

				UNITY_SETUP_INSTANCE_ID(v)
				//UNITY_TRANSFER_INSTANCE_ID(v, o)

				o.pos = UnityObjectToClipPos(v.vertex);
				float3 viewDir = normalize(ObjSpaceViewDir(v.vertex));//计算出顶点到相机的向量
				float val = 1 - saturate(dot(v.normal, viewDir));//计算点乘值

				o.color = _RimColor * val * (1 + _RimIntensity);//计算强度

				return o;
			}

			fixed4 frag(v2f i) : COLOR
			{
				return i.color;
			}
			ENDCG
		}
	}
}