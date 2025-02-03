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
            float4x4 _CameraInvProjectionArray[4];

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
                 // Choose the correct matrix based on pixel position
                int index = (i.uv.x > 0.5) + (i.uv.y > 0.5) * 2; // 0:BL, 1:BR, 2:TL, 3:TR
                float4x4 invProj = _CameraInvProjectionArray[index];

                // Sample depth texture
                float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);

                // Convert from screen space to world space
                float4 clipPos = float4(i.uv * 2.0 - 1.0, depth, 1.0);
                float4 worldPos = mul(invProj, clipPos);
                worldPos /= worldPos.w;

                return worldPos; // Output world position
            }
            ENDCG
        }
    }
}