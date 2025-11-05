Shader "Custom/URPPointCloud"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (1,1,1,1)
        _PointSize("Point Size", Float) = 6.0
    }
        SubShader
    {
        Tags {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalRenderPipeline"
        }

        Pass
        {
            Name "Forward"
            Tags{"LightMode" = "UniversalForward"}

            Cull Off
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4 _BaseColor;
            float _PointSize;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 color       : COLOR;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionHCS = TransformWorldToHClip(positionWS);
                OUT.color = IN.color * _BaseColor;

                #if defined(SHADER_API_GLES) || defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)
                    gl_PointSize = _PointSize;
                #endif

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                return IN.color;
            }
            ENDHLSL
        }
    }
        FallBack Off
}
