Shader "Custom/Fog"
{
    Properties
    {
        _Color("Color", Color) = (1, 1, 1, 1)
        _MaxDistance("Max distance", float) = 100
        _StepSize("Step size", Range(0.1, 20)) = 1
        _DensityMultiplier("Density multiplier", Range(0, 10)) = 1
        _NoiseOffset("Noise offset", float) = 0
        
        _Speed("Speed", Range(0, 10)) = 1
        _Direction("Direction", float) = 1

        _FogNoise("Fog noise", 3D) = "white" {}
        _NoiseTiling("Noise tiling", float) = 1
        _DensityThreshold("Density threshold", Range(0, 1)) = 0.1
        _FogClearAttenuation("Fog Clear Attenuation", Range(0, 10)) = 1
        
        [HDR]_LightContribution("Light contribution", Color) = (1, 1, 1, 1)
        _LightScattering("Light scattering", Range(-1, 1)) = 0.2
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            float4 _Color;
            float _MaxDistance;
            float _DensityMultiplier;
            float _StepSize;

            float _Speed;
            float _Direction;

            float _NoiseOffset;
            TEXTURE3D(_FogNoise);
            float _NoiseTiling;
            float _DensityThreshold;
            float _FogClearAttenuation;

            float4 _LightContribution;
            float _LightScattering;

            int _ClearZoneCount; 
            StructuredBuffer<float4> _ClearZonesPositions; // xyz = position, w = radius
            StructuredBuffer<float3> _ClearZonesAnimations; // x = target radius, y = anim speed, z = start time

            float henyey_greenstein(float angle, float scattering)
            {
                return (1.0 - angle * angle) / (4.0 * PI * pow(1.0 + scattering * scattering - (2.0 * scattering) * angle, 1.5f));
            }
            
            float get_density(float3 worldPos)
            {
                float delta = _Time.y;
                float startTime = delta;

                float3 fogMovement = _Direction * delta * _Speed; 
                float3 fogPosAnim = worldPos * 0.01 * _NoiseTiling + fogMovement;

                float4 noise = _FogNoise.SampleLevel(sampler_TrilinearRepeat, fogPosAnim, 0);
                float density = dot(noise, noise);
                density = saturate(density - _DensityThreshold) * _DensityMultiplier;

                 // Clear fog zones
                for (int i = 0; i < _ClearZoneCount; i++)
                {
                    float3 clearPos = _ClearZonesPositions[i].xyz;  
                    float startRadius = _ClearZonesPositions[i].w;

                    float targetRadius = _ClearZonesAnimations[i].x;
                    float animSpeed = _ClearZonesAnimations[i].y;
                    float startTime = _ClearZonesAnimations[i].z;

                    // Cleared zones
                    float elapsedTime = delta - startTime;                   
                    float fogDir = sign(targetRadius - startRadius);
                    float currentRadius = startRadius + animSpeed * elapsedTime * fogDir;
                    currentRadius = clamp(currentRadius, min(startRadius, targetRadius), max(startRadius,targetRadius));

                    float dist = distance(worldPos, clearPos); 
                    float attenuation = smoothstep(0, currentRadius * _FogClearAttenuation, dist);

                    density *= attenuation;
                }

                return density;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float4 col = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, IN.texcoord);
                float depth = SampleSceneDepth(IN.texcoord);
                float3 worldPos = ComputeWorldSpacePosition(IN.texcoord, depth, UNITY_MATRIX_I_VP);

                float3 entryPoint = _WorldSpaceCameraPos;
                float3 viewDir = worldPos - _WorldSpaceCameraPos;
                float viewLength = length(viewDir);
                float3 rayDir = normalize(viewDir);

                float2 pixelCoords = IN.texcoord * _BlitTexture_TexelSize.zw;
                float distLimit = min(viewLength, _MaxDistance);
                float distTravelled = InterleavedGradientNoise(pixelCoords, (int)(_Time.y / max(HALF_EPS, unity_DeltaTime.x))) * _NoiseOffset;
                float transmittance = 1;
                float4 fogCol = _Color;

                while(distTravelled < distLimit)
                {
                    float3 rayPos = entryPoint + rayDir * distTravelled;
                    float density = get_density(rayPos);
                    if (density > 0)
                    {
                        Light mainLight = GetMainLight(TransformWorldToShadowCoord(rayPos));
                        fogCol.rgb += mainLight.color.rgb * _LightContribution.rgb * henyey_greenstein(dot(rayDir, mainLight.direction), _LightScattering) * density * mainLight.shadowAttenuation * _StepSize;
                        // transmittance *= exp(-density * _StepSize); 
                        transmittance *= pow(exp(-density * _StepSize), 2);
                    }
                    distTravelled += _StepSize;
                }
                
                return lerp(col, fogCol, 0.99 - saturate(transmittance));
            }
            ENDHLSL
        }
    }
}