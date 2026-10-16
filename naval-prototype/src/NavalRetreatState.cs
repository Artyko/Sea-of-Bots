// Shared tactical state for allied and enemy helmsmen. All times are seconds.
public sealed class NavalRetreatState
{
    public bool Active {get;private set;}
    private float elapsed,quiet;
    public void Step(float dt,bool gunsEmptyAndCrewRepairing,bool floodEmergency,bool threat)
    {
        if(!threat){Active=false;elapsed=quiet=0f;return;}
        if(!Active)
        {
            if(!gunsEmptyAndCrewRepairing||!floodEmergency)return;
            Active=true;elapsed=quiet=0f;
        }
        elapsed+=dt;
        quiet=floodEmergency?0f:quiet+dt;
        // Brief pauses between patches/waves must not send the ship back into battle.
        if(elapsed>=10f&&quiet>=8f){Active=false;elapsed=quiet=0f;}
    }
}
