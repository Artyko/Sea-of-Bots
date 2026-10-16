using System;
// One projectile contributes at most one hit to each timber voxel.
public static class NavalMastDurability
{
    public static float Accumulate(float wear,float power){return wear+Math.Max(0f,Math.Min(1f,power));}
    public static bool Broken(float wear){return wear>=2f;}
}
