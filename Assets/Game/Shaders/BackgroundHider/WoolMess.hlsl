#ifndef GET_TANGLE_INCLUDED
#define GET_TANGLE_INCLUDED

// Легальное глобальное объявление массива, которое Shader Graph НЕ вырежет
float4 _TileColors[64];
float _AvailableColorsCount;

void GetTangle_float(
    float2 UV,
    float Count,
    float NoiseScale,
    float NoiseStrength,
    UnityTexture2D LineTex,
    UnitySamplerState LineSampler,
    float AlphaClip,
    float AlphaDensity,
    float RopeTiling,
    float Z_Entangle,
    float Time,
    out float3 Out,
    out float AlphaOut
)
{
    // --- РЕЖИМ ВИЗУАЛЬНОГО ДЕБАГА ---
    AlphaOut = 1.0;

    int totalColors = int(max(_AvailableColorsCount, 1.0));
    int colorIndex = int(UV.x * float(totalColors));
    colorIndex = clamp(colorIndex, 0, totalColors - 1);

    // Читаем напрямую из легального массива
    float4 debugColor = _TileColors[colorIndex];

    // Выводим цвет на экран
    Out = debugColor.rgb;
}

#endif
