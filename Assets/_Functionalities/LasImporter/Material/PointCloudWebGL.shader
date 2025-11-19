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
                float  psize : PSIZE;   // point size in pixels
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color * _Tint;
                o.color.a *= _Alpha;

                // Point size in pixels; PointCloudAutoSize can drive this
                o.psize = _PointSize;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Square points (fast & compatible).
                // If you want perfect round points later, we can switch
                // to instanced quads instead of MeshTopology.Points.
                return i.color;
            }
            ENDCG
        }
    }

        FallBack Off
}
