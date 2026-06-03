Shader "VoxelEngine/VoxelEngineShader_URP"
{
	Properties
	{
		_Color ("Tint Color", Color) = (1,1,1,1)
		_MainTexArray ("Texture Array", 2DArray) = "white" {}
		_WaterAnimationSpeed ("Water Animation Speed", Float) = 20
	}

	SubShader
	{
		Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" }
		LOD 100

		Pass
		{
			Tags { "LightMode" = "UniversalForward" }

			HLSLPROGRAM
			#pragma target 4.5
			#pragma require 2darray
			#pragma vertex vert
			#pragma fragment frag
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			struct Attributes
			{
				float4 positionOS : POSITION;
				float4 color : COLOR;
				float2 uv : TEXCOORD0;
				float2 textureLayer : TEXCOORD1;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float4 color : COLOR;
				float2 uv : TEXCOORD0;
				float2 textureLayer : TEXCOORD1;
			};

			TEXTURE2D_ARRAY(_MainTexArray);
			SAMPLER(sampler_MainTexArray);

			CBUFFER_START(UnityPerMaterial)
			float4 _Color;
			float _WaterAnimationSpeed;
			CBUFFER_END

			Varyings vert(Attributes IN)
			{
				Varyings OUT;
				OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
				OUT.color = IN.color;
				OUT.uv = IN.uv;
				OUT.textureLayer = IN.textureLayer;
				return OUT;
			}

			half4 frag(Varyings IN) : SV_Target
			{
				float frameCount = max(1.0, IN.textureLayer.y);
				float textureLayer = IN.textureLayer.x;

				if (frameCount > 1.0)
				{
					float frameIndex = fmod(floor(_Time.y * _WaterAnimationSpeed), frameCount);
					textureLayer += frameIndex;
				}

				half4 textureColor = SAMPLE_TEXTURE2D_ARRAY(_MainTexArray, sampler_MainTexArray, IN.uv, textureLayer);
				return textureColor * IN.color * _Color;
			}
			ENDHLSL
		}
	}
	Fallback "Universal Render Pipeline/Unlit"
}