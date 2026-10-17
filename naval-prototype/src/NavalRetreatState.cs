// Shared tactical state. Earlier repair-retreat hysteresis lives in NavalCombatEmergency.
public sealed class NavalRetreatState
{
    public bool Active {get;private set;}
    public void Step(float dt,bool repairNeeded,bool retreatDanger,bool threat)
    {
        if(!threat||!retreatDanger){Active=false;return;}
        if(repairNeeded)Active=true;
    }
}
