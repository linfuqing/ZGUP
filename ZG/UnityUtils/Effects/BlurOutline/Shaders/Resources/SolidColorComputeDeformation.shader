Shader "ZG/SolidColorComputeDeformation"
{
	SubShader
	{
		Tags{ "RenderType" = "Opaque" }
		Pass
		{ 
			CGPROGRAM
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

			uint _ComputeMeshIndex;

			uniform StructuredBuffer<DeformedVertexData> _DeformedMeshData : register(t1);

			float3 ComputeDeformedVertex(uint vertexID)
			{
				const DeformedVertexData vertexData = _DeformedMeshData[_ComputeMeshIndex + vertexID];
				return vertexData.Position;

				//normalOut = vertexData.Normal;
				//tangentOut = vertexData.Tangent;
			}

			v2f vert(appdata_base v, uint vertexID:SV_VertexID)
			{
				v2f o;
				float3 position = ComputeDeformedVertex(vertexID);

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