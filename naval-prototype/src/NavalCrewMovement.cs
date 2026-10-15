using System;
public static class NavalCrewMovement
{
    public static float BaseSpeed(float nativeSpeed)
    {return nativeSpeed>0f&&!float.IsNaN(nativeSpeed)&&!float.IsInfinity(nativeSpeed)?nativeSpeed:5.5f;}
    public static float TargetSpeed(float nativeSpeed,float alignment,float distance,bool finalPoint,bool stairs)
    {
        float speed=BaseSpeed(nativeSpeed)*(0.7f+0.3f*Math.Max(0f,Math.Min(1f,alignment)))*(stairs?0.8f:1f);
        // Intermediate waypoints steer the route; they are not stopping points.
        return finalPoint?Math.Min(speed,(float)Math.Sqrt(40f*Math.Max(0f,distance))):speed;
    }
    public static float Accelerate(float current,float target,float dt)
    {
        float step=20f*Math.Max(0f,dt);
        return current<target?Math.Min(target,current+step):Math.Max(target,current-step);
    }
}
