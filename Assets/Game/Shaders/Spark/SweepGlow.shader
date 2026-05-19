Shader "Game/Spark/SweepGlow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1) // Связано напрямую со _spriteRenderer.color

        _SweepMin ("Sweep Min (Начало)", Float) = -13.0
        _SweepMax ("Sweep Max (Конец)", Float) = 11.0
        _SweepWidth ("Sweep Width (Ширина)", Float) = 4.0
        _SweepSoftness ("Sweep Softness (Мягкость)", Float) = 3.0
        _SweepSpeed ("Sweep Speed (Скорость)", Float) = 0.3
        _GlowColor ("Glow Color (Цвет блика)", Color) = (1,1,1,1)
        _GlowIntensity ("Glow Intensity (Яркость)", Float) = 1.5
    }

    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "RenderPipeline"="UniversalPipeline" 
            "Queue"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float3 positionWS : TEXCOORD1;
            };

            // ВАЖНО ДЛЯ URP SPRITES: _Color должен быть снаружи CBUFFER,
            // чтобы динамический цвет из SpriteRenderer.color не затирался в белый.
            half4 _Color;

            CBUFFER_START(UnityPerMaterial)
            float _SweepMin;
            float _SweepMax;
            float _SweepWidth;
            float _SweepSoftness;
            float _SweepSpeed;
            half4 _GlowColor;
            float _GlowIntensity;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                
                // Перемножаем цвет из SpriteRenderer и дефолтный тинт материала
                OUT.color = IN.color * _Color;
                
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // 1. Получаем пиксель текстуры
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                
                // 2. Применяем цвет из скрипта (теперь он точно применится)
                half4 color = texColor * IN.color;

                // 3. Логика движения полосы эффекта
                float sweepPos = lerp(_SweepMin, _SweepMax, frac(_Time.y * _SweepSpeed));
                
                // 4. Мировые координаты плитки
                float pixelSweep = IN.positionWS.x + IN.positionWS.y;
                float dist = abs(pixelSweep - sweepPos);
                
                // 5. Ограничиваем область блика
                float halfWidth = max(_SweepWidth * 0.5, 0.001);
                float glow = 1.0 - saturate(dist / halfWidth);
                
                // Настраиваем мягкость перехода блика
                glow = pow(glow, max(_SweepSoftness, 1.0));

                // 6. Накладываем блик поверх покрашенной плитки
                color.rgb += _GlowColor.rgb * _GlowIntensity * glow * color.a;

                return color;
            }
            ENDHLSL
        }
    }
}