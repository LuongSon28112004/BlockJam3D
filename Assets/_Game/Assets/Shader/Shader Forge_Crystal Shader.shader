Shader "Shader Forge/Crystal Shader" {
	Properties {
		_BaseColortex ("Base Color tex", 2D) = "white" {}
		_BasaColor ("Basa Color", Vector) = (1,1,1,1)
		_SurfaceClolrtex ("Surface Clolr tex", 2D) = "white" {}
		_SurfaceClolr ("Surface Clolr", Vector) = (0,0,0,1)
		_Normal ("Normal", 2D) = "bump" {}
		_alpha ("alpha", 2D) = "white" {}
		_Metallic ("Metallic", Range(0, 1)) = 0.5
		_Gloss ("Gloss", Range(0, 1)) = 0.9
		_Repetition ("Repetition", Range(1, 20)) = 10
		_ColorLoop ("ColorLoop", Range(1, 5)) = 1
		_Width ("Width", Range(0, 1)) = 0.5
		_ColoeLevel ("Coloe Level", Range(0, 1)) = 0.5
		_PasutelColor ("Pasutel Color", Range(0, 1)) = 0.3
		_Distortion ("Distortion", Range(0, 10)) = 5
		_ChromaticAberration ("Chromatic Aberration", Range(-1, 1)) = 0
		[MaterialToggle] _LightCompletion ("Light Completion", Float) = 1
		[HideInInspector] _Cutoff ("Alpha cutoff", Range(0, 1)) = 0.5
	}
	//DummyShaderTextExporter
	SubShader{
		Tags { "RenderType" = "Opaque" }
		LOD 200

		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			float4x4 unity_ObjectToWorld;
			float4x4 unity_MatrixVP;

			struct Vertex_Stage_Input
			{
				float4 pos : POSITION;
			};

			struct Vertex_Stage_Output
			{
				float4 pos : SV_POSITION;
			};

			Vertex_Stage_Output vert(Vertex_Stage_Input input)
			{
				Vertex_Stage_Output output;
				output.pos = mul(unity_MatrixVP, mul(unity_ObjectToWorld, input.pos));
				return output;
			}

			float4 frag(Vertex_Stage_Output input) : SV_TARGET
			{
				return float4(1.0, 1.0, 1.0, 1.0); // RGBA
			}

			ENDHLSL
		}
	}
	Fallback "Diffuse"
	//CustomEditor "ShaderForgeMaterialInspector"
}