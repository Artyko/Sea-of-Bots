using System;
// Actual horizontal travel, not commanded speed. Shared by both helmsmen.
public sealed class NavalHelmRecovery
{
    public int Phase {get;private set;} // 0 normal, 1 back out, 2 rejoin around other side
    public int Side {get;private set;}
    public bool SternBlocked {get{return blockedStern>=2.5f;}}
    public float BackwardDistance {get{return backed;}}
    private float stalled,age,backed,reverseAge,blockedStern,forwardAge,forwardDistance,blockedBow;
    public void Reset(){Phase=0;stalled=age=backed=reverseAge=blockedStern=forwardAge=forwardDistance=blockedBow=0f;}
    public static float ReverseDistance(float scale){return Math.Max(10f,6.5f*Math.Abs(scale));}
    public static float StraightReverseDistance(float scale)
    {return ReverseDistance(scale)*.5f;}
    public bool BeginReverse()
    {
        if(Phase!=0)return false;
        Phase=1;age=backed=stalled=reverseAge=blockedStern=forwardAge=forwardDistance=blockedBow=0f;Side=Side==0?1:-Side;return true;
    }
    public bool Step(float dt,float travel,float backwardTravel,bool driving,bool shoreRisk,float shipScale,float sailSetting=-1f,float signedSpeed=-1f,float backwardDisplacement=float.NaN)
    {
        dt=Math.Max(0f,dt);
        if(!driving){Reset();return false;}
        if(Phase==1)
        {
            age+=dt;
            bool reverseReady=sailSetting<=-.7f&&signedSpeed<-.1f;
            if(reverseReady){reverseAge+=dt;backed=float.IsNaN(backwardDisplacement)?Math.Max(0,backed+backwardTravel):Math.Max(0,backwardDisplacement);}
            blockedStern=sailSetting<=-.7f&&shoreRisk&&travel<dt*.12f?blockedStern+dt:0;
            // Time walking to the ropes, furling and braking is not backing time.
            if(backed>=ReverseDistance(shipScale)||reverseAge>=30f||age>=90f||blockedStern>=2.5f){Phase=2;age=stalled=forwardAge=forwardDistance=blockedBow=0f;}
            return false;
        }
        if(Phase==2){
            age+=dt;bool forwardReady=sailSetting>=.4f&&signedSpeed>.1f;
            if(forwardReady){forwardAge+=dt;forwardDistance+=Math.Max(0,travel);}
            blockedBow=sailSetting>=.4f&&shoreRisk&&travel<dt*.12f?blockedBow+dt:0;
            if(blockedBow>=4f){Phase=0;return BeginReverse();}
            // Keep the rope operator reserved through braking, furling and actual departure.
            if(forwardReady&&forwardDistance>=3f&&forwardAge>=2f){Phase=0;age=stalled=0f;}
            return false;
        }
        if(shoreRisk&&travel<dt*.12f)stalled+=dt;else stalled=0f;
        if(stalled<2.5f)return false;
        return BeginReverse();
    }
}
