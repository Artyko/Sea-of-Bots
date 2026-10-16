using System;
// Actual horizontal travel, not commanded speed. Shared by both helmsmen.
public sealed class NavalHelmRecovery
{
    public int Phase {get;private set;} // 0 normal, 1 back out, 2 rejoin around other side
    public int Side {get;private set;}
    public float BackwardDistance {get{return backed;}}
    private float stalled,age,backed;
    public void Reset(){Phase=0;stalled=age=backed=0f;}
    public bool Step(float dt,float travel,float backwardTravel,bool driving,bool shoreRisk,float shipScale)
    {
        dt=Math.Max(0f,dt);
        if(!driving){Reset();return false;}
        if(Phase==1)
        {
            age+=dt;backed+=Math.Max(0f,backwardTravel);
            if(backed>=Math.Max(3f,shipScale*2f)||age>=8f){Phase=2;age=stalled=0f;}
            return false;
        }
        if(Phase==2){age+=dt;if(age>=4f){Phase=0;age=stalled=0f;}return false;}
        if(shoreRisk&&travel<dt*.12f)stalled+=dt;else stalled=0f;
        if(stalled<2.5f)return false;
        Phase=1;age=backed=stalled=0f;Side=Side==0?1:-Side;return true;
    }
}
