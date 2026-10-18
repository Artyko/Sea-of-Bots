using System;
// Closed-loop range control; distance changes are infrequent and deterministic per ship.
public sealed class NavalEngagementRange
{
    private static readonly float[] ranges={.60f,.78f,.66f,.84f};
    private static readonly float[] durations={22f,28f,24f,30f};
    private int phase;
    private uint randomState;
    private bool randomInitialized;
    private float duration,jitter;
    public float CourseBias {get;private set;}
    public float CruiseThrottle {get;private set;}
    public bool KeepBroadside {get;private set;}
    private float age,unreachable;
    public bool Pursuing {get;private set;}
    public float Desired {get;private set;}
    public void Reset(int seed)
    {randomState=(uint)seed^0x9e3779b9u;randomInitialized=true;phase=Next(ranges.Length);ChooseVariation();age=unreachable=0f;Pursuing=KeepBroadside=false;Desired=0f;}
    private int Next(int bound){randomState=unchecked(randomState*1664525u+1013904223u);return (int)((randomState>>8)%(uint)bound);}
    private float NextFloat(){return Next(1000000)/1000000f;}
    private void ChooseVariation()
    {duration=durations[phase]+NextFloat()*8f-4f;jitter=NextFloat()*.04f-.02f;CourseBias=NextFloat()*6f-3f;CruiseThrottle=.6f+NextFloat()*.2f;}
    public void Step(float dt,float distance,bool reachable,float ownScale,float otherScale)
    {
        dt=Math.Max(0f,dt);age+=dt;
        if(!randomInitialized)Reset(0);
        if(age>=duration){age-=duration;phase=(phase+1+Next(ranges.Length-1))%ranges.Length;ChooseVariation();}
        Desired=Math.Max(NavalHelmTactics.Clearance(ownScale,otherScale)+6f,NavalGunneryMath.LevelRange*(ranges[phase]+jitter));
        // Do not turn an early long-range approach into an endless broadside orbit.
        if(!reachable&&distance<NavalGunneryMath.LevelRange*1.7f)unreachable+=dt;else unreachable=0f;
        if(unreachable>=3f&&distance>Desired+8f)Pursuing=true;
        if(reachable){Pursuing=false;KeepBroadside=true;}
        else if(unreachable>=3f)KeepBroadside=false;
    }
    public static float ApproachCourse(float bearing,int side,float distance,float ownSpeed,float enemyAwaySpeed,float bias)
    {
        float angle=Math.Max(15f,Math.Min(65f,65f-(distance-NavalGunneryMath.LevelRange-8f)*.5f));
        if(enemyAwaySpeed>0f)
        {
            float ratio=Math.Max(0f,Math.Min(.97f,(enemyAwaySpeed+.2f)/Math.Max(2.5f,Math.Abs(ownSpeed))));
            angle=Math.Min(angle,(float)(Math.Acos(ratio)*180/Math.PI));
        }
        return bearing+(side==1?1f:-1f)*Math.Max(10f,angle+bias);
    }
    public static bool BallisticReach(float distance,float height)
    {float degrees;return NavalBattleMath.FiringAngle(distance,height,32f,out degrees)&&degrees<=45f;}
    public static float Course(float bearing,int side,float distance,float desired,bool pursuing)
    {
        // Positive weight closes range; negative opens it. Normal trim remains in gun traverse.
        if(pursuing)return bearing;
        float weight=Math.Max(-.35f,Math.Min(.35f,(distance-desired)/22f));
        float angle=90f-(float)(Math.Atan(weight)*180/Math.PI);
        return bearing+(side==1?angle:-angle);
    }
}
