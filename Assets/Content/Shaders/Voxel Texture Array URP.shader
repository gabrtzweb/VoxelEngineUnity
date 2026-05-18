Shader "Custom/Voxel Texture Array URP"
{
    Properties
    {
        _BlockTexArray ("Block Texture Array", 2DArray) = "" {}
        _GrassTint ("Grass Tint", Color) = (0.75, 1, 0.75, 1)
        _GrassLayerIndex ("Grass Layer Index", Float) = 0
        _Tint ("Tint", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" }
        LOD 100

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma require 2darray
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D_ARRAY(_BlockTexArray);
            SAMPLER(sampler_BlockTexArray);

            CBUFFER_START(UnityPerMaterial)
            float4 _GrassTint;
            float _GrassLayerIndex;
            float4 _Tint;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float2 blockData : TEXCOORD1;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float blockIndex : TEXCOORD1;
                float4 color : COLOR;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.blockIndex = IN.blockData.x;
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 blockColor = SAMPLE_TEXTURE2D_ARRAY_LOD(_BlockTexArray, sampler_BlockTexArray, IN.uv, IN.blockIndex, 0);
                if (abs(IN.blockIndex - _GrassLayerIndex) < 0.5)
                    blockColor.rgb *= _GrassTint.rgb;

                return half4(blockColor.rgb * IN.color.rgb * _Tint.rgb, blockColor.a * IN.color.a * _Tint.a);
            }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/Unlit"
}