Shader "Custom/EarthClouds"
{
    Properties
    {
        _CloudTex ("Cloud Texture (RGBA)", 2D) = "white" {}
        _SunDir   ("Sun Direction", Vector) = (0,1,0,0)
        _Tint     ("Cloud Tint", Color) = (1,1,1,1)
        _Opacity  ("Opacity", Range(0,1)) = 0.5
        _MinNight ("Night Min Brightness", Range(0,1)) = 0.3
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 200
        Cull Back
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _CloudTex;
            float4 _CloudTex_ST;
            float4 _SunDir;
            fixed4 _Tint;
            float  _Opacity;
            float  _MinNight;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv        : TEXCOORD0;
                float3 normalWS  : TEXCOORD1;
                float4 pos       : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos      = UnityObjectToClipPos(v.vertex);
                o.uv       = TRANSFORM_TEX(v.uv, _CloudTex);
                o.normalWS = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 c = tex2D(_CloudTex, i.uv);
                float3 N = normalize(i.normalWS);
                float3 L = normalize(_SunDir.xyz);

                // 0..1 (1=gündüz)
                float ndl = saturate(dot(N, L));

                // --- PARLAKLIK (gündüzü boostla, gece tabanı küçük tut) ---
                // pow ile gündüzü öne çekersin; _MinNight gece tabanı.
                float intensity = lerp(_MinNight, 1.0, pow(ndl, 0.6));

                // --- OP AKLIK (gecede çok az, gündüzde yüksek) ---
                // terminator yakınında yumuşak artış: 0.15 altı neredeyse yok, 0.6 üstü tam
                float alphaCurve = smoothstep(0.15, 0.60, ndl);
                float a = c.a * _Opacity * _Tint.a * alphaCurve;

                // İsteğe bağlı: aşırı saydam bölgelerde tamamen kaybolmasın diye min clamp
                // a = max(a, 0.02);

                fixed3 rgb = c.rgb * _Tint.rgb * intensity;
                return fixed4(rgb, a);

            }
            ENDCG
        }
    }
}
