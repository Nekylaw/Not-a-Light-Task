Shader "Custom/GlowEffect"
{
    Properties
    {
        _GlowColor ("Glow Color", Color) = (1, 1, 1, 1)
        _GlowCenter ("Glow Center (UV)", Vector) = (0.5, 0.5, 0, 0)
        _GlowRadius ("Glow Radius", Float) = 0.2
        _GlowIntensity ("Glow Intensity", Float) = 2.0
    }
    
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Overlay" }
        Blend SrcAlpha One // Additive blending
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
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float4 _GlowColor;
            float4 _GlowCenter;
            float _GlowRadius;
            float _GlowIntensity;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex);
                o.uv = v.uv;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                float dist = distance(i.uv, _GlowCenter.xy);
                float glow = _GlowIntensity * saturate(1.0 - (dist / _GlowRadius));
                return half4(_GlowColor.rgb * glow, glow);
            }
            ENDHLSL
        }
    }
}
