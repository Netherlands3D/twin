// This Unity shader reconstructs the world space positions for pixels using a depth
// texture and screen space UV coordinates. the world position is then clipped to the bounding box of the mesh this shader is applied to to create a decal projection effect.
// instead of rendering a decal, the stencil buffer is set so that it can be used for masking in a next render pass
Shader "Netherlands3D/StencilDecal"
{
    Properties
    {
        _DecalTex ("Decal Texture", 2D) = "white" {}
        _AlphaClipThreshold ("Alpha Clip Threshold", Float) = 0.5
        _StencilRefValue("Stencil Reference Value", Int) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue"="Geometry" "RenderPipeline" = "UniversalPipeline"
        }
        
        ZWrite Off
        ZTest Off
//        ColorMask Off

        Pass
        {
            //stencil will be applied to all pixels that are not clipped in the end
            Stencil
            {
                Ref [_StencilRefValue]
                Comp Always
                Pass Replace
            }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // The Core.hlsl file contains definitions of frequently used HLSL
            // macros and functions, and also contains #include references to other
            // HLSL files (for example, Common.hlsl, SpaceTransforms.hlsl, etc.).
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // The DeclareDepthTexture.hlsl file contains utilities for sampling the
            // Camera depth texture.
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            sampler2D _DecalTex;
            float _AlphaClipThreshold;
            
            struct appdata
            {
                // The positionOS variable contains the vertex positions in object space.
                float4 positionOS : POSITION;
            };

            struct v2f
            {
                float4 positionHCS : SV_POSITION;
            };
            
            v2f vert(appdata i)
            {
                v2f o;
                // The TransformObjectToHClip function transforms vertex positions
                // from object space to homogenous clip space.
                o.positionHCS = TransformObjectToHClip(i.positionOS.xyz);
                return o;
            }

            // The fragment (pixel) shader definition.
            // The v2f input structure contains interpolated values from the
            // vertex shader. The fragment shader uses the `positionHCS` property
            // from the `v2f` struct to get locations of pixels.
            half4 frag(v2f i) : SV_Target
            {
                // To calculate the UV coordinates for sampling the depth buffer,
                // divide the pixel location by the render target resolution
                // _ScaledScreenParams.
                float2 UV = i.positionHCS.xy / _ScaledScreenParams.xy;

                // Sample the depth from the Camera depth texture.
                #if UNITY_REVERSED_Z
                    real depth = SampleSceneDepth(UV);
                #else
                // Adjust Z to match NDC for OpenGL ([-1, 1])
                real depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(UV)); //real is high precision floating point (similar to double)
                #endif

                // Reconstruct the world space positions.
                float3 worldPos = ComputeWorldSpacePosition(UV, depth, UNITY_MATRIX_I_VP);
                float3 opos = TransformWorldToObject(float4(worldPos, 1)).xyz;
                clip(float3(0.5, 0.5, 0.5) - abs(opos.xyz)); //clip pixels outside of the cube

                float2 topUV = opos.xz * 0.5 + 0.5; // Map from [-0.5, 0.5] to [0,1], project the texture top down
                float alpha = tex2D(_DecalTex, topUV).a;
                clip(alpha - _AlphaClipThreshold); //clip pixes that do not satisfy the alpha requirement

                return  half4(0,0,0,1); //no color is needed, but a value must be returned
            }
            ENDHLSL
        }
    }
}