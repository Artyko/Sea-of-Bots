using System;
public static class NavalBoardingJumpMath
{
    public const float MaxDistance=8f;
    public static float Height(float fromY,float toY,float fromRailY,float toRailY)
    {
        float lift=(float)Math.Sin(Math.PI*0.18);
        float early=(fromRailY+0.35f-(fromY*0.82f+toY*0.18f))/lift;
        float late=(toRailY+0.35f-(fromY*0.18f+toY*0.82f))/lift;
        return Math.Max(3.5f,Math.Max(early,late));
    }
    public static float TravelFraction(float time)
    {return Math.Max(0,Math.Min(1,(time-.15f)/.7f));}
    public static float Lift(float fraction,float height)
    {return (float)Math.Sin(Math.Max(0f,Math.Min(1f,fraction))*Math.PI)*height;}
    public static bool Reachable(float distance,float height){return distance<=MaxDistance&&height<=8f;}
}
