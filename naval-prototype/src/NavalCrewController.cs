using UnityEngine;

public class NavalCrewController
{
    private NavalHull hull;
    private NavalStations stations;
    private Character[] crew;
    private NavalRepairSailor[] workers;
    private int[] roles,assigned;
    private NavalBoarder[] boarders;
    private NavalDeckDefense defense;
    private int emergencyGunner=-1;
    public int MannedGuns {get;private set;}
    public bool Repairing {get;private set;}
    public bool NeedsRetreat {get{return Repairing||hull.RepairRetreat||hull.Bilge!=null&&!hull.Bilge.Functional;}}
    public bool HasHelmsman {get;private set;}
    public string Role(int i)
    {return workers[i]!=null&&workers[i].Rescuing?workers[i].RescueStatus:boarders[i]!=null&&boarders[i].Busy?boarders[i].Status:defense!=null&&defense.Guard==i?"Defending deck":workers[i]!=null&&workers[i].Busy?workers[i].Status:roles[i]==0?"Helmsman":roles[i]>0?"Gunner "+roles[i]:"Standby";}
    public void Initialize(NavalHull ship,NavalStations controls,Character[] sailors,NavalRepairSailor[] repair)
    {
        hull=ship;stations=controls;crew=sailors;workers=repair;
        roles=new int[crew.Length];assigned=new int[crew.Length];boarders=new NavalBoarder[crew.Length];
        for(int i=0;i<roles.Length;i++)roles[i]=-1;
        defense=new NavalDeckDefense(hull,crew,workers);
    }
    private bool Free(int i)
    {return i>=0&&i<crew.Length&&crew[i]!=null&&crew[i].IsAlive()&&workers[i]!=null&&!workers[i].RepairDuty&&!workers[i].Stranded&&!workers[i].Defending&&!workers[i].Rescuing&&(boarders[i]==null||!boarders[i].Busy);}
    public void Tick(float dt,Transform enemy,Vector3 enemyVelocity,bool helmAllowed,float feetOffset)
    {
        using(NavalPerformance.Measure(NavalPerfArea.Crew))
        {
        for(int i=0;i<crew.Length;i++)if(crew[i]!=null){NavalSailorRescue rescue=crew[i].GetComponent<NavalSailorRescue>();if(rescue!=null)rescue.CheckForWater();}
        HasHelmsman=false;MannedGuns=0;Repairing=false;defense.Tick(roles,helmAllowed);if(hull.Sunk)return;
        for(int i=0;i<crew.Length;i++)if(crew[i]!=null&&workers[i]!=null)
        {
            if(boarders[i]==null){boarders[i]=crew[i].gameObject.AddComponent<NavalBoarder>();boarders[i].Initialize(crew[i],workers[i],hull);}
        }
        if(emergencyGunner>=0&&(crew[emergencyGunner]==null||!crew[emergencyGunner].IsAlive()))emergencyGunner=-1;
        // Recall safely once critical danger clears; finish any already-loaded bucket.
        if(!hull.CombatCritical&&emergencyGunner>=0&&enemy!=null)
        {
            int sideToUse=stations.CombatSideToward(enemy.position),gun=-1;
            if(sideToUse>0)for(int station=sideToUse;station<=stations.GunCount;station+=2)
                if(stations.IsFunctional(station)&&stations.PlayerStation!=station){gun=station;break;}
            if(gun>0&&workers[emergencyGunner]!=null&&workers[emergencyGunner].ReturnToGun(stations.Post(gun)))
            {roles[emergencyGunner]=gun;emergencyGunner=-1;}
            else if(stations.AllGunsBroken)emergencyGunner=-1;
        }
        for(int i=0;i<assigned.Length;i++)assigned[i]=-1;
        // Reserve a helmsman before gun allocation. With a two-person crew,
        // assigning guns first would pull the last free helmsman into maintenance.
        int availableCrew=0;
        for(int i=0;i<crew.Length;i++)if(crew[i]!=null&&crew[i].IsAlive()&&workers[i]!=null&&!workers[i].Stranded&&!workers[i].Defending&&!workers[i].Rescuing&&!workers[i].Assaulting)availableCrew++;
        bool maintenanceNeeded=hull.HasRepairDemand||hull.Volume>.12f;
        int helm=-1;
        if(helmAllowed&&stations.PlayerStation!=0&&stations.IsFunctional(0)&&NavalCrewPriorities.ReserveHelm(availableCrew,hull.CombatCritical,maintenanceNeeded))
        {
            for(int i=0;i<crew.Length;i++)if(Free(i)&&assigned[i]<0&&roles[i]==0){helm=i;break;}
            if(helm<0)for(int i=0;i<crew.Length;i++)if(Free(i)&&assigned[i]<0){helm=i;break;}
            if(helm>=0)assigned[helm]=0;
        }
        int side=enemy!=null?stations.CombatSideToward(enemy.position):-1;
        if(side>0)for(int station=side;station<=stations.GunCount;station+=2)
        {
            if(!stations.IsFunctional(station)||stations.PlayerStation==station)continue;
            int selected=-1;
            for(int i=0;i<crew.Length;i++)if(Free(i)&&assigned[i]<0&&roles[i]==station){selected=i;break;}
            if(selected<0)for(int i=0;i<crew.Length;i++)if(Free(i)&&assigned[i]<0){selected=i;break;}
            if(selected>=0)assigned[selected]=station;
        }
        if(NavalCrewPriorities.CanBoard(stations.AllGunsBroken,hull.HasRepairDemand,
            hull.Bilge!=null&&(hull.Bilge.WantsOperator||!hull.Bilge.Functional),hull.RepairRetreat))
            for(int i=0;i<crew.Length;i++)if(Free(i)&&assigned[i]<0)boarders[i].TryStart(enemy);
        bool repairAssigned=false,bailAssigned=false,pumpRepairAssigned=false,helmRepairAssigned=false,maintenanceReserved=false;
        for(int i=0;i<workers.Length;i++)if(crew[i]!=null&&crew[i].IsAlive()&&workers[i]!=null)
        {if(workers[i].RepairDuty)maintenanceReserved=true;if(workers[i].RepairingLeak)repairAssigned=true;if(workers[i].RepairingPump)pumpRepairAssigned=true;if(workers[i].RepairingHelm)helmRepairAssigned=true;if(workers[i].Bailing)bailAssigned=true;}
        bool pumpNeeded=hull.Bilge!=null&&hull.Bilge.WantsOperator&&!hull.Bilge.PlayerUsing;
        bool pumpAssigned=hull.Bilge!=null&&hull.Bilge.Operator!=null;
        int needed=NavalMaintenanceMath.FreeWorkersNeeded(hull.HasRepairDemand,repairAssigned,pumpNeeded,pumpAssigned,hull.CombatCritical,bailAssigned,hull.Bilge!=null&&hull.Bilge.NeedsRepair,pumpRepairAssigned,hull.ParallelMaintenance),spare=0;
        if(!hull.ParallelMaintenance&&maintenanceReserved)needed=0;
        if(stations.HelmNeedsRepair&&!helmRepairAssigned&&(hull.ParallelMaintenance||!maintenanceReserved))
        {if(hull.ParallelMaintenance)needed++;else if(!repairAssigned&&!pumpAssigned&&!pumpRepairAssigned&&!bailAssigned)needed=Mathf.Max(needed,1);}
        for(int i=0;i<crew.Length;i++)if(Free(i)&&assigned[i]<0)spare++;
        int assignedGuns=0;
        foreach(int station in assigned)if(station>0)assignedGuns++;
        // Extra gunners can help normally; the final gunner only leaves in a critical emergency.
        for(int i=crew.Length-1;i>=0&&spare<needed;i--)
            if(Free(i)&&assigned[i]>0&&NavalCombatEmergency.ReleaseGunner(assignedGuns,hull.CombatCritical))
            {
                if(assignedGuns==1)emergencyGunner=i;
                assigned[i]=-1;spare++;assignedGuns--;
            }
        for(int i=0;i<crew.Length;i++)if(workers[i]!=null)workers[i].Tick(dt,Free(i)&&assigned[i]<0);
        foreach(NavalRepairSailor worker in workers)if(worker!=null&&worker.MaintenanceDuty)Repairing=true;
        for(int i=0;i<crew.Length;i++)
        {
            if(!Free(i))continue;
            roles[i]=assigned[i];
            Vector3 post=roles[i]>=0?stations.Post(roles[i]):new Vector3(i%2==0?-1.8f:1.8f,.825f,4.9f-i/2*.9f);
            workers[i].SetPost(post);if(workers[i].Busy)continue;
            Character sailor=crew[i];sailor.DisableAI();
            CharacterController controller=sailor.GetComponentInChildren<CharacterController>();bool enabled=controller!=null&&controller.enabled;
            if(enabled)controller.enabled=false;
            sailor.transform.position=hull.Root.TransformPoint(post)+hull.Root.up*workers[i].FootOffset;
            Vector3 facing=roles[i]>=0?stations.CrewForward(roles[i]):hull.Root.forward;facing.y=0f;
            if(facing.sqrMagnitude>.01f)sailor.transform.rotation=Quaternion.LookRotation(facing);
            if(enabled)controller.enabled=true;
            if(roles[i]==0)HasHelmsman=true;
            if(roles[i]>0)MannedGuns++;
            if(roles[i]>0&&enemy!=null)stations.AimAtShipAndFire(roles[i]-1,enemy,enemyVelocity,dt,sailor);
        }
            }
    }
}
