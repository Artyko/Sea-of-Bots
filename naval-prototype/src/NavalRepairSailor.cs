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
    private float RepairSeconds {get{return leak!=null&&leak.RepairMechanism!=null?leak.RepairMechanism.RepairSeconds:4f;}}
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
    public bool RepairDuty { get { return ammoTarget!=null || HandlingSails || nextTaskPending || fireJob!=null || waitingForRepair || shieldJob!=null || pumpDuty || bailState!=0 || leak!=null || returning&&!changingPost; } }
    public void SetPost(Vector3 post,bool force=false)
    {
        if(RepairDuty||stranded)return;
        // A cancelled task can leave the sailor far from an unchanged home post.
        // The post being unchanged does not mean the sailor has arrived there.
        if(!force&&(home-post).sqrMagnitude<.01f&&(returning||AtStation(post)))return;
        feet=boat.InverseTransformPoint(robot.transform.position-Vector3.up*footOffset);
        home=post;route.Clear();returning=true;changingPost=true;
        PlanTo(home);
    }
    public bool AtStation(Vector3 post)
    {Vector3 delta=feet-post;return NavalStationArrival.CanAnchor(boat.TransformVector(delta).sqrMagnitude)||
        NavalStationArrival.OnRaisedSupport(delta.x,delta.y,delta.z,hull.WorldScale)&&Supported(feet);}
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
    {RestoreStepHeight();combatReturnPending=false;Release();route.Clear();returning=false;changingPost=false;stranded=false;Defending=false;Assaulting=false;Rescuing=true;if(gait!=null)gait.enabled=false;robot.DisableAI();}
    public void EndRescue()
    {Rescuing=false;stranded=false;Defending=false;Assaulting=false;feet=boat.InverseTransformPoint(robot.transform.position-Vector3.up*footOffset);home=feet;returning=false;if(gait!=null)gait.enabled=true;}
    public bool SpareMaintenance {get;set;}
    public bool FinalAttackDuty {get;private set;}
    public void SetFinalAttack(bool active)
    {
        if(FinalAttackDuty==active)return;
        FinalAttackDuty=active;
        if(!active||Rescuing||Assaulting)return;
        combatReturnPending=false;Release();route.Clear();returning=false;changingPost=false;
        feet=boat.InverseTransformPoint(robot.transform.position-Vector3.up*footOffset);home=feet;
    }
    public void PrepareBoardingPost(Vector3 post)
    {Release();route.Clear();returning=false;changingPost=false;SetPost(post,true);}
    public void BeginAssault(){BeginDefense();Assaulting=true;}
    public void BoardingWalk(bool active)
    {if(gait!=null){gait.enabled=active;if(!active)gait.Stop();}}
    public void BoardingMoved(float distance){if(gait!=null)gait.Moved(distance);}
    public void BeginDefense()
    {
        RestoreStepHeight();
        combatReturnPending=false;Release();route.Clear();returning=false;changingPost=false;Defending=true;
        if(gait!=null)gait.enabled=false;
    }
    public void EndDefense()
    {
        Defending=false;robot.DisableAI();
        if(gait!=null)gait.enabled=true;
        feet=boat.InverseTransformPoint(robot.transform.position-Vector3.up*footOffset);
        BeginReturn();
    }
    public Vector3 CarriedPosition {get{return feet;}}
    public float CarriedSide {get{return feet.x;}}
    public float FootOffset { get { return footOffset; } }
    private bool movingInWater;
    private float waterBodyHeight=2.6f;
    public float DeckWalkSpeed
    {
        get
        {
            FirstPersonMover mover=robot as FirstPersonMover;
            float walking=NavalCrewMovement.BaseSpeed(robot.GetBaseMoveSpeed())*NavalCrewMovement.InjuryMultiplier(
                robot.IsDamaged(MechBodyPartType.RightLeg),robot.IsDamaged(MechBodyPartType.LeftLeg),mover!=null?mover.CrawlingMoveSpeedMultiplier:.2f);
            NavalTestWorld world=NavalTestWorld.Active;
            if(world==null||!world.SimulationActive){movingInWater=false;return walking;}
            Vector3 foot=robot.transform.position-Vector3.up*footOffset;
            movingInWater=NavalSwimMovement.InWater(world.WaterLevelAt(foot)-foot.y,waterBodyHeight,movingInWater);
            return NavalSwimMovement.Speed(walking,movingInWater);
        }
    }
    private static readonly FieldInfo Velocity=typeof(FirstPersonMover).GetField("_velocity",BindingFlags.Instance|BindingFlags.NonPublic);
    public bool Busy { get { return ammoTarget!=null||HandlingSails||nextTaskPending||fireJob!=null||waitingForRepair||shieldJob!=null||pumpDuty||bailState!=0||leak!=null||returning; } }
    public bool MaintenanceDuty {get{return nextTaskPending||fireJob!=null||waitingForRepair||shieldJob!=null||leak!=null||pumpDuty||bailState!=0;}}
    public bool RepairingCannon {get{return leak!=null&&leak.RepairMechanism!=null&&leak.RepairMechanism.IsCannon;}}
    public bool ReturningWithoutWork {get{return returning&&!changingPost&&!MaintenanceDuty&&!HandlingSails;}}
    public void RecallCannonForCombat()
    {
        if(!RepairingCannon)return;
        Release();route.Clear();returning=false;changingPost=false;stuck=0;
    }
    public void AssignFreeCombatPost(Vector3 post)
    {
        if(!ReturningWithoutWork)return;
        Release();route.Clear();returning=false;changingPost=false;SetPost(post,true);
    }
    public bool HasJob { get { return leak!=null; } }
    public bool RepairingLeak {get{return leak!=null&&!leak.RepairableEquipment;}}
    public bool RepairingPump {get{return leak!=null&&leak.RepairMechanism!=null&&leak.RepairMechanism.IsPump;}}
    public bool RepairingHelm {get{return leak!=null&&leak.RepairMechanism!=null&&!leak.RepairMechanism.IsPump&&!leak.RepairMechanism.IsCannon;}}
    public bool Bailing {get{return bailState!=0;}}
    public Vector3 JobPoint { get { return leak!=null?leak.LocalCenter:Vector3.zero; } }
    public string Status { get { return HandlingSails?(route.Count>0?"Going to sails":"Handling sails"):nextTaskPending?"Choosing next task":fireJob!=null?FireStatus:waitingForRepair?"Waiting for leak access":cannonPartsStage>0?CannonPartsStatus:plankStage>0?PlankStatus:ShieldWorking?ShieldStatus:pumpDuty?(route.Count>0?"Going to pump":hull.Bilge.Status):bailState!=0?BailStatus:leak!=null?(route.Count>0?"Going to leak":(leak.RepairMechanism!=null?("Repair "+leak.RepairMechanism.Name+" "):"Repair ")+Mathf.RoundToInt(work/RepairSeconds*100f)+"%"):returning?"Returning":"On duty"; } }
    public void Initialize(Character sailor,NavalHull owner,NavalStructure frame,Vector3 station,float feetOffset)
    {
        NavalCharacters.Register(sailor);
        robot=sailor;hull=owner;structure=frame;boat=owner.Root;home=station;footOffset=feetOffset;feet=home;
        cachedController=robot.GetComponentInChildren<CharacterController>();
        CharacterController bodyController=Controller();
        if(bodyController!=null&&bodyController.enabled&&bodyController.bounds.size.y>0.5f)
        {float offset=robot.transform.position.y-bodyController.bounds.min.y;if(offset>=0f&&offset<4f)footOffset=offset;}
        waterBodyHeight=Mathf.Max(1f,NavalDrowning.HeadTop(robot).y-(robot.transform.position.y-footOffset));
        // Spawn using the measured sailor collider offset, not the player's approximate offset.
        HoldPosition();
        InitializeDeckCarry();
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
        if(robot==null||!robot.IsAlive()||hull.Sunk||stranded){planks.Drop();Release();return;}
        if(TickSails(dt))return;
        if(TickAmmo(dt,mayTakeJob))return;
        if(nextTaskPending){DispatchNextTask();return;}
        if(leak!=null&&leak.RepairMechanism!=null&&!leak.RepairMechanism.CanRepair){ContinueRepair();return;}
        if(TickCannonParts(dt))return;
        // Retain the accepted leak reservation throughout its supply trip.
        if(plankStage>0&&TickPlanks(dt))return;
        if(shieldJob!=null){ReleaseShieldJob();route.Clear();BeginReturn();return;}
        if(waitingForRepair&&!MayAcquireMaintenance()){waitingForRepair=false;BeginReturn();return;}
        bool mayMaintain=(mayTakeJob||waitingForRepair||returning&&!changingPost)&&MayAcquireMaintenance();
        if(TickFire(dt,mayMaintain)||nextTaskPending)return;
        // Pump acquisition first reserves the only available leak repairer.
        if(TickPump(dt,mayMaintain)||nextTaskPending)return;
        if(TickBailing(dt,mayMaintain)||nextTaskPending)return;
        if(leak==null&&MayAcquireMaintenance()&&(mayTakeJob||RepairDuty)&&Time.time>=scanAt)
        {
            if(Busy)HoldPosition();TryFindLeak();
            if(leak==null&&ReserveRepairWait())
            {waitingForRepair=true;returning=false;changingPost=false;route.Clear();}
        }
        if(TickPlanks(dt))return;
        if(leak!=null)
        {
            // Only accept flowing leaks, but finish an accepted patch across wave troughs.
            progress.StepAccepted(dt,route.Count==0&&CanReachLeak());
            if(!leak.Missing&&!leak.Detached){ContinueRepair();return;}
        }
        if(waitingForRepair&&leak==null)
        {
            if(!ReserveRepairWait()){waitingForRepair=false;BeginReturn();}
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
            if(work>=RepairSeconds)
            {
                if(leak.RepairMechanism!=null&&leak.RepairMechanism.IsCannon)
                {leak.RepairMechanism.CompleteCannonRepair(structure,work);ContinueRepair();return;}
                Vector3 patch=leak.LocalCenter;bool usesBoard=NeedsPlanks(leak),restored=false;
                foreach(NavalHullVoxel cell in claimed)
                    if(cell.RepairClaim==this&&(cell.HydraulicShell||cell.InternalBarrier||cell.RepairableEquipment)&&(cell.Missing||cell.Detached)&&
                        (cell.LocalCenter-patch).sqrMagnitude<=0.85f*0.85f){structure.RestoreCell(cell);if(!cell.Missing&&!cell.Detached)restored=true;}
                if(usesBoard&&restored)planks.Consume();
                ContinueRepair();
            }
        }
    }
    private static int RepairTier(NavalHullVoxel cell)
    {return NavalMaintenancePriority.RepairTier(cell.RepairableEquipment,cell.RepairMechanism!=null&&cell.RepairMechanism.IsPump,cell.RepairMechanism!=null&&cell.RepairMechanism.IsCannon);}
    private int CompareCandidatePriority(NavalHullVoxel a,NavalHullVoxel b)
    {int tier=RepairTier(b).CompareTo(RepairTier(a));return tier!=0?tier:priorities[b].CompareTo(priorities[a]);}
    private void FindLeak()
    {
        if(!MayAcquireMaintenance())return;
        feet=boat.InverseTransformPoint(robot.transform.position-Vector3.up*footOffset);
        candidates.Clear();priorities.Clear();NavalHullVoxel selected=null;
        bool otherCannonRepair=false;int otherGunRepairs=0;
        foreach(var worker in hull.RepairWorkers)
            if(worker!=null&&worker!=this&&worker.robot!=null&&worker.robot.IsAlive()&&worker.RepairingCannon){otherGunRepairs++;}
        otherCannonRepair=otherGunRepairs>=2;
        bool activeFire=hull.Fire!=null&&hull.Fire.HasFire;
        bool pumpDemand=hull.Bilge!=null&&hull.Bilge.Functional&&hull.Bilge.WantsOperator;
        foreach(NavalHullVoxel cell in hull.GetDamagedVoxels())
            if((cell.HydraulicShell||cell.InternalBarrier||cell.RepairableEquipment)&&(cell.Missing||cell.Detached)&&cell.RepairClaim==null&&(cell.RepairMechanism==null||cell.RepairMechanism.CanRepair))
            {
                if(cell.RepairableEquipment&&NavalMaintenancePriority.DeferEquipment(
                    cell.RepairMechanism!=null&&cell.RepairMechanism.IsCannon,cell.RepairMechanism!=null&&cell.RepairMechanism.IsPump,activeFire,pumpDemand&&!(hull.CrewCannonRepairAllowed&&cell.RepairMechanism!=null&&cell.RepairMechanism.IsCannon)))continue;
                if(cell.RepairMechanism!=null&&cell.RepairMechanism.IsCannon&&
                    (!NavalMaintenancePriority.CanClaimCannon(hull.CrewCannonRepairAllowed,otherCannonRepair)||!cell.RepairMechanism.CanFetchParts||cell.RepairMechanism.PlayerRepairUntil>Time.time||cell.RepairMechanism.RepairWorker!=null&&cell.RepairMechanism.RepairWorker!=this))continue;
                if(NeedsPlanks(cell)&&!CanSupplyPlanks)continue;
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
                (Mathf.Abs(feet.y-0.825f)<0.25f||Mathf.Abs(feet.y-hull.Floor)<0.25f||Mathf.Abs(feet.y-hull.Definition.WeatherDeck)<0.25f)&&ReachFrom(feet,candidate))
            {workPoint=feet;selected=candidate;break;}
            if(FindWorkPoint(candidate,out workPoint)){selected=candidate;break;}
            retryAfter[candidate]=Time.time+NavalRepairDispatch.RetryDelay(candidate.InflowRate>.00001f);
        }
        if(selected==null)return;
        Debug.Log("[NavalPrototype] Repair job: hole="+selected.LocalCenter+" work="+workPoint+" wet age="+(Time.time-selected.LastWetAt).ToString("F1"));
        leak=selected;
        if(leak.RepairMechanism!=null&&leak.RepairMechanism.IsCannon)leak.RepairMechanism.RepairWorker=this;
        waitingForRepair=false;repairReplans=0;rejectedRepairPoints.Clear();repairDestination=workPoint;progress.Reset();route.Clear();returning=false;changingPost=false;stuck=0f;
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
        Vector3 hand=boat.TransformPoint(stand)+boat.up*NavalRepairAim.HandHeight;
        Vector3 local=hull.RepairAccess(hole,stand);
        if(hole.HydraulicShell&&(local-hole.LocalCenter).sqrMagnitude<.001f){
            if(NavalRepairAim.AtEnd(local.z))local.z=NavalRepairAim.InnerEnd(local.z,hull.WorldScale);
            else if(NavalRepairAim.AtSide(local.x,hull.Definition.HullHalfWidth(local.z)))local.x=NavalRepairAim.InnerSide(local.x,hull.Definition.HullHalfWidth(local.z),hull.WorldScale);
        }
        local.y=hull.Definition.RepairAimY(local.y,stand.y);
        Vector3 point=boat.TransformPoint(local),offset=point-hand;
        if(!NavalRepairSite.WithinReach(offset.x,offset.y,offset.z))return false;
        return RepairSightClear(hand,offset,hole.RepairMechanism!=null&&hole.RepairMechanism.IsCannon?hole.RepairMechanism:null);
    }
    private bool FindWorkPoint(NavalHullVoxel hole,out Vector3 point)
    {
        float preferred=NavalRepairSite.Floor(hole.LocalCenter.y,hull.Floor);
        bool stairs=NavalWalkRoutes.InStairwell(feet.x,feet.y,feet.z,hull.Definition.StairWidth*.5f+.175f,hull.Floor);
        if(NavalRepairDispatch.PreferCurrentDeck(feet.y,hull.Floor,stairs))preferred=feet.y>.3f?.825f:hull.Floor;
        if(hull.Definition.DeckCount>2&&(feet.y>hull.Definition.WeatherDeck-.25f||hole.LocalCenter.y>hull.Definition.WeatherDeck))preferred=hull.Definition.WeatherDeck;
        bool endWall=NavalRepairWorkSites.EndWall(hole.LocalCenter.z);
        for(int deck=0;deck<hull.Definition.DeckCount;deck++)
        {
            float floor=hull.Definition.RepairFloor(preferred,deck);
            if(endWall)for(int i=0;i<NavalRepairWorkSites.ShiftCount;i++)
            {
                float shift=NavalRepairWorkSites.Shift(i);
                point=new Vector3(Mathf.Clamp(hole.LocalCenter.x+shift,-2.35f,2.35f),floor,Mathf.Sign(hole.LocalCenter.z)*5.5f);
                if(hull.Definition.MaintenanceFloorAt(floor)&&hull.Bilge!=null)point.y=NavalBallastLayout.Surface(point.x,point.z,floor,hull.WorldScale);
                if(InsideWalkingDeck(point)&&Supported(point)&&!RejectedPoint(hole,point)&&ReachFrom(point,hole)&&ClearBody(point))return true;
            }
            for(int c=0;c<NavalRepairWorkSites.CorridorCount;c++)for(int i=0;i<NavalRepairWorkSites.ShiftCount;i++)
            {
                float corridor=NavalRepairWorkSites.Corridor(c),shift=NavalRepairWorkSites.Shift(i);
                point=new Vector3(hole.LocalCenter.x<0f?-corridor:corridor,floor,Mathf.Clamp(hole.LocalCenter.z+shift,-5.1f,5.1f));
                if(hull.Definition.MaintenanceFloorAt(floor)&&hull.Bilge!=null)point.y=NavalBallastLayout.Surface(point.x,point.z,floor,hull.WorldScale);
                if(InsideWalkingDeck(point)&&Supported(point)&&!RejectedPoint(hole,point)&&ReachFrom(point,hole)&&ClearBody(point))return true;
            }
        }
        // A flooded hold can be repaired from a swimming position without floor support.
        var world=NavalTestWorld.Active;
        if(world!=null&&RescueComponent!=null){
            float y=Mathf.Clamp(hole.LocalCenter.y-NavalRepairAim.HandHeight/hull.WorldScale,hull.Floor,hull.Definition.WeatherDeck);
            for(int i=0;i<NavalRepairWorkSites.ShiftCount;i++){
                float shift=NavalRepairWorkSites.Shift(i);
                point=endWall?new Vector3(Mathf.Clamp(hole.LocalCenter.x+shift,-2.1f,2.1f),y,Mathf.Sign(hole.LocalCenter.z)*5.3f):
                    new Vector3(hole.LocalCenter.x<0?-2.1f:2.1f,y,Mathf.Clamp(hole.LocalCenter.z+shift,-5.1f,5.1f));
                Vector3 wet=boat.TransformPoint(point);
                float travel=boat.TransformVector(point-feet).magnitude/2.8f;
                if(world.WaterLevelAt(wet)<wet.y+2.6f||!RescueComponent.HasAirForWork(travel,RepairSeconds)||
                   !InsideWalkingDeck(point)||RejectedPoint(hole,point)||!ClearBody(point)||!ReachFrom(point,hole))continue;
                return true;
            }
        }
        point=Vector3.zero;return false;
    }
    private void PlanTo(Vector3 goal)
    {
        var world=NavalTestWorld.Active;
        if(leak!=null&&world!=null&&world.WaterLevelAt(boat.TransformPoint(goal))>boat.TransformPoint(goal).y+2.6f){
            float floor=hull.Floor;for(int level=0;level<hull.Definition.MaintenanceDeckCount;level++)if(hull.Definition.MaintenanceFloor(level)<=goal.y+.001f)floor=hull.Definition.MaintenanceFloor(level);
            float surface=NavalBallastLayout.Surface(goal.x,goal.z,floor,hull.WorldScale);
            if(goal.y>surface+.1f){Append(NavalSupplyRoutes.Plan(new NavalWalkPoint(feet.x,feet.y,feet.z),new NavalWalkPoint(goal.x,surface,goal.z),hull.Definition));route.Add(goal);return;}
        }
        if(TryPlanLedgeRecovery(goal))return;
        Append(NavalSupplyRoutes.Plan(new NavalWalkPoint(feet.x,feet.y,feet.z),new NavalWalkPoint(goal.x,goal.y,goal.z),hull.Definition));
    }
    private bool OtherMaintenanceWorker()
    {
        foreach(NavalRepairSailor worker in hull.RepairWorkers)
            if(worker!=null&&worker!=this&&worker.robot!=null&&worker.robot.IsAlive()&&worker.MaintenanceDuty)return true;
        return false;
    }
    private bool MayAcquireMaintenance(){if(SpareMaintenance&&!FinalAttackDuty&&hull.CrewCannonRepairAllowed&&!hull.HasRepairDemand&&(hull.Fire==null||!hull.Fire.HasFire))return true;return NavalMaintenanceMath.CanAcquireMaintenance(FinalAttackDuty,SpareMaintenance,hull.ParallelMaintenance,OtherMaintenanceWorker());}
    private bool OtherLeakRepairer()
    {
        foreach(NavalRepairSailor worker in hull.RepairWorkers)
            if(worker!=null&&worker!=this&&worker.robot!=null&&worker.robot.IsAlive()&&worker.RepairingLeak)return true;
        return false;
    }
    private bool ReserveRepairWait()
    {
        if(!PendingLeaks())return false;
        // Stable incumbent: one inaccessible leak cannot immobilize the crew.
        foreach(NavalRepairSailor worker in hull.RepairWorkers)
            if(worker!=null&&worker.robot!=null&&worker.robot.IsAlive()&&worker.waitingForRepair&&!worker.Rescuing&&!worker.Assaulting)
                return worker==this;
        return true;
    }
    private bool PendingLeaks()
    {
        if(!CanSupplyPlanks)return false;
        foreach(NavalHullVoxel cell in hull.GetDamagedVoxels())
            if((cell.HydraulicShell||cell.InternalBarrier)&&!cell.RepairableEquipment&&
                NavalRepairDispatch.Pending(cell.Missing||cell.Detached,cell.RepairClaim==null||cell.RepairClaim==this,
                    cell.RepairMechanism==null||cell.RepairMechanism.CanRepair,cell.InflowRate,cell.LastWetAt,Time.time))return true;
        return false;
    }
    private void BeginReturn()
    {
        Release();returning=false;changingPost=false;route.Clear();stuck=0f;
        nextTaskPending=true;
    }
    private int returnRetries;
    private void ReplanReturn()
    {
        route.Clear();stuck=0f;returnRetries++;
        feet=boat.InverseTransformPoint(robot.transform.position-Vector3.up*footOffset);
        if(TryPlanLedgeRecovery(home))return;
        // Try a different corridor instead of replaying the same blocked segment.
        if(!NavalShipRoutes.InStairwell(new NavalWalkPoint(feet.x,feet.y,feet.z),hull.Definition))
        {
            float floor=feet.y>hull.Definition.WeatherDeck-.2f?hull.Definition.WeatherDeck:feet.y>.3f?.825f:hull.Floor;
            float side=feet.x<0f?-1f:1f;
            Vector3 detour=new Vector3(side*(returnRetries%2==1?2.1f:1.4f),floor,feet.z);
            if(!Supported(detour)||!ClearBody(detour)||!NavigationWalkClear(feet,detour)){PlanTo(home);return;}
            route.Add(detour);
            Append(NavalSupplyRoutes.Plan(new NavalWalkPoint(detour.x,detour.y,detour.z),new NavalWalkPoint(home.x,home.y,home.z),hull.Definition));
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
        feet=boat.InverseTransformPoint(robot.transform.position-Vector3.up*footOffset);
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
            float delay=Mathf.Max(8f,NavalRepairDispatch.RetryDelay(leak.InflowRate>.00001f));retryAfter[leak]=Time.time+delay;
            // Exclude the whole blocked repair patch, not just one voxel from the same hole.
            foreach(var cell in hull.GetDamagedVoxels())
                if((cell.LocalCenter-leak.LocalCenter).sqrMagnitude<.85f*.85f)retryAfter[cell]=Time.time+delay;
            Debug.Log("[NavalPrototype] Repair access retry: hole="+leak.LocalCenter+" flow="+leak.InflowRate.ToString("F3")+" retry="+delay+"s");
        }
        ContinueRepair();
    }
    private void ContinueRepair(){BeginReturn();}
    private void Append(List<NavalWalkPoint> points)
    {foreach(NavalWalkPoint point in points)route.Add(new Vector3(point.X,point.Y,point.Z));}
    private void Release()
    {
        CancelAmmo();ReleaseSails();CancelCannonParts();CancelPlankTrip();
        nextTaskPending=false;waitingForRepair=false;
        CancelFire();
        ReleaseShieldJob();
        ReleasePump();
        CancelBailing();
        Walking(false,0f);
        foreach(NavalHullVoxel cell in claimed)if(cell!=null&&cell.RepairClaim==this)cell.RepairClaim=null;
        if(leak!=null&&leak.RepairMechanism!=null&&leak.RepairMechanism.RepairWorker==this)
        {leak.RepairMechanism.RepairWorker=null;leak.RepairMechanism.ProgressUntil=0;}
        claimed.Clear();leak=null;progress.Reset();
        if(hammer!=null)hammer.localRotation=hammerRest;
    }
    public void AnchorAtPost(Vector3 post){
        // Preserve a real raised footing instead of anchoring through a trim/step into the floor.
        if((feet.y-post.y)*hull.WorldScale<=.12f||!Supported(feet))feet=post;
        HoldPosition();
    }
    public CharacterController BodyController {get{return Controller();}}
    private CharacterController Controller()
    {if(cachedController==null&&robot!=null)cachedController=robot.GetComponentInChildren<CharacterController>();return cachedController;}
    private void HoldPosition()
    {
        Vector3 wanted=boat.TransformPoint(feet)+Vector3.up*footOffset;
        if((robot.transform.position-wanted).sqrMagnitude>.000001f){
            CharacterController controller=Controller();bool enabled=controller!=null&&controller.enabled;
            if(enabled)controller.enabled=false;
            robot.transform.position=wanted;
            if(enabled){controller.enabled=true;NavalCrewPassage.Prepare(controller);}
        }
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
        bool stairs=NavalShipRoutes.InStairwell(new NavalWalkPoint(feet.x,feet.y,feet.z),hull.Definition)||NavalShipRoutes.InStairwell(new NavalWalkPoint(destination.x,destination.y,destination.z),hull.Definition);
        float wanted=NavalCrewMovement.TargetSpeed(DeckWalkSpeed,alignment,offset.magnitude,finalPoint,stairs);
        bool escapeBurst=Time.time<recoveryDashUntil&&!stairs;
        if(escapeBurst&&!movingInWater){wanted*=2f;moveSpeed=wanted;}
        moveSpeed=NavalCrewMovement.Accelerate(moveSpeed,wanted,dt);
        if(movingInWater)moveSpeed=Mathf.Min(moveSpeed,wanted);
        Vector3 step=Vector3.ClampMagnitude(offset,moveSpeed*dt);
        if(hopTime>0)step.y=HopStep(dt);
        CharacterController controller=Controller();DeckStepHeight(controller);
        if(controller!=null&&controller.enabled)controller.Move(step+Vector3.down*0.3f*dt);
        else robot.transform.position+=step;
        feet=boat.InverseTransformPoint(robot.transform.position-Vector3.up*footOffset);
        if(InsideWalkingDeck(before)&&!InsideWalkingDeck(feet))
        {feet=before;HoldPosition();moveSpeed=0;}
        Vector3 travel=boat.TransformVector(feet-before);travel.y=0f;
        if(gait!=null)gait.Moved(travel.magnitude);
    }
    private void Walking(bool active,float dt)
    {
        if(!active){movementWatchdog.Reset();moveSpeed=0f;if(gait!=null)gait.Stop();}
    }
    private void OnDestroy(){NavalCharacters.Remove(robot);Release();if(hull!=null)hull.RepairWorkers.Remove(this);}
    private void Update()
    {if(robot==null||!robot.IsAlive()||(hull!=null&&hull.Sunk)){Release();returning=false;}}
}
