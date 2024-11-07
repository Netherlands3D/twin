Shader "Custom/GeoJsonVisualizationShader"
{
    Properties 
    {
        _Color ("Main Color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID //for now good enough to use per vertex instance
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float instanceID : INSTANCEID; 
            };

            //default color
            fixed4 _Color;

            //float4 _SegmentColors[1023]; //lets keep this as fallback for incompatibility
            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _SegmentColors)
                //bind other buffer data properties here for segments
            UNITY_INSTANCING_BUFFER_END(Props)

            v2f vert(appdata v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // int segmentIndex = int(i.instanceID) % 1023; 
                // return _SegmentColors[segmentIndex]; 
                UNITY_SETUP_INSTANCE_ID(i);
                return UNITY_ACCESS_INSTANCED_PROP(Props, _SegmentColors); //auto buffer
            }
            ENDCG
        }
    }
}
