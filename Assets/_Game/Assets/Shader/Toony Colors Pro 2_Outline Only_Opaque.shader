Shader "Toony Colors Pro 2/Outline Only/Opaque" {
	Properties {
		_Outline ("Outline Width", Float) = 1
		_OutlineColor ("Outline Color", Vector) = (0.2,0.2,0.2,1)
		_TexLod ("#OUTLINETEX# Texture LOD", Range(0, 10)) = 5
		_MainTex ("#OUTLINETEX# Texture (RGB)", 2D) = "white" {}
		_ZSmooth ("#OUTLINEZ# Z Correction", Range(-3, 3)) = -0.5
		_Offset1 ("#OUTLINEZ# Z Offset 1", Float) = 0
		_Offset2 ("#OUTLINEZ# Z Offset 2", Float) = 0
		_SrcBlendOutline ("#BLEND# Blending Source", Float) = 0
		_DstBlendOutline ("#BLEND# Blending Dest", Float) = 0
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
	//CustomEditor "TCP2_OutlineInspector"
}