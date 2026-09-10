Shader "Hidden/FocusLayerFX"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Offset ("Offset", Float) = 0
        _Color ("Color", Color) = (1,1,1,1)
    }

    SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            Name "KawaseBlur"

            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment FragKawase
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _Offset;

            half4 FragKawase(v2f_img i) : SV_Target
            {
                float2 offset = _Offset * _MainTex_TexelSize.xy;
                half4 color = tex2D(_MainTex, i.uv + float2(offset.x, offset.y));
                color += tex2D(_MainTex, i.uv + float2(offset.x, -offset.y));
                color += tex2D(_MainTex, i.uv + float2(-offset.x, offset.y));
                color += tex2D(_MainTex, i.uv + float2(-offset.x, -offset.y));
                return color * 0.25h;
            }
            ENDCG
        }

        Pass
        {
            Name "PremulBlend"
            Blend One OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment FragBlit
            #include "UnityCG.cginc"

            sampler2D _MainTex;

            half4 FragBlit(v2f_img i) : SV_Target
            {
                return tex2D(_MainTex, i.uv);
            }
            ENDCG
        }

        Pass
        {
            Name "FillColor"

            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment FragFill
            #include "UnityCG.cginc"

            float4 _Color;

            half4 FragFill(v2f_img i) : SV_Target
            {
                return _Color;
            }
            ENDCG
        }
    }

    FallBack Off
}
