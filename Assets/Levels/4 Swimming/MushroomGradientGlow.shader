Shader "Custom/MushroomGradientGlow"
{
    Properties
    {
        _BottomColor ("Bottom Color", Color) = (1, 0.3, 0.1, 1)
        _TopColor ("Top Color", Color) = (1, 0.9, 0.2, 1)
        _EmissionStrength ("Emission Strength", Range(0, 10)) = 3
        _GradientHeight ("Gradient Height", Range(0, 1)) = 0.5
        _GradientSmoothness ("Gradient Smoothness", Range(0.01, 1)) = 0.3
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float height : TEXCOORD3;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BottomColor;
                float4 _TopColor;
                float _EmissionStrength;
                float _GradientHeight;
                float _GradientSmoothness;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);
                output.normalWS = normalInput.normalWS;
                
                output.uv = input.uv;
                
                // Use object space Y for consistent gradient
                output.height = input.positionOS.y;
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Normalize height based on typical mushroom bounds
                float normalizedHeight = saturate((input.height + 5.0) / 10.0);
                
                // Create smooth gradient
                float gradientFactor = smoothstep(_GradientHeight - _GradientSmoothness, 
                                                  _GradientHeight + _GradientSmoothness, 
                                                  normalizedHeight);
                
                // Blend between bottom and top colors
                half4 baseColor = lerp(_BottomColor, _TopColor, gradientFactor);
                
                // Get main light
                Light mainLight = GetMainLight();
                half3 lighting = mainLight.color * mainLight.distanceAttenuation;
                
                // Simple diffuse lighting
                half3 normalWS = normalize(input.normalWS);
                half NdotL = saturate(dot(normalWS, mainLight.direction));
                half3 diffuse = lighting * NdotL;
                
                // Add ambient lighting
                half3 ambient = half3(0.1, 0.1, 0.1);
                
                // Combine lighting with base color
                half3 litColor = baseColor.rgb * (diffuse + ambient);
                
                // Add strong emission to make it glow in darkness
                half3 emission = baseColor.rgb * _EmissionStrength;
                
                // Final color combines lit surface with emission
                half3 finalColor = litColor + emission;
                
                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
        
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            float3 _LightDirection;

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_TARGET
            {
                return 0;
            }
            ENDHLSL
        }
    }
    
    FallBack "Universal Render Pipeline/Lit"
}