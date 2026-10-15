using UnityEngine;
using System.Reflection;
[DefaultExecutionOrder(250)]
public class NavalBoarder : MonoBehaviour
{
    private Character robot;
    private NavalRepairSailor worker;
    private NavalHull home,target;
    private Vector3 launch,landing;
    private int phase;
    private float started,jumpTime,duration,retryAt,scanAt;
    private Matrix4x4 previousBoat;
    private Character opponent;
    private static readonly MethodInfo SetOpponent=typeof(AIController).GetMethod("SetTargetOpponentCharacter",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
    public bool Busy {get{return phase!=0;}}
    public string Status {get{return phase==1?"Preparing boarding":phase==2?"Boarding jump":phase==3?"Boarding melee":"Overboard";}}
    public void Initialize(Character sailor,NavalRepairSailor repair,NavalHull ship)
    {robot=sailor;worker=repair;home=ship;}
    public void SuspendForRescue(){phase=0;opponent=null;}
    public void AdoptShip(NavalHull ship)
    {target=ship;phase=3;opponent=null;previousBoat=ship.Root.localToWorldMatrix;worker.BeginAssault();robot.ActivateAI();}
    public bool InFlight {get{return phase==2;}}
    private static Vector3 DeckPoint(Vector3 point)
    {
        point.x=Mathf.Clamp(point.x,-2.2f,2.2f);point.z=Mathf.Clamp(point.z,-5.1f,5.1f);point.y=0.88f;
        if(Mathf.Abs(point.x)<1.3f&&point.z> -3.5f&&point.z<0.8f)point.x=point.x<0?-1.5f:1.5f;
        return point;
    }
    private bool ClearDeck(NavalHull ship,Vector3 point)
    {
        Vector3 world=ship.Root.TransformPoint(point);RaycastHit hit;
        if(!Physics.Raycast(world+ship.Root.up*0.6f,-ship.Root.up,out hit,1f,PhysicsManager.GetEnvironmentLayerMask(),QueryTriggerInteraction.Ignore)||
            !hit.collider.transform.IsChildOf(ship.Root)||Mathf.Abs(ship.Root.InverseTransformPoint(hit.point).y-0.825f)>0.18f)return false;
        return !Physics.CheckCapsule(world+ship.Root.up*0.45f,world+ship.Root.up*1.75f,0.32f,
            PhysicsManager.GetEnvironmentLayerMask(),QueryTriggerInteraction.Ignore);
    }
    public void TryStart(Transform enemy)
    {
        if(Busy||Time.time<retryAt||enemy==null||worker.Defending||worker.Rescuing||worker.Stranded||!robot.IsAlive())return;
        NavalRamDamage ram=home.Root.GetComponent<NavalRamDamage>(),other=enemy.GetComponent<NavalRamDamage>();
        if(ram==null||other==null||other.Hull.Sunk||ram.Opponent!=other||Time.time-ram.LastContact>0.6f)return;
        target=other.Hull;
        launch=DeckPoint(ram.ContactLocal);
        Vector3 centre=DeckPoint(other.ContactLocal);
        float best=8f*8f;bool found=false;
        foreach(Vector3 offset in new Vector3[]{Vector3.zero,new Vector3(0,0,1.3f),new Vector3(0,0,-1.3f),new Vector3(1.3f,0,0),new Vector3(-1.3f,0,0)})
        {
            Vector3 candidate=DeckPoint(centre+offset);
            float distance=(target.Root.TransformPoint(candidate)-home.Root.TransformPoint(launch)).sqrMagnitude;
            if(distance>=best||!ClearDeck(target,candidate))continue;
            landing=candidate;best=distance;found=true;
        }
        if(!found||!ClearDeck(home,launch)){retryAt=Time.time+2f;return;}
        phase=1;started=Time.time;worker.PrepareBoardingPost(launch);
    }
    private void Place(Vector3 feet)
    {
        CharacterController controller=robot.GetComponentInChildren<CharacterController>();
        bool enabled=controller!=null&&controller.enabled;if(enabled)controller.enabled=false;
        robot.transform.position=feet+target.Root.up*worker.FootOffset;
        if(enabled)controller.enabled=true;
    }
    private AIController AI()
    {FirstPersonMover mover=robot as FirstPersonMover;return mover!=null?mover.GetAIController():robot.GetComponent<AIController>();}
    private bool OnTarget(Character candidate)
    {
        if(candidate==null||!candidate.IsAlive()||candidate.IsNeutral||candidate.IsPlayerTeam==robot.IsPlayerTeam)return false;
        Vector3 p=target.Root.InverseTransformPoint(candidate.transform.position);
        if(Mathf.Abs(p.x)>3.2f||Mathf.Abs(p.z)>6.2f||p.y<BoatDimensions.HoldFloor+0.15f||p.y>3.1f)return false;
        RaycastHit hit;
        return Physics.Raycast(candidate.transform.position+target.Root.up*0.2f,-target.Root.up,out hit,4f,
            PhysicsManager.GetEnvironmentLayerMask(),QueryTriggerInteraction.Ignore)&&hit.collider.transform.IsChildOf(target.Root);
    }
    private void LateUpdate()
    {
        if(!Busy||worker.Rescuing||Time.timeScale==0f||robot==null||!robot.IsAlive()||NavalTestWorld.Active!=null&&!NavalTestWorld.Active.SimulationActive)return;
        if(target==null||target.Root==null){phase=4;robot.DisableAI();return;}
        if(phase==1)
        {
            if(worker.Defending||Time.time-started>12f||home.Sunk||target.Sunk)
            {phase=0;retryAt=Time.time+3f;return;}
            NavalRamDamage ram=home.Root.GetComponent<NavalRamDamage>();
            if(Time.time-ram.LastContact>1f){phase=0;retryAt=Time.time+2f;return;}
            if(worker.Busy)return;
            Vector3 feet=home.Root.InverseTransformPoint(robot.transform.position-home.Root.up*worker.FootOffset);
            if((feet-launch).magnitude>0.45f)return;
            if(!ClearDeck(target,landing)||(target.Root.TransformPoint(landing)-robot.transform.position).magnitude>8f)
            {phase=0;retryAt=Time.time+2f;return;}
            launch=feet;phase=2;jumpTime=0f;duration=Mathf.Clamp(Vector3.Distance(home.Root.TransformPoint(launch),target.Root.TransformPoint(landing))/5f,1.2f,2f);
            worker.BeginAssault();robot.DisableAI();
        }
        if(phase==2)
        {
            jumpTime+=Time.deltaTime;float t=Mathf.Clamp01(jumpTime/duration);
            Vector3 from=home.Root.TransformPoint(launch),to=target.Root.TransformPoint(landing);
            if(Vector3.Distance(from,to)>9f){phase=4;return;}
            Vector3 position=Vector3.Lerp(from,to,t)+Vector3.up*Mathf.Sin(t*Mathf.PI)*4.5f;
            Vector3 current=robot.transform.position-target.Root.up*worker.FootOffset;
            Vector3 step=position-current;RaycastHit hit;
            if(t>0.08f&&t<0.95f&&Physics.SphereCast(current+Vector3.up*0.5f,0.25f,step.normalized,out hit,step.magnitude,
                PhysicsManager.GetEnvironmentLayerMask(),QueryTriggerInteraction.Ignore))
            {phase=4;return;}
            Place(position);
            Vector3 look=to-from;look.y=0f;if(look.sqrMagnitude>0.01f)robot.transform.rotation=Quaternion.LookRotation(look);
            if(t>=1f){phase=3;previousBoat=target.Root.localToWorldMatrix;robot.ActivateAI();}
            return;
        }
        if(phase!=3)return;
        Vector3 carried=target.Root.TransformPoint(previousBoat.inverse.MultiplyPoint3x4(robot.transform.position));
        Place(carried-target.Root.up*worker.FootOffset);previousBoat=target.Root.localToWorldMatrix;
        Vector3 local=target.Root.InverseTransformPoint(robot.transform.position);
        if(Mathf.Abs(local.x)>3.3f||Mathf.Abs(local.z)>6.4f||local.y<BoatDimensions.HoldFloor-0.3f)
        {robot.DisableAI();phase=4;return;}
        if(opponent!=null&&!OnTarget(opponent))opponent=null;
        if(Time.time>=scanAt)
        {
            scanAt=Time.time+0.3f;float nearest=float.MaxValue;
            foreach(Character candidate in Object.FindObjectsByType<Character>(FindObjectsSortMode.None))
            {
                if(!OnTarget(candidate))continue;
                float distance=(candidate.transform.position-robot.transform.position).sqrMagnitude;
                if(distance<nearest){nearest=distance;opponent=candidate;}
            }
        }
        AIController ai=AI();
        if(opponent==null){robot.DisableAI();if(ai!=null&&SetOpponent!=null&&ai.GetTargetOpponentCharacter()!=null)SetOpponent.Invoke(ai,new object[]{null});return;}
        robot.ActivateAI();
        if(ai!=null){ai.SetTargetOverrides(false,false);if(ai.GetTargetOpponentCharacter()!=opponent&&SetOpponent!=null)SetOpponent.Invoke(ai,new object[]{opponent});}
    }
}
