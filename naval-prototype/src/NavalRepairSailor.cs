using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public static class NavalCrewToolPolicy
{
    public static bool HammerVisible(bool steamPowered,bool requested){return requested&&!steamPowered;}
}

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
    public float ActiveHullPatchSeconds {get{return leak!=null&&leak.HydraulicShell&&leak.RepairMechanism==null&&route.Count==0&&work>0f?Mathf.Max(0f,RepairSeconds-work):0f;}}
    private float RepairSeconds {get{return leak!=null&&leak.RepairMechanism!=null?leak.RepairMechanism.RepairSeconds:4f;}}
    private NavalHullVoxel leak;
    private readonly List<Vector3> route=new List<Vector3>();
    private readonly List<NavalHullVoxel> claimed=new List<NavalHullVoxel>();
    private readonly Dictionary<NavalHullVoxel,float> retryAfter=new Dictionary<NavalHullVoxel,float>();
    private readonly List<NavalHullVoxel> candidates=new List<NavalHullVoxel>();
    private readonly List<Vector3> testedRepairSites=new List<Vector3>();
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
        if(Yielding||RepairDuty||stranded)return;
        // A rotating main-battery seat moves a little every frame. Rebuilding
        // its complete route each time discarded all walking progress, so the
        // moving ship could leave its operator many metres astern. Retarget
        // the final waypoint in place while the same nearby post is moving.
        if(returning&&changingPost&&boat.TransformVector(home-post).sqrMagnitude<.75f*.75f)
        {
            if(!home.Equals(post)&&route.Count>0&&route[route.Count-1].Equals(home))
                route[route.Count-1]=post;
            home=post;return;
        }
        // A cancelled task can leave the sailor far from an unchanged home post.
        // The post being unchanged does not mean the sailor has arrived there.
        if(!force&&(home-post).sqrMagnitude<.01f&&(returning||AtStation(post)))return;
        feet=boat.InverseTransformPoint(robot.transform.position-Vector3.up*footOffset);
        home=post;route.Clear();returning=true;changingPost=true;
        PlanTo(home);
    }
    public void FollowDestroyerGunPost(Vector3 post)
    {
        if(home.Equals(post)||Busy||stranded||!AtStation(home)||!NavalStationArrival.CanRideMovingPost(boat.TransformVector(home-post).sqrMagnitude))return;
        if(!Supported(post)||!ClearBody(post)||!NavigationClear(feet,post))return;
        home=post;feet=post;
        HoldPosition();
    }
    public bool TrackMovingPost(Vector3 post)
    {
        if(RepairDuty||stranded||robot==null||!robot.IsAlive())return false;
        // Once seated on the raised turntable, follow its small rigid motion
        // directly. Sending that motion through the deck router made it plan a
        // descent from the turntable on every traverse and eventually stall at
        // a vertical deck waypoint.
        if(!AtMainBatteryPost(home)||!NavalStationArrival.CanRideMovingPost(boat.TransformVector(home-post).sqrMagnitude))
        {SetPost(post);return false;}
        home=post;feet=post;route.Clear();returning=false;changingPost=false;
        ledgeWaypoints=0;stairExitWaypoints=0;stuck=0f;moveSpeed=0f;
        HoldPosition();return true;
    }
    public bool TrackMovingPostVia(Vector3 post,Vector3 approach)
    {
        float movingShift=boat.TransformVector(home-post).sqrMagnitude;
        // Once the operator has reached the previous seat pose, the mount owns
        // that footing. A small training displacement must be ridden directly;
        // testing a capsule path through the surrounding shield would send the
        // seated sailor back to the rear entrance on every traverse tick.
        if(NavalStationArrival.KeepMovingPost(AtMainBatteryPost(home),movingShift))return TrackMovingPost(post);
        if(AtMainBatteryPost(post))return TrackMovingPost(post);
        bool goingToPost=returning&&changingPost&&boat.TransformVector(home-post).sqrMagnitude<.75f*.75f;
        // Continue the short final movement only while it is physically clear.
        // If a rotating shield crossed the route, back out to its moving rear
        // entrance before attempting the seat again.
        if(goingToPost&&NavigationClear(feet,post)){TrackMovingPost(post);return false;}
        if(AtStation(approach)){SetPost(post,true);return false;}
        SetPost(approach,goingToPost);return false;
    }
    public bool AtStation(Vector3 post)
    {Vector3 delta=feet-post;return NavalStationArrival.CanAnchor(boat.TransformVector(delta).sqrMagnitude)||
        NavalStationArrival.OnRaisedSupport(delta.x,delta.y,delta.z,hull.WorldScale)&&Supported(feet);}
    public bool AtMainBatteryPost(Vector3 post)
    {Vector3 delta=feet-post;return NavalStationArrival.AtMainBatterySeat(delta.x,delta.y,delta.z,hull.WorldScale);}
    private Transform hammer;
    private Quaternion hammerRest;
    private void ShowHammer(bool visible)
    {if(hammer!=null)hammer.gameObject.SetActive(NavalCrewToolPolicy.HammerVisible(hull.Definition.SteamPowered,visible));}
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
    public void JoinCrew(NavalHull ship,NavalStructure frame)
    {
        var oldControls=NavalStations.ForShip(boat);
        if(CapitalReserved&&oldControls!=null)ClearCapitalAccess(oldControls);
        Release();
        route.Clear();retryAfter.Clear();rejectedRepairPoints.Clear();
        hull.RepairWorkers.Remove(this);
        hull=ship;structure=frame;boat=ship.Root;
        hull.RepairWorkers.Add(this);
        FinalAttackDuty=false;StationDutyProtected=false;SpareMaintenance=true;
        changingPost=false;escapePending=false;ledgeWaypoints=0;
        stableFootingRevision=-1;yieldUntil=0;unsupportedFallSpeed=0;
        EndRescue();
        InitializeDeckCarry();
    }
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
    private static readonly System.Action<FirstPersonMover,Vector3> SetVelocity=CreateVelocitySetter();
    private static System.Action<FirstPersonMover,Vector3> CreateVelocitySetter()
    {
        FieldInfo field=typeof(FirstPersonMover).GetField("_velocity",BindingFlags.Instance|BindingFlags.NonPublic);
        if(field==null)return null;
        try
        {
            var method=new System.Reflection.Emit.DynamicMethod("SetNavalCrewVelocity",typeof(void),
                new[]{typeof(FirstPersonMover),typeof(Vector3)},typeof(NavalRepairSailor),true);
            var il=method.GetILGenerator();
            il.Emit(System.Reflection.Emit.OpCodes.Ldarg_0);
            il.Emit(System.Reflection.Emit.OpCodes.Ldarg_1);
            il.Emit(System.Reflection.Emit.OpCodes.Stfld,field);
            il.Emit(System.Reflection.Emit.OpCodes.Ret);
            return (System.Action<FirstPersonMover,Vector3>)method.CreateDelegate(typeof(System.Action<FirstPersonMover,Vector3>));
        }
        catch(System.Exception){return (mover,value)=>field.SetValue(mover,value);}
    }
    public bool Busy { get { return Yielding||CapitalReserved||ammoTarget!=null||HandlingSails||nextTaskPending||fireJob!=null||waitingForRepair||shieldJob!=null||pumpDuty||bailState!=0||leak!=null||returning; } }
    public bool MaintenanceDuty {get{return nextTaskPending||fireJob!=null||waitingForRepair||shieldJob!=null||leak!=null||pumpDuty||bailState!=0;}}
    public bool RepairingCannon {get{return leak!=null&&leak.RepairMechanism!=null&&leak.RepairMechanism.IsCannon;}}
    public bool ReturningWithoutWork {get{return returning&&!changingPost&&!MaintenanceDuty&&!HandlingSails;}}
    public void RecallCannonForCombat()
    {
        if(!RepairingCannon)return;
        Release();route.Clear();returning=false;changingPost=false;stuck=0;
    }
    public void RecallToMainBattery(Vector3 post,Vector3 approach)
    {
        bool mainRoute=returning&&changingPost&&
            (boat.TransformVector(home-post).sqrMagnitude<.75f*.75f||boat.TransformVector(home-approach).sqrMagnitude<.75f*.75f);
        if(mainRoute)return;
        if(!Busy&&!RepairDuty&&!HandlingSails&&!DeliveringAmmo)return;
        // The emergency-release transition already clears the seat pose in
        // NavalCrewController. Do not clear it here: a rotating mount can move
        // farther than the moving-post tolerance and make an ordinary retarget
        // look like a cancelled task for one frame.
        Release();route.Clear();returning=false;changingPost=false;stuck=0f;moveSpeed=0f;
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
    public string Status { get { return Yielding?"Yielding passage":CapitalReserved?CapitalAccessStatus:HandlingSails?(sailDuty!=null&&sailDuty.SteamPowered?(route.Count>0?"Going to engine":"Tending engine"):(route.Count>0?"Going to sails":"Handling sails")):nextTaskPending?"Choosing next task":fireJob!=null?FireStatus:waitingForRepair?"Waiting for leak access":cannonPartsStage>0?CannonPartsStatus:plankStage>0?PlankStatus:ShieldWorking?ShieldStatus:pumpDuty?(route.Count>0?"Going to pump":hull.Bilge.Status):bailState!=0?BailStatus:leak!=null?(route.Count>0?"Going to leak":(leak.RepairMechanism!=null?("Repair "+leak.RepairMechanism.Name+" "):"Repair ")+Mathf.RoundToInt(work/RepairSeconds*100f)+"%"):returning?"Returning":"On duty"; } }
    public void Initialize(Character sailor,NavalHull owner,NavalStructure frame,Vector3 station,float feetOffset)
    {
        NavalCharacters.Register(sailor);
        var mission=NavalTestWorld.Active;if(mission!=null)mission.RecordConvoySailor(sailor,owner);
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
            if(tool!=null){hammer=tool.transform;hammerRest=hammer.localRotation;ShowHammer(true);}
            gait=robot.gameObject.AddComponent<NavalSailorGait>();gait.Initialize(robot,model);
            if(hull.Definition.SteamPowered)robot.gameObject.AddComponent<NavalSteamSailorUniform>().Initialize(robot,model);
        }
    }
    public void Tick(float dt,bool mayTakeJob)
    {
        using(NavalPerformance.Measure(NavalPerfArea.CrewWorkerTick))TickWorker(dt,mayTakeJob);
    }

    private void TickWorker(float dt,bool mayTakeJob)
    {
        if(robot!=null&&robot.IsAlive()&&TickPassageYield(dt))return;
        if(capitalStage>=2)return;
        if(Defending||Rescuing)return;
        if(robot==null||!robot.IsAlive()||hull.Sunk||stranded){planks.Drop();Release();return;}
        ApplyUnsupportedFall(dt);
        if(TickSails(dt))return;
        if(TickAmmo(dt,mayTakeJob))return;
        if(nextTaskPending){if(StationDutyProtected){nextTaskPending=false;SetPost(home,true);}else {using(NavalPerformance.Measure(NavalPerfArea.CrewNextTask))DispatchNextTask();return;}}
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
            if(Busy)HoldPosition();
            using(NavalPerformance.Measure(NavalPerfArea.CrewLeakSearch))TryFindLeak();
            if(leak==null&&ReserveRepairWait())
            {waitingForRepair=true;returning=false;changingPost=false;route.Clear();}
        }
        if(TickPlanks(dt))return;
        bool reachablePatch=false;
        if(leak!=null)
        {
            // Only accept flowing leaks, but finish an accepted patch across wave troughs.
            reachablePatch=route.Count==0&&CanReachLeak();
            progress.StepAccepted(dt,reachablePatch);
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
            if(!structure.CanRestoreCell(leak)){retryAfter[leak]=Time.time+30f;ContinueRepair();return;}
            Walking(false,dt);
            HoldPosition();
            Vector3 face=boat.TransformPoint(leak.LocalCenter)-robot.transform.position;face.y=0f;
            if(face.sqrMagnitude>0.01f)robot.transform.rotation=Quaternion.LookRotation(face);
            if(hammer!=null)hammer.localRotation=hammerRest*Quaternion.Euler(-35f+Mathf.Sin(work*12f)*45f,0f,0f);
            if(!reachablePatch)
            {
                AbandonBlockedJob();
                return;
            }
            if(work>=RepairSeconds)
            {
                if(leak.RepairMechanism!=null&&leak.RepairMechanism.IsCannon)
                {leak.RepairMechanism.CompleteCannonRepair(structure,work);ContinueRepair();return;}
                Vector3 patch=leak.LocalCenter;bool usesBoard=NeedsPlanks(leak),restored=false;int restoredCells=0;
                foreach(NavalHullVoxel cell in claimed)
                    if(cell.RepairClaim==this&&(cell.HydraulicShell||cell.InternalBarrier||cell.RepairableEquipment||IsWalkwayHole(cell))&&(cell.Missing||cell.Detached)&&
                        (cell.LocalCenter-patch).sqrMagnitude<=0.85f*0.85f){structure.RestoreCell(cell);if(!cell.Missing&&!cell.Detached){if(usesBoard)hull.ApplyWoodPatch(cell);restored=true;restoredCells++;}}
                if(!restored){
                    retryAfter[leak]=Time.time+30f;
                    Debug.Log("[NavalPrototype] Repair attempt deferred: sailor="+robot.GetInstanceID()+" target="+patch+" reason=no restorable claimed cells");
                    ContinueRepair();return;
                }
                if(usesBoard&&restored)planks.Consume();
                Debug.Log("[NavalPrototype] Repair finished: sailor="+robot.GetInstanceID()+" ship="+boat.name+" patch="+patch+" restored="+restoredCells+" boardUsed="+(usesBoard&&restored));
                ContinueRepair();
            }
        }
    }
    private static int RepairTier(NavalHullVoxel cell)
    {return NavalMaintenancePriority.RepairTier(cell.RepairableEquipment,cell.RepairMechanism!=null&&cell.RepairMechanism.IsPump,cell.RepairMechanism!=null&&cell.RepairMechanism.IsCannon,cell.RepairMechanism!=null&&cell.RepairMechanism.IsSteamMain);}
    private int CompareCandidatePriority(NavalHullVoxel a,NavalHullVoxel b)
    {int tier=RepairTier(b).CompareTo(RepairTier(a));return tier!=0?tier:priorities[b].CompareTo(priorities[a]);}
    private bool IsWalkwayHole(NavalHullVoxel cell){
        if(cell==null||cell.ManagedRig||cell.RepairableEquipment||cell.Detached||cell.transform.parent!=boat)return false;
        // Stair treads are repairable access structure even between deck levels.
        if(NavalShipRoutes.InStairwell(new NavalWalkPoint(cell.LocalCenter.x,cell.LocalCenter.y,cell.LocalCenter.z),hull.Definition)&&
           Mathf.Abs(cell.LocalCenter.y-feet.y)*hull.WorldScale<2.5f&&new Vector2(cell.LocalCenter.x-feet.x,cell.LocalCenter.z-feet.z).magnitude*hull.WorldScale<3f)return true;
        foreach(float deck in new float[]{hull.Floor,.825f,hull.Definition.WeatherDeck,hull.Definition.HelmDeck})
            if(Mathf.Abs(feet.y-deck)*hull.WorldScale<.6f&&cell.LocalCenter.y>=deck-.3f&&cell.LocalCenter.y<=deck+.01f&&new Vector2(cell.LocalCenter.x-feet.x,cell.LocalCenter.z-feet.z).magnitude*hull.WorldScale<3f)return true;
        return false;
    }
    private bool ProtectedFiringGunner(){
        if(hull.CombatCritical)return false;
        var world=NavalTestWorld.Active;var controls=NavalStations.ForShip(boat);
        Transform target=world==null?null:world.PlayerHull==hull?world.EnemyBoat:world.PlayerHull!=null?world.PlayerHull.Root:null;
        if(controls==null||target==null)return false;
        for(int station=1;station<=controls.GunCount;station++)if((home-controls.Post(station)).sqrMagnitude<.01f&&AtStation(home)&&controls.CrewGunAssignable(station)&&controls.CanBearOnShip(station,target))return true;
        return false;
    }
    private bool CloserRepairer(NavalHullVoxel hole,Vector3 point){
        float mine=SupportTravelCost(point);
        foreach(var w in hull.RepairWorkers)if(w!=null&&w!=this&&w.robot!=null&&w.robot.IsAlive()&&!w.Stranded&&!w.Rescuing&&!w.Defending&&!w.HandlingSails&&(!w.Busy||w.ReturningWithoutWork)&&w.MayAcquireMaintenance()&&w.CanSupplyPlanks&&!w.ProtectedFiringGunner()){
            float retry;if(w.retryAfter.TryGetValue(hole,out retry)&&Time.time<retry)continue;
            if(w.SupportTravelCost(point)+.25f<mine){Vector3 accessible;if(w.FindWorkPoint(hole,out accessible))return true;}
        }return false;
    }
    private void FindLeak()
    {
        if(!MayAcquireMaintenance()||ProtectedFiringGunner())return;
        feet=boat.InverseTransformPoint(robot.transform.position-Vector3.up*footOffset);
        candidates.Clear();priorities.Clear();NavalHullVoxel selected=null;
        bool otherCannonRepair=false;int otherGunRepairs=0;
        foreach(var worker in hull.RepairWorkers)
            if(worker!=null&&worker!=this&&worker.robot!=null&&worker.robot.IsAlive()&&worker.RepairingCannon){otherGunRepairs++;}
        otherCannonRepair=otherGunRepairs>=2;
        bool activeFire=hull.Fire!=null&&hull.Fire.HasFire;
        bool pumpDemand=hull.Bilge!=null&&hull.Bilge.Functional&&hull.Bilge.WantsOperator;
        foreach(NavalHullVoxel cell in hull.GetDamagedVoxels())
            if((cell.HydraulicShell||cell.InternalBarrier||cell.RepairableEquipment||IsWalkwayHole(cell))&&(cell.Missing||cell.Detached)&&cell.RepairClaim==null&&(cell.RepairMechanism==null||cell.RepairMechanism.CanRepair))
            {
                if(!structure.CanRestoreCell(cell))continue;
                if(cell.RepairableEquipment&&NavalMaintenancePriority.DeferEquipment(
                    cell.RepairMechanism!=null&&cell.RepairMechanism.IsCannon,cell.RepairMechanism!=null&&cell.RepairMechanism.IsPump,activeFire,pumpDemand&&!(hull.CrewCannonRepairAllowed&&cell.RepairMechanism!=null&&cell.RepairMechanism.IsCannon),cell.RepairMechanism!=null&&cell.RepairMechanism.IsSteamMain))continue;
                if(cell.RepairMechanism!=null&&cell.RepairMechanism.IsCannon&&
                    (!NavalMaintenancePriority.CanClaimCannon(hull.CrewCannonRepairAllowed,otherCannonRepair)||!cell.RepairMechanism.CanFetchParts||cell.RepairMechanism.PlayerRepairUntil>Time.time||cell.RepairMechanism.RepairWorker!=null&&cell.RepairMechanism.RepairWorker!=this))continue;
                if(NeedsPlanks(cell)&&!CanSupplyPlanks)continue;
                float priority=cell.RepairableEquipment?.001f:NavalLeakPriority.Score(cell.InflowRate,cell.LastWetAt,cell.LastWetFlow,Time.time);
                if(hull.BoilerProtection!=null&&hull.BoilerProtection.IsPanel(cell))priority=Mathf.Max(priority,hull.SteamMachinery!=null&&hull.SteamMachinery.PropulsionFloodEmergency?1f:.1f);
                if(hull.SteamMachinery!=null&&hull.SteamMachinery.PropulsionFloodEmergency&&cell.HydraulicShell&&cell.LocalCenter.x<0&&Mathf.Abs(cell.LocalCenter.z-NavalSteamLayout.BoilerZFor(hull.Definition))<1.5f)priority*=3f;
                if(priority<=0f&&IsWalkwayHole(cell))priority=.2f;
                if(priority<=0f)continue;
                float targetY=NavalRepairSite.Floor(cell.LocalCenter.y,hull.Floor);
                priority=NavalRepairRoutes.Priority(priority,NavalRepairRoutes.TravelCost(feet.x,feet.y,feet.z,cell.LocalCenter.x,targetY,cell.LocalCenter.z));
                float retry;if(retryAfter.TryGetValue(cell,out retry)&&Time.time<retry)continue;
                bool reserved=false;
                foreach(NavalRepairSailor worker in hull.RepairWorkers)
                    if(worker!=null&&worker!=this&&worker.HasJob&&(worker.JobPoint-cell.LocalCenter).sqrMagnitude*hull.WorldScale*hull.WorldScale<2f*2f){reserved=true;break;}
                if(!reserved){candidates.Add(cell);priorities[cell]=priority;}
            }
        if(compareCandidates==null)compareCandidates=CompareCandidatePriority;
        candidates.Sort(compareCandidates);
        Vector3 workPoint=Vector3.zero;
        int checkedSites=0;testedRepairSites.Clear();
        foreach(NavalHullVoxel candidate in candidates)
        {
            bool duplicate=false;foreach(Vector3 tested in testedRepairSites)
                if((candidate.LocalCenter-tested).sqrMagnitude*hull.WorldScale*hull.WorldScale<.75f*.75f){duplicate=true;break;}
            if(duplicate)continue;
            if(checkedSites++>=12)break;
            if(!NavalWalkRoutes.InStairwell(feet.x,feet.y,feet.z,hull.Definition.StairWidth*.5f+.175f,hull.Floor)&&
                (Mathf.Abs(feet.y-0.825f)<0.25f||Mathf.Abs(feet.y-hull.Floor)<0.25f||Mathf.Abs(feet.y-hull.Definition.WeatherDeck)<0.25f)&&ReachFrom(feet,candidate))
            {workPoint=feet;if(CloserRepairer(candidate,workPoint))continue;selected=candidate;break;}
            if(FindWorkPoint(candidate,out workPoint)){if(CloserRepairer(candidate,workPoint))continue;selected=candidate;break;}
            testedRepairSites.Add(candidate.LocalCenter);
            retryAfter[candidate]=Time.time+NavalRepairDispatch.RetryDelay(candidate.InflowRate>.00001f);
        }
        if(selected==null)
        {
            if(candidates.Count>0&&hull.ShouldReportUnavailableRepair(candidates[0],Time.time))
            {
                NavalHullVoxel blocked=candidates[0];
                Debug.Log("[NavalPrototype] Repair work point unavailable: sailor="+robot.GetInstanceID()+" ship="+boat.name+
                    " hole="+blocked.LocalCenter+" flow="+blocked.InflowRate.ToString("F3")+" feet="+feet+" candidates="+candidates.Count);
            }
            return;
        }
        string wetAge=float.IsNegativeInfinity(selected.LastWetAt)?"dry/never wet":(Time.time-selected.LastWetAt).ToString("F1");
        Debug.Log("[NavalPrototype] Repair job: sailor="+robot.GetInstanceID()+" ship="+boat.name+" hole="+selected.LocalCenter+" work="+workPoint+" wet age="+wetAge);
        leak=selected;
        if(leak.RepairMechanism!=null&&leak.RepairMechanism.IsCannon)leak.RepairMechanism.RepairWorker=this;
        waitingForRepair=false;repairReplans=0;rejectedRepairPoints.Clear();repairDestination=workPoint;progress.Reset();route.Clear();returning=false;changingPost=false;stuck=0f;
        foreach(NavalHullVoxel cell in hull.GetDamagedVoxels())
            if((cell.HydraulicShell||cell.InternalBarrier||cell.RepairableEquipment||IsWalkwayHole(cell))&&(cell.Missing||cell.Detached)&&cell.RepairClaim==null&&structure.CanRestoreCell(cell)&&(cell.LocalCenter-leak.LocalCenter).sqrMagnitude<0.85f*0.85f)
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
        Vector3 hand,offset;
        return RepairReachVector(stand,hole,out hand,out offset)&&RepairSightClear(hand,offset,CannonRepairTarget(hole));
    }
    private static NavalRepairableMechanism CannonRepairTarget(NavalHullVoxel hole)
    {
        return hole.RepairMechanism!=null&&hole.RepairMechanism.IsCannon?hole.RepairMechanism:null;
    }
    private bool RepairReachVector(Vector3 stand,NavalHullVoxel hole,out Vector3 hand,out Vector3 offset)
    {
        hand=boat.TransformPoint(stand)+boat.up*NavalRepairAim.HandHeight;
        Vector3 local=hull.RepairAccess(hole,stand);
        if(hole.HydraulicShell&&(local-hole.LocalCenter).sqrMagnitude<.001f){
            if(NavalRepairAim.AtEnd(local.z))local.z=NavalRepairAim.InnerEnd(local.z,hull.WorldScale);
            else if(NavalRepairAim.AtSide(local.x,hull.Definition.HullHalfWidth(local.z)))local.x=NavalRepairAim.InnerSide(local.x,hull.Definition.HullHalfWidth(local.z),hull.WorldScale);
        }
        local.y=hull.Definition.RepairAimY(local.y,stand.y);
        offset=boat.TransformPoint(local)-hand;
        return NavalRepairSite.WithinReach(offset.x,offset.y,offset.z);
    }
    private bool CanUseRepairSite(Vector3 point,NavalHullVoxel hole)
    {
        Vector3 hand,offset;
        return InsideWalkingDeck(point)&&!RejectedPoint(hole,point)&&RepairReachVector(point,hole,out hand,out offset)&&
            Supported(point)&&!hull.DeckGapAt(point,NavigationRadius)&&ClearBody(point)&&
            RepairSightClear(hand,offset,CannonRepairTarget(hole));
    }
    private static readonly float[] destroyerRepairOffsets={0f,-1.2f,1.2f,-2.2f,2.2f};
    private bool FindWorkPoint(NavalHullVoxel hole,out Vector3 point)
    {
        if(hull.Definition.Destroyer){
            float q=hull.WorldScale;foreach(float floor in new[]{hull.Floor,hull.Definition.WeatherDeck,NavalDestroyerLayout.Foredeck(hull.Definition),hull.Definition.HelmDeck})foreach(float dx in destroyerRepairOffsets)foreach(float dz in destroyerRepairOffsets){
                point=new Vector3(hole.LocalCenter.x+dx/q,floor,hole.LocalCenter.z+dz/q);float limit=hull.Definition.HullHalfWidth(point.z)-.9f/q;point.x=Mathf.Clamp(point.x,-limit,limit);point.z=Mathf.Clamp(point.z,-6+.9f/q,6-.9f/q);
                if(CanUseRepairSite(point,hole))return true;
            }
            point=Vector3.zero;return false;
        }
        float preferred=NavalRepairSite.Floor(hole.LocalCenter.y,hull.Floor);
        if(hull.Definition.SteamPowered&&Mathf.Abs(hole.LocalCenter.y-hull.Definition.HelmDeck)*hull.WorldScale<.6f){
            for(int x=-1;x<=1;x++)for(int z=-1;z<=1;z++){
                point=new Vector3(hole.LocalCenter.x+x*.8f/hull.WorldScale,hull.Definition.HelmDeck,hole.LocalCenter.z+z*.8f/hull.WorldScale);
                if(NavalRaisedBridge.Upper(new NavalWalkPoint(point.x,point.y,point.z),hull.Definition)&&Supported(point)&&!hull.DeckGapAt(point,NavigationRadius)&&ClearBody(point)&&!RejectedPoint(hole,point)&&ReachFrom(point,hole))return true;
            }
        }
        // Gun mechanisms are repaired at their own service point.  Only a
        // hull patch must avoid the occupied gunner bay; otherwise this guard
        // would make a damaged casemate itself impossible to restore.
        bool avoidCasemate=(hole.HydraulicShell||hole.InternalBarrier)&&!hole.RepairableEquipment;
        bool stairs=NavalWalkRoutes.InStairwell(feet.x,feet.y,feet.z,hull.Definition.StairWidth*.5f+.175f,hull.Floor);
        if(NavalRepairDispatch.PreferCurrentDeck(feet.y,hull.Floor,stairs))preferred=feet.y>.3f?.825f:hull.Floor;
        if(hull.Definition.DeckCount>2&&(feet.y>hull.Definition.WeatherDeck-.25f||hole.LocalCenter.y>hull.Definition.WeatherDeck))preferred=hull.Definition.WeatherDeck;
        float serviceX,serviceY,serviceZ;
        for(int service=0;service<NavalRepairWorkSites.SteamSternBeltCandidateCount;service++)
        {
            if(!NavalRepairWorkSites.SteamSternBelt(hull.Definition,hole.LocalCenter.x,hole.LocalCenter.y,hole.LocalCenter.z,
                service,out serviceX,out serviceY,out serviceZ))break;
            point=new Vector3(serviceX,serviceY,serviceZ);
            if(CanUseRepairSite(point,hole))return true;
        }
        for(int service=0;service<NavalRepairWorkSites.SteamBowBeltCandidateCount;service++)
        {
            if(!NavalRepairWorkSites.SteamBowBelt(hull.Definition,hole.LocalCenter.x,hole.LocalCenter.y,hole.LocalCenter.z,
                service,out serviceX,out serviceY,out serviceZ))break;
            point=new Vector3(serviceX,serviceY,serviceZ);
            if(CanUseRepairSite(point,hole))return true;
        }
        bool endWall=NavalRepairWorkSites.EndWall(hole.LocalCenter.z);
        for(int deck=0;deck<hull.Definition.DeckCount;deck++)
        {
            float floor=hull.Definition.RepairFloor(preferred,deck);
            if(endWall)for(int i=0;i<NavalRepairWorkSites.ShiftCount;i++)
            {
                float shift=NavalRepairWorkSites.Shift(i);
                point=new Vector3(Mathf.Clamp(hole.LocalCenter.x+shift,-2.35f,2.35f),floor,NavalRepairWorkSites.EndZ(hull.Definition,hole.LocalCenter.z));
                if(hull.Definition.MaintenanceFloorAt(floor)&&hull.Bilge!=null)point.y=NavalBallastLayout.Surface(point.x,point.z,floor,hull.Definition);
                if((!avoidCasemate||NavalRepairWorkSites.ClearOfCasemate(hull.Definition,floor,point.z))&&CanUseRepairSite(point,hole))return true;
            }
            for(int c=0;c<NavalRepairWorkSites.CorridorCount;c++)for(int i=0;i<NavalRepairWorkSites.ShiftCount;i++)
            {
                float corridor=NavalRepairWorkSites.Corridor(hull.Definition,c),shift=NavalRepairWorkSites.Shift(i);
                point=new Vector3(hole.LocalCenter.x<0f?-corridor:corridor,floor,Mathf.Clamp(hole.LocalCenter.z+shift,-5.1f,5.1f));
                if(hull.Definition.MaintenanceFloorAt(floor)&&hull.Bilge!=null)point.y=NavalBallastLayout.Surface(point.x,point.z,floor,hull.Definition);
                if((!avoidCasemate||NavalRepairWorkSites.ClearOfCasemate(hull.Definition,floor,point.z))&&CanUseRepairSite(point,hole))return true;
            }
        }
        if(endWall)for(int deck=0;deck<hull.Definition.DeckCount;deck++){
            float floor=hull.Definition.RepairFloor(preferred,deck);
            for(int depth=0;depth<3;depth++)for(int lateral=-1;lateral<=1;lateral++){
                float z=hole.LocalCenter.z-Mathf.Sign(hole.LocalCenter.z)*(1.5f+depth)/hull.WorldScale;
                float x=hole.LocalCenter.x+lateral*1.2f/hull.WorldScale;
                point=new Vector3(x,floor,z);
                if(hull.Definition.MaintenanceFloorAt(floor)&&hull.Bilge!=null)point.y=NavalBallastLayout.Surface(x,z,floor,hull.Definition);
                if(CanUseRepairSite(point,hole))return true;
            }
        }
        // Bow deck-seam holes need positions inward and aft of the destroyed slab.
        // Fixed side aisles can lie outside the taper, or inside the hole's safety margin.
        if(hull.Definition.SteamPowered&&hole.LocalCenter.z>4.2f)for(int deck=0;deck<hull.Definition.DeckCount;deck++){
            float floor=hull.Definition.RepairFloor(preferred,deck);
            for(int depth=0;depth<3;depth++)for(int aft=0;aft<3;aft++){
                float x=hole.LocalCenter.x-Mathf.Sign(hole.LocalCenter.x)*(1.7f+depth*.8f)/hull.WorldScale;
                float z=hole.LocalCenter.z-aft*1.1f/hull.WorldScale;
                point=new Vector3(x,floor,z);
                if(hull.Definition.MaintenanceFloorAt(floor)&&hull.Bilge!=null)point.y=NavalBallastLayout.Surface(x,z,floor,hull.Definition);
                if(CanUseRepairSite(point,hole))return true;
            }
        }
        // Fixed corridor lanes can miss a narrow service pocket at the bow or a deck seam.
        // Test nearby positions in world metres, retaining all original physical checks.
        for(int deck=0;deck<hull.Definition.DeckCount;deck++){
            float floor=hull.Definition.RepairFloor(preferred,deck);
            for(int side=-1;side<=1;side++)for(int along=-1;along<=1;along++){
                float x=hole.LocalCenter.x+(side*1.2f-Mathf.Sign(hole.LocalCenter.x)*1.2f)/hull.WorldScale;
                float z=hole.LocalCenter.z+along*1.2f/hull.WorldScale;
                point=new Vector3(x,floor,z);
                if(hull.Definition.MaintenanceFloorAt(floor)&&hull.Bilge!=null)point.y=NavalBallastLayout.Surface(x,z,floor,hull.Definition);
                if(CanUseRepairSite(point,hole))return true;
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
    {using(NavalPerformance.Measure(NavalPerfArea.CrewPlan))BuildRoute(goal);}

    private void BuildRoute(Vector3 goal)
    {
        stairExitWaypoints=0;
        var world=NavalTestWorld.Active;
        if(leak!=null&&world!=null&&world.WaterLevelAt(boat.TransformPoint(goal))>boat.TransformPoint(goal).y+2.6f){
            float floor=hull.Floor;for(int level=0;level<hull.Definition.MaintenanceDeckCount;level++)if(hull.Definition.MaintenanceFloor(level)<=goal.y+.001f)floor=hull.Definition.MaintenanceFloor(level);
            float surface=NavalBallastLayout.Surface(goal.x,goal.z,floor,hull.Definition);
            if(goal.y>surface+.1f){Append(NavalSupplyRoutes.Plan(new NavalWalkPoint(feet.x,feet.y,feet.z),new NavalWalkPoint(goal.x,surface,goal.z),hull.Definition));route.Add(goal);return;}
        }
        if(TryPlanLedgeRecovery(goal))return;
        float routeY=feet.y;
        if(!NavigationStairs(feet))for(int level=0;level<hull.Definition.MaintenanceDeckCount;level++){
            float floor=hull.Definition.MaintenanceFloor(level);
            if(Mathf.Abs(feet.y-floor)*hull.WorldScale<.3f){routeY=floor;break;}
        }
        var from=new NavalWalkPoint(feet.x,routeY,feet.z);var to=new NavalWalkPoint(goal.x,goal.y,goal.z);
        Append(hull.Definition.Destroyer?NavalDestroyerLiveRoutes.Plan(hull,from,to):NavalSupplyRoutes.Plan(from,to,hull.Definition));
    }
    private bool OtherMaintenanceWorker()
    {
        foreach(NavalRepairSailor worker in hull.RepairWorkers)
            if(worker!=null&&worker!=this&&worker.robot!=null&&worker.robot.IsAlive()&&worker.MaintenanceDuty)return true;
        return false;
    }
    public bool StationDutyProtected;
    private bool MayAcquireMaintenance(){if(StationDutyProtected)return false;if(SpareMaintenance&&!FinalAttackDuty&&hull.CrewCannonRepairAllowed&&!hull.HasRepairDemand&&(hull.Fire==null||!hull.Fire.HasFire))return true;return NavalMaintenanceMath.CanAcquireMaintenance(FinalAttackDuty,SpareMaintenance,hull.ParallelMaintenance,
        !FinalAttackDuty&&!SpareMaintenance&&!hull.ParallelMaintenance&&OtherMaintenanceWorker());}
    private bool OtherLeakRepairer()
    {
        foreach(NavalRepairSailor worker in hull.RepairWorkers)
            if(worker!=null&&worker!=this&&worker.robot!=null&&worker.robot.IsAlive()&&worker.RepairingLeak)return true;
        return false;
    }
    private float repairWaitSince=-1f,repairWaitRetryAt;
    private bool ReserveRepairWait()
    {
        if(Time.time<repairWaitRetryAt)return false;
        if(!waitingForRepair)repairWaitSince=-1f;
        if(!PendingLeaks()){repairWaitSince=-1f;return false;}
        if(repairWaitSince<0)repairWaitSince=Time.time;
        if(Time.time-repairWaitSince>2f){
            repairWaitRetryAt=Time.time+12f;repairWaitSince=-1f;
            Debug.Log("[NavalPrototype] Repair access wait released: sailor="+robot.GetInstanceID()+" ship="+boat.name);
            return false;
        }
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
        stairExitWaypoints=0;
        route.Clear();stuck=0f;returnRetries++;
        if(returnRetries>=3){returnRetries=0;BeginReturn();return;}
        feet=boat.InverseTransformPoint(robot.transform.position-Vector3.up*footOffset);
        if(TryPlanLedgeRecovery(home))return;
        if(hull.Definition.Destroyer){Append(NavalDestroyerLiveRoutes.Alternate(hull,new NavalWalkPoint(feet.x,feet.y,feet.z),new NavalWalkPoint(home.x,home.y,home.z),returnRetries));return;}
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
        Debug.Log("[NavalPrototype] Repair route replanned: sailor="+robot.GetInstanceID()+" ship="+boat.name+" hole="+leak.LocalCenter+" work="+alternative);
        return true;
    }
    private bool RepairNearbyAccessGap(){
        if(planks.Count==0||!Supported(feet))return false;
        NavalHullVoxel selected=null;float best=3.5f*3.5f;
        foreach(var cell in hull.GetDamagedVoxels()){
            if(cell==null||!IsWalkwayHole(cell)||!NeedsPlanks(cell)||!structure.CanRestoreCell(cell)||cell.RepairClaim!=null&&cell.RepairClaim!=this)continue;
            float retry;if(retryAfter.TryGetValue(cell,out retry)&&Time.time<retry)continue;
            Vector3 shape=cell.transform.localScale;
            if(cell.NonPhysicalDecoration||!NavalDeckGapMath.IsDeckLayer(cell.LocalCenter.y,Mathf.Abs(shape.y),hull.Definition))continue;
            float distance=boat.TransformVector(cell.LocalCenter-feet).sqrMagnitude;
            if(distance<best&&ReachFrom(feet,cell)){best=distance;selected=cell;}
        }
        if(selected==null||selected==leak)return false;
        Release();route.Clear();returning=changingPost=false;leak=selected;selected.RepairClaim=this;claimed.Add(selected);
        repairDestination=feet;repairReplans=0;rejectedRepairPoints.Clear();progress.Reset();stuck=0;
        Debug.Log("[NavalPrototype] Repair access deck first: sailor="+robot.GetInstanceID()+" ship="+boat.name+" target="+selected.LocalCenter);
        return true;
    }
    private void AbandonBlockedJob()
    {
        if(RepairNearbyAccessGap())return;
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
        stairExitWaypoints=0;
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
    public bool Navigating {get{return Yielding||route.Count>0||capitalStage==2||capitalStage==4;}}
    private CharacterController Controller()
    {if(cachedController==null&&robot!=null)cachedController=robot.GetComponentInChildren<CharacterController>();return cachedController;}
    private void HoldPosition()
    {using(NavalPerformance.Measure(NavalPerfArea.CrewAnchor))ApplyStationPosition();}

    private void ApplyStationPosition()
    {
        Vector3 wanted=boat.TransformPoint(feet)+Vector3.up*footOffset;
        if((robot.transform.position-wanted).sqrMagnitude>.000001f){
            CharacterController controller=Controller();bool enabled=controller!=null&&controller.enabled;
            if(enabled)controller.enabled=false;
            robot.transform.position=wanted;
            if(enabled)controller.enabled=true;
        }
        FirstPersonMover mover=robot as FirstPersonMover;if(mover!=null&&SetVelocity!=null)SetVelocity(mover,Vector3.zero);
    }
    private float unsupportedFallSpeed;
    private int stableFootingRevision=-1;
    private Vector3 stableFootingPoint,stableFootingUp;

    private bool HasStableFooting()
    {
        if(stableFootingRevision==hull.Revision&&
            (stableFootingPoint-feet).sqrMagnitude<.000001f&&
            Vector3.Dot(stableFootingUp,boat.up)>.9994f)return true;
        bool stable;
        bool supported=NavalPhysicsQueries.HasLocalSupport(hull,feet,out stable);
        if(stable)
        {
            stableFootingRevision=hull.Revision;
            stableFootingPoint=feet;
            stableFootingUp=boat.up;
        }
        else stableFootingRevision=-1;
        return supported;
    }
    private void ApplyUnsupportedFall(float dt){
        using(NavalPerformance.Measure(NavalPerfArea.CrewFooting))
        if(movingInWater||HasStableFooting()){unsupportedFallSpeed=0;return;}
        var controller=Controller();if(controller==null||!controller.enabled)return;
        unsupportedFallSpeed=Mathf.Min(12f,unsupportedFallSpeed+9.81f*dt);
        NavalCrewPassage.Prepare(controller);
        movementContacts=controller.Move(Vector3.down*unsupportedFallSpeed*dt);
        feet=boat.InverseTransformPoint(robot.transform.position-Vector3.up*footOffset);
    }
    private void Move(Vector3 destination,float dt,bool finalPoint)
    {using(NavalPerformance.Measure(NavalPerfArea.CrewMotion))MoveWithController(destination,dt,finalPoint);}

    private void MoveWithController(Vector3 destination,float dt,bool finalPoint)
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
        bool hopping=hopTime>0f;
        if(hopping)step.y=HopStep(dt);
        bool checkSupport=!hopping&&!movingInWater&&step.y>0f;
        bool checkGap=!stairs&&hopTime<=0f&&step.sqrMagnitude>.000001f;
        Vector3 localStep=checkSupport||checkGap?boat.InverseTransformVector(step):Vector3.zero;
        if(checkSupport&&!NavalPhysicsQueries.HasLocalSupport(hull,feet+localStep))
        {
            step.y=0f;
            if(checkGap)localStep=boat.InverseTransformVector(step);
        }
        if(checkGap&&step.sqrMagnitude>.000001f)
        {
            Vector3 proposed=feet+localStep;
            bool currentGap,proposedGap;
            hull.DeckGapsAlongStep(feet,proposed,NavigationRadius,out currentGap,out proposedGap);
            if(!currentGap)deckGapEscape=false;
            else if(!deckGapEscape&&Time.time>=deckGapRetryAt)
            {
                deckGapRetryAt=Time.time+.75f;
                if(RecoverDeckGap(destination)){
                    // Execute the newly inserted safe endpoint on the next frame.
                    HoldPosition();return;
                }
            }
            if(NavalDeckGapMath.BlocksStep(currentGap,proposedGap))
            {
                // Do not let the native controller bridge or fall through a
                // destroyed plank. The bounded grid recovery inserts a safe
                // same-deck detour around the newly opened gap.
                moveSpeed=0f;RecoverObstruction(destination);HoldPosition();ApplyUnsupportedFall(dt);return;
            }
        }
        CharacterController controller=Controller();DeckStepHeight(controller);
        if(controller!=null&&controller.enabled)
        {
            NavalCrewPassage.Prepare(controller);
            movementContacts=controller.Move(step+Vector3.down*.3f*dt);
        }
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
