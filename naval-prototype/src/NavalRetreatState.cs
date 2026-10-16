// Shared tactical state. Critical-state hysteresis lives in NavalCombatEmergency.
public sealed class NavalRetreatState
{
    public bool Active {get;private set;}
    public void Step(float dt,bool gunsEmptyAndCrewRepairing,bool critical,bool threat)
    {
        if(!threat||!critical){Active=false;return;}
        if(gunsEmptyAndCrewRepairing)Active=true;
    }
}
