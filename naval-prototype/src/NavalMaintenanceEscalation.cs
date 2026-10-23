using System;
// Fraction of loss-of-buoyancy capacity; smooth actual water growth, including pump/bucket effects.
public sealed class NavalMaintenanceEscalation
{
    public bool Parallel {get;private set;}
    private bool initialized;
    private float previous,rate,stable;
    public void Step(float dt,float fill,float heel,bool critical,bool urgentLeak=false)
    {
        dt=Math.Max(0f,dt);heel=Math.Abs(heel);
        if(initialized&&dt>0f)
        {float alpha=1f-(float)Math.Exp(-dt/2f);rate+=((fill-previous)/dt-rate)*alpha;}
        previous=fill;initialized=true;
        float projected=fill+Math.Max(0f,rate)*25f;
        bool danger=urgentLeak||critical||fill>=.25f||fill>=.1f&&heel>=12f||fill>=.05f&&projected>=.5f;
        if(danger){Parallel=true;stable=0f;}
        else if(Parallel)
        {
            if(fill<=.18f&&heel<=8f&&projected<.35f)stable+=dt;else stable=0f;
            if(stable>=4f){Parallel=false;stable=0f;}
        }
    }
}
