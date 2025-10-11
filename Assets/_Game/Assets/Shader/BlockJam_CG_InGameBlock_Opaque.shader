Shader "BlockJam/CG_InGameBlock_Opaque" {
	Properties {
		_MainTex ("Main Texture", 2D) = "white" {}
		_Tex1 ("Expression Texture 1", 2D) = "white" {}
		_Tex2 ("Expression Texture 2", 2D) = "white" {}
		_OutlineColor ("Outline Color", Vector) = (0,0,0,1)
		_OutlineWidth ("Outline Width (px)", Range(0, 1)) = 0.5
		_AngleThreshold ("Silhouette Threshold", Range(0, 1)) = 0.2
		_SpecularPower ("Specular Power", Range(1, 128)) = 32
		_SpecularIntensity ("Specular Intensity", Range(0, 2)) = 0.5
		_SpecularCurve ("Specular Curve", Range(0.1, 5)) = 1
		_LightDir ("Light Direction", Vector) = (0,0,0,0)
		_LightColor ("Light Color", Vector) = (1,1,1,1)
		_ShadowColor ("Shadow Tint", Vector) = (1,1,1,1)
		_MouthColor ("Mouth Color", Vector) = (0,0,0,1)
		_AlphaClipThreshold ("Alpha Clip Threshold", Range(0, 1)) = 0.95
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
	//CustomEditor "InGameBlockShaderGUI"
}