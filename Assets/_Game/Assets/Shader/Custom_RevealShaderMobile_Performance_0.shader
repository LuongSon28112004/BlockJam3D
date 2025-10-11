Shader "Custom/RevealShaderMobile_Performance" {
	Properties {
		[Header(Reveal Control)] _DissolveFactor ("Dissolve Factor", Range(-150, 150)) = 1
		_EdgeWidth ("Edge Width", Range(0, 20)) = 10
		_EdgeColor ("Edge Color", Vector) = (1,0.5,0,1)
		[Header(Edge Wave)] _WaveAmplitude ("Edge Wave Amplitude", Float) = 1
		_WaveFrequency ("Edge Wave Frequency", Float) = 1
		_WaveSpeed ("Edge Wave Speed", Float) = 1
		[Header(Wiggle Deformation)] _WiggleFactor ("Wiggle Factor (Amplitude)", Range(0, 1)) = 1
		_WiggleSpeed ("Wiggle Speed", Range(-10, 10)) = 1
		_WiggleXZScale ("Wiggle XZ Scale", Range(-10, 10)) = 1
		[Header(Materials)] _MainTex ("Albedo (RGB)", 2D) = "white" {}
		_NoiseTex ("Noise Texture (Edge Detail)", 2D) = "white" {}
		_DetailTex ("Detail Texture (Grain - Triplanar)", 2D) = "gray" {}
		_DetailTexTiling ("Detail Tex Tiling", Float) = 1
		[Header(Dissolved Material)] _GradientStart ("Gradient Start", Vector) = (0.5,0.8,1,1)
		_GradientEnd ("Gradient End", Vector) = (0.2,0.4,1,1)
		_ShadowColor ("Shadow Color", Vector) = (0,0,0,1)
		_GrainIntensity ("Grain Intensity", Range(0, 1)) = 0.2
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
	Fallback "Mobile/VertexLit"
}