Shader "Custom/FreshCreature"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1, 0, 1, 1)
        _BaseColorMultiplier ("Base Color Multiplier", Range(1, 10)) = 3
        _BodyAlpha ("Body Alpha", Range(0, 1)) = 0.8
        
        // Textures
        _MainTex ("Base Texture", 2D) = "white" {}
        _EyesTex ("Eyes Texture", 2D) = "black" {}
        _EyesScale ("Eyes Scale", Range(0.1, 10)) = 1
        _EyesOffsetX ("Eyes Offset X", Range(-1, 1)) = 0
        _EyesOffsetY ("Eyes Offset Y", Range(-1, 1)) = 0
        _EyesColor ("Eyes Color", Color) = (1, 1, 1, 1)
        _EyesIntensity ("Eyes Intensity", Range(0, 10)) = 3
        _EyesEmission ("Eyes Emission", Range(0, 5)) = 2
        
        // Cœur avec texture
        _HeartTex ("Heart Texture", 2D) = "white" {}
        _HeartCenter ("Heart Center", Vector) = (0, 0, 0, 0)
        _HeartRadius ("Heart Radius", Range(0, 1)) = 0.3
        _HeartColor ("Heart Color", Color) = (1, 0.8, 0.2, 1)
        _HeartIntensity ("Heart Intensity", Range(0, 15)) = 8
        _HeartBeatSpeed ("Heart Beat Speed", Range(0, 5)) = 1.5
        
        // Twirl parameters
        _HeartTwirlSpeed ("Heart Twirl Speed", Range(-20, 20)) = 1.0
        _HeartTwirlStrength ("Heart Twirl Strength", Range(0, 10)) = 3.0
        _HeartTextureScale ("Heart Texture Scale", Range(0.1, 5)) = 1.0
        _HeartTextureOffset ("Heart Texture Offset", Vector) = (0, 0, 0, 0)
        
        // Rim lighting
        _RimPower ("Rim Power", Range(1, 8)) = 3
        _RimColor ("Rim Color", Color) = (1, 1, 1, 1)
        _RimIntensity ("Rim Intensity", Range(0, 5)) = 2
    }
    
    SubShader
    {
        Tags { 
            "RenderType"="Opaque" 
            "Queue"="Geometry" 
            "RenderPipeline"="UniversalPipeline" 
        }
        LOD 200
        
        Pass
        {
            Name "MainPass"
            Tags { "LightMode"="UniversalForward" }
            
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On
            ZTest LEqual
            Cull Back
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
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
                // === BASE COLOR AVEC TEXTURE ===
                float4 baseTexture = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                float3 baseColor = baseTexture.rgb * _BaseColor.rgb * _BaseColorMultiplier;
                
                // === YEUX AVEC TEXTURE ET COULEUR ===
                // Transformer les UVs pour positionner et redimensionner les yeux
                float2 eyesUV = input.uv;
                
                // Centrer les UVs autour de 0.5
                eyesUV -= 0.5;
                
                // Appliquer le scale
                eyesUV /= _EyesScale;
                
                // Appliquer l'offset
                eyesUV.x += _EyesOffsetX;
                eyesUV.y += _EyesOffsetY;
                
                // Remettre dans l'espace 0-1
                eyesUV += 0.5;
                
                // CLAMP pour éviter les artefacts
                eyesUV = saturate(eyesUV);
                
                // Masque pour éviter les coupes nettes en dehors de 0-1
                float2 uvMask = step(0.0, eyesUV) * step(eyesUV, 1.0);
                float validUV = uvMask.x * uvMask.y;
                
                float4 eyesTexture = SAMPLE_TEXTURE2D(_EyesTex, sampler_EyesTex, eyesUV);
                eyesTexture.a *= validUV; // Masquer en dehors des UVs valides
                
                // Appliquer la couleur personnalisée aux yeux
                float3 eyesColor = eyesTexture.rgb * _EyesColor.rgb * _EyesIntensity;
                float eyesMask = eyesTexture.a; // Utilise l'alpha pour le masque
                
                // === CŒUR AVEC TEXTURE TWIRL ===
                float3 heartCenter = _HeartCenter.xyz;
                float distToHeart = distance(input.positionOS, heartCenter);
                
                // Battement cardiaque
                float time = _TimeParameters.x;
                float heartBeat = 0.2 + sin(time * _HeartBeatSpeed) * 0.3 + 1.0;
                float heartRadius = _HeartRadius  * heartBeat ;
                
                // Masque du cœur avec falloff plus doux
                float heartMask = 1.0 - saturate(distToHeart / heartRadius);
                heartMask = pow(heartMask, 2.0);
                
                // === TEXTURE TWIRL POUR LE CŒUR ===
                float3 heartColor = _HeartColor.rgb;
                float4 heartTexture = float4(1, 1, 1, 1);
                
                if (heartMask > 0.01) // Seulement si on est dans la zone du cœur
                {
                    // Calculer les coordonnées relatives au centre du cœur
                    float3 relativePos = input.positionOS - heartCenter;
                    
                    // Projection sur un plan perpendiculaire à la normale principale
                    // On peut utiliser XY, XZ ou YZ selon l'orientation souhaitée
                    float2 heartUV = relativePos.xy / heartRadius;
                    
                    // Distance du centre pour l'effet twirl
                    float distFromCenter = length(heartUV);
                    
                    // Angle twirl qui varie selon la distance et le temps
                    float twirlAngle = distFromCenter * _HeartTwirlStrength + time * _HeartTwirlSpeed;
                    
                    // Rotation des UVs
                    float cosAngle = cos(twirlAngle);
                    float sinAngle = sin(twirlAngle);
                    
                    float2 rotatedUV = float2(
                        heartUV.x * cosAngle - heartUV.y * sinAngle,
                        heartUV.x * sinAngle + heartUV.y * cosAngle
                    );
                    
                    // Appliquer le scale et l'offset de la texture
                    rotatedUV *= _HeartTextureScale;
                    rotatedUV += _HeartTextureOffset.xy;
                    
                    // Normaliser en UVs 0-1
                    rotatedUV = rotatedUV * 0.5 + 0.5;
                    
                    // Échantillonner la texture
                    heartTexture = SAMPLE_TEXTURE2D(_HeartTex, sampler_HeartTex, rotatedUV);
                    
                    // Combiner avec la couleur du cœur
                    heartColor = heartTexture.rgb * _HeartColor.rgb;
                    
                    // Optionnel : ajouter un effet de pulsation à la couleur
                    float pulse = sin(time * _HeartBeatSpeed * 2.0) * 0.2 + 1.0;
                    heartColor *= pulse;
                }
                
                // === RIM LIGHTING ===
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(input.viewDirWS);
                float fresnel = 1.0 - saturate(dot(viewDirWS, normalWS));
                float rim = pow(fresnel, _RimPower);
                
                // === ASSEMBLAGE FINAL ===
                // Commencer avec la base
                float3 finalColor = baseColor;
                
                // Ajouter les yeux (texture avec couleur)
                finalColor = lerp(finalColor, eyesColor, eyesMask);
                
                // Ajouter le cœur (avec texture twirl)
                finalColor = lerp(finalColor, heartColor, heartMask * 0.6);
                
                // Rim lighting
                finalColor += _RimColor.rgb * rim * _RimIntensity;
                
                // === ÉMISSION ===
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