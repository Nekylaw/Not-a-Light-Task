Shader "Custom/PropLight"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _GlowColor ("Glow Color", Color) = (1,1,1,1)

        _GlowRadius ("Glow Radius", Float) = 0.2

        _BaseIntensity ("Base Intensity", Float) = 1.0
        _GlowIntensity ("Glow Intensity", Float) = 2.0

        _GlowAnimationSpeed ("Animation Speed", Float) = 2.0
        _GlowAnimationAmplitude ("Animation Amplitude", Float) = 0.2

        _GlowVisibilityThreshold ("Visibility distance", Float) = 20.0
        _MaxScreenDist  ("Max Screen Distance", Float) = 0.5
        _PlayerPosition ("Player World Position", Vector) = (0,0,0,0)
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha One
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float4 clipPos : TEXCOORD1;
            };

            float4 _BaseColor;
            float4 _GlowColor;

            float _GlowRadius;

            float _GlowIntensity;
            float _BaseIntensity;

            float _GlowAnimationSpeed;
            float _GlowAnimationAmplitude;

            float _GlowVisibilityThreshold;
            float _MaxScreenDist;

            float4 _PlayerPosition;

            v2f vert (appdata v)
            {
                v2f o;
                float4 world = mul(unity_ObjectToWorld, v.vertex);
                o.worldPos = world.xyz;
                o.clipPos = TransformWorldToHClip(world.xyz);
                o.vertex = o.clipPos;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                // Distance
                float dist = distance(i.worldPos, _PlayerPosition.xyz);
                float distRatio = saturate(1 - dist / _GlowVisibilityThreshold);

                // Screen focus
                float2 screenUV = (i.clipPos.xy / i.clipPos.w) * 0.5 + 0.5;
                float2 screenCenter = float2(0.5, 0.5);
                float screenDist = distance(screenUV, screenCenter);
                float screenFocus = saturate(1.0 - screenDist / _MaxScreenDist);

                float glowFactor = distRatio * screenFocus;

                // Animation
                float anim = sin(_Time.y * _GlowAnimationSpeed) * 0.5 + 0.5;
                float animIntensity = 1.0 + anim * _GlowAnimationAmplitude;

                // glow
                float glow = _GlowIntensity * glowFactor * animIntensity;

                float3 baseCol = _BaseColor.rgb * _BaseIntensity;
                float3 glowCol = _GlowColor.rgb * glow;

                return half4(baseCol + glowCol, glow);
            }

            ENDHLSL
        }
    }
}
