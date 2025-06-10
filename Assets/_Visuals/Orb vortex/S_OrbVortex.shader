Shader "Custom/OrbVortex"
{
    Properties
    {
        _MainTex("Main Texture", 2D) = "white" {}
        _NoiseTex("Noise Texture", 2D) = "white" {}
        _SceneTex("Scene Color", 2D) = "white" {} 
        _Radius("Radius", Float) = 1.0
        _NoiseScale("Noise Scale", Float) = 1.0
        _NoiseSpeed("Noise Speed", Float) = 1.0
        _NoiseColor("Noise Color", Color) = (1,1,1,1)
        _Intensity("Intensity", Float) = 1.0
        _DistortionStrength("Distortion", Float) = 0.05
        _DistortionLength("Distortion Length", Float) = 0.1
        _TimeSpeed("Time Speed", Float) = 1.0
        _MainColor("Main Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            Name "OrbVortex"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            TEXTURE2D(_MainTex);     SAMPLER(sampler_MainTex);
            TEXTURE2D(_NoiseTex);    SAMPLER(sampler_NoiseTex);
            TEXTURE2D(_SceneTex);    SAMPLER(sampler_SceneTex);
            
            float4 _MainTex_ST;
            float _Radius;
            float _NoiseScale;
            float _NoiseSpeed;
            float _Intensity;
            float _DistortionStrength;
            float _DistortionLength;
            float _TimeSpeed;
            float4 _MainColor;
            float4 _NoiseColor;
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };
            
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float4 screenPos   : TEXCOORD1;
            };
            
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.screenPos = OUT.positionHCS;
                return OUT;
            }
            
            half4 frag(Varyings IN) : SV_Target
            {
                float2 uvCentered = IN.uv * 2.0 - 1.0;
                float dist = length(uvCentered);
                float angle = atan2(uvCentered.y, uvCentered.x);
                
                float rotationSpeed = _TimeSpeed * 0.5;
                float2x2 rotMatrix = float2x2(cos(rotationSpeed * _Time.y), -sin(rotationSpeed * _Time.y),
                                             sin(rotationSpeed * _Time.y), cos(rotationSpeed * _Time.y));
                float2 rotatedUV = mul(rotMatrix, uvCentered);
                
                // Swirl distortion
                float swirl = sin(angle * _DistortionLength + _Time.y * _TimeSpeed) * _DistortionStrength;
                
                // Noise
                float2 noiseUV = (rotatedUV * _NoiseScale * 0.5 + 0.5) + float2(_Time.y * _NoiseSpeed, 0);
                noiseUV = saturate(noiseUV);
                float noise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUV).r;
                
                noise = 1.0 - noise;
                // color noise
                noise = lerp(1.0, noise, _NoiseColor.r);
                
                float alpha = saturate(1.0 - dist / _Radius);
                alpha *= noise * _Intensity;
                
                float4 mainTexColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                
                float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
                screenUV = screenUV * 0.5 + 0.5;
                screenUV += swirl * alpha;
                float4 sceneColor = SAMPLE_TEXTURE2D(_SceneTex, sampler_SceneTex, screenUV);
                
                float4 color = mainTexColor * _MainColor;
                color.rgb = lerp(sceneColor.rgb, color.rgb, alpha);
                color.a = alpha;
                
                return color;
            }
            ENDHLSL
        }
    }
}