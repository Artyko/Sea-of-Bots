using System;
public static class NavalFloodVisibilityMath
{
    public static bool HideEffects(float highestHullClearance){return highestHullClearance<=0f;}
    public static float Visibility(float highestHullClearance)
    {return Math.Max(0f,Math.Min(1f,highestHullClearance/0.8f));}
}
