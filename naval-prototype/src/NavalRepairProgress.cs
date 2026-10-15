using System;
public class NavalRepairProgress
{
    public float Elapsed;
    public float DrySeconds;
    public void StepAccepted(float dt,bool atLeak)
    {Step(dt,atLeak,true);}
    public void Step(float dt,bool atLeak,bool flowing)
    {
        dt=Math.Max(0f,dt);
        DrySeconds=flowing?0f:DrySeconds+dt;
        if(atLeak&&flowing)Elapsed+=dt;
    }
    public void Reset(){Elapsed=0f;DrySeconds=0f;}
}
