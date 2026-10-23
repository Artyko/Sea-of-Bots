using System;
// Tracks actual boat-relative displacement across waypoint changes and route replans.
public sealed class NavalMovementWatchdog
{
    private bool initialized;
    private float x,y,z,elapsed;
    public bool Step(float dt,float px,float py,float pz)
    {
        if(!initialized){x=px;y=py;z=pz;initialized=true;elapsed=0;}
        float dx=px-x,dy=py-y,dz=pz-z;
        if(dx*dx+dy*dy+dz*dz>=.64f){x=px;y=py;z=pz;elapsed=0;}
        else elapsed+=Math.Max(0,dt);
        return elapsed>=8f;
    }
    public void Reset(){initialized=false;elapsed=0;}
}
