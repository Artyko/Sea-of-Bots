using System;
public static class NavalRepairAccessMath
{
    public static float StandY(float holeY,float scale,float horizontal)
    {
        const float workingReach=4.5f;
        if(horizontal>=workingReach)return float.PositiveInfinity;
        float vertical=(float)Math.Sqrt(workingReach*workingReach-horizontal*horizontal)/scale;
        float minimum=holeY-.8f-vertical+.08f;
        return Math.Max(BoatDimensions.HoldFloor,minimum);
    }
}
