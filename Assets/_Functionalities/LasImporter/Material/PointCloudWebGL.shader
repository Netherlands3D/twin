Shader "Custom/PointCloudWebGL"
{
    Properties
    {
        _Tint("Tint", Color) = (1,1,1,1)
        _PointSize("Point Size", Float) = 6.0
    }
        SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }

        Pass
        {
            Cull Off
            ZWrite On
            ZTest LEqual

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Tint;
            float  _PointSize;

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

                // WebGL / GL path → set point size
                #if defined(SHADER_API_GLES) || defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)
                    gl_PointSize = _PointSize;
                #endif

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
