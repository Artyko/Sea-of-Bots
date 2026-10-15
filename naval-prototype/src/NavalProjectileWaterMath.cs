using System;
public static class NavalProjectileWaterMath
{
    public const float DamagePower=0.5f;
    public const float DamageRadiusScale=0.7071068f; // half the patch area in the thin voxel hull
    public const float MaxTravel=2f;
    public const float EntrySpeedScale=0.4f;
    public static float Damping(float dt){return (float)Math.Exp(-7f*Math.Max(0f,dt));}
}
