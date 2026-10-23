using System;

public static class NavalRepairDispatch
{
    public static bool ResumeGunnery(bool opportunity,bool queued,bool activeJob,bool fire,bool leaks,bool pumpDemand,bool danger,float carriedWater)
    {return opportunity&&queued&&!activeJob&&!fire&&!leaks&&!pumpDemand&&!danger&&carriedWater<=0f;}
    public static bool CanRecall(bool acceptedPatch,bool leaks,bool otherRepairer,float carriedWater)
    {return carriedWater<=0f&&!acceptedPatch&&(!leaks||otherRepairer);}
    public static float RetryDelay(bool flowing){return flowing?1f:3f;}
    public static bool Pending(bool damaged,bool available,bool repairable,float flow,float wetAt,float now)
    {return damaged&&available&&repairable&&(flow>.00001f||now-wetAt<=5f);}
    public static bool PreferCurrentDeck(float feetY,float holdFloor,bool onStairs)
    {return !onStairs&&(Math.Abs(feetY-.825f)<.25f||Math.Abs(feetY-holdFloor)<.25f);}
}
