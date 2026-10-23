using System;
public static class NavalCrewSupportPolicy
{
    public static int Choose(int owner,bool[] eligible,float[] costs)
    {
        if(owner>=0&&owner<eligible.Length&&eligible[owner])return owner;
        int best=-1;float distance=float.PositiveInfinity;
        for(int i=0;i<eligible.Length;i++)if(eligible[i]&&costs[i]<distance){best=i;distance=costs[i];}
        return best;
    }
}
public sealed class NavalPumpDemand
{
    public bool Active {get;private set;}
    private float quiet;
    public void Step(float dt,bool requested,bool empty)
    {
        if(requested){Active=true;quiet=0;return;}
        if(!Active)return;
        quiet=empty?quiet+Math.Max(0,dt):0;
        if(quiet>=2){Active=false;quiet=0;}
    }
    public void Reset(){Active=false;quiet=0;}
}
