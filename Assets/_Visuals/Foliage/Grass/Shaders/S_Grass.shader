Shader "Custom/GrassWind"
{
    Properties
    {
        _MainTex("Albedo", 2D) = "white" {}
        _AlphaTex("Alpha", 2D) = "white" {}
        _Color("Color", Color) = (1,1,1,1)

        _MinScale("Min Scale", Range(0.1, 1)) = 0.3
        _MaxDistance("Max Distance", Float) = 10.0
        _Cutoff("Alpha Cutoff", Range(0,1)) = 0.5

        _SwayMax("Sway Max", Float) = 0.05
        _YOffset("Sway Y Offset", Float) = 0.0

        _PlayerPos("Player Position", Vector) = (0,0,0,0)
        _MatrixOffset("Matrix Offset", Int) = 0
    }

    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
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

            // Textures
            TEXTURE2D(_MainTex);     
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_AlphaTex);   
            SAMPLER(sampler_AlphaTex);

            TEXTURE2D(_WindMap);     
            SAMPLER(sampler_WindMap);

            // Props
            float4 _MainTex_ST;
            float4 _Color;
            float _MinScale, _MaxDistance, _Cutoff;
            float _SwayMax, _YOffset;

            float3 _PlayerPos;
            int _MatrixOffset;
            float _WindMap_Scale; //  windScale from Wind compute

            // Buffers
            StructuredBuffer<float4x4> _Matrices;
            StructuredBuffer<float4> _BaseScale;

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
                float dist : TEXCOORD1;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                uint idx = v.instanceID + _MatrixOffset;
                float4x4 modelMatrix = _Matrices[idx];
                float3 baseScale = _BaseScale[idx].xyz;

                // World position
                float3 worldPos = mul(modelMatrix, float4(v.positionOS, 1.0)).xyz;
                float dist = distance(worldPos, _PlayerPos);
                o.dist = dist;

                float t = saturate(1.0 - dist / _MaxDistance);
                float scale = lerp(_MinScale, baseScale.x, t);

                // Apply scale
                float3 scaled = v.positionOS;
                float pivot = _YOffset;
                scaled.y = pivot + (scaled.y - pivot) * scale;
                scaled.xz *= scale;

                // Wind sampling
                float2 windUV = worldPos.xz / _WindMap_Scale;
                float4 windSample = SAMPLE_TEXTURE2D_LOD(_WindMap, sampler_WindMap, windUV, 0);
                float2 windDir = windSample.xy;
                float windStrength = windSample.z;

                float heightFactor = max(0, scaled.y - _YOffset);
                float2 sway = windDir * windStrength * heightFactor * _SwayMax * t;

                scaled.x += sway.x;
                scaled.z += sway.y;

                float4 finalWorldPos = mul(modelMatrix, float4(scaled, 1.0));
                o.positionHCS = mul(UNITY_MATRIX_VP, finalWorldPos);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                clip(_MaxDistance - i.dist); // Fade distance

                float4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                float4 alpha = SAMPLE_TEXTURE2D(_AlphaTex, sampler_AlphaTex, i.uv);
                clip(alpha.a - _Cutoff);

                return col * _Color;
            }
            ENDHLSL
        }
    }
}
