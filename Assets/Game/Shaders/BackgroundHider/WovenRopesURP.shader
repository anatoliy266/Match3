Shader "Game/Spark/WovenRopesURP_Wool"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _LineTex ("LineTex", 2D) = "white" {}
        Count ("Count (Количество нитей)", Float) = 100.0 
        RopeBrightness ("Rope Brightness", Range(0, 1)) = 0.5
        PatternStrength ("Pattern Strength", Range(0, 2)) = 1.0
        NoiseScale ("NoiseScale", Float) = 5.0
        NoiseStrength ("NoiseStrength", Float) = 1.0
        AlphaClip ("AlphaClip", Range(0,1)) = 0.5
        AlphaDensity ("AlphaDensity", Float) = 1.0
        RopeTiling ("RopeTiling", Float) = 2.0
        Z_Entangle ("Z Entangle Frequency", Float) = 25.0 
        Z_WeaveStrength ("Z Weave Strength", Range(0, 5)) = 2.0 
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
            #pragma target 3.0

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
            };

            half4 _Color;

            CBUFFER_START(UnityPerMaterial)
            float Count;
            float RopeBrightness;
            float PatternStrength;
            float NoiseScale;
            float NoiseStrength;
            float AlphaClip;
            float AlphaDensity;
            float RopeTiling;
            float Z_Entangle;
            float Z_WeaveStrength; 
            CBUFFER_END

            TEXTURE2D(_RopeDataTex);
            SAMPLER(sampler_RopeDataTex);

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_LineTex);
            SAMPLER(sampler_LineTex);

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 finalRopeColor = float3(0.0, 0.0, 0.0);
                float finalRopeAlpha = 0.0;
                float currentMaxDepth = -9999.0;
                float2 UV = IN.uv; 

                int loopCount = clamp(int(Count), 0, 128); 
                float colorsCountMax = 16.0;

                [loop]
                for (int i = 0; i < loopCount; i++)
                {
                    float fi = float(i);
                    bool isVertical = (i % 2 == 0);
                    
                    float shiftX = frac(sin(fi * 45.32f) * 43758.5453f) * 100.0f;
                    float shiftY = frac(sin(fi * 89.12f) * 23145.1243f) * 100.0f;
                    float2 threadShift = float2(shiftX, shiftY);
                    
                    // Читаем прогресс уезда из текстуры данных (0.0 - на месте, 1.0 - уехала)
                    float2 dataUV = float2((fi + 0.5f) / max(Count, 1.0f), 0.5f);
                    float4 ropeData = SAMPLE_TEXTURE2D_LOD(_RopeDataTex, sampler_RopeDataTex, dataUV, 0.0);
                    float individualGrowth = saturate(ropeData.r);

                    // ФИКС: Координаты генерации шума теперь СТАТИЧНЫ. Изгиб "зю" жестко сидит на поле.
                    float2 uvScale = UV * NoiseScale + threadShift;
                    
                    float2 ipA = floor(uvScale); float2 fpA = frac(uvScale); float2 uA = fpA * fpA * (3.0f - 2.0f * fpA);
                    float n00_A = frac(sin(dot(ipA + float2(0.0f,0.0f), float2(127.1f,311.7f))) * 43758.5453f);
                    float n10_A = frac(sin(dot(ipA + float2(1.0f,0.0f), float2(127.1f,311.7f))) * 43758.5453f);
                    float n01_A = frac(sin(dot(ipA + float2(0.0f,1.0f), float2(127.1f,311.7f))) * 43758.5453f);
                    float n11_A = frac(sin(dot(ipA + float2(1.0f,1.0f), float2(127.1f,311.7f))) * 43758.5453f);
                    float noiseA = lerp(lerp(n00_A, n10_A, uA.x), lerp(n01_A, n11_A, uA.x), uA.y);
                    
                    float2 uvScaleB = uvScale + float2(0.01f, 0.01f);
                    float2 ipB = floor(uvScaleB); float2 fpB = frac(uvScaleB); float2 uB = fpB * fpB * (3.0f - 2.0f * fpB);
                    float n00_B = frac(sin(dot(ipB + float2(0.0f,0.0f), float2(127.1f,311.7f))) * 43758.5453f);
                    float n10_B = frac(sin(dot(ipB + float2(1.0f,0.0f), float2(127.1f,311.7f))) * 43758.5453f);
                    float n01_B = frac(sin(dot(ipB + float2(0.0f,1.0f), float2(127.1f,311.7f))) * 43758.5453f);
                    float n11_B = frac(sin(dot(ipB + float2(1.0f,1.0f), float2(127.1f,311.7f))) * 43758.5453f);
                    float noiseB = lerp(lerp(n00_B, n10_B, uB.x), lerp(n01_B, n11_B, uB.x), uB.y);
                    
                    float2 curlVector = float2(noiseB - noiseA, noiseA - noiseB) * 50.0f;
                    
                    // Получаем финальные искривленные UV
                    float2 distortedUV = UV + curlVector * NoiseStrength * 0.01f;
                    
                    float baseCoord = isVertical ? distortedUV.y : distortedUV.x;
                    float crossCoord = isVertical ? distortedUV.x : distortedUV.y;

                    // ФИКС МАРШРУТА: Сравнение идет по искривленной baseCoord!
                    // Если текущая точка на изогнутой оси верёвки меньше, чем прогресс уезда, 
                    // мы просто скипаем этот пиксель, заставляя нить таять строго вдоль своего изгиба.
                    if (baseCoord >= individualGrowth)
                    {
                        float positionRandom = frac(sin(fi * 67.89f) * 98765.4321f);
                        float targetPos = 0.05f + positionRandom * 0.9f;
                        
                        float distanceToLine = crossCoord - targetPos;
                        float texU = distanceToLine * 8.5f + 0.5f;
                        float texV = baseCoord * RopeTiling;
                        
                        float2 finalTexUV = float2(texU, texV);
                        
                        float4 packedSample = SAMPLE_TEXTURE2D_LOD(_LineTex, sampler_LineTex, finalTexUV, 0.0f);
                        
                        float silhouetteMask = step(AlphaClip, packedSample.r);
                        
                        if (silhouetteMask > 0.0f)
                        {
                            float weavePulse = cos(baseCoord * Z_Entangle + fi * 3.14f) * 0.4f;
                            float depthRandom = frac(sin(fi * 12.54f) * 78912.34f);
                            
                            float pixelDepth = (weavePulse + (curlVector.x - curlVector.y) * 0.5f + depthRandom) * Z_WeaveStrength + (1.0f - abs(distanceToLine * 8.5f));

                            if (pixelDepth > currentMaxDepth)
                            {
                                currentMaxDepth = pixelDepth;
                                
                                float3 ropeColor = ropeData.gba * RopeBrightness;
                                
                                float internalPattern = packedSample.g;                  
                                float shadowModifier = saturate(1.0f - (1.0f - internalPattern) * PatternStrength);
                                float3 colorWithPattern = ropeColor * shadowModifier;
                                
                                finalRopeColor = colorWithPattern;
                                finalRopeAlpha = 1.0f * AlphaDensity; 
                            }
                        }
                    }
                }

                half4 finalColor = half4(finalRopeColor, finalRopeAlpha) * IN.color;
                return finalColor;
            }
            ENDHLSL
        }
    }
}
