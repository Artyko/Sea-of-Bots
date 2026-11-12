using UnityEngine;

public partial class NavalCrewController
{
    private NavalHull hull;
    private NavalStations stations;
    private Character[] crew;
    private NavalRepairSailor[] workers;
    private int[] roles,assigned;
    private NavalBoarder[] boarders;
    private NavalDeckDefense defense;
    private NavalCrewPassage passage;
    private Transform cachedTarget;
    private NavalRamDamage cachedTargetDamage;
    private int emergencyGunner=-1;
    private int mainBatteryHandsReleased;
    private readonly NavalCrewReleaseLatch mainRelease=new NavalCrewReleaseLatch();
    private int loggedMainBatteryHandsReleased=-1;
    private int[] mainBatteryCrew={-1,-1};
    private readonly NavalSkeletonCrewPolicy skeleton=new NavalSkeletonCrewPolicy();
    private int reliefHelmsman=-1;
    private readonly NavalCannonBattlePolicy cannonBattle=new NavalCannonBattlePolicy();
    public int MannedGuns {get;private set;}
    public bool Repairing {get;private set;}
    public bool NeedsRetreat {get{return Repairing||hull.RepairRetreat||hull.Bilge!=null&&!hull.Bilge.Functional;}}
    private bool helmReady;
    public bool HasHelmsman {get{
        if(!helmReady)return false;
        for(int i=0;i<workers.Length;i++)if(roles[i]==0&&crew[i]!=null&&crew[i].IsAlive()&&workers[i]!=null&&
            !workers[i].Busy&&workers[i].AtStation(stations.Post(0)))return true;
        return false;
    }private set{helmReady=value;}}
    public bool RecoveringOverboard {get;private set;}
    private int MainBatteryCrewSlot(int i)
    {
        if(hull==null||!hull.Definition.SteamPowered||crew.Length<2)return -1;
        for(int slot=0;slot<mainBatteryCrew.Length;slot++)if(mainBatteryCrew[slot]==i)
        {if(slot>=mainBatteryCrew.Length-mainBatteryHandsReleased||!stations.MainBatterySlotFunctional(slot))return -1;return slot;}
        return -1;
    }
    private bool DedicatedMainBatteryCrew(int i)
    {
        for(int slot=0;slot<mainBatteryCrew.Length;slot++)if(mainBatteryCrew[slot]==i)
            return stations==null||stations.MainBatterySlotFunctional(slot);
        return false;
    }
    private void RefreshMainBatteryCrew()
    {
        if(hull==null||!hull.Definition.SteamPowered)return;
        for(int slot=0;slot<mainBatteryCrew.Length;slot++)
        {
            int i=mainBatteryCrew[slot];
            if(i<0||i>=crew.Length||crew[i]==null||!crew[i].IsAlive()||workers[i]==null)mainBatteryCrew[slot]=-1;
        }
        // Do not conscript a replacement into an already disabled mounting.
        // The surviving crew and deck hands repair it first; after restoration
        // the closest idle relief replaces any casualty.
        if(stations!=null&&stations.HasMainBattery&&!NavalMainBatteryCrewPolicy.MansMount(stations.MainBatteryFunctional))return;
        for(int slot=0;slot<mainBatteryCrew.Length;slot++)if(mainBatteryCrew[slot]<0)
        {
            if(!stations.MainBatterySlotFunctional(slot))continue;
            int selected=-1;float best=float.PositiveInfinity;
            Vector3 approach=stations.MainBatteryCrewApproachPost(slot);
            for(int pass=0;pass<2&&selected<0;pass++)for(int i=crew.Length-1;i>=0;i--)
            {
                if(crew[i]==null||!crew[i].IsAlive()||workers[i]==null||workers[i].MaintenanceDuty||workers[i].Stranded||workers[i].Defending||workers[i].Rescuing||workers[i].Assaulting)continue;
                if(workers[i].DeliveringAmmo||workers[i].HandlingSails||workers[i].CapitalReserved)continue;
                if(roles!=null&&i<roles.Length&&roles[i]==0)continue;
                if(pass==0&&workers[i].Busy)continue;
                bool taken=false;foreach(int tracked in mainBatteryCrew)if(tracked==i)taken=true;if(taken)continue;
                float distance=(workers[i].CarriedPosition-approach).sqrMagnitude;
                if(roles!=null&&i<roles.Length&&roles[i]==0)distance+=25f;
                if(distance<best){best=distance;selected=i;}
            }
            mainBatteryCrew[slot]=selected;
            if(selected>=0)Debug.Log("[NavalPrototype] Main battery relief assigned: ship="+hull.Root.name+" slot="+slot+" sailor="+crew[selected].GetInstanceID());
        }
    }
    public string Role(int i)
    {if(roles[i]>=400&&roles[i]<404)return "Зенитчик "+(roles[i]-399)+(workers[i].Busy?": "+workers[i].Status:"");if(roles[i]>=300&&roles[i]<302)return "Машинист"+(workers[i].Busy?": "+workers[i].Status:"");if(roles[i]>=200&&roles[i]<204)return "Торпедист "+(roles[i]-199)+(workers[i].Busy?": "+workers[i].Status:"");int main=MainBatteryCrewSlot(i);return main>=0?(hull.Definition.CapitalShip?"Turret "+NavalCapitalLayout.Name(main)+": "+workers[i].CapitalAccessStatus:main==0?"Main battery: breech":"Main battery: trainer"):workers[i]!=null&&workers[i].Rescuing?workers[i].RescueStatus:boarders[i]!=null&&boarders[i].Busy?boarders[i].Status:defense!=null&&defense.Guard==i?"Defending deck":workers[i]!=null&&workers[i].DeliveringAmmo?(workers[i].CarriedAmmo>0?(hull.Definition.RifledWeapons?"Carrying shells":"Carrying cannonballs"):(hull.Definition.RifledWeapons?"Going to shell stock":"Going to cannonball stock")):workers[i]!=null&&workers[i].Busy?workers[i].Status:reliefHelmsman==i?"Helmsman: relief gunner":roles[i]==0?(workers[i].FinalAttackDuty?"Last attack: ramming":"Helmsman"):roles[i]>0&&roles[i]<=stations.GunCount?"Gunner "+roles[i]:workers[i]!=null&&workers[i].FinalAttackDuty?"Last attack: boarding":"Standby";}
    public void Initialize(NavalHull ship,NavalStations controls,Character[] sailors,NavalRepairSailor[] repair)
    {
        hull=ship;hull.CrewController=this;stations=controls;crew=sailors;workers=repair;
        hull.Root.gameObject.AddComponent<NavalCrewAccessAudit>().Initialize(hull,stations);
        roles=new int[crew.Length];assigned=new int[crew.Length];boarders=new NavalBoarder[crew.Length];
        for(int i=0;i<roles.Length;i++)roles[i]=-1;
        mainBatteryCrew=new int[hull.Definition.HeavyCrewCount];for(int slot=0;slot<mainBatteryCrew.Length;slot++)mainBatteryCrew[slot]=crew.Length-mainBatteryCrew.Length+slot;
        defense=new NavalDeckDefense(hull,crew,workers);
        passage=hull.Root.gameObject.AddComponent<NavalCrewPassage>();
        passage.Initialize(hull,crew,workers);
    }
    public bool ReceiveSurvivor(NavalHull origin,Character sailor,NavalRepairSailor worker,Transform parent,
        out Character[] sailors,out NavalRepairSailor[] repairers)
    {
        sailors=crew;repairers=workers;
        var old=origin!=null?origin.CrewController:null;
        var frame=hull.Root.GetComponent<NavalStructure>();
        if(old==null||old==this||hull.Sunk||frame==null)return false;
        int oldSlot=System.Array.IndexOf(old.crew,sailor);
        if(oldSlot<0)return false;
        old.passage.RemoveSailor(oldSlot);
        old.crew[oldSlot]=null;
        old.workers[oldSlot]=null;
        old.roles[oldSlot]=old.assigned[oldSlot]=-1;
        old.boarders[oldSlot]=null;

        int slot=crew.Length;
        System.Array.Resize(ref crew,slot+1);
        System.Array.Resize(ref workers,slot+1);
        System.Array.Resize(ref roles,slot+1);
        System.Array.Resize(ref assigned,slot+1);
        System.Array.Resize(ref boarders,slot+1);
        crew[slot]=sailor;workers[slot]=worker;
        roles[slot]=assigned[slot]=-1;
        boarders[slot]=sailor.GetComponent<NavalBoarder>();
        if(boarders[slot]!=null)boarders[slot].JoinCrew(hull);
        sailor.transform.SetParent(parent,true);
        worker.JoinCrew(hull,frame);
        defense.SetRoster(crew,workers);
        passage.ResizeRoster(crew,workers);
        sailors=crew;repairers=workers;
        Debug.Log("[NavalPrototype] Survivor joined crew: ship="+hull.Root.name+" sailor="+sailor.GetInstanceID());
        return true;
    }

