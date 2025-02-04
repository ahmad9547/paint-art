Shader "Custom/PaintShader"
{
    Properties
    {
        _BaseTex ("Base Texture", 2D) = "white" {}
        _PaintTex ("Paint Texture", 2D) = "black" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _BaseTex;
            sampler2D _PaintTex;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 baseColor = tex2D(_BaseTex, i.uv);
                fixed4 paintColor = tex2D(_PaintTex, i.uv);

                // Blend paint over base texture (only if paint is not transparent)
                fixed4 finalColor = lerp(baseColor, paintColor, paintColor.a);

                return finalColor;
            }
            ENDCG
        }
    }
}
