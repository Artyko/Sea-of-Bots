using System;
public static class NavalRigImpactMath
{
    public const float CannonRadius=0.4f;
    // Large hulls must not magnify a cannon impact beyond the brigantine size.
    public static float CannonRadiusForScale(float scale)
    {return CannonRadius*Math.Min(1f,3f/Math.Max(.01f,scale));}
    // Preserve even the conservative cell-AABB-in-hull-space predicate of TouchesHull.
    public static float BroadphasePadding(float halfX,float halfY,float halfZ)
    {return (float)Math.Sqrt(3.0*((double)halfX*halfX+(double)halfY*halfY+(double)halfZ*halfZ))+.002f;}
    public static int SweepSteps(float distance,float angle)
    {return Math.Max(1,(int)Math.Ceiling(Math.Max(distance/0.2f,angle/0.5f)));}
    // World-space hull volume, including rails but excluding the mast and sail.
    public static bool TouchesHull(float x,float y,float z,float halfX,float halfY,float halfZ,float scale=1f,float top=float.NaN,float bottom=float.NaN)
    {
        return Math.Abs(x)<=6.35f*scale+halfX&&Math.Abs(z)<=12.65f*scale+halfZ&&
            y+halfY>=(float.IsNaN(bottom)?-4.95f*scale:bottom)&&y-halfY<=(float.IsNaN(top)?4.5f*scale:top);
    }
    public static float RetainedSpeed(bool fabric,bool attached)
    {return fabric?0.96f:attached?0.72f:0.94f;}
}
