Shader "Unlit/FlowerBloom"
{
     Properties
    {
        _MainTex("Albedo", 2D) = "white" {}
        _Color("Color", Color) = (1,1,1,1)
        _MinScale("Min Scale", Range(0.1, 1)) = 0.3
        _MaxDistance("Max Distance", Float) = 10.0
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

            // mpb
            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _PlayerPos)
                UNITY_DEFINE_INSTANCED_PROP(float4, _BaseScale)
            UNITY_INSTANCING_BUFFER_END(Props)

            Varyings vert(Attributes v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                Varyings o;
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                float3 worldPos = TransformObjectToWorld(v.positionOS.xyz);
                float3 playerPos = UNITY_ACCESS_INSTANCED_PROP(Props, _PlayerPos).xyz;
                float3 baseScale = UNITY_ACCESS_INSTANCED_PROP(Props, _BaseScale).xyz;

                float dist = distance(worldPos, playerPos);
                float t = saturate(1.0 - dist / _MaxDistance);

                // float scale = lerp(_MinScale, 1.0, t);
                float scale = lerp(_MinScale, baseScale.x, t);
                v.positionOS.xyz *= scale;

                // Snap on ground
                float baseHeight = unity_ObjectToWorld._m11; 
                float yOffset = (1.0 - scale) * baseHeight;
                v.positionOS.y -= yOffset;


                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                texColor.a = 1;
                return texColor * _Color;
            }
            ENDHLSL
        }
    }
}