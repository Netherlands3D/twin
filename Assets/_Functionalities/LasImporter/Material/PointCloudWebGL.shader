Shader "NL3D/PointCloudWebGL"
{
    Properties
    {
        _Tint("Tint", Color) = (1,1,1,1)
        _PointSize("Point Size (px)", Float) = 3.0
        _Alpha("Alpha", Range(0,1)) = 1.0
    }

        SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Tint;
            float  _PointSize;
            float  _Alpha;

            struct appdata
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 pos   : SV_POSITION;
                fixed4 color : COLOR;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color * _Tint;
                o.color.a *= _Alpha;

                #if defined(UNITY_WEBGL) || defined(SHADER_API_GLES) || defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)
                // Set point size per-vertex in WebGL
                gl_PointSize = _PointSize;
            #endif

            return o;
        }

        fixed4 frag(v2f i) : SV_Target
        {
            #if defined(UNITY_WEBGL) || defined(SHADER_API_GLES) || defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)
            // Make points round
            float2 uv = gl_PointCoord * 2.0 - 1.0;
            if (dot(uv, uv) > 1.0)
                discard;
        #endif

        return i.color;
    }
    ENDCG
}
    }

        FallBack Off
}
