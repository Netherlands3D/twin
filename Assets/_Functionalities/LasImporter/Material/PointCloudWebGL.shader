Shader "NL3D/PointCloudWebGL"
{
    Properties
    {
        _Tint("Tint", Color) = (1,1,1,1)
        _PointSize("Point Size (px)", Float) = 4.0
        _Alpha("Alpha", Range(0,1)) = 1.0

        // Multiplier applied only on WebGL builds, so you can make
        // WebGL points smaller or larger without affecting the Editor.
        _WebGLSizeMul("WebGL Size Multiplier", Float) = 0.7
    }

        SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

        // 🔥 Make point material double-sided
        Cull Off

        CGPROGRAM
        #pragma vertex vert
        #pragma fragment frag

        #include "UnityCG.cginc"

        fixed4 _Tint;
        float  _PointSize;
        float  _Alpha;
        float  _WebGLSizeMul;

        struct appdata
        {
            float4 vertex : POSITION;
            fixed4 color : COLOR;
        };

        struct v2f
        {
            float4 pos   : SV_POSITION;
            fixed4 color : COLOR;
            float  psize : PSIZE;
        };

        v2f vert(appdata v)
        {
            v2f o;
            o.pos = UnityObjectToClipPos(v.vertex);
            o.color = v.color * _Tint;
            o.color.a *= _Alpha;

            float size = _PointSize;

            // Platform-specific adjustment
            #if defined(UNITY_WEBGL) || defined(SHADER_API_GLES)
                size *= _WebGLSizeMul;
            #endif

            o.psize = size;
            return o;
        }

        fixed4 frag(v2f i) : SV_Target
        {
            return i.color;
        }
        ENDCG
    }
    }


        FallBack Off
}
