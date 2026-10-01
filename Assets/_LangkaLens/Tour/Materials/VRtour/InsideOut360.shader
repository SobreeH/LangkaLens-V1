Shader "Custom/InsideOut360"
{
    Properties
    {
        _MainTex ("360 Texture", 2D) = "white" {}
        _InspectionFade ("Inspection Background Fade", Range(0, 1)) = 0
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
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            half _InspectionFade;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                // Flip X UV so the equirectangular projection isn't mirrored
                o.uv = TRANSFORM_TEX(float2(1.0 - v.uv.x, v.uv.y), _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 color = tex2D(_MainTex, i.uv);
                color.rgb *= 1.0h - saturate(_InspectionFade);
                return color;
            }
            ENDCG
        }
    }
}
