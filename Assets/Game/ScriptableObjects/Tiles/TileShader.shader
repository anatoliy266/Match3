Shader "Custom/Match3ThreeColor"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture (Grayscale)", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (0.5, 0.5, 0.5, 1)
        _HighlightColor ("Highlight Color", Color) = (1, 1, 1, 1)
        _ShadowColor ("Shadow Color", Color) = (0, 0, 0, 1)
    }

    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
            "RenderType"="Transparent" 
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            fixed4 _BaseColor;
            fixed4 _HighlightColor;
            fixed4 _ShadowColor;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // Сэмплируем ч/б текстуру маски фишки
                fixed4 texColor = tex2D(_MainTex, IN.texcoord);
                
                // Используем красный канал как карту яркости
                float value = texColor.r; 

                fixed3 finalColor;

                // Математика без пересвета: строгое разделение диапазонов через lerp
                if (value < 0.5)
                {
                    // От Тени (при 0.0) до Основы (при 0.5)
                    finalColor = lerp(_ShadowColor.rgb, _BaseColor.rgb, value * 2.0);
                }
                else
                {
                    // От Основы (при 0.5) до Блика (при 1.0)
                    finalColor = lerp(_BaseColor.rgb, _HighlightColor.rgb, (value - 0.5) * 2.0);
                }

                // Умножаем на альфу текстуры и на вертексную альфу (если фишка прозрачная/мигает)
                return fixed4(finalColor, texColor.a * IN.color.a);
            }
            ENDCG
        }
    }
}