Shader "Unlit/DebugRenderVolumeTex"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}

	CGINCLUDE

	#include "UnityCG.cginc"

	struct appdata
	{
		float4 vertex : POSITION;
		float2 uv     : TEXCOORD0;
	};

	struct v2f
	{
		//float2 uv     : TEXCOORD0;
		float4 vertex : SV_POSITION;
		float4 color  : COLOR;
	};

	sampler2D _MainTex;
	float4 _MainTex_ST;

	sampler3D _VolumeTex;

	float4 _Size;
	
	v2f vert (uint id : SV_VertexID)
	{
		v2f o;
		float4 pos = float4(
			fmod(id, _Size.x), 
			floor(fmod(id, (_Size.x * _Size.y)) / _Size.y), 
			floor(id / (_Size.x * _Size.y)), 
			1.0
		);

		o.vertex = mul(UNITY_MATRIX_MVP, pos);
		//o.uv     = TRANSFORM_TEX(v.uv, _MainTex);
		//o.color = float4(id/(_Size.x * _Size.y * _Size.z), 0, 0, 1);
		float vol = tex3Dlod(_VolumeTex, float4(pos.x / _Size.x, pos.y / _Size.y, pos.z / _Size.z, 0.0)).r;
		//o.color = float4(pos.x / _Size.x, pos.y / _Size.y, pos.z / _Size.z, 1.0);
		o.color = float4 (vol, 0, 0, 1);
		return o;
	}
	
	fixed4 frag (v2f i) : Color
	{
		fixed4 col = i.color;
		return col;
	}
	ENDCG

	SubShader
	{
		Tags { "RenderType"="Opaque" }
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
