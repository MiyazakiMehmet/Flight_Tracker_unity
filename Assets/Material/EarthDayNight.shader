Shader "Custom/EarthDayNight"
{
    Properties
    {
        _DayTex ("Day Texture", 2D) = "white" {}
        _NightTex ("Night Texture", 2D) = "black" {}
        _SunDir ("Sun Direction", Vector) = (0, 1, 0, 0)
        _EmissionStrength ("Emision Strength", Range(0,5)) = 1.0 
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
            float _EmissionStrength;

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
                float3 worldPos : TEXCOORD2;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.normalDir = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                //Emission Calculation for City Lights
                fixed4 dayColor = tex2D(_DayTex, i.uv);
                fixed4 nightColor = tex2D(_NightTex, i.uv);

                float3 N = normalize(i.normalDir);
                float3 L = normalize(_SunDir.xyz);

                //Angle between surface normal and sun direction to Calculate sun instensity
                float lightIntensity = saturate(dot(normalize(i.normalDir), normalize(_SunDir.xyz)));

                //Night Factor
                float nightFactor = 1.0 - lightIntensity;

                //Luminace of night texture (Brightness of Night Texture)
                float brightness = dot(nightColor.rgb, float3(0.299, 0.587, 0.114));

                //Let emission be active only night time and bright areas (e.g City Lights)
                fixed3 emission = nightColor.rgb * brightness * nightFactor * _EmissionStrength;

                //Let terminator to be smoother
                float blend = smoothstep(0.0, 0.8, lightIntensity);
                fixed3 finalColor = lerp(nightColor, dayColor, blend);

                

                //Add emission to the finalColor
                fixed3 finalRGB = finalColor + emission;

                return fixed4(finalRGB, 1.0);

            }
            ENDCG
        }
    }
}
