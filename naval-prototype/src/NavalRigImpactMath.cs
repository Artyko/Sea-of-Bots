using System;
public static class NavalRigImpactMath
{
    public const float CannonRadius=0.4f;
    public static int SweepSteps(float distance,float angle)
    {return Math.Max(1,(int)Math.Ceiling(Math.Max(distance/0.2f,angle/0.5f)));}
    // World-space hull volume, including rails but excluding the mast and sail.
    public static bool TouchesHull(float x,float y,float z,float halfX,float halfY,float halfZ)
    {
        return Math.Abs(x)<=6.35f+halfX&&Math.Abs(z)<=12.65f+halfZ&&
            y+halfY>=-4.95f&&y-halfY<=4.5f;
    }
    public static float RetainedSpeed(bool fabric,bool attached)
    {return fabric?0.96f:attached?0.72f:0.94f;}
}
