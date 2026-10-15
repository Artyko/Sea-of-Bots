using System;

// No engine dependencies, so the hydraulic invariants can be checked independently.
public static class FloodMath
{
    public static float OpeningFlow(float area, float sea, float inside, float center, float halfHeight, out float wetArea)
    {
        float bottom = center - halfHeight;
        float height = Math.Max(0.001f, halfHeight * 2f);
        wetArea = area * Math.Max(0f, Math.Min(1f, (sea - bottom) / height));
        float flowArea = area * Math.Max(0f, Math.Min(1f, (Math.Max(sea, inside) - bottom) / height));
        return Flow(flowArea, sea, inside, bottom);
    }
    public static float Flow(float wetArea, float seaLevel, float insideLevel, float openingBottom)
    {
        if (wetArea <= 0f) return 0f;
        float externalHead = Math.Max(0f, seaLevel - openingBottom);
        float internalHead = Math.Max(0f, insideLevel - openingBottom);
        float difference = externalHead - internalHead;
        return (float)(0.62 * wetArea * Math.Sign(difference) * Math.Sqrt(19.62 * Math.Abs(difference)));
    }
}
