Shader "Custom/TranslucentArrow"
{
    Properties
    {
        _MainTex ("Arrow Texture", 2D) = "white" {}
        [HDR] _ArrowColor ("Arrow Color Tint", Color) = (1,1,1,1)
        [HDR] _BackgroundColor ("Background Color", Color) = (0.9, 0.9, 0.9, 0.4)
        
        [Space(10)]
        [Header(Shadow Settings)]
        _ShadowColor ("Shadow Color", Color) = (0, 0, 0, 0.5)
        _ShadowOffset ("Shadow Offset (X, Y)", Vector) = (0.05, -0.05, 0, 0)
    }
    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent" 
            "RenderPipeline"="UniversalPipeline" 
        }
        LOD 100
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "Unlit"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 positionCS : SV_POSITION;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _ArrowColor;
                float4 _BackgroundColor;
                float4 _ShadowColor;
                float4 _ShadowOffset;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 1. Sample the texture for the main arrow
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half arrowAlpha = texColor.a;
                
                // 2. Sample the texture again with an offset for the shadow
                float2 shadowUV = input.uv - _ShadowOffset.xy;
                half4 shadowTexColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, shadowUV);
                
                // We only care about the shadow's alpha, scaled by our chosen shadow transparency
                half shadowAlpha = shadowTexColor.a * _ShadowColor.a;
                
                // 3. Blend from bottom to top (Background -> Shadow -> Arrow)
                
                // Layer A: Blend Shadow over Background
                half3 bgAndShadowRGB = lerp(_BackgroundColor.rgb, _ShadowColor.rgb, shadowAlpha);
                // Standard alpha compositing: a_out = a_src + a_dst * (1 - a_src)
                half bgAndShadowAlpha = shadowAlpha + _BackgroundColor.a * (1.0 - shadowAlpha);
                
                // Layer B: Blend Arrow over (Background + Shadow)
                half3 finalRGB = lerp(bgAndShadowRGB, texColor.rgb * _ArrowColor.rgb, arrowAlpha);
                
                half finalArrowAlpha = arrowAlpha * _ArrowColor.a;
                half finalAlpha = finalArrowAlpha + bgAndShadowAlpha * (1.0 - finalArrowAlpha);
                
                return half4(finalRGB, finalAlpha);
            }
            ENDHLSL
        }

        // Shadow Caster Pass: This allows the object to cast shadows into the Unity environment.
        Pass
        {
            Name "ShadowCaster"
            Tags{"LightMode" = "ShadowCaster"}

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 texcoord     : TEXCOORD0;
            };

            struct Varyings
            {
                float2 uv           : TEXCOORD0;
                float4 positionCS   : SV_POSITION;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _ArrowColor;
                float4 _BackgroundColor;
                float4 _ShadowColor;
                float4 _ShadowOffset;
            CBUFFER_END

            float3 _LightDirection;

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                output.uv = TRANSFORM_TEX(input.texcoord, _MainTex);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
                #if UNITY_REVERSED_Z
                positionCS.z = min(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                positionCS.z = max(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif
                output.positionCS = positionCS;
                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_TARGET
            {
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                
                // Clip out the transparent background so only the solid arrow shape casts a shadow
                clip(texColor.a - 0.5);
                
                return 0;
            }
            ENDHLSL
        }
    }
}
