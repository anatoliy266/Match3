Shader "URP/2D/RealisticCeilingDropURP"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        [Header(Drop Settings)]
        _DripColor ("Drop Color", Color) = (0.5, 0.8, 1.0, 0.7)
        _Speed ("Drop Speed", Float) = 1.5
        _Width ("Drop Width", Range(0.01, 0.1)) = 0.04
        _XPos ("Drop X Position (0-1)", Range(0.0, 1.0)) = 0.5
        
        [Header(Shape Fine Tuning)]
        _TailLength ("Tail Length", Range(0.02, 0.2)) = 0.09
        _Roundness ("Bottom Roundness", Range(0.01, 0.1)) = 0.03
        
        // Смещение времени для рандомизации капель на разных плитках
        _TimeOffset ("Time Offset", Float) = 0.0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            // Включаем поддержку GPU Instancing для SRP Batcher
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID 
            };

            Texture2D _MainTex;
            SamplerState sampler_MainTex;

            // Константный буфер для поддержки SRP Batcher
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                half4 _DripColor;
                float _Speed;
                float _Width;
                float _XPos;
                float _TailLength;
                float _Roundness;
                float _TimeOffset;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                // Современная трансформация вершин в URP
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color * _Color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                // Выборка текстуры плитки
                half4 baseColor = _MainTex.Sample(sampler_MainTex, input.uv) * input.color;
                
                // Используем глобальное URP-время _Time.y + индивидуальное смещение плитки
                float customTime = _Time.y + _TimeOffset;
                float progress = frac(customTime * _Speed);
                float dropY = 1.0 - progress; 
                
                float yDist = input.uv.y - dropY;
                float dripMask = 0.0;

                // Математика формы капли
                if (yDist > -_Roundness && yDist < _TailLength) {
                    float t = (yDist + _Roundness) / (_TailLength + _Roundness);
                    
                    float currentWidth = _Width * pow(1.0 - t, 0.4) * smoothstep(0.0, 0.25, t);
                    float xDist = abs(input.uv.x - _XPos);
                    
                    dripMask = smoothstep(currentWidth, currentWidth * 0.6, xDist);
                    dripMask *= smoothstep(0.0, 0.15, dropY);
                }

                half4 finalColor = lerp(baseColor, _DripColor, dripMask * _DripColor.a);
                finalColor.a = baseColor.a;

                return finalColor;
            }
            ENDHLSL
        }
    }
}
