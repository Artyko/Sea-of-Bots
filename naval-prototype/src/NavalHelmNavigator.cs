using UnityEngine;

// Local shore navigation. Low-frequency non-allocating terrain probes, physical movement unchanged.
public sealed class NavalHelmNavigator
{
    private readonly NavalHelmRecovery recovery=new NavalHelmRecovery();
    private static readonly float[] angles={0f,25f,-25f,50f,-50f,75f,-75f,100f,-100f,125f,-125f};
    private Vector3 previous,routeDirection,reverseDirection,reverseForward,reverseOrigin;
    private bool initialized,landBlocked,shipBlocked;
    private float nextProbe,forwardClear,chosenClear,clearSince=-1f;
    private int avoidSide;
    private readonly float[] probeClearance=new float[angles.Length];
    private float reverseProbeAt;
    private bool reverseTurnClear;
    public bool Reversing {get{return recovery.Phase==1;}}
    public bool Recovering {get{return recovery.Phase!=0;}}
    public void Reset(Vector3 position)
    {previous=position;initialized=true;recovery.Reset();landBlocked=shipBlocked=false;nextProbe=0f;avoidSide=0;clearSince=-1f;}
    public void LandBlocked(){landBlocked=true;}
    public void ShipBlocked(){shipBlocked=true;}
    public void Steer(NavalWorldMap map,Transform ship,Vector3 desired,float speed,float sailPower,float dt,ref float throttle,out float turn,bool allowShipReverse=true)
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
                for(int probe=0;probe<angles.Length;probe++)probeClearance[probe]=float.NaN;
                for(int probe=0;probe<angles.Length&&!(direct>=range-.1f&&avoidSide==0);probe++)
                {
                    float angle=angles[probe];
                    int side=angle<0f?-1:angle>0f?1:0;
                    if(avoidSide!=0&&side!=0&&side!=avoidSide)continue;
                    Vector3 candidate=Quaternion.Euler(0f,angle,0f)*desired;
                    float clearance=angle==0f?direct:map.ShoreClearance(current,candidate,ship,range);
                    probeClearance[probe]=clearance;
                    float score=clearance/range*4f+Vector3.Dot(candidate,desired)*1.25f;
                    if(side!=0&&side==avoidSide)score+=.2f;
                    if(score>best){best=score;chosen=candidate;chosenClear=clearance;chosenSide=side;}
                }
                if(chosenClear<12f&&avoidSide!=0)
                {
                    // A committed bypass side can become a cul-de-sac. Reconsider both sides.
                    for(int probe=0;probe<angles.Length;probe++)
                    {
                        float angle=angles[probe];
                        Vector3 candidate=Quaternion.Euler(0f,angle,0f)*desired;
                        float clearance=probeClearance[probe];
                        if(float.IsNaN(clearance)){clearance=angle==0f?direct:map.ShoreClearance(current,candidate,ship,range);probeClearance[probe]=clearance;}
                        float score=clearance/range*4f+Vector3.Dot(candidate,desired)*1.25f;
                        if(score>best+.2f){best=score;chosen=candidate;chosenClear=clearance;chosenSide=angle<0?-1:angle>0?1:0;}
                    }
                }
                routeDirection=chosen;
                if(direct<range-.1f&&chosenSide!=0)avoidSide=chosenSide;
            }
        }
        bool driving=(throttle>.1f||recovery.Phase!=0)&&sailPower>.01f;
        bool contact=shipBlocked;
        // Touching while sliding/pushing is not a grounding. Reverse only after
        // sustained lack of actual travel, and never abort the final ram for a touch.
        NavalStations sailControls=NavalStations.ForShip(ship);
        int priorPhase=recovery.Phase;
        bool start=recovery.Step(dt,travel.magnitude,Vector3.Dot(travel,-reverseForward),driving,
            NavalHelmIntentMath.ReverseRisk(landBlocked,contact,!allowShipReverse,forwardClear,speed),Mathf.Abs(ship.lossyScale.x),sailControls!=null?sailControls.SailSetting:0f,speed,Vector3.Dot(current-reverseOrigin,-reverseForward));
        if(priorPhase==1&&recovery.Phase==2)Debug.Log("[NavalPrototype] Helmsman forward recovery: "+ship.name+" reason="+(recovery.SternBlocked?"stern blocked":"reverse distance/time")+" sails="+(sailControls!=null?sailControls.SailSetting:0f).ToString("F2"));
        if(priorPhase==2&&recovery.Phase==0)Debug.Log("[NavalPrototype] Helmsman recovery complete after forward departure: "+ship.name);
        landBlocked=shipBlocked=false;
        if(start)
        {
            reverseProbeAt=0f;reverseTurnClear=false;
            reverseOrigin=current;reverseForward=ship.forward;reverseForward.y=0;reverseForward.Normalize();
            avoidSide=avoidSide==0?recovery.Side:-avoidSide;
            reverseDirection=Quaternion.Euler(0f,ship.eulerAngles.y+avoidSide*60f,0f)*Vector3.forward;
            nextProbe=0f;
            Debug.Log("[NavalPrototype] Helmsman reversing clear of "+(contact?"ship contact":"shore"));
        }
        if(recovery.Phase==1)
        {
            throttle=-.8f;
            desired=recovery.BackwardDistance<NavalHelmRecovery.StraightReverseDistance(ship.lossyScale.x)?reverseForward:reverseDirection;
            // Do not swing the bow into the shore while backing out.
            if(map!=null&&desired!=reverseForward)
            {
                Quaternion wantedRotation=Quaternion.LookRotation(desired);
                Quaternion trial=Quaternion.RotateTowards(ship.rotation,wantedRotation,3f);
                Vector3 backStep=current-ship.forward*Mathf.Max(.1f,Mathf.Abs(speed)*dt);
                if(Time.time>=reverseProbeAt)
                {reverseProbeAt=Time.time+.25f;reverseTurnClear=map.CanAdvance(current,ship.rotation,backStep,trial,ship);}
                // Navigation may reuse a short-lived decision; committed hull
                // movement still runs the exact shore sweep every frame.
                if(!reverseTurnClear)desired=ship.forward;
            }
        }
        else
        {
            desired=routeDirection;
            if(chosenClear<12f)throttle=Mathf.Min(throttle,.3f);
            else if(chosenClear<30f)throttle=Mathf.Min(throttle,.55f);
            if(recovery.Phase==2)throttle=.65f;
            // Keep headway for the rudder, but slow a large heading correction.
            float error=Mathf.Abs(Mathf.DeltaAngle(ship.eulerAngles.y,Mathf.Atan2(desired.x,desired.z)*Mathf.Rad2Deg));
            if(error>70f)throttle=Mathf.Min(throttle,.75f);
        }
        float wanted=Mathf.Atan2(desired.x,desired.z)*Mathf.Rad2Deg;
        turn=Mathf.Clamp(Mathf.DeltaAngle(ship.eulerAngles.y,wanted)/25f,-1f,1f);
        if(speed<-.1f)turn=-turn;
    }
}
