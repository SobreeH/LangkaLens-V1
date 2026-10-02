Shader "Custom/InsideOut360"
{
    Properties
    {
        _MainTex ("360 Texture", 2D) = "white" {}
        _InspectionFade ("Inspection Background Fade", Range(0, 1)) = 0
        _InspectionFadeColor ("Inspection Background Color", Color) = (0.32549, 0.22745, 0.17647, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Front // Renders the inside faces of the mesh

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            half _InspectionFade;
            fixed4 _InspectionFadeColor;

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                // Flip X UV so the equirectangular projection isn't mirrored
                o.uv = TRANSFORM_TEX(float2(1.0 - v.uv.x, v.uv.y), _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                fixed4 color = tex2D(_MainTex, i.uv);
                // Fade only the panorama toward the inspection canvas palette.
                color.rgb = lerp(color.rgb, _InspectionFadeColor.rgb, saturate(_InspectionFade));
                return color;
            }
            ENDCG
        }
    }
}
