Shader "Custom/EarthDayNight"
{
    Properties
    {
        _DayTex ("Day Texture", 2D) = "white" {}
        _NightTex ("Night Texture", 2D) = "black" {}
        _SunDir ("Sun Direction", Vector) = (0, 1, 0, 0)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _DayTex;
            sampler2D _NightTex;
            float4 _SunDir;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float3 normalDir : TEXCOORD1;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.normalDir = normalize(mul(v.normal, (float3x3)unity_WorldToObject));
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float lightIntensity = saturate(dot(normalize(i.normalDir), normalize(_SunDir.xyz)));

                fixed4 dayColor = tex2D(_DayTex, i.uv);
                fixed4 nightColor = tex2D(_NightTex, i.uv);

                return lerp(nightColor, dayColor, lightIntensity);
            }
            ENDCG
        }
    }
}
