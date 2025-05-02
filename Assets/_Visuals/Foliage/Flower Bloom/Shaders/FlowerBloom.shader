Shader "Custom/FlowerBloom"
{
    Properties
    {
        _MainTex("Albedo", 2D) = "white" {}
        _Color("Color", Color) = (1,1,1,1)
        _MinScale("Min Scale", Range(0.1, 1)) = 0.3
        _MaxDistance("Max Distance", Float) = 10.0

        _SwayMax("Sway Max", Float) = 0.05
        _Speed("Sway Speed", Float) = 1.0
        _Rigidness("Sway Rigidness", Float) = 5.0
        _YOffset("Sway Y Offset", Float) = 0.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;
            float4 _Color;
            float _MinScale;
            float _MaxDistance;

            float _SwayMax;
            float _Speed;
            float _Rigidness;
            float _YOffset;

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _PlayerPos)
                UNITY_DEFINE_INSTANCED_PROP(float4, _BaseScale)
            UNITY_INSTANCING_BUFFER_END(Props)

            Varyings vert(Attributes v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                Varyings o;
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                float3 playerPos = UNITY_ACCESS_INSTANCED_PROP(Props, _PlayerPos).xyz;
                float3 baseScale = UNITY_ACCESS_INSTANCED_PROP(Props, _BaseScale).xyz;

                float3 worldPos = TransformObjectToWorld(v.positionOS.xyz);
                float dist = distance(worldPos, playerPos);

                float t = saturate(1.0 - dist / _MaxDistance);
                float scale = saturate(lerp(_MinScale, baseScale.x, t));

                // Scale with pivot offset (snap to ground)
                float pivot = _YOffset;
                v.positionOS.y = pivot + (v.positionOS.y - pivot) * scale;
                v.positionOS.xz *= scale;

                // Sway
                float3 wpos = TransformObjectToWorld(v.positionOS.xyz);
                float heightFactor = max(0, v.positionOS.y - _YOffset);

                float swayX = sin(wpos.x / _Rigidness + (_Time.x * _Speed)) * heightFactor * _SwayMax * t;
                float swayZ = sin(wpos.z / _Rigidness + (_Time.x * _Speed)) * heightFactor * _SwayMax * t;

                v.positionOS.x += swayX;
                v.positionOS.z += swayZ;

                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                return texColor * _Color;
            }
            ENDHLSL
        }
    }
}
