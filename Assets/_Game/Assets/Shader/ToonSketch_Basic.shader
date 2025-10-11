Shader "ToonSketch/Basic" {
	Properties {
		_Color ("Color", Vector) = (1,1,1,1)
		_MainTex ("Albedo", 2D) = "white" {}
		_Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5
		[Enum(Soft,0,Hard,1)] _Style ("Style", Float) = 0
		[Enum(Off,0,Front,1,Back,2)] _Cull ("Cull Mode", Float) = 2
		[Enum(Opaque,0,Cutout,1,Fade,2,Transparent,3)] _Blend ("Blend Mode", Float) = 0
		[Toggle(_TS_RAMPTEX_ON)] _Ramp ("Ramp Texture?", Float) = 0
		[NoScaleOffset] _RampTex ("Ramp Texture", 2D) = "white" {}
		_RampThreshold ("Ramp Threshold", Range(0, 1)) = 0.5
		_RampCutoff ("Ramp Cutoff", Range(0, 1)) = 0.1
		[Toggle(_TS_BUMPMAP_ON)] _Bump ("Bump Mapping?", Float) = 0
		[Normal] _BumpMap ("Bump Map", 2D) = "bump" {}
		_BumpScale ("Bump Strength", Range(0, 1)) = 1
		[Toggle(_TS_SPECULAR_ON)] _Specular ("Specular Highlights?", Float) = 0
		_SpecularTex ("Specular Texture", 2D) = "white" {}
		_SpecularColor ("Specular Color", Vector) = (1,1,1,1)
		[Enum(Additive,0,Multiply,1)] _SpecularType ("Specular Type", Float) = 0
		[Enum(Albedo Alpha,0,Specular Alpha,1)] _SmoothnessType ("Smoothness Type", Float) = 0
		_Smoothness ("Smoothness", Range(0, 1)) = 0.5
		[Toggle(_TS_GLOSSREFLECT_ON)] _GlossyReflections ("Glossy Reflections?", Float) = 1
		_SpecularThreshold ("Specular Threshold", Range(0, 1)) = 0.5
		_SpecularCutoff ("Specular Cutoff", Range(0, 1)) = 0.1
		_SpecularIntensity ("Specular Intensity", Range(0, 10)) = 1
		[Toggle(_TS_RIMLIGHT_ON)] _RimLighting ("Rim Lighting?", Float) = 0
		_RimColor ("Rim Color", Vector) = (1,1,1,1)
		[Enum(Additive,0,Multiply,1)] _RimType ("Rim Type", Float) = 0
		[Toggle(_TS_RIMCOLORING_ON)] _RimColoring ("Rim Use Color?", Float) = 1
		_RimMin ("Rim Min", Range(0, 1)) = 0.6
		_RimMax ("Rim Max", Range(0, 1)) = 0.8
		_RimIntensity ("Rim Intensity", Range(0, 10)) = 1
		[Toggle(_TS_IGNOREINDIRECT_ON)] _IgnoreIndirect ("Ignore Indirect Lighting?", Float) = 0
		[HideInInspector] _SrcBlend ("__src", Float) = 1
		[HideInInspector] _DstBlend ("__dst", Float) = 0
		[HideInInspector] _ZWrite ("__zw", Float) = 1
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
	Fallback "Diffuse"
	//CustomEditor "ToonSketchBasicShaderGUI"
}