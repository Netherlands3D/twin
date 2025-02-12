Shader "Custom/WorldPositionFromDepth"
{
    Properties
    {
        _CameraDepthTexture ("Depth Texture", 2D) = "white" {}
    }
    SubShader
    {
        Pass
        {
            Tags { "LightMode" = "Always" }
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _CameraDepthTexture;
            float4x4 _CameraInvProjection;

            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = o.pos.xy * 0.5 + 0.5; // Normalize UVs
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);

                // Convert from screen space to world space
                float4 clipPos = float4(i.uv * 2.0 - 1.0, depth, 1.0);
                float4 worldPos = mul(_CameraInvProjection, clipPos);
                worldPos /= worldPos.w; // Perspective divide

                return worldPos; // Return world position as RGBA
            }
            ENDCG
        }
    }
}