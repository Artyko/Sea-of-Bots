using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public partial class NavalRepairSailor : MonoBehaviour
{
    private Character robot;
    private NavalHull hull;
    private NavalStructure structure;
    private Transform boat;
    private Vector3 home,feet;
    private float footOffset,stuck,scanAt;
    private int scanRevision=-1;
    private readonly NavalRepairProgress progress=new NavalRepairProgress();
    private float work { get { return progress.Elapsed; } }
    private NavalHullVoxel leak;
    private readonly List<Vector3> route=new List<Vector3>();
    private readonly List<NavalHullVoxel> claimed=new List<NavalHullVoxel>();
    private readonly Dictionary<NavalHullVoxel,float> retryAfter=new Dictionary<NavalHullVoxel,float>();
    private readonly List<NavalHullVoxel> candidates=new List<NavalHullVoxel>();
    private readonly Dictionary<NavalHullVoxel,float> priorities=new Dictionary<NavalHullVoxel,float>();
    private bool returning,stranded;
    private bool changingPost;
    public bool RepairDuty { get { return pumpDuty || bailState!=0 || leak!=null || returning&&!changingPost; } }
    public void SetPost(Vector3 post,bool force=false)
    {
        if((!force&&(home-post).sqrMagnitude<0.01f)||RepairDuty||stranded)return;
        feet=boat.InverseTransformPoint(robot.transform.position-boat.up*footOffset);
        home=post;route.Clear();returning=true;changingPost=true;
        PlanTo(home);
    }
    private Transform hammer;
    private Quaternion hammerRest;
    private CharacterModel model;
    private NavalSailorGait gait;
    private float moveSpeed;
    public bool Stranded { get { return stranded; } }
    public bool Defending {get;private set;}
    public bool Assaulting {get;private set;}
    public bool Rescuing {get;private set;}
    public string RescueStatus {get{NavalSailorRescue rescue=GetComponent<NavalSailorRescue>();return rescue!=null?rescue.Status:"Rescuing";}}
    public void BeginRescue()
    {Release();route.Clear();returning=false;changingPost=false;stranded=false;Defending=false;Assaulting=false;Rescuing=true;if(gait!=null)gait.enabled=false;robot.DisableAI();}
    public void EndRescue()
    {Rescuing=false;stranded=false;Defending=false;Assaulting=false;feet=boat.InverseTransformPoint(robot.transform.position-boat.up*footOffset);home=feet;returning=false;if(gait!=null)gait.enabled=true;}
    public void PrepareBoardingPost(Vector3 post)
    {Release();route.Clear();returning=false;changingPost=false;SetPost(post,true);}
    public void BeginAssault(){BeginDefense();Assaulting=true;}
    public void BoardingWalk(bool active)
    {if(gait!=null){gait.enabled=active;if(!active)gait.Stop();}}
    public void BoardingMoved(float distance){if(gait!=null)gait.Moved(distance);}
    public void BeginDefense()
    {
        Release();route.Clear();returning=false;changingPost=false;Defending=true;
        if(gait!=null)gait.enabled=false;
    }
    public void EndDefense()
    {
        Defending=false;robot.DisableAI();
        if(gait!=null)gait.enabled=true;
        feet=boat.InverseTransformPoint(robot.transform.position-boat.up*footOffset);
        BeginReturn();
    }
    public float CarriedSide {get{return feet.x;}}
    public float FootOffset { get { return footOffset; } }
    public float DeckWalkSpeed {get{return NavalCrewMovement.BaseSpeed(robot.GetBaseMoveSpeed());}}
    private static readonly FieldInfo Velocity=typeof(FirstPersonMover).GetField("_velocity",BindingFlags.Instance|BindingFlags.NonPublic);
    public bool Busy { get { return pumpDuty||bailState!=0||leak!=null||returning; } }
    public bool HasJob { get { return leak!=null; } }
    public Vector3 JobPoint { get { return leak!=null?leak.LocalCenter:Vector3.zero; } }
    public string Status { get { return pumpDuty?(route.Count>0?"Going to pump":hull.Bilge.Status):bailState!=0?BailStatus:leak!=null?(route.Count>0?"Going to leak":"Repair "+Mathf.RoundToInt(work/4f*100f)+"%"):returning?"Returning":"On duty"; } }
    public void Initialize(Character sailor,NavalHull owner,NavalStructure frame,Vector3 station,float feetOffset)
    {
        robot=sailor;hull=owner;structure=frame;boat=owner.Root;home=station;footOffset=feetOffset;feet=home;
        CharacterController bodyController=Controller();
        if(bodyController!=null&&bodyController.enabled&&bodyController.bounds.size.y>0.5f)
        {float offset=robot.transform.position.y-bodyController.bounds.min.y;if(offset>=0f&&offset<4f)footOffset=offset;}
        hull.RepairWorkers.Add(this);
        model=robot.GetComponentInChildren<CharacterModel>();
        if(model!=null)
        {
            WeaponModel tool=model.GetWeaponModel(WeaponType.Hammer);
            if(tool!=null){hammer=tool.transform;hammerRest=hammer.localRotation;}
            gait=robot.gameObject.AddComponent<NavalSailorGait>();gait.Initialize(robot,model);
        }
    }
    public void Tick(float dt,bool mayTakeJob)
    {
        if(Defending||Rescuing)return;
        if(robot==null||!robot.IsAlive()||hull.Sunk||stranded){Release();return;}
        if(TickPump(dt,mayTakeJob))return;
        if(TickBailing(dt,mayTakeJob))return;
        if(leak==null&&(mayTakeJob||RepairDuty)&&(Time.time>=scanAt||scanRevision!=hull.Revision))
        {if(Busy)HoldPosition();scanAt=Time.time+0.25f;if(scanRevision!=hull.Revision)retryAfter.Clear();scanRevision=hull.Revision;FindLeak();}
        if(leak!=null)
        {
            // Only accept flowing leaks, but finish an accepted patch across wave troughs.
            progress.StepAccepted(dt,route.Count==0&&CanReachLeak());
            if(!leak.Missing&&!leak.Detached)ContinueRepair();
        }
        if(!Busy)return;
        robot.DisableAI();
        if(route.Count>0)
        {
            Vector3 destination=route[0];
            float before=Vector3.Distance(feet,destination);
            Move(destination,dt,route.Count==1);
            if(leak!=null&&route.Count==1&&Mathf.Abs(feet.y-destination.y)<0.3f&&
                new Vector2(feet.x-destination.x,feet.z-destination.z).magnitude<0.4f&&CanReachLeak())
            {route.Clear();stuck=0f;}
            if(route.Count>0&&NavalRouteArrival.Reached(feet.x,feet.y,feet.z,destination.x,destination.y,destination.z,route.Count==1)){route.RemoveAt(0);stuck=0f;}
            else if(before-Vector3.Distance(feet,destination)<dt*0.1f)stuck+=dt;else stuck=0f;
            if(feet.y<BoatDimensions.HoldFloor-0.7f){stranded=true;Release();return;}
            if(stuck>5f){if(returning&&leak==null)ReplanReturn();else AbandonBlockedJob();stuck=0f;}
            if(returning&&route.Count==0){returning=false;changingPost=false;returnRetries=0;Walking(false,dt);}
        }
        else if(returning)
        {
            if(NavalRouteArrival.Reached(feet.x,feet.y,feet.z,home.x,home.y,home.z,true))
            {returning=false;changingPost=false;returnRetries=0;Walking(false,dt);}
            else ReplanReturn();
        }
        else if(leak!=null)
        {
            Walking(false,dt);
            HoldPosition();
            Vector3 face=boat.TransformPoint(leak.LocalCenter)-robot.transform.position;face.y=0f;
            if(face.sqrMagnitude>0.01f)robot.transform.rotation=Quaternion.LookRotation(face);
            if(hammer!=null)hammer.localRotation=hammerRest*Quaternion.Euler(-35f+Mathf.Sin(work*12f)*45f,0f,0f);
            if(!CanReachLeak())
            {
                Vector3 alternative;
                if(FindWorkPoint(leak,out alternative)&&Mathf.Abs(alternative.y-feet.y)<0.3f&&(alternative-feet).sqrMagnitude>0.04f)
                    route.Add(alternative);
                else AbandonBlockedJob();
                return;
            }
            if(work>=4f)
            {
                Vector3 patch=leak.LocalCenter;
                foreach(NavalHullVoxel cell in claimed)
                    if(cell.RepairClaim==this&&(cell.HydraulicShell||cell.InternalBarrier)&&(cell.Missing||cell.Detached)&&
                        (cell.LocalCenter-patch).sqrMagnitude<=0.85f*0.85f)structure.RestoreCell(cell);
                ContinueRepair();
            }
        }
    }
    private void FindLeak()
    {
        feet=boat.InverseTransformPoint(robot.transform.position-boat.up*footOffset);
        candidates.Clear();priorities.Clear();NavalHullVoxel selected=null;
        foreach(NavalHullVoxel cell in hull.Voxels)
            if((cell.HydraulicShell||cell.InternalBarrier)&&(cell.Missing||cell.Detached)&&cell.RepairClaim==null)
            {
                float priority=NavalLeakPriority.Score(cell.InflowRate,cell.LastWetAt,cell.LastWetFlow,Time.time);
                if(priority<=0f)continue;
                float targetY=NavalRepairSite.Floor(cell.LocalCenter.y);
                priority=NavalRepairRoutes.Priority(priority,NavalRepairRoutes.TravelCost(feet.x,feet.y,feet.z,cell.LocalCenter.x,targetY,cell.LocalCenter.z));
                float retry;if(retryAfter.TryGetValue(cell,out retry)&&Time.time<retry)continue;
                bool reserved=false;
                foreach(NavalRepairSailor worker in hull.RepairWorkers)
                    if(worker!=null&&worker!=this&&worker.HasJob&&(worker.JobPoint-cell.LocalCenter).sqrMagnitude<1.2f*1.2f){reserved=true;break;}
                if(!reserved){candidates.Add(cell);priorities[cell]=priority;}
            }
        candidates.Sort(delegate(NavalHullVoxel a,NavalHullVoxel b){return priorities[b].CompareTo(priorities[a]);});
        Vector3 workPoint=Vector3.zero;
        int checkedSites=0;
        foreach(NavalHullVoxel candidate in candidates)
        {
            if(checkedSites++>=6)break;
            if(!NavalWalkRoutes.InStairwell(feet.x,feet.y,feet.z,hull.Definition.StairWidth*.5f+.175f)&&
                (Mathf.Abs(feet.y-0.825f)<0.25f||Mathf.Abs(feet.y-BoatDimensions.HoldFloor)<0.25f)&&ReachFrom(feet,candidate))
            {workPoint=feet;selected=candidate;break;}
            if(FindWorkPoint(candidate,out workPoint)){selected=candidate;break;}
            retryAfter[candidate]=Time.time+10f;
        }
        if(selected==null)return;
        Debug.Log("[NavalPrototype] Repair job: hole="+selected.LocalCenter+" work="+workPoint+" wet age="+(Time.time-selected.LastWetAt).ToString("F1"));
        leak=selected;progress.Reset();route.Clear();returning=false;changingPost=false;stuck=0f;
        foreach(NavalHullVoxel cell in hull.Voxels)
            if((cell.HydraulicShell||cell.InternalBarrier)&&(cell.Missing||cell.Detached)&&cell.RepairClaim==null&&(cell.LocalCenter-leak.LocalCenter).sqrMagnitude<0.85f*0.85f)
            {cell.RepairClaim=this;claimed.Add(cell);}
        if((workPoint-feet).sqrMagnitude>0.01f)
            Append(NavalRepairRoutes.Plan(new NavalWalkPoint(feet.x,feet.y,feet.z),new NavalWalkPoint(workPoint.x,workPoint.y,workPoint.z),hull.Definition.Compartments,hull.Definition.StairWidth));
    }
    private bool CanReachLeak()
    {
        if(leak==null)return false;
        return ReachFrom(feet,leak);
    }
    private bool ReachFrom(Vector3 stand,NavalHullVoxel hole)
    {
        Vector3 hand=boat.TransformPoint(stand+Vector3.up*0.8f);
        Vector3 local=hole.LocalCenter;local.y=NavalRepairSite.AimY(local.y,stand.y);
        Vector3 point=boat.TransformPoint(local),offset=point-hand;
        if(!NavalRepairSite.WithinReach(offset.x,offset.y,offset.z))return false;
        RaycastHit hit;
        return !Physics.Raycast(hand,offset.normalized,out hit,Mathf.Max(0f,offset.magnitude-0.35f),
            PhysicsManager.GetEnvironmentLayerMask(),QueryTriggerInteraction.Ignore);
    }
    private bool FindWorkPoint(NavalHullVoxel hole,out Vector3 point)
    {
        float preferred=NavalRepairSite.Floor(hole.LocalCenter.y);
        float z=NavalRepairSite.WorkZ(hole.LocalCenter.z);
        float[] floors={preferred,preferred>0.3f?BoatDimensions.HoldFloor:0.825f};
        float[] shifts={0f,-0.35f,0.35f,-0.7f,0.7f,-1.2f,1.2f};
        foreach(float floor in floors)
        foreach(float corridor in (floor>0.3f?new float[]{1.4f,1.65f,2.1f}:new float[]{1.65f,2.1f,1.4f}))
        foreach(float shift in shifts)
        {
            float x=hole.LocalCenter.x<0f?-corridor:corridor;
            point=new Vector3(x,floor,Mathf.Clamp(z+shift,-5.1f,5.1f));
            Vector3 bottom=boat.TransformPoint(point)+boat.up*0.5f;
            Vector3 top=bottom+boat.up*1.3f;
            if(!ReachFrom(point,hole))continue;
            if(!Physics.CheckCapsule(bottom,top,0.38f,PhysicsManager.GetEnvironmentLayerMask(),QueryTriggerInteraction.Ignore))return true;
        }
        point=Vector3.zero;return false;
    }
    private void PlanTo(Vector3 goal)
    {
        Append(NavalRepairRoutes.Plan(new NavalWalkPoint(feet.x,feet.y,feet.z),new NavalWalkPoint(goal.x,goal.y,goal.z),hull.Definition.Compartments,hull.Definition.StairWidth));
    }
    private void BeginReturn()
    {
        Release();returning=true;changingPost=false;route.Clear();stuck=0f;
        feet=boat.InverseTransformPoint(robot.transform.position-boat.up*footOffset);
        PlanTo(home);
    }
    private int returnRetries;
    private void ReplanReturn()
    {
        route.Clear();stuck=0f;returnRetries++;
        feet=boat.InverseTransformPoint(robot.transform.position-boat.up*footOffset);
        // Try a different corridor instead of replaying the same blocked segment.
        if(!NavalWalkRoutes.InStairwell(feet.x,feet.y,feet.z,hull.Definition.StairWidth*.5f+.175f))
        {
            float floor=feet.y>0.3f?0.825f:BoatDimensions.HoldFloor;
            float side=feet.x<0f?-1f:1f;
            Vector3 detour=new Vector3(side*(returnRetries%2==1?2.1f:1.4f),floor,feet.z);
            route.Add(detour);
            Append(NavalRepairRoutes.Plan(new NavalWalkPoint(detour.x,detour.y,detour.z),new NavalWalkPoint(home.x,home.y,home.z),hull.Definition.Compartments,hull.Definition.StairWidth));
        }
        else PlanTo(home);
    }
    private void AbandonBlockedJob()
    {if(leak!=null)retryAfter[leak]=Time.time+10f;ContinueRepair();}
    private void ContinueRepair()
    {
        Release();route.Clear();returning=false;changingPost=false;stuck=0f;
        scanRevision=hull.Revision;scanAt=Time.time+0.25f;
        FindLeak();
        if(leak==null)BeginReturn();
    }
    private void Append(List<NavalWalkPoint> points)
    {foreach(NavalWalkPoint point in points)route.Add(new Vector3(point.X,point.Y,point.Z));}
    private void Release()
    {
        ReleasePump();
        CancelBailing();
        Walking(false,0f);
        foreach(NavalHullVoxel cell in claimed)if(cell!=null&&cell.RepairClaim==this)cell.RepairClaim=null;
        claimed.Clear();leak=null;progress.Reset();
        if(hammer!=null)hammer.localRotation=hammerRest;
    }
    private CharacterController Controller()
    {return robot.GetComponentInChildren<CharacterController>();}
    private void HoldPosition()
    {
        CharacterController controller=Controller();bool enabled=controller!=null&&controller.enabled;
        if(enabled)controller.enabled=false;
        robot.transform.position=boat.TransformPoint(feet)+boat.up*footOffset;
        if(enabled)controller.enabled=true;
        FirstPersonMover mover=robot as FirstPersonMover;if(mover!=null&&Velocity!=null)Velocity.SetValue(mover,Vector3.zero);
    }
    private void Move(Vector3 destination,float dt,bool finalPoint)
    {
        if(hull.Bilge!=null)hull.Bilge.RequestPassage(feet,destination);
        Vector3 before=feet;
        HoldPosition();Vector3 offset=boat.TransformVector(destination-feet);
        Vector3 look=offset;look.y=0f;
        if(look.sqrMagnitude>0.01f)robot.transform.rotation=Quaternion.RotateTowards(robot.transform.rotation,Quaternion.LookRotation(look),720f*dt);
        float alignment=look.sqrMagnitude>0.01f?Mathf.Clamp01(Vector3.Dot(robot.transform.forward,look.normalized)):1f;
        bool stairs=NavalWalkRoutes.InStairwell(feet.x,feet.y,feet.z,hull.Definition.StairWidth*.5f+.175f)||NavalWalkRoutes.InStairwell(destination.x,destination.y,destination.z,hull.Definition.StairWidth*.5f+.175f);
        float wanted=NavalCrewMovement.TargetSpeed(DeckWalkSpeed,alignment,offset.magnitude,finalPoint,stairs);
        moveSpeed=NavalCrewMovement.Accelerate(moveSpeed,wanted,dt);
        Vector3 step=Vector3.ClampMagnitude(offset,moveSpeed*dt);
        CharacterController controller=Controller();
        if(controller!=null&&controller.enabled)controller.Move(step+Vector3.down*0.3f*dt);
        else robot.transform.position+=step;
        feet=boat.InverseTransformPoint(robot.transform.position-boat.up*footOffset);
        Vector3 travel=boat.TransformVector(feet-before);travel.y=0f;
        if(gait!=null)gait.Moved(travel.magnitude);
    }
    private void Walking(bool active,float dt)
    {
        if(!active){moveSpeed=0f;if(gait!=null)gait.Stop();}
    }
    private void OnDestroy(){Release();if(hull!=null)hull.RepairWorkers.Remove(this);}
    private void Update()
    {if(robot==null||!robot.IsAlive()||(hull!=null&&hull.Sunk)){Release();returning=false;}}
}
