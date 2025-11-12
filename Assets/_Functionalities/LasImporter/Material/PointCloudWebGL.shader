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
            Cull Off
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
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
                float4 pos    : SV_POSITION;
                fixed4 color : COLOR;
            #if defined(SHADER_API_D3D11) || defined(SHADER_API_XBOXONE) || defined(UNITY_WINRT)
                float psize : PSIZE;
            #endif
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color * _Tint;
                o.color.a *= _Alpha;

            #if defined(UNITY_WEBGL) || defined(SHADER_API_GLES) || defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)
                gl_PointSize = _PointSize;
            #else
                o.psize = _PointSize;
            #endif
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
            #if defined(UNITY_WEBGL) || defined(SHADER_API_GLES) || defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)
                float2 uv = gl_PointCoord * 2.0 - 1.0;
                if (dot(uv, uv) > 1.0) discard; // round sprite
            #endif
                return i.color;
            }
            ENDCG
        }
    }
        FallBack Off
}