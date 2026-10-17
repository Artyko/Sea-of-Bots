using UnityEngine;

// Local shore navigation. Low-frequency non-allocating terrain probes, physical movement unchanged.
public sealed class NavalHelmNavigator
{
    private readonly NavalHelmRecovery recovery=new NavalHelmRecovery();
    private static readonly float[] angles={0f,25f,-25f,50f,-50f,75f,-75f,100f,-100f,125f,-125f};
    private Vector3 previous,routeDirection,reverseDirection;
    private bool initialized,landBlocked,shipBlocked;
    private float nextProbe,forwardClear,chosenClear,clearSince=-1f;
    private int avoidSide;
    public bool Reversing {get{return recovery.Phase==1;}}
    public void Reset(Vector3 position)
    {previous=position;initialized=true;recovery.Reset();landBlocked=shipBlocked=false;nextProbe=0f;avoidSide=0;clearSince=-1f;}
    public void LandBlocked(){landBlocked=true;}
    public void ShipBlocked(){shipBlocked=true;}
    public void Steer(NavalWorldMap map,Transform ship,Vector3 desired,float speed,float sailPower,float dt,ref float throttle,out float turn)
    {
        Vector3 current=ship.position;
        if(!initialized){previous=current;initialized=true;}
        Vector3 travel=current-previous;travel.y=0f;previous=current;
        desired.y=0f;if(desired.sqrMagnitude<.001f)desired=ship.forward;desired.Normalize();
        if(map==null){routeDirection=desired;forwardClear=chosenClear=float.PositiveInfinity;}
        else
        {
            if(recovery.Phase!=1&&(Time.time>=nextProbe||routeDirection.sqrMagnitude<.01f))
            {
                nextProbe=Time.time+.35f;
                float range=Mathf.Clamp(45f+Mathf.Abs(speed)*10f,45f,85f);
                float direct=map.ShoreClearance(current,desired,ship,range);
                forwardClear=map.ShoreClearance(current,Quaternion.Euler(0f,ship.eulerAngles.y,0f)*Vector3.forward,ship,range);
                if(direct>=range-.1f)
                {if(clearSince<0f)clearSince=Time.time;if(Time.time-clearSince>1.5f)avoidSide=0;}
                else clearSince=-1f;
                float best=-float.MaxValue;Vector3 chosen=desired;int chosenSide=avoidSide;chosenClear=direct;
                foreach(float angle in angles)
                {
                    int side=angle<0f?-1:angle>0f?1:0;
                    if(avoidSide!=0&&side!=0&&side!=avoidSide)continue;
                    Vector3 candidate=Quaternion.Euler(0f,angle,0f)*desired;
                    float clearance=angle==0f?direct:map.ShoreClearance(current,candidate,ship,range);
                    float score=clearance/range*4f+Vector3.Dot(candidate,desired)*1.25f;
                    if(side!=0&&side==avoidSide)score+=.2f;
                    if(score>best){best=score;chosen=candidate;chosenClear=clearance;chosenSide=side;}
                }
                routeDirection=chosen;
                if(direct<range-.1f&&chosenSide!=0)avoidSide=chosenSide;
            }
        }
        bool driving=throttle>.1f&&sailPower>.01f;
        bool contact=shipBlocked;
        bool start=contact&&driving&&recovery.BeginReverse();
        if(!start)start=recovery.Step(dt,travel.magnitude,Vector3.Dot(travel,-ship.forward),driving,
            contact||landBlocked||forwardClear<Mathf.Max(6f,Mathf.Abs(speed)*3f),Mathf.Abs(ship.lossyScale.x));
        landBlocked=shipBlocked=false;
        if(start)
        {
            avoidSide=avoidSide==0?recovery.Side:-avoidSide;
            reverseDirection=Quaternion.Euler(0f,ship.eulerAngles.y+avoidSide*60f,0f)*Vector3.forward;
            nextProbe=0f;
            Debug.Log("[NavalPrototype] Helmsman reversing clear of "+(contact?"ship contact":"shore"));
        }
        if(recovery.Phase==1)
        {
            throttle=-.65f;
            desired=recovery.BackwardDistance<Mathf.Max(1f,Mathf.Abs(ship.lossyScale.x)*.35f)?ship.forward:reverseDirection;
        }
        else
        {
            desired=routeDirection;
            if(chosenClear<12f)throttle=Mathf.Min(throttle,.3f);
            else if(chosenClear<30f)throttle=Mathf.Min(throttle,.55f);
            if(recovery.Phase==2)throttle=Mathf.Min(throttle,.65f);
            // Keep headway for the rudder, but slow a large heading correction.
            float error=Mathf.Abs(Mathf.DeltaAngle(ship.eulerAngles.y,Mathf.Atan2(desired.x,desired.z)*Mathf.Rad2Deg));
            if(error>70f)throttle=Mathf.Min(throttle,.45f);
        }
        float wanted=Mathf.Atan2(desired.x,desired.z)*Mathf.Rad2Deg;
        turn=Mathf.Clamp(Mathf.DeltaAngle(ship.eulerAngles.y,wanted)/25f,-1f,1f);
        if(speed<-.1f)turn=-turn;
    }
}
