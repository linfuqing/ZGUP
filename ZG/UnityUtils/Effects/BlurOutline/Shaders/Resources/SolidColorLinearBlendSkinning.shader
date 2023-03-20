Shader "ZG/SolidColorLinearBlendSkinning"
{
	SubShader
	{
		Tags{ "RenderType" = "Opaque" }
		Pass
		{ 
			CGPROGRAM
// Upgrade NOTE: excluded shader from OpenGL ES 2.0 because it uses non-square matrices
#pragma exclude_renderers gles
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			struct v2f
			{
				float4 vertex : SV_POSITION;
			};

			struct DeformedVertexData
			{
				float3 Position;
				float3 Normal;
				float3 Tangent;
			};

			uint _SkinMatrixIndex;

			uniform StructuredBuffer<float3x4> _SkinMatrices;

			float3 LinearBlendSkinning(
				uint4 indices, 
				float4 weights, 
				float4 vertex)
			{
				float3 position = 0.0f;
				float weight, totalWeight = 0.0f;
				for (int i = 0; i < 3; ++i)
				{
					float3x4 skinMatrix = _SkinMatrices[indices[i] + _SkinMatrixIndex];
					float3 vtransformed = mul(skinMatrix, vertex);
					//float3 ntransformed = mul(skinMatrix, float4(normalIn, 0));
					//float3 ttransformed = mul(skinMatrix, float4(tangentIn, 0));

					weight = weights[i];

					position += vtransformed * weight;
					//normalOut += ntransformed * weights[i];
					//tangentOut += ttransformed * weights[i];

					totalWeight += weight;
				}

				float3x4 skinMatrix = _SkinMatrices[indices[3] + _SkinMatrixIndex];
				float3 vtransformed = mul(skinMatrix, vertex);
				position += vtransformed * (1.0f - totalWeight);

				return position;
			}

			v2f vert(appdata_base v, float4 weights: BLENDWEIGHTS, uint4 indices: BLENDINDICES)
			{
				v2f o;
				float3 position = LinearBlendSkinning(indices, weights, v.vertex);

				o.vertex = UnityObjectToClipPos(position);
				return o;
			}

			fixed4 _SolidColor;

			fixed4 frag(v2f i) : SV_Target
			{
				return _SolidColor;
			}
			ENDCG
		}
	}
}