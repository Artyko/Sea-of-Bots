using System;
// Distances and progress thresholds are world metres, independent of ship scale.
public static class NavalRouteProgress
{
    public static float Step(float blocked,float before,float after,float dt,bool reached,bool deliberateWait)
    {
        if(reached||deliberateWait)return 0f;
        if(dt<=0f)return blocked;
        return before-after>=Math.Max(0f,dt)*.15f?0f:blocked+Math.Max(0f,dt);
    }
}
