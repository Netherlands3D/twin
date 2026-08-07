Shader "Custom/CloudVolume"
{
    Properties
    {
        _CloudVolume ("Cloud Volume", 3D) = "white" {}
        _Density ("Density", Range(0,5)) = 1
        _StepSize ("Step Size", Range(0.005,0.05)) = 0.02
        _Color ("Cloud Color", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back


        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"


            sampler3D _CloudVolume;

            float _Density;
            float _StepSize;

            float4 _Color;


            struct appdata
            {
                float4 vertex : POSITION;
            };


            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 localPos : TEXCOORD0;
            };


            v2f vert(appdata v)
{
    v2f o;

    o.vertex =
        UnityObjectToClipPos(v.vertex);

    o.localPos = v.vertex.xyz;

    return o;
}

            // Ray-box intersection
            float2 IntersectBox(
                float3 rayOrigin,
                float3 rayDir
            )
            {
                float3 boxMin = -0.5;
                float3 boxMax = 0.5;


                float3 tMin =
                    (boxMin-rayOrigin)
                    /
                    rayDir;


                float3 tMax =
                    (boxMax-rayOrigin)
                    /
                    rayDir;


                float3 t1 =
                    min(tMin,tMax);

                float3 t2 =
                    max(tMin,tMax);


                float near =
                    max(
                        max(t1.x,t1.y),
                        t1.z
                    );


                float far =
                    min(
                        min(t2.x,t2.y),
                        t2.z
                    );


                return float2(
                    near,
                    far
                );
            }



            fixed4 frag(v2f i) : SV_Target
            {

                float3 rayOrigin =
    mul(
        unity_WorldToObject,
        float4(_WorldSpaceCameraPos,1)
    ).xyz;

                float3 rayEnd =
    i.localPos;
                float3 rayDir =
    normalize(rayEnd - rayOrigin);


                float2 hit =
                    IntersectBox(
                        rayOrigin,
                        rayDir
                    );


                if(hit.x > hit.y)
                    discard;



                float start = max(hit.x, 0.0);
float end = hit.y;

float3 rayPos =
    rayOrigin + rayDir * start;

float rayLength =
    end - start;



                float density =
                    0;

float traveled = 0;

float3 step = rayDir * _StepSize;

[loop]
for(int i = 0; i < 128; i++)
{
    if(traveled > rayLength)
        break;

    float3 uv = rayPos + 0.5;

    float sample =
        tex3D(
            _CloudVolume,
            uv
        ).r;


    if(sample > 0.01)
    {
        sample *= _Density;

        density +=
            sample * (1 - density);

        if(density > 0.95)
            break;
    }


    rayPos += step;
    traveled += _StepSize;
}



                return float4(_Color.rgb, density);
            }

            ENDCG
        }
    }
}