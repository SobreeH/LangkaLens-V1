Shader "Custom/InsideOut360"
{
    Properties
    {
        _MainTex ("360 Texture", 2D) = "white" {}
        _InspectionBlur ("Inspection Background Blur", Range(0, 1)) = 0
        [HideInInspector] _InspectionFade ("Legacy Inspection Fade", Range(0, 1)) = 0
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
            float4 _MainTex_TexelSize;
            half _InspectionBlur;

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
                if (_InspectionBlur > 0.001h)
                {
                    // Only the panorama loses focus. Its existing mip pyramid supplies the blur.
                    // Interpolate levels explicitly because the photos use bilinear filtering.
                    float maxMip = max(0.0, floor(log2(max(_MainTex_TexelSize.z, _MainTex_TexelSize.w))));
                    float mip = min(saturate(_InspectionBlur) * 7.0, maxMip);
                    float lowerMip = floor(mip);
                    fixed4 soft = lerp(tex2Dlod(_MainTex, float4(i.uv, 0, lowerMip)),
                        tex2Dlod(_MainTex, float4(i.uv, 0, min(lowerMip + 1.0, maxMip))), frac(mip));
                    // Ease out of the normal derivative-selected level without a pop.
                    color = lerp(color, soft, saturate(_InspectionBlur * 8.0h));
                }
                return color;
            }
            ENDCG
        }
    }
}
