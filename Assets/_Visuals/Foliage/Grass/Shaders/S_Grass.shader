Shader "Custom/GrassWind"
{
    Properties
    {
        _ColorTop("Top Color", Color) = (1, 1, 0.3, 1)
        _ColorBottom("Bottom Color", Color) = (0.1, 0.4, 0.1, 1)
        _OldGrassHeight("Old Grass Height", Range(0,5)) = 1

        _MinScale("Min Scale", Range(0.1, 1)) = 0.3
        _MaxDistance("Max Growing Distance", Float) = 10.0

        _YOffset("Sway Y Offset", Float) = 0.0

        _FlowMap("Flow Map", 2D) = "gray" {}
        _FlowStrength("Flow Strength", Float) = 1.0
        _FlowMap_Scale("Flow Map Scale", Float) = 10.0  
        _FlowTime("Flow Time", Float) = 0.0

        _PlayerPos("Player Position", Vector) = (0,0,0,0)
        _MatrixOffset("Matrix Offset", Int) = 0
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

            Texture2D _FlowMap;
            SamplerState sampler_FlowMap;
            float _FlowStrength;
            float _FlowMap_Scale;
            float _FlowTime;

            float _YOffset;
            float3 _PlayerPos;
            int _MatrixOffset;

            float _MinScale;
            float _MaxDistance;

            StructuredBuffer<float4x4> _Matrices;
            StructuredBuffer<float4> _BaseScales;

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
            };

            Varyings vert(Attributes v)
            {
                Varyings o;

                uint idx = v.instanceID + _MatrixOffset;
                // float4x4 modelMatrix = _Matrices[idx];
                float4x4 modelMatrix = unity_ObjectToWorld;
                float3 baseScale = _BaseScales[idx].xyz;

                // World pos
                float3 worldPos = mul(modelMatrix, float4(v.positionOS, 1.0)).xyz;

                float3 scaled = v.positionOS;

                // float dist = distance(worldPos, _PlayerPos);
                // float t = saturate(dist / _MaxDistance);
                // float scale = lerp(_MinScale, baseScale.x, t);
                // scaled.y = _YOffset + (scaled.y - _YOffset) * scale;
                // scaled.xz *= scale;

                scaled.y = _YOffset + (scaled.y - _YOffset);
                // scaled.xz *= 1;

                // Wind sampling
                float2 flowUV = worldPos.xz / _FlowMap_Scale + float2(_FlowTime * 0.05, _FlowTime * 0.05);
                float2 flowTex = _FlowMap.SampleLevel(sampler_FlowMap, flowUV, 0).xy;
                float2 flowDir = normalize(flowTex * 2.0 - 1.0);

                float upperVertex = max(0, scaled.y - _YOffset);
                float2 sway = flowDir * _FlowStrength * upperVertex;
                
                scaled.x += sway.x;
                scaled.z += sway.y;

                // Gradient ratio
                float heightRatio = saturate( (v.positionOS.y - _YOffset) / _OldGrassHeight);
                o.heightRatio = heightRatio;

                float4 finalWorldPos = mul(modelMatrix, float4(scaled, 1.0));
                o.positionHCS = mul(UNITY_MATRIX_VP, finalWorldPos);
                o.uv = v.uv;

                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float4 col = lerp(_ColorBottom, _ColorTop, i.heightRatio);
                return col;
            }

            ENDHLSL
        }
    }
}
