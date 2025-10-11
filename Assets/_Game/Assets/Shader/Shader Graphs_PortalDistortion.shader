Shader "Shader Graphs/PortalDistortion" {
	Properties {
		_WavesCenter ("WavesCenter", Vector) = (0.5,0.5,0,0)
		_Speed ("Speed", Range(0, 10)) = 1
		_RotationSpeed ("RotationSpeed", Range(0, 10)) = 1
		_WaveCount ("WaveCount", Range(0, 100)) = 5
		_RippleCount ("RippleCount", Range(0, 10)) = 2
		_RippleStrength ("RippleStrength", Range(0, 100)) = 1
		_FadeRadius ("FadeRadius", Range(0, 1)) = 1
		_FadeSmoothness ("FadeSmoothness", Range(0, 1)) = 1
		_Color ("Color", Vector) = (1,1,1,1)
		_WaveCutoutSize ("WaveCutoutSize", Range(0, 10)) = 10
		[HideInInspector] _BUILTIN_Surface ("Float", Float) = 1
		[HideInInspector] _BUILTIN_Blend ("Float", Float) = 2
		[HideInInspector] _BUILTIN_AlphaClip ("Float", Float) = 1
		[HideInInspector] _BUILTIN_SrcBlend ("Float", Float) = 1
		[HideInInspector] _BUILTIN_DstBlend ("Float", Float) = 0
		[HideInInspector] _BUILTIN_ZWrite ("Float", Float) = 0
		[HideInInspector] _BUILTIN_ZWriteControl ("Float", Float) = 0
		[HideInInspector] _BUILTIN_ZTest ("Float", Float) = 8
		[HideInInspector] _BUILTIN_CullMode ("Float", Float) = 2
		[HideInInspector] _BUILTIN_QueueOffset ("Float", Float) = 0
		[HideInInspector] _BUILTIN_QueueControl ("Float", Float) = -1
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

			float4 _Color;

			float4 frag(Vertex_Stage_Output input) : SV_TARGET
			{
				return _Color; // RGBA
			}

			ENDHLSL
		}
	}
	Fallback "Hidden/Shader Graph/FallbackError"
	//CustomEditor "UnityEditor.ShaderGraph.GenericShaderGraphMaterialGUI"
}