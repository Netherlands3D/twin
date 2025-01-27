Shader "Custom/WorldPositionFromDepth"
{
    Properties
    {
        _CameraDepthTexture("Camera Depth Texture", 2D) = "white" {}
        _DepthColor("Depth Color", Color) = (0, 0, 0, 1)
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_CameraDepthTexture);
            SAMPLER(sampler_CameraDepthTexture);

            // Uniforms for inverse matrices (you will pass these from the script)
            float4x4 _CameraInvViewProjection; // Inverse of the camera's ViewProjection matrix
            float4 _MyScreenParams;            // Near and far planes (x = near, y = far)

            // Global shader variable for DepthColor (updated in fragment shader)
            float4 _DepthColor;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS);
                o.uv = v.uv;
                return o;
            }

            // Fragment Shader: Sample the depth and convert to world position
            half4 frag(Varyings i) : SV_Target
            {
                // Sample depth from the depth texture at the current UV coordinates
                float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, i.uv);

                // Normalize depth to the range [0, 1] using the near and far planes
                depth = LinearEyeDepth(depth, _MyScreenParams.x, _MyScreenParams.y);

                // Convert depth to clip space
                float4 clipSpace = float4(i.uv * 2.0 - 1.0, depth * 2.0 - 1.0, 1.0);

                // Convert from clip space to world space
                float4 worldPos = mul(_CameraInvViewProjection, clipSpace);
                worldPos /= worldPos.w; // Perspective divide

                // Return the world position in the RGB channels (for sampling)
                return half4(worldPos.xyz, 1.0);
            }

            // Function to convert from non-linear depth to linear depth
            float LinearEyeDepth(float depth, float near, float far)
            {
                // Converts the depth from non-linear to linear space
                return near * far / (far - (far - near) * depth);
            }

            ENDHLSL
        }
    }
}
