Shader "Custom/WorldPositionFromDepth"
{
    Properties
    {
        _CameraDepthTexture ("Depth Texture", 2D) = "white" { }
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _CameraDepthTexture;
            float4x4 _CameraInvProjection;  // Inverse of the camera's projection matrix
            float4x4 _CameraInvView;        // Inverse of the camera's view matrix

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float3 GetWorldPosition(float2 uv)
            {
                // Sample the depth value
                float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv);

                // Convert from screen space to normalized device coordinates
                float4 screenPos = float4(uv * 2.0 - 1.0, depth, 1.0);

                // Convert from normalized device coordinates to camera space (view space)
                float4 viewPos = mul(_CameraInvProjection, screenPos);
                viewPos /= viewPos.w; // Homogeneous division

                // Convert from camera space to world space
                float4 worldPos = mul(_CameraInvView, viewPos);

                return worldPos.xyz;
            }

            half4 frag(v2f i) : SV_Target
            {
                // Calculate world position from depth
                float3 worldPos = GetWorldPosition(i.uv);

                return half4(worldPos, 1.0);
            }
            ENDCG
        }
    }
}