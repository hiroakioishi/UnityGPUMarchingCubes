Shader "Hidden/GPUMarchingCubes/DebugParticleRender"
{
	Properties
	{
	}

	CGINCLUDE
	#include "UnityCG.cginc"

	struct Particle
	{
		float3 velocity : TEXCOORD0;
		float3 position : POSITION;
		float4 color    : COLOR;
		float  life     : TEXCOORD1;
	};

	struct v2f
	{
		float4 vertex : SV_POSITION;
		float4 color  : COLOR;
	};

	sampler2D _MainTex;
	float4 _MainTex_ST;

	StructuredBuffer<Particle> _ParticleBuffer;

	v2f vert(uint id : SV_VertexID)
	{
		v2f o = (v2f)0;
		o.vertex = mul(UNITY_MATRIX_MVP, float4(_ParticleBuffer[id].position.xyz, 1.0));
		o.color = _ParticleBuffer[id].color;
		return o;
	}

	fixed4 frag(v2f i) : SV_Target
	{
		return i.color;
	}

	ENDCG

	SubShader
	{
		Tags{ "RenderType" = "Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex   vert
			#pragma fragment frag
			ENDCG
		}
	}
}