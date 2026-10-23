using UnityEngine;
using System.Reflection;
using System.Collections.Generic;
[DefaultExecutionOrder(250)]
public partial class NavalBoarder : MonoBehaviour
{
    private static readonly List<NavalBoarder> All=new List<NavalBoarder>();
    private float reportAt;
    private void Report(string reason)
    {if(finalAttack&&Time.time>=reportAt){reportAt=Time.time+3;Debug.Log("[NavalPrototype] Boarding: "+home.Definition.Name+" "+reason);}}
    private void OnDestroy(){All.Remove(this);}
    private Character robot;
    private NavalRepairSailor worker;
    private NavalHull home,current,departure,destination;
    private Vector3 launch,landing;
    // 0 duty, 1 outbound approach, 2 outbound jump, 3 melee,
    // 4 overboard, 5 return approach, 6 return jump.
    private int phase;
    private bool finalAttack;
    private bool localDefense;
    public bool EnemyAboard(Character candidate){return OnShip(candidate,home);}
    public void Defend(Character target)
    {
        if(!localDefense){worker.BeginDefense();current=home;phase=3;localDefense=true;route.Clear();routedOpponent=null;assaultMelee=false;assaultStalled=0;previousBoat=home.Root.localToWorldMatrix;robot.DisableAI();}
        opponent=target;
    }
    public void StopDefense()
    {
        if(!localDefense)return;localDefense=false;phase=0;opponent=null;route.Clear();routedOpponent=null;assaultMelee=false;worker.BoardingWalk(false);robot.DisableAI();
        AIController ai=AI();if(ai!=null&&SetOpponent!=null)SetOpponent.Invoke(ai,new object[]{null});
        if(!worker.Rescuing)worker.EndDefense();
    }
    private float started,jumpTime,duration,retryAt,scanAt,quietSince=-1f,height,returnAt;
    private Matrix4x4 previousBoat;
    private Character opponent;
    private float returnMoveSpeed;
    private readonly List<Vector3> route=new List<Vector3>();
    private static readonly MethodInfo SetOpponent=typeof(AIController).GetMethod("SetTargetOpponentCharacter",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
    private static readonly FieldInfo Velocity=typeof(FirstPersonMover).GetField("_velocity",BindingFlags.Instance|BindingFlags.NonPublic);
    public bool Busy {get{return phase!=0;}}
    public bool Departed {get{return phase==2||phase==4||phase==3&&current!=home;}}
    public bool InFlight {get{return phase==2||phase==6;}}
    public string Status {get{return localDefense?"Defending ship":phase==1?"Preparing boarding":phase==5?"Returning to own ship":phase==6?"Jumping home":phase==2?"Boarding jump":phase==3?"Boarding melee":"Overboard";}}
    public void Initialize(Character sailor,NavalRepairSailor repair,NavalHull ship)
    {robot=sailor;worker=repair;home=ship;current=home;All.Add(this);}
    public void SuspendForRescue(){localDefense=false;phase=0;opponent=null;route.Clear();}
    public void AdoptShip(NavalHull ship)
    {current=ship;phase=3;opponent=null;quietSince=-1f;previousBoat=ship.Root.localToWorldMatrix;worker.BeginAssault();robot.ActivateAI();}
    private static bool Valid(NavalHull ship){return ship!=null&&ship.Root!=null;}
    private static Vector3 DeckPoint(Vector3 point,NavalHull ship)
    {
        point.x=Mathf.Clamp(point.x,-2.2f,2.2f);point.z=Mathf.Clamp(point.z,-NavalBoardingContactMath.EndStand(ship.WorldScale),NavalBoardingContactMath.EndStand(ship.WorldScale));point.y=ship.Definition.WeatherDeck+.055f;
        if(Mathf.Abs(point.x)<1.3f&&point.z> -3.5f&&point.z<0.8f)point.x=point.x<0?-1.5f:1.5f;
        return point;
    }
    private float Radius()
    {CharacterController body=worker.BodyController;return body!=null?Mathf.Clamp(body.radius*Mathf.Abs(body.transform.lossyScale.x),0.25f,0.45f):0.32f;}
    private float BodyHeight()
    {CharacterController body=worker.BodyController;return body!=null?Mathf.Max(1.8f,body.height*Mathf.Abs(body.transform.lossyScale.y)):2.4f;}
    private static readonly Collider[] bodyHits=new Collider[128];
    private static readonly RaycastHit[] jumpHits=new RaycastHit[128];
    private string obstruction="";
    private bool ClearDeck(NavalHull ship,Vector3 point)
    {
        Vector3 world=ship.Root.TransformPoint(point);RaycastHit hit;
        if(!Physics.Raycast(world+ship.Root.up*.6f,-ship.Root.up,out hit,1.2f,PhysicsManager.GetEnvironmentLayerMask(),QueryTriggerInteraction.Ignore))
        {obstruction="no floor at "+point;return false;}
        if(!hit.collider.transform.IsChildOf(ship.Root)||Mathf.Abs(ship.Root.InverseTransformPoint(hit.point).y-ship.Definition.WeatherDeck)*ship.WorldScale>.35f)
        {obstruction="support "+hit.collider.name+" at "+point;return false;}
        float radius=Radius();
        int count=Physics.OverlapCapsuleNonAlloc(world+Vector3.up*(radius+.08f),world+Vector3.up*(BodyHeight()-radius),radius,bodyHits,PhysicsManager.GetEnvironmentLayerMask(),QueryTriggerInteraction.Ignore);
        if(count>=bodyHits.Length){obstruction="crowded clearance buffer";return false;}
        for(int i=0;i<count;i++)
        {
            Collider obstacle=bodyHits[i];
            // A landing on the supporting plank is valid. Do not reject it just
            // because capsule skin touches that same plank on a rolling deck.
            if(obstacle==null||obstacle==hit.collider||obstacle.transform.IsChildOf(robot.transform)||obstacle.GetComponentInParent<Character>()!=null)continue;
            obstruction="body blocked by "+obstacle.name+" at "+point;return false;
        }
        return true;
    }
    private static float RailTop(NavalHull ship,Vector3 near)
    {
        // Use the rail at this crossing, not the highest opposite corner of a heeling ship.
        float top=float.NegativeInfinity;
        for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)
            top=Mathf.Max(top,ship.Root.TransformPoint(new Vector3(near.x+x*.25f,ship.Definition.AboveDeckY(2.25f)+ship.Definition.UpperDeckOffset,near.z+z*.25f)).y);
        return top;
    }
    private Vector3 Arc(float t)
    {
        Vector3 from=departure.Root.TransformPoint(launch),to=destination.Root.TransformPoint(landing);
        return Vector3.Lerp(from,to,NavalBoardingJumpMath.TravelFraction(t))+Vector3.up*NavalBoardingJumpMath.Lift(t,height);
    }
    private bool SweepClear(Vector3 from,Vector3 to)
    {
        Vector3 delta=to-from;if(delta.sqrMagnitude<0.000001f)return true;
        float radius=Radius();
        int count=Physics.CapsuleCastNonAlloc(from+Vector3.up*(radius+.08f),from+Vector3.up*(BodyHeight()-radius),radius,delta.normalized,jumpHits,delta.magnitude,PhysicsManager.GetEnvironmentLayerMask(),QueryTriggerInteraction.Ignore);
        if(count>=jumpHits.Length){obstruction="crowded arc buffer";return false;}
        for(int i=0;i<count;i++)if(jumpHits[i].collider!=null&&!jumpHits[i].collider.transform.IsChildOf(robot.transform))
        {obstruction="arc blocked by "+jumpHits[i].collider.name;return false;}
        return true;
    }
    private bool PrepareArc()
    {
        Vector3 from=departure.Root.TransformPoint(launch),to=destination.Root.TransformPoint(landing);
        height=NavalBoardingJumpMath.Height(from.y,to.y,RailTop(departure,launch),RailTop(destination,landing));
        if(!NavalBoardingJumpMath.Reachable(Vector3.Distance(from,to),height)){obstruction="range="+Vector3.Distance(from,to).ToString("F1")+" lift="+height.ToString("F1");return false;}
        for(int attempt=0;attempt<4;attempt++)
        {
            if(height>8f)break;
            Vector3 previous=from;bool clear=true;
            for(int i=1;i<=32;i++){Vector3 p=Arc(i/32f);if(!SweepClear(previous,p)){clear=false;break;}previous=p;}
            if(clear)return true;height+=.5f;
        }
        return false;
    }
    private struct Crossing
    {
        public Vector3 A,B;public float Distance;
    }
    private readonly List<Crossing> crossings=new List<Crossing>(576);
    private readonly List<Vector3> launchPoints=new List<Vector3>(24),landingPoints=new List<Vector3>(24);
    private static void EdgePoints(NavalHull ship,List<Vector3> points)
    {
        points.Clear();
        for(int side=-1;side<=1;side+=2)
        {
            for(int i=0;i<=8;i++)points.Add(DeckPoint(new Vector3(side*2.2f,0,-5.1f+i*1.275f),ship));
            for(int row=0;row<3;row++)for(int i=-2;i<=2;i++)points.Add(DeckPoint(new Vector3(i*.8f,0,side*(NavalBoardingContactMath.EndStand(ship.WorldScale)-row*.15f)),ship));
        }
    }
    private bool HasBoardingContact(NavalHull from,NavalHull to)
    {
        if(!Valid(from)||!Valid(to))return false;
        var ram=from.Root.GetComponent<NavalRamDamage>();
        return ram!=null&&ram.BoardingContactWith(to.Root.GetComponent<NavalRamDamage>());
    }
    private bool Plan(NavalHull from,NavalHull to)
    {
        if(!Valid(from)||!Valid(to)||to.Sunk)return false;
        if(Vector3.Distance(from.Root.position,to.Root.position)>6.5f*(from.WorldScale+to.WorldScale)+8f)return false;
        EdgePoints(from,launchPoints);EdgePoints(to,landingPoints);crossings.Clear();
        foreach(Vector3 a in launchPoints)foreach(Vector3 b in landingPoints)
        {
            float distance=(from.Root.TransformPoint(a)-to.Root.TransformPoint(b)).sqrMagnitude;
            if(distance<=NavalBoardingJumpMath.MaxDistance*NavalBoardingJumpMath.MaxDistance)
                crossings.Add(new Crossing{A=a,B=b,Distance=distance});
        }
        crossings.Sort(delegate(Crossing a,Crossing b){return a.Distance.CompareTo(b.Distance);});
        departure=from;destination=to;int checkedArcs=0;
        foreach(var crossing in crossings)
        {
            bool reserved=false;
            foreach(var other in All)if(other!=this&&other!=null&&other.robot!=null&&other.robot.IsAlive()&&(other.phase==1||other.InFlight))
            {
                if(other.departure==from&&(from.Root.TransformPoint(crossing.A)-from.Root.TransformPoint(other.launch)).sqrMagnitude<.9f*.9f||other.destination==to&&(to.Root.TransformPoint(crossing.B)-to.Root.TransformPoint(other.landing)).sqrMagnitude<.9f*.9f){reserved=true;break;}
            }
            if(reserved||!ClearDeck(from,crossing.A)||!ClearDeck(to,crossing.B))continue;
            launch=crossing.A;landing=crossing.B;
            if(PrepareArc())return true;
            if(++checkedArcs>=24)break;
        }
        return false;
    }
    public void TryStart(Transform enemy,bool lastAttack=false)
    {
        if(lastAttack)finalAttack=true;
        if(Busy||Time.time<retryAt||enemy==null||worker.Defending||worker.Rescuing||worker.Stranded||!robot.IsAlive())return;
        retryAt=Time.time+1.5f;
        NavalRamDamage other=enemy.GetComponent<NavalRamDamage>();if(other==null)return;
        if(!HasBoardingContact(home,other.Hull)){Report("waiting for collision / nearby contact");return;}
        bool occupied=false;
        foreach(Character candidate in NavalCharacters.Snapshot())if(OnShip(candidate,other.Hull)){occupied=true;break;}
        if(!occupied&&!finalAttack)return;
        if(!Plan(home,other.Hull)){Report("no crossing: pairs="+crossings.Count+" "+obstruction);return;}
        phase=1;started=Time.time;worker.PrepareBoardingPost(launch);Report("approaching launch "+launch+" -> "+landing);
    }
    private void ZeroVelocity()
    {FirstPersonMover mover=robot as FirstPersonMover;if(mover!=null&&Velocity!=null)Velocity.SetValue(mover,Vector3.zero);}
    private void PlaceRoot(Vector3 position)
    {
        CharacterController controller=worker.BodyController;
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
        if(Mathf.Abs(p.x)>3.2f||Mathf.Abs(p.z)>6.2f||p.y<ship.Floor+0.15f||p.y>ship.Definition.WeatherDeck+2.275f)return false;
        Vector3 foot;
        return TryAssaultFeet(candidate,ship,out foot);
    }
    private void BuildReturnRoute()
    {
        route.Clear();Vector3 feet=current.Root.InverseTransformPoint(robot.transform.position-Vector3.up*worker.FootOffset);
        foreach(NavalWalkPoint point in NavalSupplyRoutes.Plan(new NavalWalkPoint(feet.x,feet.y,feet.z),
            new NavalWalkPoint(launch.x,launch.y,launch.z),current.Definition))
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
        if(!returning&&!HasBoardingContact(departure,destination)){CancelApproach();return;}
        Vector3 actual=departure.Root.InverseTransformPoint(robot.transform.position-Vector3.up*worker.FootOffset);
        launch=actual;
        if(!ClearDeck(destination,landing)||!PrepareArc()){CancelApproach();return;}
        phase=returning?6:2;jumpTime=0f;Report("jump started");
        duration=Mathf.Clamp(Vector3.Distance(departure.Root.TransformPoint(launch),destination.Root.TransformPoint(landing))/5f,1.3f,2.2f);
        worker.BeginAssault();robot.DisableAI();ZeroVelocity();
    }
    private void UpdateReturn(float dt)
    {
        robot.DisableAI();ZeroVelocity();
        if(Time.time-started>45f||!Valid(destination)||destination.Sunk){CancelApproach();return;}
        if(route.Count==0){BeginJump(true);return;}
        MoveOnBoardingRoute(dt);
    }
    private void MoveOnBoardingRoute(float dt)
    {
        Vector3 feet=current.Root.InverseTransformPoint(robot.transform.position-Vector3.up*worker.FootOffset);
        Vector3 delta=current.Root.TransformVector(route[0]-feet),before=robot.transform.position;
        Vector3 look=delta;look.y=0f;
        if(look.sqrMagnitude>0.01f)robot.transform.rotation=Quaternion.RotateTowards(robot.transform.rotation,Quaternion.LookRotation(look),720f*dt);
        float alignment=look.sqrMagnitude>0.01f?Mathf.Clamp01(Vector3.Dot(robot.transform.forward,look.normalized)):1f;
        bool stairs=NavalShipRoutes.InStairwell(new NavalWalkPoint(feet.x,feet.y,feet.z),current.Definition)||NavalShipRoutes.InStairwell(new NavalWalkPoint(route[0].x,route[0].y,route[0].z),current.Definition);
        float wanted=NavalCrewMovement.TargetSpeed(worker.DeckWalkSpeed,alignment,delta.magnitude,route.Count==1,stairs);
        returnMoveSpeed=NavalCrewMovement.Accelerate(returnMoveSpeed,wanted,dt);
        CharacterController body=worker.BodyController;Vector3 step=Vector3.ClampMagnitude(delta,returnMoveSpeed*dt)+Vector3.down*0.3f*dt;
        if(body!=null&&body.enabled){NavalCrewPassage.Prepare(body);body.Move(step);}else robot.transform.position+=step;
        Vector3 travel=robot.transform.position-before;travel.y=0f;worker.BoardingMoved(travel.magnitude);
        feet=current.Root.InverseTransformPoint(robot.transform.position-Vector3.up*worker.FootOffset);
        if(stairs?NavalWalkRoutes.Reached(feet.x-route[0].x,feet.y-route[0].y,feet.z-route[0].z,current.WorldScale):
            Vector3.Distance(feet,route[0])<(route.Count==1?0.1f:0.28f))route.RemoveAt(0);
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
            if(worker.Defending||!finalAttack&&(home.HasActiveLeaks||home.RepairRetreat)||Time.time-started>45f||!Valid(destination)||home.Sunk||destination.Sunk||!HasBoardingContact(home,destination)){CancelApproach();return;}
            if(worker.Busy)return;
            Vector3 feet=home.Root.InverseTransformPoint(robot.transform.position-Vector3.up*worker.FootOffset);
            if((feet-launch).magnitude<0.2f)
            {
                BeginJump(false);
            }
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
        if(localDefense)
        {
            if(opponent==null){StopDefense();return;}
            TickAssault(dt,AI());return;
        }
        if(Time.time>=scanAt)
        {
            scanAt=Time.time+.3f;Character selected=opponent;
            float nearest=selected!=null?AssaultTravelCost(selected)*.7f:float.MaxValue;
            foreach(Character candidate in NavalCharacters.Snapshot())
            {
                if(!OnShip(candidate,current))continue;float distance=AssaultTravelCost(candidate);
                if(distance<nearest){nearest=distance;selected=candidate;}
            }
            opponent=selected;
        }
        if(current.Sunk||!finalAttack&&home.RepairRetreat){TryReturn();if(phase==5)return;}
        AIController ai=AI();
        if(opponent==null)
        {
            route.Clear();routedOpponent=null;assaultMelee=false;worker.BoardingWalk(false);
            robot.DisableAI();if(ai!=null&&SetOpponent!=null&&ai.GetTargetOpponentCharacter()!=null)SetOpponent.Invoke(ai,new object[]{null});
            if(quietSince<0f)quietSince=Time.time;if(Time.time-quietSince>=6f)TryReturn();return;
        }
        quietSince=-1f;TickAssault(dt,ai);
    }
}
