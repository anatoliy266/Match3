Shader "Custom/CellingShader"
{
    Properties
    {
        _LineColor ("Grid Line Color", Color) = (0.0, 0.0, 0.0, 1.0)       // Цвет линий (черный)
        _CellColor ("Cell Color (Gray Transparent)", Color) = (0.5, 0.5, 0.5, 0.3) // Цвет ячеек (серый прозрачный)
        _LineWidth ("Line Width", Range(0.001, 0.2)) = 0.03                // Толщина линий
        _GridSize ("Grid Size (Columns, Rows, 0, 0)", Vector) = (10, 10, 0, 0)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

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

            fixed4 _LineColor;
            fixed4 _CellColor;
            float _LineWidth;
            float4 _GridSize;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float columns = max(1.0, _GridSize.x);
                float rows = max(1.0, _GridSize.y);

                float2 cellCoord = i.uv * float2(columns, rows);
                float2 localFrac = frac(cellCoord);
                
                // Расстояние до правого и верхнего края поля (для фикса границ)
                float2 distToMaxEdge = float2(columns, rows) - cellCoord;

                // Проверяем внутренние линии и жестко фиксируем крайние границы
                float2 internalLines = step(localFrac, _LineWidth);
                float2 maxEdges = step(distToMaxEdge, _LineWidth);

                // Левая и нижняя внешние границы всего поля (uv = 0)
                float2 minEdges = step(cellCoord, _LineWidth);

                // Объединяем все условия для линий
                float gridX = max(internalLines.x, max(minEdges.x, maxEdges.x));
                float gridY = max(internalLines.y, max(minEdges.y, maxEdges.y));

                // Финальная маска линий (1 — линия, 0 — пустота внутри ячейки)
                float lineMask = max(gridX, gridY);

                // Смешиваем цвет ячейки и цвет линии на основе маски
                // Если lineMask == 1, берется _LineColor. Если lineMask == 0, берется _CellColor
                fixed4 finalColor = lerp(_CellColor, _LineColor, lineMask);

                return finalColor;
            }
            ENDCG
        }
    }
}
