using UnityEngine;
using System.Reflection;
using System.Collections.Generic;
[DefaultExecutionOrder(250)]
public class NavalBoarder : MonoBehaviour
{
    private Character robot;
    private NavalRepairSailor worker;
    private NavalHull home,current,departure,destination;
    private Vector3 launch,landing;
    // 0 duty, 1 outbound approach, 2 outbound jump, 3 melee,
    // 4 overboard, 5 return approach, 6 return jump.
    private int phase;
    private float started,jumpTime,duration,retryAt,scanAt,quietSince=-1f,height,returnAt;
    private Matrix4x4 previousBoat;
    private Character opponent;
    private float returnMoveSpeed;
    private readonly List<Vector3> route=new List<Vector3>();
    private static readonly MethodInfo SetOpponent=typeof(AIController).GetMethod("SetTargetOpponentCharacter",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
    private static readonly FieldInfo Velocity=typeof(FirstPersonMover).GetField("_velocity",BindingFlags.Instance|BindingFlags.NonPublic);
    public bool Busy {get{return phase!=0;}}
    public bool InFlight {get{return phase==2||phase==6;}}
    public string Status {get{return phase==1?"Preparing boarding":phase==5?"Returning to own ship":phase==6?"Jumping home":phase==2?"Boarding jump":phase==3?"Boarding melee":"Overboard";}}
    public void Initialize(Character sailor,NavalRepairSailor repair,NavalHull ship)
    {robot=sailor;worker=repair;home=ship;current=home;}
    public void SuspendForRescue(){phase=0;opponent=null;route.Clear();}
    public void AdoptShip(NavalHull ship)
    {current=ship;phase=3;opponent=null;quietSince=-1f;previousBoat=ship.Root.localToWorldMatrix;worker.BeginAssault();robot.ActivateAI();}
    private static bool Valid(NavalHull ship){return ship!=null&&ship.Root!=null;}
    private static Vector3 DeckPoint(Vector3 point)
    {
        point.x=Mathf.Clamp(point.x,-2.2f,2.2f);point.z=Mathf.Clamp(point.z,-5.1f,5.1f);point.y=0.88f;
        if(Mathf.Abs(point.x)<1.3f&&point.z> -3.5f&&point.z<0.8f)point.x=point.x<0?-1.5f:1.5f;
        return point;
    }
    private float Radius()
    {CharacterController body=robot.GetComponentInChildren<CharacterController>();return body!=null?Mathf.Clamp(body.radius*Mathf.Abs(body.transform.lossyScale.x),0.25f,0.45f):0.32f;}
    private float BodyHeight()
    {CharacterController body=robot.GetComponentInChildren<CharacterController>();return body!=null?Mathf.Max(1.8f,body.height*Mathf.Abs(body.transform.lossyScale.y)):2.4f;}
    private bool ClearDeck(NavalHull ship,Vector3 point)
    {
        Vector3 world=ship.Root.TransformPoint(point);RaycastHit hit;
        if(!Physics.Raycast(world+ship.Root.up*0.6f,-ship.Root.up,out hit,1f,PhysicsManager.GetEnvironmentLayerMask(),QueryTriggerInteraction.Ignore)||
            !hit.collider.transform.IsChildOf(ship.Root)||Mathf.Abs(ship.Root.InverseTransformPoint(hit.point).y-0.825f)>0.18f)return false;
        float radius=Radius();
        return !Physics.CheckCapsule(world+Vector3.up*(radius+0.08f),world+Vector3.up*(BodyHeight()-radius),radius,
            PhysicsManager.GetEnvironmentLayerMask(),QueryTriggerInteraction.Ignore);
    }
    private static float RailTop(NavalHull ship)
    {
        float top=float.NegativeInfinity;
        for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)
            top=Mathf.Max(top,ship.Root.TransformPoint(new Vector3(x*3.15f,2.25f,z*6.2f)).y);
        return top;
    }
    private Vector3 Arc(float t)
    {
        Vector3 from=departure.Root.TransformPoint(launch),to=destination.Root.TransformPoint(landing);
        return Vector3.Lerp(from,to,t)+Vector3.up*NavalBoardingJumpMath.Lift(t,height);
    }
    private bool SweepClear(Vector3 from,Vector3 to)
    {
        Vector3 delta=to-from;if(delta.sqrMagnitude<0.000001f)return true;
        float radius=Radius();RaycastHit hit;
        return !Physics.CapsuleCast(from+Vector3.up*(radius+0.08f),from+Vector3.up*(BodyHeight()-radius),radius,
            delta.normalized,out hit,delta.magnitude,PhysicsManager.GetEnvironmentLayerMask(),QueryTriggerInteraction.Ignore);
    }
    private bool PrepareArc()
    {
        Vector3 from=departure.Root.TransformPoint(launch),to=destination.Root.TransformPoint(landing);
        height=NavalBoardingJumpMath.Height(from.y,to.y,RailTop(departure),RailTop(destination));
        if(!NavalBoardingJumpMath.Reachable(Vector3.Distance(from,to),height))return false;
        Vector3 previous=from;
        for(int i=1;i<=32;i++){Vector3 p=Arc(i/32f);if(!SweepClear(previous,p))return false;previous=p;}
        return true;
    }
    private bool Plan(NavalHull from,NavalHull to)
    {
        if(!Valid(from)||!Valid(to)||to.Sunk||Vector3.Distance(from.Root.position,to.Root.position)>36f)return false;
        Vector3 a,b;NavalRamDamage.Closest(from.Root,to.Root,out a,out b);
        Vector3 start=DeckPoint(from.Root.InverseTransformPoint(a)),end=DeckPoint(to.Root.InverseTransformPoint(b));
        Vector3[] offsets={Vector3.zero,new Vector3(0,0,1.2f),new Vector3(0,0,-1.2f),new Vector3(1.2f,0,0),new Vector3(-1.2f,0,0)};
        departure=from;destination=to;int checkedArcs=0;
        foreach(Vector3 offsetA in offsets)
        {
            Vector3 pointA=DeckPoint(start+offsetA);if(!ClearDeck(from,pointA))continue;
            foreach(Vector3 offsetB in offsets)
            {
                Vector3 pointB=DeckPoint(end+offsetB);
                if(Vector3.Distance(from.Root.TransformPoint(pointA),to.Root.TransformPoint(pointB))>NavalBoardingJumpMath.MaxDistance||!ClearDeck(to,pointB))continue;
                if(checkedArcs++>=6)return false;
                launch=pointA;landing=pointB;if(PrepareArc())return true;
            }
        }
        return false;
    }
    public void TryStart(Transform enemy)
    {
        if(Busy||Time.time<retryAt||enemy==null||worker.Defending||worker.Rescuing||worker.Stranded||!robot.IsAlive())return;
        retryAt=Time.time+1.5f;
        NavalRamDamage other=enemy.GetComponent<NavalRamDamage>();if(other==null)return;
        bool occupied=false;
        foreach(Character candidate in NavalCharacters.Snapshot())if(OnShip(candidate,other.Hull)){occupied=true;break;}
        if(!occupied||!Plan(home,other.Hull))return;
        phase=1;started=Time.time;worker.PrepareBoardingPost(launch);
    }
    private void ZeroVelocity()
    {FirstPersonMover mover=robot as FirstPersonMover;if(mover!=null&&Velocity!=null)Velocity.SetValue(mover,Vector3.zero);}
    private void PlaceRoot(Vector3 position)
    {
        CharacterController controller=robot.GetComponentInChildren<CharacterController>();
        bool enabled=controller!=null&&controller.enabled;if(enabled)controller.enabled=false;
        robot.transform.position=position;if(enabled)controller.enabled=true;
    }
    private void Carry()
    {
        PlaceRoot(current.Root.TransformPoint(previousBoat.inverse.MultiplyPoint3x4(robot.transform.position)));
        previousBoat=current.Root.localToWorldMatrix;
    }
    private AIController AI()
    {FirstPersonMover mover=robot as FirstPersonMover;return mover!=null?mover.GetAIController():robot.GetComponent<AIController>();}
    private bool OnShip(Character candidate,NavalHull ship)
    {
        if(candidate==null||!candidate.IsAlive()||candidate.IsNeutral||candidate.IsPlayerTeam==robot.IsPlayerTeam)return false;
        if(!Valid(ship))return false;
        Vector3 p=ship.Root.InverseTransformPoint(candidate.transform.position);
        if(Mathf.Abs(p.x)>3.2f||Mathf.Abs(p.z)>6.2f||p.y<ship.Floor+0.15f||p.y>3.1f)return false;
        RaycastHit hit;
        return Physics.Raycast(candidate.transform.position+ship.Root.up*0.2f,-ship.Root.up,out hit,4f,
            PhysicsManager.GetEnvironmentLayerMask(),QueryTriggerInteraction.Ignore)&&hit.collider.transform.IsChildOf(ship.Root);
    }
    private void BuildReturnRoute()
    {
        route.Clear();Vector3 feet=current.Root.InverseTransformPoint(robot.transform.position-current.Root.up*worker.FootOffset);
        foreach(NavalWalkPoint point in NavalRepairRoutes.Plan(new NavalWalkPoint(feet.x,feet.y,feet.z),
            new NavalWalkPoint(launch.x,launch.y,launch.z),current.Definition.Compartments,current.Definition.StairWidth,current.Floor))
            route.Add(new Vector3(point.X,point.Y,point.Z));
    }
    private void TryReturn()
    {
        if(Time.time<returnAt||!Valid(home)||home.Sunk)return;
        returnAt=Time.time+2f;if(!Plan(current,home))return;
        robot.DisableAI();worker.BoardingWalk(true);BuildReturnRoute();phase=5;started=Time.time;returnMoveSpeed=0f;
    }
    private void CancelApproach()
    {
        retryAt=Time.time+2f;
        if(phase==1){phase=0;return;}
        phase=3;worker.BoardingWalk(false);quietSince=Time.time;returnAt=Time.time+2f;
    }
    private void BeginJump(bool returning)
    {
        Vector3 actual=departure.Root.InverseTransformPoint(robot.transform.position-departure.Root.up*worker.FootOffset);
        launch=actual;
        if(!ClearDeck(destination,landing)||!PrepareArc()){CancelApproach();return;}
        phase=returning?6:2;jumpTime=0f;
        duration=Mathf.Clamp(Vector3.Distance(departure.Root.TransformPoint(launch),destination.Root.TransformPoint(landing))/5f,1.3f,2.2f);
        worker.BeginAssault();robot.DisableAI();ZeroVelocity();
    }
    private void UpdateReturn(float dt)
    {
        robot.DisableAI();ZeroVelocity();
        if(Time.time-started>16f||!Valid(destination)||destination.Sunk){CancelApproach();return;}
        if(route.Count==0){BeginJump(true);return;}
        Vector3 feet=current.Root.InverseTransformPoint(robot.transform.position-current.Root.up*worker.FootOffset);
        Vector3 delta=current.Root.TransformVector(route[0]-feet),before=robot.transform.position;
        Vector3 look=delta;look.y=0f;
        if(look.sqrMagnitude>0.01f)robot.transform.rotation=Quaternion.RotateTowards(robot.transform.rotation,Quaternion.LookRotation(look),720f*dt);
        float alignment=look.sqrMagnitude>0.01f?Mathf.Clamp01(Vector3.Dot(robot.transform.forward,look.normalized)):1f;
        bool stairs=NavalWalkRoutes.InStairwell(feet.x,feet.y,feet.z,current.Definition.StairWidth*.5f+.175f,current.Floor)||NavalWalkRoutes.InStairwell(route[0].x,route[0].y,route[0].z,current.Definition.StairWidth*.5f+.175f,current.Floor);
        float wanted=NavalCrewMovement.TargetSpeed(worker.DeckWalkSpeed,alignment,delta.magnitude,route.Count==1,stairs);
        returnMoveSpeed=NavalCrewMovement.Accelerate(returnMoveSpeed,wanted,dt);
        CharacterController body=robot.GetComponentInChildren<CharacterController>();Vector3 step=Vector3.ClampMagnitude(delta,returnMoveSpeed*dt)+Vector3.down*0.3f*dt;
        if(body!=null&&body.enabled)body.Move(step);else robot.transform.position+=step;
        Vector3 travel=robot.transform.position-before;travel.y=0f;worker.BoardingMoved(travel.magnitude);
        feet=current.Root.InverseTransformPoint(robot.transform.position-current.Root.up*worker.FootOffset);
        if(Vector3.Distance(feet,route[0])<(route.Count==1?0.1f:0.28f))route.RemoveAt(0);
    }
    private void RecoverLanding()
    {
        Vector3 feet=robot.transform.position-Vector3.up*worker.FootOffset;RaycastHit hit;
        if(!Physics.Raycast(feet+Vector3.up*0.2f,Vector3.down,out hit,0.5f,PhysicsManager.GetEnvironmentLayerMask(),QueryTriggerInteraction.Ignore))return;
        NavalRamDamage ship=hit.collider.GetComponentInParent<NavalRamDamage>();
        if(ship==null||ship.Hull.Sunk)return;
        if(ship.Hull==home){phase=0;worker.EndRescue();retryAt=Time.time+3f;}
        else AdoptShip(ship.Hull);
    }
    private void LateUpdate()
    {
        if(!Busy||worker.Rescuing||Time.timeScale==0f||robot==null||!robot.IsAlive()||NavalTestWorld.Active!=null&&!NavalTestWorld.Active.SimulationActive)return;
        float dt=Mathf.Min(Time.deltaTime,0.05f);
        if(phase==4){RecoverLanding();return;}
        if(phase==1)
        {
            if(worker.Defending||home.HasActiveLeaks||home.RepairRetreat||Time.time-started>16f||!Valid(destination)||home.Sunk||destination.Sunk){CancelApproach();return;}
            if(worker.Busy)return;
            Vector3 feet=home.Root.InverseTransformPoint(robot.transform.position-home.Root.up*worker.FootOffset);
            if((feet-launch).magnitude<0.45f)BeginJump(false);
            return;
        }
        if(InFlight)
        {
            if(!Valid(departure)||!Valid(destination)){phase=4;robot.DisableAI();return;}
            jumpTime+=dt;float t=Mathf.Clamp01(jumpTime/duration);
            Vector3 from=departure.Root.TransformPoint(launch),to=destination.Root.TransformPoint(landing);
            if(Vector3.Distance(from,to)>9f){phase=4;return;}
            Vector3 position=Arc(t),feet=robot.transform.position-Vector3.up*worker.FootOffset;
            if(!SweepClear(feet,position)){phase=4;robot.DisableAI();return;}
            PlaceRoot(position+Vector3.up*worker.FootOffset);ZeroVelocity();
            Vector3 look=to-from;look.y=0f;if(look.sqrMagnitude>0.01f)robot.transform.rotation=Quaternion.LookRotation(look);
            if(t>=1f)
            {
                current=destination;previousBoat=current.Root.localToWorldMatrix;quietSince=-1f;
                if(current==home){phase=0;worker.EndRescue();retryAt=Time.time+3f;}
                else{phase=3;robot.ActivateAI();}
            }
            return;
        }
        if(!Valid(current)){phase=4;robot.DisableAI();return;}
        Carry();
        Vector3 local=current.Root.InverseTransformPoint(robot.transform.position);
        if(Mathf.Abs(local.x)>3.3f||Mathf.Abs(local.z)>6.4f||local.y<current.Floor-0.3f){robot.DisableAI();phase=4;return;}
        if(phase==5){UpdateReturn(dt);return;}
        if(opponent!=null&&!OnShip(opponent,current))opponent=null;
        if(Time.time>=scanAt)
        {
            scanAt=Time.time+0.3f;float nearest=float.MaxValue;opponent=null;
            foreach(Character candidate in NavalCharacters.Snapshot())
            {
                if(!OnShip(candidate,current))continue;float distance=(candidate.transform.position-robot.transform.position).sqrMagnitude;
                if(distance<nearest){nearest=distance;opponent=candidate;}
            }
        }
        if(current.Sunk||home.RepairRetreat){TryReturn();if(phase==5)return;}
        AIController ai=AI();
        if(opponent==null)
        {
            robot.DisableAI();if(ai!=null&&SetOpponent!=null&&ai.GetTargetOpponentCharacter()!=null)SetOpponent.Invoke(ai,new object[]{null});
            if(quietSince<0f)quietSince=Time.time;if(Time.time-quietSince>=2f)TryReturn();return;
        }
        quietSince=-1f;robot.ActivateAI();
        if(ai!=null){ai.SetTargetOverrides(false,false);if(ai.GetTargetOpponentCharacter()!=opponent&&SetOpponent!=null)SetOpponent.Invoke(ai,new object[]{opponent});}
    }
}
