Shader "Custom/Creature"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1, 0, 1, 1)
        _BaseColorMultiplier ("Base Color Multiplier", Float) = 3
        _BodyAlpha ("Body Alpha", Range(0, 1)) = 0.8
        
        // Textures
        _MainTex ("Base Texture", 2D) = "white" {}
        _EyesTex ("Eyes Texture", 2D) = "black" {}
        _EyesScale ("Eyes Scale", Range(0.1, 10)) = 1
        _EyesOffsetX ("Eyes Offset X", Range(-1, 1)) = 0
        _EyesOffsetY ("Eyes Offset Y", Range(-1, 1)) = 0
        _EyesColor ("Eyes Color", Color) = (1, 1, 1, 1)
        _EyesIntensity ("Eyes Intensity", Float) = 3
        _EyesEmission ("Eyes Emission", Float) = 2
        
        // Hearrt
        _HeartTex ("Heart Texture", 2D) = "white" {}
        _HeartCenter ("Heart Center", Vector) = (0, 0, 0, 0)
        _HeartRadius ("Heart Radius", Range(0.1, 2)) = 0.3
        _HeartColor ("Heart Color", Color) = (1, 0.8, 0.2, 1)
        _HeartIntensity ("Heart Intensity", Float) = 8
        _HeartBeatSpeed ("Heart Beat Speed", Range(0.5, 5)) = 1.5
        
        // Twirl parameters
        _HeartTwirlSpeed ("Heart Twirl Speed", Range(-10, 10)) = 1.0
        _HeartTwirlStrength ("Heart Twirl Strength", Float) = 3.0
        _HeartTextureScale ("Heart Texture Scale", Range(0.1, 5)) = 1.0
        _HeartTextureOffset ("Heart Texture Offset", Vector) = (0, 0, 0, 0)
        
        // Rim lighting
        _RimPower ("Rim Power", Float) = 3
        _RimColor ("Rim Color", Color) = (1, 1, 1, 1)
        _RimIntensity ("Rim Intensity", Float) = 2
    }
    
    SubShader
    {
        Tags { 
            "RenderType"="Transparent" 
            "Queue"="Transparent-100" 
            "RenderPipeline"="UniversalPipeline" 
        }
        LOD 200
        
        // DepthOnly pass to write depth without color
        Pass
        {
            Name "DepthOnly"
            Tags 
            { 
                "LightMode"="DepthOnly"
            }
            
            ZWrite On
            ColorMask 0 // Don't write color, only depth
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
        
        // Render with transparency
        Pass
        {
            Name "MainPass"
            Tags { "LightMode"="UniversalForward" }
            
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off // Done in DepthOnly pass
            ZTest LEqual
            Cull Back
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_EyesTex);
            SAMPLER(sampler_EyesTex);
            TEXTURE2D(_HeartTex);
            SAMPLER(sampler_HeartTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _BaseColor;
                float _BaseColorMultiplier;
                float _BodyAlpha;
                
                float _EyesScale;
                float _EyesOffsetX;
                float _EyesOffsetY;
                float4 _EyesColor;
                float _EyesIntensity;
                float _EyesEmission;
                
                float4 _HeartCenter;
                float _HeartRadius;
                float4 _HeartColor;
                float _HeartIntensity;
                float _HeartBeatSpeed;
                float _HeartTwirlSpeed;
                float _HeartTwirlStrength;
                float _HeartTextureScale;
                float4 _HeartTextureOffset;
                
                float _RimPower;
                float4 _RimColor;
                float _RimIntensity;
            CBUFFER_END
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 positionOS : TEXCOORD2;
                float3 normalWS : TEXCOORD3;
                float3 viewDirWS : TEXCOORD4;
            };
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.positionOS = input.positionOS.xyz;
                
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);
                output.normalWS = normalInput.normalWS;
                output.viewDirWS = GetWorldSpaceViewDir(output.positionWS);
                                
                return output;
            }
            
            float4 frag(Varyings input) : SV_Target
            {
                // Base color
                float4 baseTexture = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                float3 baseColor = baseTexture.rgb * _BaseColor.rgb * _BaseColorMultiplier;
                
                // Eyes position
                float2 eyesUV = input.uv;
                
                // Center UVs
                eyesUV -= 0.5;
                
                // Scale + Offset
                eyesUV /= _EyesScale;
                eyesUV.x += _EyesOffsetX;
                eyesUV.y += _EyesOffsetY;
                
                // Normalize UVs to 0-1 range
                eyesUV += 0.5;
                eyesUV = saturate(eyesUV);
                                
                // Sample eyes texture
                float4 eyesTexture = SAMPLE_TEXTURE2D(_EyesTex, sampler_EyesTex, eyesUV);
                
                // Eyes color
                float3 eyesColor = eyesTexture.rgb * _EyesColor.rgb * _EyesIntensity;
                float eyesMask = eyesTexture.a; // Utilise l'alpha pour le masque
                
                // Heart twirl
                float3 heartCenter = _HeartCenter.xyz;
                float distToHeart = distance(input.positionOS, heartCenter);
                
                // Heart beat
                float time = _Time.x * 10;
                float heartBeat = sin(time * _HeartBeatSpeed) * 0.3 + 1.0;
                float heartRadius = _HeartRadius * heartBeat;
                
                // Heart mask
                float heartMask = 1.0 - saturate(distToHeart / heartRadius);
                heartMask = pow(heartMask, 2.0);
                
                // === TEXTURE TWIRL POUR LE CŒUR ===
                float3 heartColor = _HeartColor.rgb;
                float4 heartTexture = float4(1, 1, 1, 1);
                
                // If in heart zone
                if (heartMask > 0.01)
                {
                    float3 relativePos = input.positionOS - heartCenter;
                    
                    float2 heartUV = relativePos.xy / heartRadius;
                    
                    float distFromCenter = length(heartUV);
                    
                    // UVs rotation 
                    float twirlAngle = distFromCenter * _HeartTwirlStrength + time * _HeartTwirlSpeed;
                    float cosAngle = cos(twirlAngle);
                    float sinAngle = sin(twirlAngle);
                    
                    float2 rotatedUV = float2(
                        heartUV.x * cosAngle - heartUV.y * sinAngle,
                        heartUV.x * sinAngle + heartUV.y * cosAngle
                    );
                    
                    // Heart Scale
                    rotatedUV *= _HeartTextureScale;
                    rotatedUV += _HeartTextureOffset.xy;
                    
                    // Normalize
                    rotatedUV = rotatedUV * 0.5 + 0.5;
                    
                    // Sample heart texture
                    heartTexture = SAMPLE_TEXTURE2D(_HeartTex, sampler_HeartTex, rotatedUV);
                    
                    heartColor = heartTexture.rgb * _HeartColor.rgb;
                    
                    // Heart Pusle
                    float pulse = sin(time * _HeartBeatSpeed * 2.0) * 0.2 + 1.0;
                    heartColor *= pulse;
                }
                
                // Rim 
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(input.viewDirWS);
                float fresnel = 1.0 - saturate(dot(viewDirWS, normalWS));
                float rim = pow(fresnel, _RimPower);

                float3 finalColor = baseColor;
                finalColor = lerp(finalColor, eyesColor, eyesMask);
                finalColor = lerp(finalColor, heartColor, heartMask * 0.6);
                finalColor += _RimColor.rgb * rim * _RimIntensity;
                
                // Emission
                float3 emission = float3(0, 0, 0);
                emission += eyesColor * eyesMask * _EyesEmission;
                emission += heartColor * heartMask * _HeartIntensity;

                finalColor += emission;
                return float4(finalColor, _BodyAlpha);
            }
            ENDHLSL
        }
    }
    
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}