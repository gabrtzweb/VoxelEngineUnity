Shader "Custom/VoxelEngineURP"
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
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" }
        LOD 200

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma require 2darray
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

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
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float2 blockData : TEXCOORD1;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                nointerpolation float blockIndex : TEXCOORD3;
                nointerpolation float blockIsGrass : TEXCOORD4;
                float4 color : COLOR;
                float4 shadowCoord : TEXCOORD5;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs nor = GetVertexNormalInputs(IN.normalOS);

                OUT.positionCS = pos.positionCS;
                OUT.positionWS = pos.positionWS;
                OUT.normalWS = NormalizeNormalPerVertex(nor.normalWS);
                OUT.shadowCoord = GetShadowCoord(pos);
                OUT.uv = IN.uv;
                OUT.blockIndex = IN.blockData.x;
                OUT.blockIsGrass = IN.blockData.y;
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 blockColor = SAMPLE_TEXTURE2D_ARRAY_LOD(_BlockTexArray, sampler_BlockTexArray, IN.uv, IN.blockIndex, 0);
                if (IN.blockIsGrass > 0.5)
                    blockColor.rgb *= _GrassTint.rgb;

                half3 albedo = blockColor.rgb * IN.color.rgb * _Tint.rgb;
                half alpha = blockColor.a * IN.color.a * _Tint.a;

                half3 normalWS = normalize(IN.normalWS);
                Light mainLight = GetMainLight(IN.shadowCoord);
                mainLight.shadowAttenuation = 1.0h;
                half mainNdotL = saturate(dot(normalWS, mainLight.direction));

                half3 lighting = 0;
                // small ambient floor to keep voxel AO readable
                lighting += half3(0.18h, 0.18h, 0.18h);
                lighting += mainLight.color * (mainNdotL * mainLight.distanceAttenuation * mainLight.shadowAttenuation);

                #if defined(_ADDITIONAL_LIGHTS)
                uint additionalLightsCount = GetAdditionalLightsCount();
                for (uint lightIndex = 0u; lightIndex < additionalLightsCount; ++lightIndex)
                {
                    Light light = GetAdditionalLight(lightIndex, IN.positionWS);
                    light.shadowAttenuation = 1.0h;
                    half NdotL = saturate(dot(normalWS, light.direction));
                    lighting += light.color * (NdotL * light.distanceAttenuation * light.shadowAttenuation);
                }
                #endif

                half3 finalColor = albedo * lighting;
                return half4(finalColor, alpha);
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }

    Fallback "Universal Render Pipeline/Lit"
}