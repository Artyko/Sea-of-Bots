using System;
public static class NavalSailWorkLayout
{
    public const float PostX=1.35f,PostZ=2.4f,CleatOffset=1.25f,Reach=1.85f;
    public static bool InReach(float horizontalSquared,float heightError)
    {return horizontalSquared<=Reach*Reach&&Math.Abs(heightError)<=.3f;}
}
