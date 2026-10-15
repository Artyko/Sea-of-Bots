using System;
public class NavalBreathClock
{
    public const float Capacity=30f;
    public float Air=Capacity;
    public bool Submerged;
    public bool Step(float depth,float dt)
    {
        if(depth>0.05f)Submerged=true;
        else if(depth< -0.05f)Submerged=false;
        dt=Math.Max(0f,dt);
        Air=Submerged?Math.Max(0f,Air-dt):Capacity;
        return Air<=0f;
    }
}
