Shader "Custom/VolumetricCloud"
{
    Properties
    {
        _Density ("Density", Range(0, 2)) = 1.0
        _Softness ("Softness", Range(0, 5)) = 1.5
        _EdgeFade ("Edge Fade", Range(0, 2)) = 1.0
        _CloudColor ("Cloud Color", Color) = (1, 1, 1, 1)
        _ShadowColor ("Shadow Color", Color) = (0.7, 0.8, 0.95, 1)
        _LightInfluence ("Light Influence", Range(0, 1)) = 0.5
        _AmbientLight ("Ambient Light", Range(0, 1)) = 0.3
        _NoiseScale ("Noise Scale", Range(0, 2)) = 1.0
        _NoiseStrength ("Noise Strength", Range(0, 1)) = 0.3
        _WindSpeed ("Wind Speed", Range(0, 2)) = 0.5
        _DepthFadeDistance ("Depth Fade Distance", Range(0, 50)) = 10.0
        _DepthFadeStrength ("Depth Fade Strength", Range(0, 1)) = 0.5
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent+100"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }
        
        Pass
        {
            Name "VolumetricCloud"
            Tags { "LightMode" = "UniversalForward" }
            
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile_instancing
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 viewDirWS : TEXCOORD2;
                float3 localPos : TEXCOORD3;
                float fogFactor : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            CBUFFER_START(UnityPerMaterial)
                float _Density;
                float _Softness;
                float _EdgeFade;
                float4 _CloudColor;
                float4 _ShadowColor;
                float3 _LightDir;
                float _LightInfluence;
                float _AmbientLight;
                float3 _WindDirection;
                float _WindSpeed;
                float _NoiseScale;
                float _NoiseStrength;
                float _DepthFadeDistance;
                float _DepthFadeStrength;
                float _TimeOffset;
            CBUFFER_END
            
            // Simple 3D noise function
            float hash(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }
            
            float noise3D(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                
                return lerp(
                    lerp(
                        lerp(hash(i + float3(0,0,0)), hash(i + float3(1,0,0)), f.x),
                        lerp(hash(i + float3(0,1,0)), hash(i + float3(1,1,0)), f.x),
                        f.y),
                    lerp(
                        lerp(hash(i + float3(0,0,1)), hash(i + float3(1,0,1)), f.x),
                        lerp(hash(i + float3(0,1,1)), hash(i + float3(1,1,1)), f.x),
                        f.y),
                    f.z);
            }
            
            float fbm(float3 p)
            {
                float value = 0.0;
                float amplitude = 0.5;
                float frequency = 1.0;
                
                for (int i = 0; i < 3; i++)
                {
                    value += amplitude * noise3D(p * frequency);
                    amplitude *= 0.5;
                    frequency *= 2.0;
                }
                
                return value;
            }
            
            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);
                
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.viewDirWS = GetWorldSpaceViewDir(positionInputs.positionWS);
                output.localPos = input.positionOS.xyz;
                output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                
                float3 viewDir = normalize(input.viewDirWS);
                float3 normal = normalize(input.normalWS);
                
                // Animated noise for cloud texture
                float time = _Time.y * _WindSpeed + _TimeOffset;
                float3 noisePos = input.positionWS * _NoiseScale * 0.1 + _WindDirection * time;
                float noiseValue = fbm(noisePos);
                
                // Base alpha from density
                float alpha = _Density;
                
                // Add noise variation for fluffy look
                alpha *= lerp(0.7, 1.0, noiseValue);
                
                // Fresnel effect - softer at edges (works for any mesh)
                float NdotV = abs(dot(normal, viewDir));
                float fresnel = pow(NdotV, _Softness * 0.5);
                alpha *= lerp(0.3, 1.0, fresnel);
                
                alpha = saturate(alpha);
                
                // Lighting - half lambert for soft look
                float NdotL = dot(normal, _LightDir) * 0.5 + 0.5;
                NdotL = lerp(0.5, NdotL, _LightInfluence);
                
                // Add noise variation to lighting for fluffy shadows
                NdotL *= lerp(0.8, 1.0, noiseValue);
                
                // Final color
                float3 finalColor = lerp(_ShadowColor.rgb, _CloudColor.rgb, NdotL);
                finalColor = lerp(finalColor, _CloudColor.rgb, _AmbientLight);
                
                // Rim lighting effect
                float rim = 1.0 - NdotV;
                rim = pow(rim, 2.0);
                finalColor += _CloudColor.rgb * rim * 0.2;
                
                // Apply fog
                finalColor = MixFog(finalColor, input.fogFactor);
                
                return half4(finalColor, alpha);
            }
            ENDHLSL
        }
    }
    
    FallBack "Universal Render Pipeline/Unlit"
}
