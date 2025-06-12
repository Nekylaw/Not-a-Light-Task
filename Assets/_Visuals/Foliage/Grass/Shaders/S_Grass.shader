Shader "Custom/GrassWindAnimated"
{
    Properties
    {
        _ColorTop("Top Color", Color) = (1, 1, 0.3, 1)
        _ColorBottom("Bottom Color", Color) = (0.1, 0.4, 0.1, 1)
        _OldGrassHeight("Old Grass Height", Float) = 1
        _MinScale("Min Scale", Range(0, 1)) = 0.3
        _YOffset("Sway Y Offset", Float) = 0.0
        _FlowMap("Flow Map", 2D) = "gray" {}
        _FlowStrength("Flow Strength", Float) = 1.0
        _FlowMap_Scale("Flow Map Scale", Float) = 10.0  
        _FlowTime("Flow Time", Float) = 0.0
        _MatrixOffset("Matrix Offset", Int) = 0
        _AnimationDuration("Animation Duration", Float) = 1.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        Cull Off
        ZWrite On

        Pass
        {
            Name "GrassPass"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4 _ColorTop;
            float4 _ColorBottom;
            float _OldGrassHeight;
            float _MinScale;
            float _YOffset;
            float _FlowTime;
            float _FlowStrength;
            float _FlowMap_Scale;
            float _AnimationDuration;
            int _MatrixOffset;
            int _ClearZoneCount;

            StructuredBuffer<float4x4> _Matrices;
            StructuredBuffer<float4> _BaseScales;
            StructuredBuffer<float4> _ClearZones;
            StructuredBuffer<float> _AnimationStartTimes;

            TEXTURE2D(_FlowMap);
            SamplerState sampler_FlowMap
            {
                Filter = Point;
                AddressU = Wrap;
                AddressV = Wrap;
            }; 

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                uint instanceID : SV_InstanceID;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float heightRatio : TEXCOORD1;
                float animationProgress : TEXCOORD2;
                float worldPos : TEXCOORD3; 
            };

            // Smooth step function for easing animation
            float smoothstep3(float t)
            {
                return t * t * (3.0 - 2.0 * t);
            }

            // Enhanced easing function for more natural animation
            float easeOutCubic(float t)
            {
                return 1.0 - pow(1.0 - t, 3.0);
            }

            Varyings vert(Attributes v)
            {
                Varyings o;
                uint idx = v.instanceID + _MatrixOffset;
                float4x4 modelMatrix = _Matrices[idx];
                float3 baseScale = _BaseScales[idx].xyz;

                float3 worldPos = mul(modelMatrix, float4(v.positionOS, 1)).xyz;

                // Calculate distance factor based on clear zones
                float targetDistFactor = 0;
                for (int i = 0; i < _ClearZoneCount; i++)
                {
                    float3 zonePos = _ClearZones[i].xyz;
                    float radius = _ClearZones[i].w;
                    float d = distance(worldPos, zonePos);
                    float t = saturate(1.0 - d / radius);
                    targetDistFactor = max(targetDistFactor, t);
                }

                // Get animation start time for this instance
                float animStartTime = _AnimationStartTimes[idx];
                float currentTime = _FlowTime;
                
                // Calculate animation progress (0 to 1)
                float animationProgress = 0;
                float currentDistFactor = targetDistFactor;
                
                if (animStartTime > 0) // Animation has been triggered
                {
                    float elapsed = currentTime - animStartTime;
                    animationProgress = saturate(elapsed / _AnimationDuration);
                    
                    // Apply easing function for smooth animation
                    float easedProgress = easeOutCubic(animationProgress);
                    
                    // Interpolate from previous state to target state
                    // Note: This assumes we're animating from MinScale to target
                    currentDistFactor = lerp(0, targetDistFactor, easedProgress);
                }

                // Calculate final scale
                float scale = lerp(_MinScale, baseScale.x, currentDistFactor);

                // Apply scaling
                float3 scaled = v.positionOS;
                scaled.y = _YOffset + (scaled.y - _YOffset) * scale;
                scaled.xz *= scale;

                // Enhanced wind effect that responds to scale changes
                float2 flowUV = worldPos.xz / _FlowMap_Scale + float2(_FlowTime * 0.05, _FlowTime * 0.05);
                float2 flow = SAMPLE_TEXTURE2D_LOD(_FlowMap, sampler_FlowMap, flowUV, 0).rg;
                float2 flow2 = SAMPLE_TEXTURE2D_LOD(_FlowMap, sampler_FlowMap, flowUV * 7.2, 0).rg;
                float2 combinedFlow = (flow + flow2 * 0.5) / 1.5;
                float2 flowDir = normalize(combinedFlow * 2.0 - 1.0);
                
                // Wind strength varies with scale and animation progress
                float windMultiplier = 1.0 + (currentDistFactor * 0.5); // More wind when grass is larger
                float swayAmount = _FlowStrength * windMultiplier * max(0, scaled.y - _YOffset);
                
                // Add slight animation wobble during scaling
                if (animationProgress > 0 && animationProgress < 1)
                {
                    float wobble = sin(animationProgress * 3.14159 * 4) * 0.1 * (1 - animationProgress);
                    swayAmount += wobble;
                }
                
                scaled.xz += flowDir * swayAmount;

                // Final transformation
                float4 finalWorldPos = mul(modelMatrix, float4(scaled, 1));
                o.positionHCS = mul(UNITY_MATRIX_VP, finalWorldPos);
                o.uv = v.uv;
                o.heightRatio = saturate((v.positionOS.y - _YOffset) / _OldGrassHeight);
                o.animationProgress = animationProgress;
                o.worldPos = finalWorldPos.y; 
                
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 baseColor = lerp(_ColorBottom, _ColorTop, i.heightRatio);
                float randomSeed = frac(sin(dot(i.worldPos, float2(12.9898, 78.233))) * 1.4898);
                float colorVariation = lerp(0.9, 1.1, randomSeed );
                
                // Bright grass during animation
                if (i.animationProgress > 0 && i.animationProgress < 1)
                {
                    float brightness = 1.0 + (sin(i.animationProgress * 3.14159) * 10);
                    baseColor.rgb *= brightness;
                }
                
                return baseColor * colorVariation;
            }
            ENDHLSL
        }
    }
}