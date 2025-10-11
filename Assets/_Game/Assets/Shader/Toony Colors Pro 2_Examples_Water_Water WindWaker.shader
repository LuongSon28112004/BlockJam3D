Shader "Toony Colors Pro 2/Examples/Water/Water WindWaker" {
	Properties {
		[TCP2HelpBox(Warning,Make sure that the Camera renders the depth texture for this material to work properly.    You can use the script __TCP2_CameraDepth__ for this.)] [TCP2HeaderHelp(BASE, Base Properties)] _HColor ("Highlight Color", Vector) = (0.6,0.6,0.6,1)
		_SColor ("Shadow Color", Vector) = (0.3,0.3,0.3,1)
		_MainTex ("Main Texture (RGB)", 2D) = "white" {}
		[TCP2Separator] _RampThreshold ("Ramp Threshold", Range(0, 1)) = 0.5
		_RampSmooth ("Ramp Smoothing", Range(0.001, 1)) = 0.1
		[TCP2Separator] [TCP2HeaderHelp(WATER)] _Color ("Water Color", Vector) = (0.5,0.5,0.5,1)
		[Header(Depth Color)] _DepthColor ("Depth Color", Vector) = (0.5,0.5,0.5,1)
		[PowerSlider(5.0)] _DepthDistance ("Depth Distance", Range(0.01, 3)) = 0.5
		[Header(Foam)] _FoamSpread ("Foam Spread", Range(0.01, 5)) = 2
		_FoamStrength ("Foam Strength", Range(0.01, 10)) = 0.8
		_FoamColor ("Foam Color (RGB) Opacity (A)", Vector) = (0.9,0.9,0.9,1)
		[NoScaleOffset] _FoamTex ("Foam (RGB)", 2D) = "white" {}
		_FoamSmooth ("Foam Smoothness", Range(0, 0.5)) = 0.02
		_FoamSpeed ("Foam Speed", Vector) = (2,2,2,2)
		[Header(Vertex Waves Animation)] _WaveSpeed ("Speed", Float) = 2
		_WaveHeight ("Height", Float) = 0.1
		_WaveFrequency ("Frequency", Range(0, 10)) = 1
		[Header(UV Scrolling)] _UVScrollingX ("X Speed", Float) = 0.1
		_UVScrollingY ("Y Speed", Float) = 0.1
		[Header(UV Waves Animation)] _UVWaveSpeed ("Speed", Float) = 1
		_UVWaveAmplitude ("Amplitude", Range(0.001, 0.5)) = 0.05
		_UVWaveFrequency ("Frequency", Range(0, 10)) = 1
		[TCP2Separator] [TCP2HeaderHelp(RIM, Rim)] _RimColor ("Rim Color", Vector) = (0.8,0.8,0.8,0.6)
		_RimMin ("Rim Min", Range(0, 1)) = 0.5
		_RimMax ("Rim Max", Range(0, 1)) = 1
		[TCP2Separator] [HideInInspector] __dummy__ ("unused", Float) = 0
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
			float4 _Color;

			struct Fragment_Stage_Input
			{
				float2 uv : TEXCOORD0;
			};

			float4 frag(Fragment_Stage_Input input) : SV_TARGET
			{
				return _MainTex.Sample(sampler_MainTex, input.uv.xy) * _Color;
			}

			ENDHLSL
		}
	}
	//CustomEditor "TCP2_MaterialInspector_SG"
}