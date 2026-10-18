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
    private System.Comparison<NavalHullVoxel> compareCandidates;
    private readonly Dictionary<NavalHullVoxel,float> priorities=new Dictionary<NavalHullVoxel,float>();
    private bool returning,stranded;
    private bool changingPost,waitingForRepair;
    private int repairReplans;
    private Vector3 repairDestination;
    private readonly List<Vector3> rejectedRepairPoints=new List<Vector3>();
    public bool RepairDuty { get { return waitingForRepair || shieldJob!=null || pumpDuty || bailState!=0 || leak!=null || returning&&!changingPost; } }
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
    private CharacterController cachedController;
    public bool Stranded { get { return stranded; } }
    public bool Defending {get;private set;}
    public bool Assaulting {get;private set;}
    public bool Rescuing {get;private set;}
    private NavalSailorRescue cachedRescue;
    public NavalSailorRescue RescueComponent {get{if(cachedRescue==null)cachedRescue=GetComponent<NavalSailorRescue>();return cachedRescue;}}
    public string RescueStatus {get{NavalSailorRescue rescue=RescueComponent;return rescue!=null?rescue.Status:"Rescuing";}}
    public void BeginRescue()
    {combatReturnPending=false;Release();route.Clear();returning=false;changingPost=false;stranded=false;Defending=false;Assaulting=false;Rescuing=true;if(gait!=null)gait.enabled=false;robot.DisableAI();}
    public void EndRescue()
    {Rescuing=false;stranded=false;Defending=false;Assaulting=false;feet=boat.InverseTransformPoint(robot.transform.position-boat.up*footOffset);home=feet;returning=false;if(gait!=null)gait.enabled=true;}
    public bool SpareMaintenance {get;set;}
    public bool FinalAttackDuty {get;private set;}
    public void SetFinalAttack(bool active)
    {
        if(FinalAttackDuty==active)return;
        FinalAttackDuty=active;
        if(!active||Rescuing||Assaulting)return;
        combatReturnPending=false;Release();route.Clear();returning=false;changingPost=false;
        feet=boat.InverseTransformPoint(robot.transform.position-boat.up*footOffset);home=feet;
    }
    public void PrepareBoardingPost(Vector3 post)
    {Release();route.Clear();returning=false;changingPost=false;SetPost(post,true);}
    public void BeginAssault(){BeginDefense();Assaulting=true;}
    public void BoardingWalk(bool active)
    {if(gait!=null){gait.enabled=active;if(!active)gait.Stop();}}
    public void BoardingMoved(float distance){if(gait!=null)gait.Moved(distance);}
    public void BeginDefense()
    {
        combatReturnPending=false;Release();route.Clear();returning=false;changingPost=false;Defending=true;
        if(gait!=null)gait.enabled=false;
    }
    public void EndDefense()
    {
        Defending=false;robot.DisableAI();
        if(gait!=null)gait.enabled=true;
        feet=boat.InverseTransformPoint(robot.transform.position-boat.up*footOffset);
        BeginReturn();
    }
    public Vector3 CarriedPosition {get{return feet;}}
    public float CarriedSide {get{return feet.x;}}
    public float FootOffset { get { return footOffset; } }
    public float DeckWalkSpeed {get{return NavalCrewMovement.BaseSpeed(robot.GetBaseMoveSpeed());}}
    private static readonly FieldInfo Velocity=typeof(FirstPersonMover).GetField("_velocity",BindingFlags.Instance|BindingFlags.NonPublic);
    public bool Busy { get { return waitingForRepair||shieldJob!=null||pumpDuty||bailState!=0||leak!=null||returning; } }
    public bool MaintenanceDuty {get{return waitingForRepair||shieldJob!=null||leak!=null||pumpDuty||bailState!=0;}}
    public bool HasJob { get { return leak!=null; } }
    public bool RepairingLeak {get{return leak!=null&&!leak.RepairableEquipment;}}
    public bool RepairingPump {get{return leak!=null&&leak.RepairMechanism!=null&&leak.RepairMechanism.IsPump;}}
    public bool RepairingHelm {get{return leak!=null&&leak.RepairMechanism!=null&&!leak.RepairMechanism.IsPump;}}
    public bool Bailing {get{return bailState!=0;}}
    public Vector3 JobPoint { get { return leak!=null?leak.LocalCenter:Vector3.zero; } }
    public string Status { get { return waitingForRepair?"Waiting for leak access":ShieldWorking?ShieldStatus:pumpDuty?(route.Count>0?"Going to pump":hull.Bilge.Status):bailState!=0?BailStatus:leak!=null?(route.Count>0?"Going to leak":(leak.RepairMechanism!=null?(leak.RepairMechanism.IsPump?"Repair pump ":"Repair helm "):"Repair ")+Mathf.RoundToInt(work/4f*100f)+"%"):returning?"Returning":"On duty"; } }
    public void Initialize(Character sailor,NavalHull owner,NavalStructure frame,Vector3 station,float feetOffset)
    {
        NavalCharacters.Register(sailor);
        robot=sailor;hull=owner;structure=frame;boat=owner.Root;home=station;footOffset=feetOffset;feet=home;
        cachedController=robot.GetComponentInChildren<CharacterController>();
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
        if(leak!=null&&leak.RepairMechanism!=null&&!leak.RepairMechanism.CanRepair)ContinueRepair();
        // No autonomous board-hauling jobs. Passage removal still works inside FollowRoute.
        if(shieldJob!=null){ReleaseShieldJob();route.Clear();BeginReturn();}
        if(waitingForRepair&&!MayAcquireMaintenance()){waitingForRepair=false;BeginReturn();}
        bool mayMaintain=(mayTakeJob||waitingForRepair||returning&&!changingPost)&&MayAcquireMaintenance();
        // Pump acquisition first reserves the only available leak repairer.
        if(TickPump(dt,mayMaintain))return;
        if(TickBailing(dt,mayMaintain))return;
        if(leak==null&&MayAcquireMaintenance()&&(mayTakeJob||RepairDuty)&&(Time.time>=scanAt||scanRevision!=hull.Revision))
        {
            if(Busy)HoldPosition();TryFindLeak();
            if(leak==null&&PendingLeaks())
            {waitingForRepair=true;returning=false;changingPost=false;route.Clear();}
        }
        if(leak!=null)
        {
            // Only accept flowing leaks, but finish an accepted patch across wave troughs.
            progress.StepAccepted(dt,route.Count==0&&CanReachLeak());
            if(!leak.Missing&&!leak.Detached)ContinueRepair();
        }
        if(waitingForRepair&&leak==null)
        {
            if(!PendingLeaks()){waitingForRepair=false;BeginReturn();}
            else {robot.DisableAI();HoldPosition();Walking(false,dt);}
            return;
        }
        if(!Busy)return;
        robot.DisableAI();
        if(route.Count>0)
        {
            RouteResult movement=FollowRoute(dt,returning?1.5f:3f,true);
            if(movement==RouteResult.Fell){stranded=true;Release();return;}
            if(movement==RouteResult.Blocked){if(returning&&leak==null)ReplanReturn();else AbandonBlockedJob();stuck=0f;}
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
                AbandonBlockedJob();
                return;
            }
            if(work>=4f)
            {
                Vector3 patch=leak.LocalCenter;
                foreach(NavalHullVoxel cell in claimed)
                    if(cell.RepairClaim==this&&(cell.HydraulicShell||cell.InternalBarrier||cell.RepairableEquipment)&&(cell.Missing||cell.Detached)&&
                        (cell.LocalCenter-patch).sqrMagnitude<=0.85f*0.85f)structure.RestoreCell(cell);
                ContinueRepair();
            }
        }
    }
    private int CompareCandidatePriority(NavalHullVoxel a,NavalHullVoxel b){return priorities[b].CompareTo(priorities[a]);}
    private void FindLeak()
    {
        if(!MayAcquireMaintenance())return;
        feet=boat.InverseTransformPoint(robot.transform.position-boat.up*footOffset);
        candidates.Clear();priorities.Clear();NavalHullVoxel selected=null;
        foreach(NavalHullVoxel cell in hull.GetDamagedVoxels())
            if((cell.HydraulicShell||cell.InternalBarrier||cell.RepairableEquipment)&&(cell.Missing||cell.Detached)&&cell.RepairClaim==null&&(cell.RepairMechanism==null||cell.RepairMechanism.CanRepair))
            {
                float priority=cell.RepairableEquipment?.001f:NavalLeakPriority.Score(cell.InflowRate,cell.LastWetAt,cell.LastWetFlow,Time.time);
                if(priority<=0f)continue;
                float targetY=NavalRepairSite.Floor(cell.LocalCenter.y,hull.Floor);
                priority=NavalRepairRoutes.Priority(priority,NavalRepairRoutes.TravelCost(feet.x,feet.y,feet.z,cell.LocalCenter.x,targetY,cell.LocalCenter.z));
                float retry;if(retryAfter.TryGetValue(cell,out retry)&&Time.time<retry)continue;
                bool reserved=false;
                foreach(NavalRepairSailor worker in hull.RepairWorkers)
                    if(worker!=null&&worker!=this&&worker.HasJob&&(worker.JobPoint-cell.LocalCenter).sqrMagnitude<1.2f*1.2f){reserved=true;break;}
                if(!reserved){candidates.Add(cell);priorities[cell]=priority;}
            }
        if(compareCandidates==null)compareCandidates=CompareCandidatePriority;
        candidates.Sort(compareCandidates);
        Vector3 workPoint=Vector3.zero;
        int checkedSites=0;
        foreach(NavalHullVoxel candidate in candidates)
        {
            if(checkedSites++>=6)break;
            if(!NavalWalkRoutes.InStairwell(feet.x,feet.y,feet.z,hull.Definition.StairWidth*.5f+.175f,hull.Floor)&&
                (Mathf.Abs(feet.y-0.825f)<0.25f||Mathf.Abs(feet.y-hull.Floor)<0.25f)&&ReachFrom(feet,candidate))
            {workPoint=feet;selected=candidate;break;}
            if(FindWorkPoint(candidate,out workPoint)){selected=candidate;break;}
            retryAfter[candidate]=Time.time+NavalRepairDispatch.RetryDelay(candidate.InflowRate>.00001f);
        }
        if(selected==null)return;
        Debug.Log("[NavalPrototype] Repair job: hole="+selected.LocalCenter+" work="+workPoint+" wet age="+(Time.time-selected.LastWetAt).ToString("F1"));
        leak=selected;waitingForRepair=false;repairReplans=0;rejectedRepairPoints.Clear();repairDestination=workPoint;progress.Reset();route.Clear();returning=false;changingPost=false;stuck=0f;
        foreach(NavalHullVoxel cell in hull.GetDamagedVoxels())
            if((cell.HydraulicShell||cell.InternalBarrier||cell.RepairableEquipment)&&(cell.Missing||cell.Detached)&&cell.RepairClaim==null&&(cell.LocalCenter-leak.LocalCenter).sqrMagnitude<0.85f*0.85f)
            {cell.RepairClaim=this;claimed.Add(cell);}
        Vector3 destination=workPoint;
        if((destination-feet).sqrMagnitude>.01f)
            PlanTo(destination);
    }
    private bool CanReachLeak()
    {
        if(leak==null)return false;
        return ReachFrom(feet,leak);
    }
    private bool ReachFrom(Vector3 stand,NavalHullVoxel hole)
    {
        Vector3 hand=boat.TransformPoint(stand+Vector3.up*0.8f);
        Vector3 local=hull.RepairAccess(hole,stand);local.y=NavalRepairSite.AimY(local.y,stand.y);
        Vector3 point=boat.TransformPoint(local),offset=point-hand;
        if(!NavalRepairSite.WithinReach(offset.x,offset.y,offset.z))return false;
        return RepairSightClear(hand,offset);
    }
    private bool FindWorkPoint(NavalHullVoxel hole,out Vector3 point)
    {
        float preferred=NavalRepairSite.Floor(hole.LocalCenter.y,hull.Floor);
        bool stairs=NavalWalkRoutes.InStairwell(feet.x,feet.y,feet.z,hull.Definition.StairWidth*.5f+.175f,hull.Floor);
        if(NavalRepairDispatch.PreferCurrentDeck(feet.y,hull.Floor,stairs))preferred=feet.y>.3f?.825f:hull.Floor;
        bool endWall=NavalRepairWorkSites.EndWall(hole.LocalCenter.z);
        for(int deck=0;deck<2;deck++)
        {
            float floor=NavalRepairWorkSites.Floor(preferred,hull.Floor,deck);
            if(endWall)for(int i=0;i<NavalRepairWorkSites.ShiftCount;i++)
            {
                float shift=NavalRepairWorkSites.Shift(i);
                point=new Vector3(Mathf.Clamp(hole.LocalCenter.x+shift,-2.35f,2.35f),floor,Mathf.Sign(hole.LocalCenter.z)*5.5f);
                if(floor<.3f&&hull.Bilge!=null)point.y=NavalBallastLayout.Surface(point.x,point.z,hull.Floor,hull.WorldScale);
                if(!RejectedPoint(hole,point)&&ReachFrom(point,hole)&&ClearBody(point))return true;
            }
            for(int c=0;c<NavalRepairWorkSites.CorridorCount;c++)for(int i=0;i<NavalRepairWorkSites.ShiftCount;i++)
            {
                float corridor=NavalRepairWorkSites.Corridor(c),shift=NavalRepairWorkSites.Shift(i);
                point=new Vector3(hole.LocalCenter.x<0f?-corridor:corridor,floor,Mathf.Clamp(hole.LocalCenter.z+shift,-5.1f,5.1f));
                if(floor<.3f&&hull.Bilge!=null)point.y=NavalBallastLayout.Surface(point.x,point.z,hull.Floor,hull.WorldScale);
                if(!RejectedPoint(hole,point)&&ReachFrom(point,hole)&&ClearBody(point))return true;
            }
        }
        point=Vector3.zero;return false;
    }
    private void PlanTo(Vector3 goal)
    {
        if(TryPlanLedgeRecovery(goal))return;
        Append(NavalBallastRoutes.Plan(new NavalWalkPoint(feet.x,feet.y,feet.z),new NavalWalkPoint(goal.x,goal.y,goal.z),hull.Definition.Partitions,hull.Definition.StairWidth,hull.Floor,hull.WorldScale));
    }
    private bool OtherMaintenanceWorker()
    {
        foreach(NavalRepairSailor worker in hull.RepairWorkers)
            if(worker!=null&&worker!=this&&worker.robot!=null&&worker.robot.IsAlive()&&worker.MaintenanceDuty)return true;
        return false;
    }
    private bool MayAcquireMaintenance(){return NavalMaintenanceMath.CanAcquireMaintenance(FinalAttackDuty,SpareMaintenance,hull.ParallelMaintenance,OtherMaintenanceWorker());}
    private bool OtherLeakRepairer()
    {
        foreach(NavalRepairSailor worker in hull.RepairWorkers)
            if(worker!=null&&worker!=this&&worker.robot!=null&&worker.robot.IsAlive()&&worker.RepairingLeak)return true;
        return false;
    }
    private bool PendingLeaks()
    {
        foreach(NavalHullVoxel cell in hull.GetDamagedVoxels())
            if((cell.HydraulicShell||cell.InternalBarrier)&&!cell.RepairableEquipment&&
                NavalRepairDispatch.Pending(cell.Missing||cell.Detached,cell.RepairClaim==null||cell.RepairClaim==this,
                    cell.RepairMechanism==null||cell.RepairMechanism.CanRepair,cell.InflowRate,cell.LastWetAt,Time.time))return true;
        return false;
    }
    private void BeginReturn()
    {
        Release();returning=false;changingPost=false;route.Clear();stuck=0f;
        feet=boat.InverseTransformPoint(robot.transform.position-boat.up*footOffset);
        if(combatReturnPending&&NavalRepairDispatch.CanRecall(false,hull.HasRepairDemand,OtherLeakRepairer(),BucketWater))
        {ResumeCombatPost();return;}
        scanRevision=hull.Revision;scanAt=Time.time+.25f;FindLeak();
        if(leak!=null)return;
        if(MayAcquireMaintenance()&&PendingLeaks())
        {waitingForRepair=true;Debug.Log("[NavalPrototype] Repair waiting nearby: active leak has no reachable site yet");return;}
        returning=true;PlanTo(home);
    }
    private int returnRetries;
    private void ReplanReturn()
    {
        route.Clear();stuck=0f;returnRetries++;
        feet=boat.InverseTransformPoint(robot.transform.position-boat.up*footOffset);
        if(TryPlanLedgeRecovery(home))return;
        // Try a different corridor instead of replaying the same blocked segment.
        if(!NavalWalkRoutes.InStairwell(feet.x,feet.y,feet.z,hull.Definition.StairWidth*.5f+.175f,hull.Floor))
        {
            float floor=feet.y>0.3f?0.825f:hull.Floor;
            float side=feet.x<0f?-1f:1f;
            Vector3 detour=new Vector3(side*(returnRetries%2==1?2.1f:1.4f),floor,feet.z);
            route.Add(detour);
            Append(NavalBallastRoutes.Plan(new NavalWalkPoint(detour.x,detour.y,detour.z),new NavalWalkPoint(home.x,home.y,home.z),hull.Definition.Partitions,hull.Definition.StairWidth,hull.Floor,hull.WorldScale));
        }
        else PlanTo(home);
    }
    private bool RejectedPoint(NavalHullVoxel hole,Vector3 point)
    {
        if(hole!=leak)return false;
        foreach(Vector3 rejected in rejectedRepairPoints)if((point-rejected).sqrMagnitude<.06f)return true;
        return false;
    }
    private bool ReplanRepair()
    {
        if(leak==null||repairReplans>=3)return false;
        repairReplans++;rejectedRepairPoints.Add(repairDestination);
        feet=boat.InverseTransformPoint(robot.transform.position-boat.up*footOffset);
        Vector3 alternative;if(!FindWorkPoint(leak,out alternative))return false;
        repairDestination=alternative;route.Clear();stuck=0f;PlanTo(alternative);
        Debug.Log("[NavalPrototype] Repair route replanned: hole="+leak.LocalCenter+" work="+alternative);
        return true;
    }
    private void AbandonBlockedJob()
    {
        if(ReplanRepair())return;
        if(leak!=null)
        {
            float delay=NavalRepairDispatch.RetryDelay(leak.InflowRate>.00001f);retryAfter[leak]=Time.time+delay;
            Debug.Log("[NavalPrototype] Repair access retry: hole="+leak.LocalCenter+" flow="+leak.InflowRate.ToString("F3")+" retry="+delay+"s");
        }
        ContinueRepair();
    }
    private void ContinueRepair(){BeginReturn();}
    private void Append(List<NavalWalkPoint> points)
    {foreach(NavalWalkPoint point in points)route.Add(new Vector3(point.X,point.Y,point.Z));}
    private void Release()
    {
        waitingForRepair=false;
        ReleaseShieldJob();
        ReleasePump();
        CancelBailing();
        Walking(false,0f);
        foreach(NavalHullVoxel cell in claimed)if(cell!=null&&cell.RepairClaim==this)cell.RepairClaim=null;
        claimed.Clear();leak=null;progress.Reset();
        if(hammer!=null)hammer.localRotation=hammerRest;
    }
    public CharacterController BodyController {get{return Controller();}}
    private CharacterController Controller()
    {if(cachedController==null&&robot!=null)cachedController=robot.GetComponentInChildren<CharacterController>();return cachedController;}
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
        if(hull.Bilge!=null)destination=hull.Bilge.PassageTarget(feet,destination,this,dt);
        Vector3 before=feet;
        HoldPosition();Vector3 offset=boat.TransformVector(destination-feet);
        Vector3 look=offset;look.y=0f;
        if(look.sqrMagnitude>0.01f)robot.transform.rotation=Quaternion.RotateTowards(robot.transform.rotation,Quaternion.LookRotation(look),720f*dt);
        float alignment=look.sqrMagnitude>0.01f?Mathf.Clamp01(Vector3.Dot(robot.transform.forward,look.normalized)):1f;
        bool stairs=NavalWalkRoutes.InStairwell(feet.x,feet.y,feet.z,hull.Definition.StairWidth*.5f+.175f,hull.Floor)||NavalWalkRoutes.InStairwell(destination.x,destination.y,destination.z,hull.Definition.StairWidth*.5f+.175f,hull.Floor);
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
    private void OnDestroy(){NavalCharacters.Remove(robot);Release();if(hull!=null)hull.RepairWorkers.Remove(this);}
    private void Update()
    {if(robot==null||!robot.IsAlive()||(hull!=null&&hull.Sunk)){Release();returning=false;}}
}
