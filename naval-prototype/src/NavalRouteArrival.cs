using System;
public static class NavalRouteArrival
{
    public static bool Reached(float x,float y,float z,float goalX,float goalY,float goalZ,bool final)
    {
        float dx=x-goalX,dz=z-goalZ;
        // Controller skin, gravity and ship roll prevent exact floor-height matches.
        float radius=final?0.14f:0.28f;
        return dx*dx+dz*dz<radius*radius&&Math.Abs(y-goalY)<0.3f;
    }
}
