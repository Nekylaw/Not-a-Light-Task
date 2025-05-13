Shader "Custom/FoliageBloom"
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
        _Speed("Sway Speed", Float) = 1.0
        _Rigidness("Sway Rigidness", Float) = 5.0
        _YOffset("Sway Y Offset", Float) = 0.0

        _PlayerPos("Player Position", Vector) = (0,0,0,0)
        _MatrixOffset("Matrix Offset", Int) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "TransparentCutout" "Queue" = "Geometry" }
        LOD 100
        Cull Off
        ZWrite On

        Pass
        {
            Name "Foliage"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

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

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_AlphaTex); SAMPLER(sampler_AlphaTex);

            float4 _MainTex_ST;
            float4 _Color;
            float _MinScale, _MaxDistance, _Cutoff;
            float _SwayMax, _Speed, _Rigidness, _YOffset;
            float3 _PlayerPos;
            int _MatrixOffset;

            StructuredBuffer<float4x4> _Matrices;
            StructuredBuffer<float4> _BaseScale;

            Varyings vert(Attributes v)
            {
                Varyings o;
                uint idx = v.instanceID + _MatrixOffset;

                float4x4 modelMatrix = _Matrices[idx];
                float3 baseScale = _BaseScale[idx].xyz;

                float3 worldPos = mul(modelMatrix, float4(v.positionOS, 1.0)).xyz;
                float dist = distance(worldPos, _PlayerPos);
                o.dist = dist;

                float t = saturate(1.0 - dist / _MaxDistance);
                float scale = lerp(_MinScale, baseScale.x, t);

                float3 scaled = v.positionOS;
                float pivot = _YOffset;
                scaled.y = pivot + (scaled.y - pivot) * scale;
                scaled.xz *= scale;

                float heightFactor = max(0, scaled.y - _YOffset);
                float swayX = sin(worldPos.x / _Rigidness + (_Time.x * _Speed)) * heightFactor * _SwayMax * t;
                float swayZ = sin(worldPos.z / _Rigidness + (_Time.x * _Speed)) * heightFactor * _SwayMax * t;

                scaled.x += swayX;
                scaled.z += swayZ;

                float4 finalWorldPos = mul(modelMatrix, float4(scaled, 1.0));
                o.positionHCS = mul(UNITY_MATRIX_VP, finalWorldPos);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                clip(_MaxDistance - i.dist); 

                float4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                float4 alpha = SAMPLE_TEXTURE2D(_AlphaTex, sampler_AlphaTex, i.uv);
                clip(alpha.a - _Cutoff); 

                return col * _Color;
            }
            ENDHLSL
        }
    }
}
