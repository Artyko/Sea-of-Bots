using System;
public sealed class NavalCrewReleaseLatch
{
    private int released;
    private float quiet;
    public int Step(float dt,int requested,bool working)
    {
        requested=Math.Max(0,Math.Min(2,requested));
        if(requested>=released){released=requested;quiet=0;return released;}
        if(working){quiet=0;return released;}
        quiet+=Math.Max(0,dt);
        if(quiet>=10f){released=requested;quiet=0;}
        return released;
    }
}
