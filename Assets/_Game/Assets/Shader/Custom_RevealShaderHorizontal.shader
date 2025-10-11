Shader "Custom/RevealShaderHorizontal" {
	Properties {
		_DissolveFactor ("Dissolve Factor", Range(-150, 150)) = 1
		_WiggleFactor ("Wiggle Factor", Range(0, 1)) = 1
		_WiggleTime ("Wiggle Time", Float) = 0
		_EdgeColor ("Edge Color", Vector) = (1,0.5,0,1)
		_DisableColor ("Disabled Color", Vector) = (0.2,0.2,0.2,1)
		_EdgeWidth ("Edge Width", Range(0, 20)) = 10
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_WiggleOffset ("Wiggle Offset", Range(-3, 3)) = 0
		_WiggleSpeed ("Wiggle Speed", Range(-10, 10)) = 1
		_WiggleXZScale ("Wiggle XZ scale", Range(-10, 10)) = 1
		_PlanePos ("Plane Position", Vector) = (0,0,0,0)
		_PlaneNorm ("Plane Normal", Vector) = (0,0,1,0)
	}
	//DummyShaderTextExporter
	SubShader{
		Tags { "RenderType"="Opaque" }
		LOD 200

		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			float4x4 unity_ObjectToWorld;
			float4x4 unity_MatrixVP;
			float4 _MainTex_ST;

			struct Vertex_Stage_Input
			{
				float4 pos : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct Vertex_Stage_Output
			{
				float2 uv : TEXCOORD0;
				float4 pos : SV_POSITION;
			};

			Vertex_Stage_Output vert(Vertex_Stage_Input input)
			{
				Vertex_Stage_Output output;
				output.uv = (input.uv.xy * _MainTex_ST.xy) + _MainTex_ST.zw;
				output.pos = mul(unity_MatrixVP, mul(unity_ObjectToWorld, input.pos));
				return output;
			}

			Texture2D<float4> _MainTex;
			SamplerState sampler_MainTex;

			struct Fragment_Stage_Input
			{
				float2 uv : TEXCOORD0;
			};

			float4 frag(Fragment_Stage_Input input) : SV_TARGET
			{
				return _MainTex.Sample(sampler_MainTex, input.uv.xy);
			}

			ENDHLSL
		}
	}
	Fallback "Diffuse"
}