Shader "Custom/VisualizeWorldPosition"
{
    Properties
    {
        _WorldPositionTexture ("World Position Texture", 2D) = "white" {}
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

            sampler2D _WorldPositionTexture;

            // Define a simple structure to pass vertex data
            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            // Simple vertex shader to create a full-screen quad
            v2f vert(float4 vertex : POSITION)
            {
                v2f o;
                o.pos = vertex; // Use the default vertex positions of the full screen quad
                o.uv = vertex.xy * 0.5 + 0.5; // Convert from [-1, 1] to [0, 1] for texture sampling
                return o;
            }

            // Fragment shader to visualize the world position data
            float4 frag(v2f i) : SV_Target
            {
                // Sample world position from the texture
                float4 worldPos = tex2D(_WorldPositionTexture, i.uv);
                
                // Colorize based on world position (for visualization purposes)
                return float4(worldPos.xyz * 0.5 + 0.5, 1.0); // Normalize to [0, 1] range for visualization
            }
            ENDCG
        }
    }
}