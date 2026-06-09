Shader "Netherlands3D/PointCloudVertexColor"
{
    Properties
    {
        _PointSize ("Point Size", Float) = 3
        _PointSizeReferenceDistance ("Point Size Reference Distance", Float) = 250
        _MinPointSize ("Min Point Size", Float) = 2
        _MaxPointSize ("Max Point Size", Float) = 14
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            HLSLPROGRAM
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
                half4 color : COLOR;
                float2 corner : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                half4 color : COLOR;
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
                float worldUnitsPerPixel = (2.0 * abs(centerHCS.w) / UNITY_MATRIX_P._m11) / _ScreenParams.y;
                float halfSizeWorld = worldUnitsPerPixel * pointSize * 0.5;

                float3 cameraRightWS = UNITY_MATRIX_I_V[0].xyz;
                float3 cameraUpWS = UNITY_MATRIX_I_V[1].xyz;
                float3 offsetWS = (cameraRightWS * input.corner.x + cameraUpWS * input.corner.y) * halfSizeWorld;

                output.positionHCS = TransformWorldToHClip(positionWS + offsetWS);
                output.corner = input.corner;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                clip(1.0 - dot(input.corner, input.corner));
                return input.color;
            }
            ENDHLSL
        }
    }
}
