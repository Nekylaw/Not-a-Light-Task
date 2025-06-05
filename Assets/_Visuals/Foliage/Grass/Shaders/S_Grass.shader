Shader "Custom/GrassWind"
{
    Properties
    {
        _ColorTop("Top Color", Color) = (1, 1, 0.3, 1)
        _ColorBottom("Bottom Color", Color) = (0.1, 0.4, 0.1, 1)
        _OldGrassHeight("Old Grass Height", Range(0,5)) = 1
        _MinScale("Min Scale", Range(0.1, 1)) = 0.3
        _YOffset("Sway Y Offset", Float) = 0.0
        _FlowMap("Flow Map", 2D) = "gray" {}
        _FlowStrength("Flow Strength", Float) = 1.0
        _FlowMap_Scale("Flow Map Scale", Float) = 10.0  
        _FlowTime("Flow Time", Float) = 0.0
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
            float _MinScale;
            float _YOffset;
            float _FlowTime;
            float _FlowStrength;
            float _FlowMap_Scale;
            int _MatrixOffset;
            int _ClearZoneCount;

            StructuredBuffer<float4x4> _Matrices;
            StructuredBuffer<float4> _BaseScales;
            StructuredBuffer<float4> _ClearZones;

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
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                uint idx = v.instanceID + _MatrixOffset;
                float4x4 modelMatrix = _Matrices[idx];
                float3 baseScale = _BaseScales[idx].xyz;

                float3 worldPos = mul(modelMatrix, float4(v.positionOS, 1)).xyz;

                float distFactor = 0;
                for (int i = 0; i < _ClearZoneCount; i++)
                {
                    float3 zonePos = _ClearZones[i].xyz;
                    float radius = _ClearZones[i].w;
                    float d = distance(worldPos, zonePos);
                    float t = saturate(1.0 - d / radius);
                    distFactor = max(distFactor, t);
                }

                float scale = lerp(_MinScale, baseScale.x, distFactor);

                float3 scaled = v.positionOS;
                scaled.y = _YOffset + (scaled.y - _YOffset) * scale;
                scaled.xz *= scale;

                // Wind
                float2 flowUV = worldPos.xz / _FlowMap_Scale + float2(_FlowTime * 0.05, _FlowTime * 0.05);
                // sample the flow map to get the flow direction
                float flow = SAMPLE_TEXTURE2D_LOD(_FlowMap, sampler_FlowMap, flowUV, 0); 
                float2 flowDir = normalize(flow * 2.0 - 1.0);
                float swayAmount = _FlowStrength * max(0, scaled.y - _YOffset);
                scaled.xz += flowDir * swayAmount;

                float4 finalWorldPos = mul(modelMatrix, float4(scaled, 1));
                o.positionHCS = mul(UNITY_MATRIX_VP, finalWorldPos);
                o.uv = v.uv;
                o.heightRatio = saturate((v.positionOS.y - _YOffset) / _OldGrassHeight);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                return lerp(_ColorBottom, _ColorTop, i.heightRatio);

                // return float4(0,0,1,1);
            }
            ENDHLSL
        }
    }
}
