Shader "Netherlands3D/PointCloudVertexColor"
{
    Properties
    {
        _PointSize ("Point Size", Float) = 4
        _PointSizeReferenceDistance ("Point Size Reference Distance", Float) = 200
        _MinPointSize ("Min Point Size", Float) = 1
        _MaxPointSize ("Max Point Size", Float) = 8
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        LOD 100

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode"="UniversalForward" }

            Cull Off
            ZWrite On
            ZTest LEqual
            Blend Off
            Offset -2, -2

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float _PointSize;
            float _PointSizeReferenceDistance;
            float _MinPointSize;
            float _MaxPointSize;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 corner : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 color : COLOR;
                float2 corner : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.color = input.color;

                float cameraDistance = max(distance(positionWS, _WorldSpaceCameraPos.xyz), 1.0);
                float distanceSize = _PointSize * (_PointSizeReferenceDistance / cameraDistance);
                float pointSize = clamp(distanceSize, _MinPointSize, _MaxPointSize);
                float4 centerHCS = TransformWorldToHClip(positionWS);
                float2 clipUnitsPerPixel = 2.0 / _ScreenParams.xy;
                centerHCS.xy += input.corner * pointSize * 0.5 * clipUnitsPerPixel * centerHCS.w;

                output.positionHCS = centerHCS;
                output.corner = input.corner;
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                clip(1.0 - dot(input.corner, input.corner));
                return input.color;
            }
            ENDHLSL
        }
    }
}
