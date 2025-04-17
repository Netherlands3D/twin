Shader "Custom/Transparency_Dithering_Shading"
{
    //this shader tries to fake transparency on the geometry render queue
    //a first pass to accumulate the pixels dither and blur
    //and a final pass to blend everything together

    //todo, there are some clipping issues still and lighting is still incorrect
    Properties
    {
        _Color("Color", Color) = (1, 1, 1, 1)
        _Alpha("Alpha", Range(0, 1)) = 1
        _UseGaussianBlur("Use Gaussian Blur", Float) = 1.0
    }

    SubShader
    {
        Tags { "Queue"="Geometry" "RenderType"="Opaque" }
        LOD 200
        Cull Off

        Pass
        {
            Name "Accum"
            ZWrite On
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            float _Alpha;
            float _UseGaussianBlur;

            static const float thresholdDivisor = 16.0;
            static const float thresholdMatrix[16] = {
                0.0/thresholdDivisor, 8.0/thresholdDivisor, 2.0/thresholdDivisor, 10.0/thresholdDivisor,
                12.0/thresholdDivisor, 4.0/thresholdDivisor, 14.0/thresholdDivisor, 6.0/thresholdDivisor,
                3.0/thresholdDivisor, 11.0/thresholdDivisor, 1.0/thresholdDivisor, 9.0/thresholdDivisor,
                15.0/thresholdDivisor, 7.0/thresholdDivisor, 13.0/thresholdDivisor, 5.0/thresholdDivisor
            };

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
                float4 color : COLOR;
                float2 screenUV : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.color = v.color * _Color;
                o.screenUV = o.pos.xy / o.pos.w;
                return o;
            }

            float random(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            float gaussianBlur(float2 screenPos)
            {
                float3 sum = 0;
                float2 offsets[9] = {
                    float2(-1,  1), float2(0,  1), float2(1,  1),
                    float2(-1,  0), float2(0,  0), float2(1,  0),
                    float2(-1, -1), float2(0, -1), float2(1, -1)
                };

                float weights[9] = {
                    0.075, 0.125, 0.075,
                    0.125, 0.200, 0.125,
                    0.075, 0.125, 0.075
                };

                for (int i = 0; i < 9; i++)
                {
                    float2 offset = offsets[i] * 0.01;
                    sum += random(screenPos + offset) * weights[i];
                }

                return sum / 9.0;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float alpha = saturate(i.color.a * _Alpha);
                float2 screenPos = i.screenUV * 0.5 + 0.5;
                float noise = random(screenPos);

                if (_UseGaussianBlur > 0.5)
                {
                    noise = gaussianBlur(screenPos);
                }

                if (alpha < noise) discard;

                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float diff = max(dot(i.worldNormal, lightDir), 0.0);
                float3 diffuse = diff * float3(1, 1, 1);

                float3 shadedColor = (diffuse) * i.color.rgb;
                return float4(shadedColor, alpha);
            }
            ENDCG
        }

        Pass
        {
            Name "Final"
            ZWrite On
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragFinal
            #include "UnityCG.cginc"

            fixed4 _Color;

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 color : COLOR;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color * _Color;
                return o;
            }

            fixed4 fragFinal(v2f i) : SV_Target
            {
                return i.color;
            }
            ENDCG
        }
    }

    FallBack "Diffuse"
}