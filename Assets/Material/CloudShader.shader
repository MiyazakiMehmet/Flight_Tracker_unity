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
                fixed4 c = tex2D(_CloudTex, i.uv);       // RGBA, A = bulut maskesi
                float  NdotL = saturate(dot(normalize(i.normalWS), normalize(_SunDir.xyz)));

                // Gündüz parlak, gece çok sönük olmasýn diye taban aydýnlýk:
                float lightFactor = lerp(_MinNight, 1.0, NdotL);

                fixed3 rgb = c.rgb * _Tint.rgb * lightFactor;
                float  a   = c.a   * _Opacity * _Tint.a;

                return fixed4(rgb, a);
            }
            ENDCG
        }
    }
}
