Shader "Big Retail/Characters/Textured Garment Lit"
{
    Properties
    {
        [PerRendererData] _MainTex("Garment Sprite", 2D) = "white" {}
        _MaskTex("Light Mask", 2D) = "white" {}
        _NormalMap("Normal Map", 2D) = "bump" {}
        [Enum(Full Color PNG, 0, Tintable Grayscale PNG, 1)]
        _AuthoringMode("PNG Color Mode", Float) = 0
        _RendererTintStrength("Outfit Tint Strength", Range(0, 1)) = 0
        _Brightness("Brightness", Range(0.5, 1.5)) = 1
        _Contrast("Contrast", Range(0.5, 1.5)) = 1
        _EdgeShading("Subtle Edge Shading", Range(0, 0.35)) = 0
        [MaterialToggle] _ZWrite("ZWrite", Float) = 0

        [HideInInspector] _Color("Tint", Color) = (1, 1, 1, 1)
        [HideInInspector][PerRendererData]
        _RendererColor("Renderer Color", Color) = (1, 1, 1, 1)
        [HideInInspector] _AlphaTex("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite [_ZWrite]

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

        CBUFFER_START(UnityPerMaterial)
            half4 _Color;
            half _AuthoringMode;
            half _RendererTintStrength;
            half _Brightness;
            half _Contrast;
            half _EdgeShading;
        CBUFFER_END

        half3 BuildGarmentColor(
            half3 pngColor,
            half3 rendererColor,
            float2 uv)
        {
            half luminance = dot(
                pngColor,
                half3(0.2126h, 0.7152h, 0.0722h));
            half3 fullColor = lerp(
                pngColor,
                pngColor * rendererColor,
                _RendererTintStrength);
            half3 tintableColor = luminance * rendererColor;
            half tintable = step(0.5h, _AuthoringMode);
            half3 result = lerp(fullColor, tintableColor, tintable);

            result = (result - 0.5h) * _Contrast + 0.5h;
            result *= _Brightness;

            half edgeDistance = min(
                min(uv.x, 1.0 - uv.x),
                min(uv.y, 1.0 - uv.y));
            half edgeBlend = smoothstep(0.0h, 0.09h, edgeDistance);
            result *= lerp(1.0h - _EdgeShading, 1.0h, edgeBlend);

            return saturate(result);
        }
        ENDHLSL

        Pass
        {
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex LitVertex
            #pragma fragment GarmentLitFragment
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/ShapeLightShared.hlsl"
            #pragma multi_compile_instancing
            #pragma multi_compile _ DEBUG_DISPLAY
            #pragma multi_compile _ SKINNED_SPRITE

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_LIT_OUTPUTS
                half4 color : COLOR;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Lit2DCommon.hlsl"

            Varyings LitVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(
                    input.positionOS,
                    unity_SpriteProps.xy);

                Varyings output = CommonLitVertex(input);
                output.color = input.color * _Color * unity_SpriteColor;
                return output;
            }

            half4 GarmentLitFragment(Varyings input) : SV_Target
            {
                half4 sprite = SAMPLE_TEXTURE2D(
                    _MainTex,
                    sampler_MainTex,
                    input.uv);
                half4 mask = SAMPLE_TEXTURE2D(
                    _MaskTex,
                    sampler_MaskTex,
                    input.uv);
                half3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(
                    _NormalMap,
                    sampler_NormalMap,
                    input.uv));
                half3 garmentColor = BuildGarmentColor(
                    sprite.rgb,
                    input.color.rgb,
                    input.uv);

                SurfaceData2D surfaceData;
                InputData2D inputData;
                InitializeSurfaceData(
                    garmentColor,
                    sprite.a * input.color.a,
                    mask,
                    normalTS,
                    surfaceData);
                InitializeInputData(input.uv, input.lightingUV, inputData);

                #if defined(DEBUG_DISPLAY)
                    SETUP_DEBUG_TEXTURE_DATA_2D_NO_TS(
                        inputData,
                        input.positionWS,
                        input.positionCS,
                        _MainTex);
                    surfaceData.normalWS = input.normalWS;
                #endif

                return CombinedShapeLightShared(surfaceData, inputData);
            }
            ENDHLSL
        }

        Pass
        {
            Tags { "LightMode" = "NormalsRendering" }

            HLSLPROGRAM
            #pragma vertex NormalsRenderingVertex
            #pragma fragment NormalsRenderingFragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ SKINNED_SPRITE

            struct Attributes
            {
                COMMON_2D_NORMALS_INPUTS
                float4 color : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_NORMALS_OUTPUTS
                half4 color : COLOR;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Normals2DCommon.hlsl"

            Varyings NormalsRenderingVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(
                    input.positionOS,
                    unity_SpriteProps.xy);

                Varyings output = CommonNormalsVertex(input);
                output.color = input.color * _Color * unity_SpriteColor;
                return output;
            }

            half4 NormalsRenderingFragment(Varyings input) : SV_Target
            {
                SetUpSpriteInstanceProperties();
                return CommonNormalsFragment(input, input.color);
            }
            ENDHLSL
        }

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex UnlitVertex
            #pragma fragment GarmentUnlitFragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ DEBUG_DISPLAY SKINNED_SPRITE

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_OUTPUTS
                half4 color : COLOR;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"

            Varyings UnlitVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(
                    input.positionOS,
                    unity_SpriteProps.xy);

                Varyings output = CommonUnlitVertex(input);
                output.color = input.color * _Color * unity_SpriteColor;
                return output;
            }

            half4 GarmentUnlitFragment(Varyings input) : SV_Target
            {
                half4 sprite = SAMPLE_TEXTURE2D(
                    _MainTex,
                    sampler_MainTex,
                    input.uv);
                half3 garmentColor = BuildGarmentColor(
                    sprite.rgb,
                    input.color.rgb,
                    input.uv);
                return half4(
                    garmentColor,
                    sprite.a * input.color.a);
            }
            ENDHLSL
        }
    }
}
