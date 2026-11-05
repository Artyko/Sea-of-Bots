using System;
public sealed class NavalFireHeatState
{
    public float Heat {get;private set;}
    public bool Step(float dt,float intensity)
    {
        dt=Math.Max(0,dt);intensity=Math.Max(0,Math.Min(1,intensity));
        Heat=Math.Max(0,Heat+dt*(intensity>.05f?intensity:-1.5f));
        if(Heat<8f)return false;Heat=0;return true;
    }
    public void Reset(){Heat=0;}
}