    private bool Free(int i)
    {return i>=0&&i<crew.Length&&MainBatteryCrewSlot(i)<0&&crew[i]!=null&&crew[i].IsAlive()&&workers[i]!=null&&!workers[i].CapitalReserved&&(!workers[i].RepairDuty||workers[i].ReturningWithoutWork)&&!workers[i].Stranded&&!workers[i].Defending&&!workers[i].Rescuing&&(boarders[i]==null||!boarders[i].Busy);}
    public void Tick(float dt,Transform enemy,Vector3 enemyVelocity,bool helmAllowed,float feetOffset)
    {
        using(NavalPerformance.Measure(NavalPerfArea.Crew))
        {
        using(NavalPerformance.Measure(NavalPerfArea.CrewCarry))
        {
            Matrix4x4 toLocal=hull.Root.worldToLocalMatrix;
            for(int i=0;i<workers.Length;i++)if(workers[i]!=null)
            {workers[i].StationDutyProtected=false;workers[i].CarrySupportedDeck(toLocal);}
        }
        using(NavalPerformance.Measure(NavalPerfArea.CrewRescue))
        for(int i=0;i<crew.Length;i++)if(crew[i]!=null){NavalSailorRescue rescue=workers[i]!=null?workers[i].RescueComponent:crew[i].GetComponent<NavalSailorRescue>();if(rescue!=null)rescue.CheckForWater();}
        RecoveringOverboard=false;
        for(int i=0;i<workers.Length;i++)if(workers[i]!=null&&workers[i].RescueComponent!=null&&workers[i].RescueComponent.AwaitingHomeRescue)RecoveringOverboard=true;
        RecoveringOverboard|=hull.RescuePickupUntil>Time.time;
        // Overboard recovery must never override helm or propulsion orders.
        stations.RescueStop=false;
        HasHelmsman=false;MannedGuns=0;Repairing=false;if(hull.Sunk){if(hull.Definition.CapitalShip){foreach(var worker in workers)if(worker!=null&&worker.CapitalReserved)worker.TickCapitalExit(stations,dt);stations.UpdateCapitalAccess(dt);}cannonBattle.Reset();stations.CrewRepairRetreat=false;return;}
        for(int i=0;i<crew.Length;i++)if(crew[i]!=null&&workers[i]!=null)
        {
            if(boarders[i]==null){boarders[i]=crew[i].gameObject.AddComponent<NavalBoarder>();boarders[i].Initialize(crew[i],workers[i],hull);}
        }
        if(enemy==null){cachedTarget=null;cachedTargetDamage=null;}
        else if(cachedTarget!=enemy||cachedTargetDamage==null){cachedTarget=enemy;cachedTargetDamage=enemy.GetComponent<NavalRamDamage>();}
        NavalRamDamage targetShip=cachedTargetDamage;

        if(targetShip!=null&&targetShip.Hull.Sunk)
        {enemy=null;targetShip=null;}
        bool combatThreat=enemy!=null;
        int observedFiringSide=combatThreat?stations.FiringSideAtShip(enemy):0;
        bool fireOpportunity=observedFiringSide>0;
        if(hull.Bilge!=null)hull.Bilge.UpdateCrewDemand(dt);
        cannonBattle.ObserveFire(dt,fireOpportunity);
        bool survivalFire=hull.Fire!=null&&hull.Fire.HasFire;
        bool pumpDemand=hull.Bilge!=null&&hull.Bilge.WantsOperator;
        hull.CrewCannonRepairAllowed=NavalMaintenancePriority.AllowCannonRepair(cannonBattle.AllowRepairs,
            survivalFire,hull.HasRepairDemand,pumpDemand,hull.CombatCritical,hull.UrgentTrim,
            hull.Bilge!=null&&hull.Bilge.NeedsRepair);
        // Spare hands can refurbish disabled guns while another sailor pumps.
        // A global pump/quiet flag must not leave these hands idle indefinitely.
        bool spareRepairHand=false;
        for(int i=0;i<workers.Length;i++)if(workers[i]!=null&&(workers[i].RepairingCannon||Free(i)&&roles[i]!=0))spareRepairHand=true;
        bool pumpCovered=!pumpDemand||hull.Bilge!=null&&hull.Bilge.Operator!=null;
        if(spareRepairHand&&pumpCovered&&!survivalFire&&!hull.HasRepairDemand&&!hull.CombatCritical&&!hull.UrgentTrim)
            hull.CrewCannonRepairAllowed=true;
        if(!hull.CrewCannonRepairAllowed)foreach(var worker in workers)if(worker!=null)worker.RecallCannonForCombat();
        // A finished fire/patch job becomes a combat candidate in this allocation pass.
        // Otherwise nextTaskPending hides the sailor from Free() for another dispatch cycle.
        foreach(var worker in workers)if(worker!=null)worker.OfferCombatHandoff(fireOpportunity,
            survivalFire,hull.HasRepairDemand,pumpDemand,hull.CombatCritical||hull.UrgentTrim);
        bool lastAttack=!stations.RescueStop&&stations.MustBoard&&targetShip!=null&&!targetShip.Hull.Sunk;
        NavalRamDamage ownRam=hull.Root.GetComponent<NavalRamDamage>();
        bool boarding=lastAttack&&ownRam!=null&&ownRam.BoardingContactWith(targetShip);
        stations.LastAttackApproach=lastAttack&&!boarding;
        for(int i=0;i<workers.Length;i++)if(workers[i]!=null)workers[i].SetFinalAttack(boarding);
        if(lastAttack){cannonBattle.Reset();stations.CrewRepairRetreat=false;skeleton.Reset();reliefHelmsman=-1;defense.Stop();FinalAttackTick(dt,enemy,helmAllowed,boarding);return;}
        using(NavalPerformance.Measure(NavalPerfArea.CrewDispatch))
        {
        bool urgentPump=hull.Bilge!=null&&(hull.UrgentTrim||hull.Bilge.BoilerEmergency);
        bool fireUrgent=hull.Fire!=null&&hull.Fire.Urgent;
        bool fireActive=hull.Fire!=null&&hull.Fire.HasFire;
        NavalSteamPlant steamPlant=hull.Definition.SteamPowered?hull.Root.GetComponent<NavalSteamPlant>():null;
        bool liveSteam=steamPlant!=null&&steamPlant.HasLiveSteamLeak;
        int livingGeneralCrew=0;
        for(int i=0;i<crew.Length;i++)if(!DedicatedMainBatteryCrew(i)&&crew[i]!=null&&crew[i].IsAlive()&&workers[i]!=null&&
            !workers[i].Stranded&&!workers[i].Rescuing&&!workers[i].Assaulting)livingGeneralCrew++;
        int emergencyDemand=NavalMainBatteryCrewPolicy.EmergencyDemand(hull.Fire!=null&&hull.Fire.CriticalFire,
            hull.CombatCritical&&urgentPump,liveSteam,urgentPump,fireUrgent);
        bool releasedBusy=false;
        for(int slot=0;slot<mainBatteryCrew.Length;slot++){
            int i=mainBatteryCrew[slot];bool released=slot>=mainBatteryCrew.Length-mainBatteryHandsReleased;
            if(released&&i>=0&&i<workers.Length&&crew[i]!=null&&crew[i].IsAlive()&&workers[i]!=null&&
                workers[i].Busy&&!workers[i].ReturningWithoutWork)releasedBusy=true;
        }
        mainBatteryHandsReleased=mainRelease.Step(dt,NavalMainBatteryCrewPolicy.ReleasedHands(emergencyDemand,livingGeneralCrew),releasedBusy);
        stations.MainBatteryReleasedHands=mainBatteryHandsReleased;
        if(loggedMainBatteryHandsReleased!=mainBatteryHandsReleased)
        {
            loggedMainBatteryHandsReleased=mainBatteryHandsReleased;
            Debug.Log("[NavalPrototype] Main battery crew emergency policy: ship="+hull.Root.name+" demand="+emergencyDemand+" general="+livingGeneralCrew+" released="+mainBatteryHandsReleased);
        }
        // Released hands no longer have a MainBatteryCrewSlot, therefore the
        // dedicated branch below cannot clear their seated animation itself.
        for(int i=0;i<workers.Length;i++)if(workers[i]!=null&&MainBatteryCrewSlot(i)<0)workers[i].SetMainBatterySeated(false);
        RefreshMainBatteryCrew();
        using(NavalPerformance.Measure(NavalPerfArea.CrewDefense))defense.Tick(roles,helmAllowed);
        if(emergencyGunner>=0&&(crew[emergencyGunner]==null||!crew[emergencyGunner].IsAlive()))emergencyGunner=-1;
        // Recall safely once critical danger clears; finish any already-loaded bucket.
        if(!hull.CombatCritical&&!urgentPump&&!fireActive&&emergencyGunner>=0&&enemy!=null)
        {
            int sideToUse=(observedFiringSide!=0?observedFiringSide:stations.CombatSideToward(enemy.position)),gun=-1;
            if(sideToUse>0)for(int station=sideToUse;station<=stations.GunCount;station+=2)
                if(stations.IsFunctional(station)&&stations.PlayerStation!=station){gun=station;break;}
            if(gun>0&&workers[emergencyGunner]!=null&&workers[emergencyGunner].ReturnToGun(stations.Post(gun)))
            {roles[emergencyGunner]=gun;emergencyGunner=-1;}
            else if(stations.AllGunsBroken)emergencyGunner=-1;
        }
        for(int i=0;i<assigned.Length;i++)assigned[i]=-1;
        // The two extra steam-era sailors form a dedicated heavy-gun crew.
        // Positive sentinels reserve them from helm, pump, fire and ammunition
        // dispatch without pretending that the main gun is a broadside station.
        if(hull.Definition.SteamPowered&&assigned.Length>=2)for(int i=0;i<assigned.Length;i++)
        {int slot=MainBatteryCrewSlot(i);if(slot>=0)assigned[i]=100+slot;}
        // Reserve a helmsman before gun allocation. With a two-person crew,
        // assigning guns first would pull the last free helmsman into maintenance.
        int availableCrew=0;
        for(int i=0;i<crew.Length;i++)if(crew[i]!=null&&crew[i].IsAlive()&&workers[i]!=null&&!workers[i].Stranded&&!workers[i].Defending&&!workers[i].Rescuing&&!workers[i].Assaulting)availableCrew++;
        bool maintenanceNeeded=hull.HasRepairDemand||hull.Volume>.12f||fireActive;
        bool soloMaintenance=NavalSkeletonCrewPolicy.SoloMaintenance(availableCrew,fireActive||hull.HasRepairDemand||
            hull.Bilge!=null&&(hull.Bilge.WantsOperator||hull.Bilge.NeedsRepair)||stations.HelmNeedsRepair||hull.CombatCritical&&hull.Volume>.12f);
        bool fireCovered=false;foreach(var worker in workers)if(worker!=null&&worker.FightingFire)fireCovered=true;
        int handsBeforeHelm=0;for(int i=0;i<crew.Length;i++)if(Free(i))handsBeforeHelm++;
        bool yieldForFire=NavalFireResponseMath.YieldHelm(fireUrgent,fireCovered,handsBeforeHelm);
        int helm=-1;
        if(!yieldForFire&&!soloMaintenance&&helmAllowed&&stations.PlayerStation!=0&&stations.IsFunctional(0)&&NavalCrewPriorities.ReserveHelm(availableCrew,hull.CombatCritical||urgentPump,maintenanceNeeded))
        {
            for(int i=0;i<crew.Length;i++)if(Free(i)&&assigned[i]<0&&roles[i]==0){helm=i;break;}
            if(helm<0)for(int i=0;i<crew.Length;i++)if(Free(i)&&assigned[i]<0){helm=i;break;}
            if(helm>=0)assigned[helm]=0;
        }
        AssignPlantHands(hull.CombatCritical||hull.Fire!=null&&hull.Fire.CriticalFire,fireUrgent||urgentPump);
        AssignTorpedoHands(hull.CombatCritical||hull.UrgentTrim||hull.Fire!=null&&hull.Fire.CriticalFire,enemy);
        AssignAirDefense(hull.CombatCritical||fireUrgent||urgentPump);
        for(int i=0;i<workers.Length;i++)if(workers[i]!=null)workers[i].StationDutyProtected=
            assigned[i]>=200&&assigned[i]<204||assigned[i]>=400&&assigned[i]<404;
        int firingSide=combatThreat?observedFiringSide:(enemy!=null?stations.FiringSideAtShip(enemy):0);
        int side=enemy!=null?(firingSide!=0?firingSide:stations.CombatSideToward(enemy.position)):-1;
        uint bearingKnown=0,bearingMask=0;
        if(!soloMaintenance&&side>0)for(int pass=0;pass<2;pass++)for(int station=hull.Definition.Destroyer?1:side;station<=stations.GunCount;station+=hull.Definition.Destroyer?1:2)
        {
            if(!stations.CrewGunAssignable(station))continue;
            uint bit=1u<<(station-1);
            if((bearingKnown&bit)==0)
            {
                bearingKnown|=bit;
                if(enemy!=null&&stations.CanEngageAnyShip(station,enemy))bearingMask|=bit;
            }
            bool canFire=(bearingMask&bit)!=0;
            if((pass==0)!=canFire)continue;
            int selected=-1;
            for(int i=0;i<crew.Length;i++)if(Free(i)&&assigned[i]<0&&roles[i]==station){selected=i;break;}
            if(selected<0)
            {
                float nearest=float.PositiveInfinity;Vector3 gunPost=stations.CrewPosition(station);
                for(int i=0;i<crew.Length;i++)if(Free(i)&&assigned[i]<0)
                {float distance=(crew[i].transform.position-gunPost).sqrMagnitude;if(distance<nearest){nearest=distance;selected=i;}}
            }
            if(selected>=0)assigned[selected]=station;
        }
        int freeHands=0;for(int i=0;i<crew.Length;i++)if(Free(i))freeHands++;
        int reliefGun=skeleton.GunStation;
        if(reliefGun<=0&&enemy!=null)
            for(int station=1;station<=stations.GunCount;station++)
                if(stations.CrewGunReady(station)&&stations.CanEngageAnyShip(station,enemy)){reliefGun=station;break;}
        bool reliefReady=reliefGun>0&&stations.CrewGunReady(reliefGun);
        bool reliefBearing=enemy!=null&&reliefGun>0&&stations.PlayerStation!=reliefGun&&stations.CanEngageAnyShip(reliefGun,enemy);
        int borrowed=skeleton.Step(dt,helm>=0&&freeHands==1&&enemy!=null&&!soloMaintenance&&!stations.RescueStop,
            helm>=0&&roles[helm]==0&&!workers[helm].Busy,reliefGun,reliefReady,reliefBearing,
            hull.CombatCritical||urgentPump||hull.RepairRetreat||stations.CrewRepairRetreat);
        reliefHelmsman=borrowed>0?helm:-1;
        if(borrowed>0)assigned[helm]=borrowed;
        if(!stations.RescueStop&&NavalCrewPriorities.CanBoard(stations.MustBoard,hull.HasRepairDemand,
            hull.Bilge!=null&&(hull.Bilge.WantsOperator||!hull.Bilge.Functional),hull.RepairRetreat))
            for(int i=0;i<crew.Length;i++)if(Free(i)&&assigned[i]<0)boarders[i].TryStart(enemy);
        bool repairAssigned=false,bailAssigned=false,pumpRepairAssigned=false,helmRepairAssigned=false,maintenanceReserved=false;
        for(int i=0;i<workers.Length;i++)if(crew[i]!=null&&crew[i].IsAlive()&&workers[i]!=null)
        // Propulsion handling and ammunition runs are jobs, but they must not
        // suppress an ordinary-fire responder. Only real maintenance reserves
        // the shared repair slot.
        {if(NavalFireResponseMath.ReservesMaintenance(workers[i].MaintenanceDuty,workers[i].HandlingSails))maintenanceReserved=true;if(workers[i].RepairingLeak)repairAssigned=true;if(workers[i].RepairingPump)pumpRepairAssigned=true;if(workers[i].RepairingHelm)helmRepairAssigned=true;if(workers[i].Bailing)bailAssigned=true;}
        bool pumpNeeded=hull.Bilge!=null&&hull.Bilge.WantsOperator&&!hull.Bilge.PlayerUsing;
        bool pumpAssigned=hull.Bilge!=null&&hull.Bilge.Operator!=null;
        int needed=NavalMaintenanceMath.FreeWorkersNeeded(hull.HasRepairDemand,repairAssigned,pumpNeeded,pumpAssigned,hull.CombatCritical,bailAssigned,hull.Bilge!=null&&hull.Bilge.NeedsRepair,pumpRepairAssigned,hull.ParallelMaintenance),spare=0;
        if(!hull.ParallelMaintenance&&maintenanceReserved)needed=0;
        bool fireAssigned=false;foreach(var worker in workers)if(worker!=null&&worker.FightingFire)fireAssigned=true;
        if(NavalFireResponseMath.NeedsResponder(fireActive,fireAssigned,maintenanceReserved,fireUrgent))needed=Mathf.Max(needed,1);
        if(urgentPump&&pumpNeeded&&!pumpAssigned)needed=Mathf.Max(needed,1);
        if(stations.HelmNeedsRepair&&!helmRepairAssigned&&(hull.ParallelMaintenance||!maintenanceReserved))
        {if(hull.ParallelMaintenance)needed++;else if(!repairAssigned&&!pumpAssigned&&!pumpRepairAssigned&&!bailAssigned)needed=Mathf.Max(needed,1);}
        for(int i=0;i<crew.Length;i++)if(Free(i)&&assigned[i]<0)spare++;
        int assignedGuns=0;
        foreach(int station in assigned)if(station>0&&station<=stations.GunCount)assignedGuns++;
        // Extra gunners can help normally; the final gunner only leaves in a critical emergency.
        for(int i=crew.Length-1;i>=0&&spare<needed;i--)
            if(Free(i)&&assigned[i]>0&&NavalCombatEmergency.ReleaseGunner(assignedGuns,hull.CombatCritical||urgentPump||fireUrgent))
            {
                if(assignedGuns==1)emergencyGunner=i;
                assigned[i]=-1;spare++;assignedGuns--;
            }
        CoordinateSupport();CoordinateSails();
        }
        for(int i=0;i<crew.Length;i++)if(workers[i]!=null)
        {
            if(TickPlantHand(i,dt)||TickTorpedoHand(i,dt,enemy,enemyVelocity)||TickAirDefenseHand(i,dt))continue;
            int mainSlot=MainBatteryCrewSlot(i);
            if(mainSlot>=0)
            {
                if(hull.Definition.CapitalShip){workers[i].TickCapitalStation(stations,mainSlot,dt);continue;}
                Vector3 mainPost=stations.MainBatteryCrewPost(mainSlot);
                Vector3 mainApproach=stations.MainBatteryCrewApproachPost(mainSlot);
                // As soon as the emergency that released this hand is over,
                // cancel its stale return/support state. v237 otherwise kept
                // both healthy operators away from the gun for the whole duel.
                workers[i].RecallToMainBattery(mainPost,mainApproach);
                workers[i].SpareMaintenance=false;workers[i].TrackMovingPostVia(mainPost,mainApproach);workers[i].Tick(dt,false);
                continue;
            }
            if(workers[i].CapitalReserved){workers[i].TickCapitalExit(stations,dt);continue;}
            // An unassigned hand costs no gun or helm station: allow useful parallel work.
            if(assigned[i]>=0){
                Vector3 assignedPost=stations.Post(assigned[i]);
                workers[i].AssignFreeCombatPost(assignedPost);
                // Retarget the existing helmsman before walking this tick, in both directions.
                if(hull.Definition.Destroyer&&assigned[i]>0&&roles[i]==assigned[i])
                    workers[i].FollowDestroyerGunPost(assignedPost);
                workers[i].SetPost(assignedPost);
            }
            workers[i].SpareMaintenance=assigned[i]<0&&!workers[i].Rescuing&&!workers[i].Assaulting&&!workers[i].Defending;
            workers[i].Tick(dt,Free(i)&&assigned[i]<0);
        }
        foreach(NavalRepairSailor worker in workers)if(worker!=null&&worker.MaintenanceDuty)Repairing=true;
        int mainCrewReady=0;stations.ClearHeavyCrewReadiness();
        for(int i=0;i<crew.Length;i++)
        {
            int mainSlot=MainBatteryCrewSlot(i);
            if(mainSlot>=0)
            {
                // Keep the public role outside the 1..GunCount broadside
                // namespace; defense and retreat accounting must not mistake
                // these two sailors for two additional casemate guns.
                roles[i]=-2-mainSlot;
                if(crew[i]==null||!crew[i].IsAlive()||workers[i]==null||(hull.Definition.CapitalShip?!workers[i].CapitalSeated:workers[i].Busy))continue;
                Vector3 mainPost=stations.MainBatteryCrewPost(mainSlot);
                if(!workers[i].AtMainBatteryPost(mainPost)){workers[i].TrackMovingPostVia(mainPost,stations.MainBatteryCrewApproachPost(mainSlot));continue;}
                crew[i].DisableAI();workers[i].AnchorAtPost(mainPost);
                workers[i].SetMainBatterySeated(true);
                Vector3 mainFacing=stations.HeavyCrewFacing(mainSlot);mainFacing.y=0;
                if(mainFacing.sqrMagnitude>.01f)crew[i].transform.rotation=Quaternion.LookRotation(mainFacing);
                stations.SetHeavyCrewReady(mainSlot);mainCrewReady++;if(hull.Definition.CapitalShip)MannedGuns++;continue;
            }
            if(assigned[i]>=200&&assigned[i]<204||assigned[i]>=300&&assigned[i]<302||assigned[i]>=400&&assigned[i]<404)continue;
            if(!Free(i))continue;
            roles[i]=assigned[i];
            Vector3 post=roles[i]>=0?stations.Post(roles[i]):workers[i].SafeStandby(NavalShipDefinition.StandbyPost(hull.Definition,i),i);
            workers[i].SetPost(post);if(workers[i].Busy)continue;
            if(!workers[i].AtStation(post)){workers[i].SetPost(post,true);continue;}
            Character sailor=crew[i];sailor.DisableAI();
            workers[i].AnchorAtPost(post);
            Vector3 facing=roles[i]>=0?stations.CrewForward(roles[i]):hull.Root.forward;facing.y=0f;
            if(facing.sqrMagnitude>.01f)sailor.transform.rotation=Quaternion.LookRotation(facing);
            if(roles[i]==0)HasHelmsman=true;
            if(roles[i]>0)MannedGuns++;
            if(roles[i]>0)stations.AimAtShipAndFire(roles[i]-1,enemy,enemyVelocity,dt,sailor);
        }
        stations.TickMainBattery(enemy,enemyVelocity,dt,mainCrewReady);
        int gunRepairs=0,plannedGuns=0,idleHands=0;
        for(int i=0;i<crew.Length;i++)if(crew[i]!=null&&crew[i].IsAlive()&&workers[i]!=null)
        {
            if(workers[i].RepairingCannon)gunRepairs++;
            if(assigned[i]>0&&assigned[i]<=stations.GunCount&&Free(i))plannedGuns++;
            else if(assigned[i]<0&&Free(i)&&workers[i].CanHandleSails)idleHands++;
        }
        bool playerGunner=stations.PlayerStation>0&&stations.PlayerStation<=stations.GunCount&&stations.IsFunctional(stations.PlayerStation);
        cannonBattle.ObserveCrew(dt,combatThreat,gunRepairs,plannedGuns+(playerGunner?1:0),MannedGuns+(playerGunner?1:0),idleHands);
        stations.CrewRepairRetreat=cannonBattle.Retreat;
            }
    }
    // The helmsman keeps headway until the other available sailors have departed.
    private void FinalAttackTick(float dt,Transform enemy,bool helmAllowed,bool boarding)
    {
        if(hull.Definition.CapitalShip){foreach(var worker in workers)if(worker!=null&&worker.CapitalReserved)worker.TickCapitalExit(stations,dt);stations.UpdateCapitalAccess(dt);}
        emergencyGunner=-1;int helm=-1;
        if(helmAllowed&&stations.PlayerStation!=0&&stations.IsFunctional(0))
        {
            for(int i=0;i<crew.Length;i++)if(Free(i)&&roles[i]==0){helm=i;break;}
            if(helm<0)for(int i=0;i<crew.Length;i++)if(Free(i)){helm=i;break;}
        }
        if(helm<0&&helmAllowed&&stations.PlayerStation!=0&&stations.IsFunctional(0))
        {
            // Reserve one hand to steer even when all surviving hands were repairing.
            for(int i=0;i<crew.Length;i++)if(crew[i]!=null&&crew[i].IsAlive()&&workers[i]!=null&&!workers[i].CapitalReserved&&!workers[i].Rescuing&&!workers[i].Stranded&&!workers[i].Defending&&!workers[i].Assaulting&&(boarders[i]==null||!boarders[i].Busy))
            {helm=i;workers[i].SetFinalAttack(true);workers[i].SetFinalAttack(boarding);break;}
        }
        for(int i=0;i<assigned.Length;i++)assigned[i]=i==helm?0:-1;
        if(!boarding){if(hull.Bilge!=null)hull.Bilge.UpdateCrewDemand(dt);CoordinateSupport();}
        if(!boarding)CoordinateSails(helm,true);
        bool ready=boarding;
        for(int i=0;i<crew.Length;i++)
        {
            if(i==helm||crew[i]==null||!crew[i].IsAlive()||workers[i]==null||workers[i].Rescuing||workers[i].Stranded)continue;
            if(workers[i].CapitalReserved){ready=false;continue;}
            roles[i]=-1;
            workers[i].SpareMaintenance=!boarding;
            if(!boarding)
            {
                if(!workers[i].MaintenanceDuty&&!workers[i].HandlingSails)workers[i].SetPost(workers[i].SafeStandby(NavalShipDefinition.StandbyPost(hull.Definition,i),i));
                continue;
            }
            if(workers[i].HandlingSails){ready=false;continue;}
            boarders[i].TryStart(enemy,true);
            if(!boarders[i].Departed)ready=false;
            if(!boarders[i].Busy)
            {
                workers[i].SetPost(workers[i].SafeStandby(NavalShipDefinition.StandbyPost(hull.Definition,i),i));
            }
        }
        if(helm>=0)
        {
            roles[helm]=0;workers[helm].SetPost(stations.Post(0));
            if(ready)boarders[helm].TryStart(enemy,true);
        }
        for(int i=0;i<workers.Length;i++)if(workers[i]!=null){workers[i].SpareMaintenance=!boarding&&i!=helm;workers[i].Tick(dt,!boarding&&i!=helm);if(workers[i].MaintenanceDuty)Repairing=true;}
        if(helm<0||boarders[helm].Busy||workers[helm].Busy)return;
        if(!workers[helm].AtStation(stations.Post(0))){workers[helm].SetPost(stations.Post(0),true);return;}
        Character sailor=crew[helm];sailor.DisableAI();
        workers[helm].AnchorAtPost(stations.Post(0));
        Vector3 facing=stations.CrewForward(0);facing.y=0f;
        if(facing.sqrMagnitude>.01f)sailor.transform.rotation=Quaternion.LookRotation(facing);
        HasHelmsman=true;
    }

}
