Shader "Custom/Vortex"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _Center("Center (UV)", Vector) = (0.5, 0.5, 0, 0)
        _Strength("Strength", Float) = 0.1
        _Radius("Radius", Float) = 0.5
        _Speed("Speed", Float) = 2.0
        _AlphaCutoff("Alpha Cutoff", Range(0,1)) = 0.1
    }

    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
        LOD 100

        Pass
        {
            ZWrite On
            AlphaToMask On
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float2 _Center;
            float _Strength;
            float _Radius;
            float _Speed;
            float _AlphaCutoff;

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

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 delta = i.uv - _Center;
                float dist = length(delta);

                if (dist < _Radius)
                {
                    float angle = atan2(delta.y, delta.x);
                    float strengthFactor = (_Radius - dist) / _Radius;

                    float rotation = strengthFactor * _Strength;
                    float pull = strengthFactor * _Strength * 0.5;

                    angle += _Time.y * _Speed * rotation;

                    dist -= _Time.y * _Speed * pull;
                    dist = max(dist, 0.001);

                    angle += sin(_Time.y * 10 + dist * 30) * 0.05;

                    float2 offset = float2(cos(angle), sin(angle)) * dist;
                    i.uv = _Center + offset;
                }

                // fixed4 col =  tex2D(_MainTex, i.uv) ;
                // clip(col.a - _AlphaCutoff);

                // return col;

                fixed4 col = tex2D(_MainTex, i.uv);
float brightness = dot(col.rgb, float3(0.299, 0.587, 0.114)); // calcul de la luminance
clip(brightness - _AlphaCutoff); // coupe les pixels sombres
return fixed4(1,1,1,1); // ou "return col;" si tu veux la couleur d’origine
            }
            ENDCG
        }
    }
    FallBack "Unlit/Transparent Cutout"
}
