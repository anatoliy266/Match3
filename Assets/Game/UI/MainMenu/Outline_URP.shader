Shader "UI/Outline_Pulse"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (1,1,0,1)
        _OutlineThickness ("Outline Thickness", Range(0, 10)) = 2
        
        // Новые параметры для плавной анимации альфы
        _PulseSpeed ("Pulse Speed", Range(0, 10)) = 3.0
        _MaxAlpha ("Max Outline Alpha", Range(0, 1)) = 1.0

        // Обязательные параметры для интеграции с Unity UI
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
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

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [ZTest]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

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
                float4 worldPosition : TEXCOORD1;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _OutlineColor;
            float _OutlineThickness;
            float4 _MainTex_TexelSize;
            
            // Переменные для пульсации
            float _PulseSpeed;
            float _MaxAlpha;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(o.worldPosition);
                o.texcoord = v.texcoord;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 mainColor = tex2D(_MainTex, IN.texcoord) * IN.color;
                
                // Считаем толщину обводки
                float2 size = _MainTex_TexelSize.xy * _OutlineThickness;
                float alpha = mainColor.a;
                
                alpha += tex2D(_MainTex, IN.texcoord + float2(size.x, 0)).a;
                alpha += tex2D(_MainTex, IN.texcoord - float2(size.x, 0)).a;
                alpha += tex2D(_MainTex, IN.texcoord + float2(0, size.y)).a;
                alpha += tex2D(_MainTex, IN.texcoord - float2(0, size.y)).a;

                // Если пиксель пустой, но вокруг есть спрайт — это зона обводки
                if (mainColor.a < 0.1 && alpha > 0.1)
                {
                    // Вычисляем синусоиду времени: sin(_Time.y) выдает значения от -1 до 1.
                    // Переводим диапазон в 0..1 с помощью формулы (sin * 0.5 + 0.5)
                    float pulse = sin(_Time.y * _PulseSpeed) * 0.5 + 0.5;
                    
                    // Ограничиваем максимальную прозрачность до выбранного оптимального значения
                    pulse *= _MaxAlpha;

                    // Применяем альфу к итоговому цвету обводки
                    fixed4 finalOutline = _OutlineColor;
                    finalOutline.a *= pulse;
                    
                    return finalOutline;
                }

                return mainColor;
            }
            ENDCG
        }
    }
}
