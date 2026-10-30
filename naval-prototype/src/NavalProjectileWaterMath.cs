using System;
public static class NavalProjectileWaterMath
{
    // Positive clearance is air; a dry interior returns positive infinity.
    public static bool EntersWater(float startClearance,float endClearance)
    {return startClearance<=0f||endClearance<=0f;}
    public const float DamagePower=0.5f;
    public const float DamageRadiusScale=0.7071068f; // half the patch area in the thin voxel hull
    public const float RifledDamagePower=0.72f;
    public const float RifledDamageRadiusScale=0.82f;
    public static float ImpactPowerScale(float projectilePower)
    {return projectilePower>1.05f?RifledDamagePower:DamagePower;}
    public static float ImpactRadiusScale(float projectilePower)
    {return projectilePower>1.05f?RifledDamageRadiusScale:DamageRadiusScale;}
    // World-space limits.  The ironclad is almost twenty metres across, so the
    // former 3.6 m cap stopped a waterline shot just behind the outer belt.
    // Side-gun shells can now reach the first inner spaces and the much faster
    // main-calibre shell can reach farther, while neither can cross the ship.
    public const float MaxTravel=3f;
    public const float RifledTravel=6.5f;
    public const float HeavyRifledTravel=10f;
    public static float TravelLimit(float projectilePower)
    {return projectilePower>2f?HeavyRifledTravel:projectilePower>1.05f?RifledTravel:MaxTravel;}
    public const float EntrySpeedScale=0.46f;
    public static float Damping(float dt){return (float)Math.Exp(-5.5f*Math.Max(0f,dt));}
    public static float Damping(float dt,float projectilePower)
    {return (float)Math.Exp(-(projectilePower>1.05f?3.4f:5.5f)*Math.Max(0f,dt));}
}
