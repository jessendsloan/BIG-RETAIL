Shader "Big Retail/Characters/Experiments/Procedural Chest Art"
{
    Properties
    {
        _MainTex("Sprite Texture", 2D) = "white" {}
        _MaskTex("Mask", 2D) = "white" {}
        _NormalMap("Normal Map", 2D) = "bump" {}
        [MaterialToggle] _ZWrite("ZWrite", Float) = 0

        [Header(Procedural Chest Art)]
        [KeywordEnum(None, Star, Heart, Pocket, Stripe, Smile, Badge)] _GraphicType("Graphic Style", Float) = 1
        _GraphicColor("Graphic Color", Color) = (0.98, 0.88, 0.35, 1.0)
        _GraphicScale("Graphic Scale", Range(0.2, 2.0)) = 0.85
        _GraphicOffsetY("Graphic Y Offset", Range(-0.4, 0.4)) = 0.05

        [Header(Garment Details)]
        _CollarStyle("Collar Neckline", Range(0.0, 1.0)) = 0.6
        _CollarColor("Collar Color", Color) = (0.95, 0.95, 0.95, 1.0)
        _SeamShading("Border Seam Shading", Range(0.0, 0.8)) = 0.25

        [HideInInspector] _Color("Tint", Color) = (1, 1, 1, 1)
        [HideInInspector] _RendererColor("Renderer Color", Color) = (1, 1, 1, 1)
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
            half4 _GraphicColor;
            half4 _CollarColor;
            half _GraphicType;
            half _GraphicScale;
            half _GraphicOffsetY;
            half _CollarStyle;
            half _SeamShading;
        CBUFFER_END

        float GetStarMask(float2 p, float size)
        {
            float angle = atan2(p.y, p.x);
            float dist = length(p);
            float starRadius = size * (0.65 + 0.35 * cos(5.0 * angle));
            return smoothstep(starRadius, starRadius - 0.02, dist);
        }

        float GetHeartMask(float2 p, float size)
        {
            p /= max(size, 0.001);
            p.y -= 0.1;
            float x = p.x;
            float y = p.y;
            float a = x*x + y*y - 0.15;
            float h = a*a*a - x*x*y*y*y;
            return smoothstep(0.0, -0.015, h);
        }

        float GetPocketMask(float2 uv)
        {
            float2 p = uv - float2(0.35, 0.62);
            bool inBox = (abs(p.x) < 0.11) && (p.y > -0.11 && p.y < 0.07);
            float2 b = abs(p) - float2(0.11, 0.07);
            float isBorder = smoothstep(0.015, 0.0, max(b.x, b.y)) - smoothstep(0.0, -0.015, max(b.x, b.y));
            return inBox ? (1.0 - 0.3 * isBorder) : 0.0;
        }

        float GetStripeMask(float2 uv)
        {
            float y = uv.y;
            float band1 = (y > 0.52 && y < 0.66) ? 1.0 : 0.0;
            float band2 = (y > 0.42 && y < 0.48) ? 0.7 : 0.0;
            return max(band1, band2);
        }

        float GetSmileMask(float2 p, float size)
        {
            p /= max(size, 0.001);
            float eyeL = smoothstep(0.045, 0.02, length(p - float2(-0.14, 0.10)));
            float eyeR = smoothstep(0.045, 0.02, length(p - float2(0.14, 0.10)));
            float mouthR = length(p - float2(0.0, 0.02));
            float mouthArc = (mouthR > 0.13 && mouthR < 0.20 && p.y < 0.02) ? 1.0 : 0.0;
            return max(max(eyeL, eyeR), mouthArc);
        }

        float GetBadgeMask(float2 p, float size)
        {
            p /= max(size, 0.001);
            float d = abs(p.x) + abs(p.y);
            return smoothstep(0.30, 0.28, d);
        }

        half3 ApplyChestArt(float2 uv, half3 baseColor)
        {
            // 1. Cutout Edge Seam Shading (Papercraft / felt look)
            float dEdge = min(min(uv.x, 1.0 - uv.x), min(uv.y, 1.0 - uv.y));
            float edgeShade = smoothstep(0.0, 0.12, dEdge);
            baseColor = lerp(baseColor * (1.0 - _SeamShading), baseColor, edgeShade);

            // 2. Collar / Neckline Trim
            if (_CollarStyle > 0.01)
            {
                float dx = (uv.x - 0.5) * 2.0;
                float collarThreshold = 0.88 - (1.0 - _CollarStyle) * 0.12 + 0.12 * (dx * dx);
                if (uv.y > collarThreshold && uv.y < 0.98)
                {
                    baseColor = lerp(baseColor, _CollarColor.rgb, _CollarColor.a);
                }
            }

            // 3. Graphic Art
            int type = (int)_GraphicType;
            if (type > 0)
            {
                float2 p = uv - float2(0.5, 0.55 + _GraphicOffsetY);
                float mask = 0.0;

                if (type == 1) mask = GetStarMask(p, 0.22 * _GraphicScale);
                else if (type == 2) mask = GetHeartMask(p, 0.70 * _GraphicScale);
                else if (type == 3) mask = GetPocketMask(uv);
                else if (type == 4) mask = GetStripeMask(uv);
                else if (type == 5) mask = GetSmileMask(p, 0.80 * _GraphicScale);
                else if (type == 6) mask = GetBadgeMask(p, 0.80 * _GraphicScale);

                baseColor = lerp(baseColor, _GraphicColor.rgb, mask * _GraphicColor.a);
            }

            return baseColor;
        }
        ENDHLSL

        Pass
        {
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex LitVertex
            #pragma fragment PlainLitFragment

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

            half4 PlainLitFragment(Varyings input) : SV_Target
            {
                half spriteAlpha = SAMPLE_TEXTURE2D(
                    _MainTex,
                    sampler_MainTex,
                    input.uv).a;
                half4 mask = SAMPLE_TEXTURE2D(
                    _MaskTex,
                    sampler_MaskTex,
                    input.uv);
                half3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(
                    _NormalMap,
                    sampler_NormalMap,
                    input.uv));

                half3 finalColor = ApplyChestArt(input.uv, input.color.rgb);

                SurfaceData2D surfaceData;
                InputData2D inputData;

                InitializeSurfaceData(
                    finalColor,
                    input.color.a * spriteAlpha,
                    mask,
                    normalTS,
                    surfaceData);
                InitializeInputData(
                    input.uv,
                    input.lightingUV,
                    inputData);

                #if defined(DEBUG_DISPLAY)
                    SETUP_DEBUG_TEXTURE_DATA_2D_NO_TS(
                        inputData,
                        input.positionWS,
                        input.positionCS,
                        _MainTex);
                    surfaceData.normalWS = input.normalWS;
                #endif

                return CombinedShapeLightShared(
                    surfaceData,
                    inputData);
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
            Tags
            {
                "LightMode" = "UniversalForward"
                "Queue" = "Transparent"
                "RenderType" = "Transparent"
            }

            HLSLPROGRAM
            #pragma vertex UnlitVertex
            #pragma fragment PlainUnlitFragment
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

            half4 PlainUnlitFragment(Varyings input) : SV_Target
            {
                half spriteAlpha = SAMPLE_TEXTURE2D(
                    _MainTex,
                    sampler_MainTex,
                    input.uv).a;

                half3 finalColor = ApplyChestArt(input.uv, input.color.rgb);

                half4 output = half4(
                    finalColor,
                    input.color.a * spriteAlpha);

                #if defined(DEBUG_DISPLAY)
                    SurfaceData2D surfaceData;
                    InputData2D inputData;
                    half4 debugColor = 0;

                    InitializeSurfaceData(
                        output.rgb,
                        output.a,
                        surfaceData);
                    InitializeInputData(input.uv, inputData);
                    SETUP_DEBUG_TEXTURE_DATA_2D(
                        inputData,
                        input.positionWS,
                        input.positionCS,
                        _MainTex);

                    if (CanDebugOverrideOutputColor(
                            surfaceData,
                            inputData,
                            debugColor))
                    {
                        return debugColor;
                    }
                #endif

                return output;
            }
            ENDHLSL
        }
    }
}
