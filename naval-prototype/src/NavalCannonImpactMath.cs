using System;
public static class NavalCannonImpactMath
{
    public static bool ValidContact(float distance,float localX,float localY,float localZ)
    {
        return distance>0.0001f&&Math.Abs(localX)<=0.501f&&Math.Abs(localY)<=0.501f&&Math.Abs(localZ)<=0.501f;
    }
    public static float InsetFraction(float centreDistance)
    {return centreDistance<=0.00001f?0f:Math.Min(1f,0.12f/centreDistance);}
}
