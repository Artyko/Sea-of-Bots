using System.Collections.Generic;
using UnityEngine;

public partial class NavalStations
{
    private sealed class CapitalMount
    {
        public Transform Root,Barrels,DoorHinge,Target;
        public NavalHullVoxel AimCell;
        public bool AccessRequested;public float DoorAngle,AutoDoorUntil,NextDoorScan;
        public NavalRepairableMechanism Mechanism;
        public float Yaw,Pitch,Reload,SecondShot;
        public int Ammo=80,Sequence;
        public bool Ready;public float NextStateLog;
        public int TraverseDirection;public float TraverseGoal,TraverseRetry,TraverseLog;
    }
    private CapitalMount[] capitalTurrets;
    public float EngagementPlanningSpeed {get{return capitalTurrets!=null&&MainBatteryCombatAvailable?Mathf.Sqrt(NavalCapitalLayout.Range*9.81f):ProjectileSpeed;}}
    private int HeavySideWorking(int side){if(capitalTurrets==null)return 0;int count=0;for(int i=0;i<capitalTurrets.Length;i++)if(MainBatterySlotFunctional(i)&&NavalCapitalLayout.Bears(i,side==1?-90f:90f))count++;return count;}
    private int CapitalAmmo {get{int count=0;foreach(var t in capitalTurrets)count+=t.Ammo;return count;}}
    private bool CapitalFunctional {get{for(int i=0;i<capitalTurrets.Length;i++)if(MainBatterySlotFunctional(i))return true;return false;}}
    private bool CapitalCanRepair {get{for(int i=0;i<capitalTurrets.Length;i++){var t=capitalTurrets[i];if(t.Ammo>0&&t.Mechanism.NeedsRepair&&t.Mechanism.RecoverableWithParts&&(hull.Fire==null||hull.Fire.MainBatteryMagazineFunctionalAt(i)))return true;}return false;}}
    private string CapitalDiagnostics {get{int ready=0,live=0;foreach(var t in capitalTurrets){if(t.Ready)ready++;if(t.Mechanism.Functional)live++;}return "calibre=305mm turrets="+live+"/5 crew="+ready+"/5 ammo="+CapitalAmmo;}}
    public bool MainBatterySlotFunctional(int slot){return capitalTurrets!=null?slot>=0&&slot<capitalTurrets.Length&&capitalTurrets[slot].Ammo>0&&capitalTurrets[slot].Mechanism.Functional&&(hull.Fire==null||hull.Fire.MainBatteryMagazineFunctionalAt(slot)):MainBatteryFunctional;}
    public void ClearHeavyCrewReadiness(){if(capitalTurrets!=null)foreach(var t in capitalTurrets)t.Ready=false;}
    public void SetHeavyCrewReady(int slot){if(capitalTurrets!=null&&slot>=0&&slot<capitalTurrets.Length)capitalTurrets[slot].Ready=true;}
    public Vector3 HeavyCrewFacing(int slot){return capitalTurrets!=null?capitalTurrets[slot].Root.forward:MainBatteryCrewForward;}
    private void InitializeCapitalBattery(Material armor,Material accent)
    {
        capitalTurrets=new CapitalMount[NavalCapitalLayout.Turrets];
        for(int slot=0;slot<capitalTurrets.Length;slot++){
            Vector3 p=NavalCapitalLayout.Mount(slot);p.y=hull.Definition.WeatherDeck;
            int first=hull.Voxels.Count;
            float rise=NavalCapitalLayout.TurretRise(slot);
            ArmoredDecoration(boat,armor,p+Vector3.up*(rise+NavalTurretAccessLayout.RootLift)*.5f,new Vector3(.74f,rise+NavalTurretAccessLayout.RootLift,.74f),true);
            var t=new CapitalMount();capitalTurrets[slot]=t;t.Yaw=NavalCapitalLayout.BaseYaw(slot);t.Sequence=slot;t.Reload=5f+slot*1.7f;
            t.Root=Root("305mm twin turret "+NavalCapitalLayout.Name(slot),p+Vector3.up*(rise+NavalTurretAccessLayout.RootLift));t.Root.localRotation=Quaternion.Euler(0,t.Yaw,0);
            ArmoredDecoration(t.Root,armor,new Vector3(0,.02f,0),new Vector3(.84f,.04f,.72f),true);
            for(int side=-1;side<=1;side+=2){
                if(NavalTurretAccessLayout.Side(slot)==side){
                    for(int end=-1;end<=1;end+=2)ArmoredDecoration(t.Root,armor,new Vector3(side*.40f,.29f,end*.27f),new Vector3(.06f,.50f,.18f),true);
                }else ArmoredDecoration(t.Root,armor,new Vector3(side*.40f,.29f,0),new Vector3(.06f,.50f,.72f),true);
            }
            if(NavalTurretAccessLayout.Side(slot)==0){
                for(int side=-1;side<=1;side+=2)ArmoredDecoration(t.Root,armor,new Vector3(side*.30f,.29f,-.33f),new Vector3(.24f,.50f,.06f),true);
            }else ArmoredDecoration(t.Root,armor,new Vector3(0,.29f,-.33f),new Vector3(.84f,.50f,.06f),true);
            ArmoredDecoration(t.Root,armor,new Vector3(0,.12f,.33f),new Vector3(.84f,.16f,.06f),true);
            ArmoredDecoration(t.Root,armor,new Vector3(0,.48f,.33f),new Vector3(.84f,.12f,.06f),true);
            foreach(float x in new float[]{-.37f,0,.37f})ArmoredDecoration(t.Root,armor,new Vector3(x,.31f,.33f),new Vector3(x==0?.12f:.10f,.22f,.06f),true);
            ArmoredDecoration(t.Root,accent,new Vector3(0,.57f,0),new Vector3(.88f,.06f,.76f),true);
            ArmoredDecoration(t.Root,armor,new Vector3(0,.615f,-.025f),new Vector3(.79f,.035f,.68f),true);
            // Chair is visible and destructible but does not block its own seated capsule.
            ArmoredDecoration(t.Root,accent,new Vector3(0,.135f,-.13f),new Vector3(.28f,.035f,.24f),false);
            ArmoredDecoration(t.Root,armor,new Vector3(0,.26f,-.265f),new Vector3(.28f,.25f,.035f),false);
            t.Barrels=new GameObject("Twin 305 mm barrels").transform;t.Barrels.SetParent(t.Root,false);t.Barrels.localPosition=new Vector3(0,NavalTurretAccessLayout.BarrelAxis,.22f);
            for(int tube=-1;tube<=1;tube+=2){
                ArmoredDecoration(t.Barrels,armor,new Vector3(tube*.18f,0,.10f),new Vector3(.23f,.23f,.19f),true);
                CalibratedTube(t.Barrels,armor,accent,NavalGunDimensions.CapitalCalibre,hull.WorldScale,tube*.18f,CapitalBarrelLengthScale);
            }
            BuildCapitalAccessSteps(slot,armor,accent);
            var cells=new List<NavalHullVoxel>();for(int i=first;i<hull.Voxels.Count;i++)cells.Add(hull.Voxels[i]);
            t.Mechanism=new NavalRepairableMechanism(hull,cells,false,true,"305 mm / "+NavalCapitalLayout.Name(slot),false,true);
            t.Mechanism.RepairPoint=NavalCapitalLayout.ServicePoint(hull.Definition,slot);
            BuildCapitalDoor(t,slot,armor,accent);
            // Iron working deck cannot catch fire underneath the inaccessible turntable.
            foreach(var cell in hull.Voxels)if(cell.transform.parent==boat&&Mathf.Abs(cell.LocalCenter.x-p.x)<.55f&&Mathf.Abs(cell.LocalCenter.z-p.z)<.55f&&Mathf.Abs(cell.LocalCenter.y+cell.transform.localScale.y*.5f-p.y)<.04f){cell.Flammable=false;cell.Metallic=true;cell.Armored=true;}
        }
    }
    private readonly Collider[] barrelContacts=new Collider[64];
    private bool CapitalBarrelsClear(CapitalMount mount,float yaw,float pitch)
    {
        Matrix4x4 frame=Matrix4x4.TRS(mount.Root.position,boat.rotation*Quaternion.Euler(0,yaw,0),mount.Root.lossyScale);
        Quaternion elevation=Quaternion.Euler(-pitch,0,0);
        for(int side=-1;side<=1;side+=2){
            Vector3 pivot=new Vector3(0,NavalTurretAccessLayout.BarrelAxis,.22f);
            Vector3 a=frame.MultiplyPoint3x4(pivot+elevation*new Vector3(side*.18f,0,.40f));
            Vector3 b=frame.MultiplyPoint3x4(pivot+elevation*new Vector3(side*.18f,0,CapitalMuzzleZ));
            int count=Physics.OverlapCapsuleNonAlloc(a,b,NavalGunDimensions.BreechDiameter(NavalGunDimensions.CapitalCalibre)*.5f,barrelContacts,PhysicsManager.GetEnvironmentLayerMask(),QueryTriggerInteraction.Ignore);
            if(count==barrelContacts.Length)return false;
            for(int i=0;i<count;i++)if(barrelContacts[i]!=null&&barrelContacts[i].transform.IsChildOf(boat)&&!barrelContacts[i].transform.IsChildOf(mount.Root))return false;
        }
        return true;
    }
    private bool TraverseCapital(CapitalMount mount,float goal,float pitch,float speed,float dt)
    {
        float error=Mathf.DeltaAngle(mount.Yaw,goal),step=speed*Mathf.Min(dt,.02f);
        if(Mathf.Abs(error)<.1f){
            mount.TraverseDirection=0;
            if(mount.Pitch==pitch)return true;
            if(!CapitalBarrelsClear(mount,mount.Yaw,pitch))return false;
            mount.Pitch=pitch;mount.Barrels.localRotation=Quaternion.Euler(-pitch,0,0);return true;
        }
        if(Mathf.Abs(Mathf.DeltaAngle(mount.TraverseGoal,goal))>30f){mount.TraverseDirection=0;mount.TraverseRetry=0;}
        mount.TraverseGoal=goal;
        if(Time.time<mount.TraverseRetry)return false;
        int direction=mount.TraverseDirection!=0?mount.TraverseDirection:error>0?1:-1;
        float remaining=direction>0?Mathf.Repeat(goal-mount.Yaw,360):Mathf.Repeat(mount.Yaw-goal,360);
        float next=mount.Yaw+direction*Mathf.Min(step,remaining);
        if(!CapitalBarrelsClear(mount,next,pitch)){
            // A collision later along a previously chosen arc also requires reversal.
            // Keep the last safe elevation while backing away from an obstacle.
            direction=-direction;remaining=direction>0?Mathf.Repeat(goal-mount.Yaw,360):Mathf.Repeat(mount.Yaw-goal,360);
            next=mount.Yaw+direction*Mathf.Min(step,remaining);
            if(!CapitalBarrelsClear(mount,next,pitch)&&CapitalBarrelsClear(mount,next,mount.Pitch))pitch=mount.Pitch;
            if(!CapitalBarrelsClear(mount,next,pitch)){
                mount.TraverseRetry=Time.time+.5f;
                if(Time.time>=mount.TraverseLog){mount.TraverseLog=Time.time+5f;Debug.Log("[NavalPrototype] Turret traverse obstructed: ship="+boat.name+" yaw="+mount.Yaw+" goal="+goal+" pitch="+pitch);}
                return false;
            }
        }
        mount.TraverseDirection=direction;mount.Yaw=Mathf.Repeat(next,360);mount.Pitch=pitch;
        mount.Root.localRotation=Quaternion.Euler(0,mount.Yaw,0);mount.Barrels.localRotation=Quaternion.Euler(-pitch,0,0);return true;
    }
    private RaycastHit[] capitalMuzzleHits=new RaycastHit[32];
    private bool CapitalMuzzleClear(Vector3 muzzle,Vector3 direction,float distance)
    {
        float length=Mathf.Min(distance,24f);
        int count=NavalRayQueries.Ray(muzzle,direction,length,PhysicsManager.GetEnvironmentLayerMask(),ref capitalMuzzleHits);
        if(count<0)
        {
            foreach(var hit in Physics.RaycastAll(muzzle,direction,length,PhysicsManager.GetEnvironmentLayerMask(),QueryTriggerInteraction.Ignore))
                if(hit.collider!=null&&hit.collider.transform.IsChildOf(boat))return false;
            return true;
        }
        for(int i=0;i<count;i++)
        {
            var collider=capitalMuzzleHits[i].collider;
            if(collider!=null&&collider.transform.IsChildOf(boat))return false;
        }
        return true;
    }
    private void CapitalState(CapitalMount mount,int slot,string reason){
        if(Time.time<mount.NextStateLog)return;mount.NextStateLog=Time.time+15f;
        Debug.Log("[NavalPrototype] Turret hold: ship="+boat.name+" turret="+NavalCapitalLayout.Name(slot)+" reason="+reason+" ready="+mount.Ready+" ammo="+mount.Ammo+" yaw="+mount.Yaw.ToString("F1")+" pitch="+mount.Pitch.ToString("F1"));
    }
    private void TickCapitalBattery(Transform target,Vector3 targetVelocity,float dt)
    {
        TickCapitalDoors(dt);
        var victim=ForShip(target);Vector3 relative=targetVelocity-FiringPlatformVelocity();
        bool withinSearch=target!=null&&TargetInsideSearchRange(target,targetVelocity,NavalCapitalLayout.Range);
        for(int slot=0;slot<capitalTurrets.Length;slot++){
            if(slot==playerCapitalSlot)continue;
            var t=capitalTurrets[slot];if(t.AccessRequested||Time.time<t.AutoDoorUntil||t.DoorAngle>.1f||!t.Ready||!MainBatterySlotFunctional(slot)||t.Ammo<=0){CapitalState(t,slot,t.AccessRequested||t.DoorAngle>.1f?"crew access":!t.Ready?"no seated operator":t.Ammo<=0?"empty":"damaged or automatic access");continue;}
            float hoist=SteamPlant!=null?SteamPlant.AuxiliaryFactor:1f;t.Reload=Mathf.Max(0,t.Reload-dt*hoist);
            if(t.Target!=target){t.Target=target;t.AimCell=null;t.SecondShot=0;}
            bool secondPending=t.SecondShot>0f;
            if(secondPending)t.SecondShot=Mathf.Max(.0001f,t.SecondShot-dt);
            // A second tube can wait briefly for alignment, then yields to the reload cycle.
            if(secondPending&&t.Reload<NavalCapitalLayout.Reload-2f){t.SecondShot=0;secondPending=false;}
            if(!withinSearch)continue;
            Vector3 muzzle=t.Barrels.TransformPoint(new Vector3(-.18f,0,CapitalMuzzleZ));
            Vector3 point=target.position;
            if(victim!=null)
            {
                if(!victim.TrackedHeavyAimValid(t.AimCell))
                    point=target.TransformPoint(victim.CurrentMainAim(t.Sequence,muzzle,out t.AimCell));
                else point=t.AimCell.transform.position;
            }
            if(victim!=null&&point.y<ocean.Height(point.x,point.z)+.03f)continue;
            Vector3 predicted=point;for(int iteration=0;iteration<3;iteration++)predicted=point+relative*(Vector3.Distance(muzzle,predicted)/NavalCapitalLayout.Speed);
            Vector3 delta=predicted-muzzle,flat=delta;flat.y=0;float distance=flat.magnitude,elevation;
            if(distance<10f||distance>NavalCapitalLayout.Range||!NavalBattleMath.FiringAngle(distance,delta.y,NavalCapitalLayout.Speed,out elevation))continue;
            Vector3 launch=flat.normalized*Mathf.Cos(elevation*Mathf.Deg2Rad)+Vector3.up*Mathf.Sin(elevation*Mathf.Deg2Rad),local=boat.InverseTransformDirection(launch);
            float yaw=Mathf.Atan2(local.x,local.z)*Mathf.Rad2Deg;if(!NavalCapitalLayout.Bears(slot,yaw)){CapitalState(t,slot,"target outside firing sector");continue;}
            float pitch=Mathf.Atan2(local.y,new Vector2(local.x,local.z).magnitude)*Mathf.Rad2Deg;
            if(pitch<NavalCapitalLayout.MinimumPitch(slot)||pitch>NavalTurretAccessLayout.MaximumPitch)continue;
            float nextPitch=Mathf.MoveTowards(t.Pitch,pitch,6f*Mathf.Min(dt,.05f));
            if(!TraverseCapital(t,yaw,nextPitch,8f,dt))continue;
            if(Mathf.Abs(Mathf.DeltaAngle(t.Yaw,yaw))>.6f||Mathf.Abs(t.Pitch-pitch)>.5f)continue;
            if(secondPending)
            {
                if(t.SecondShot<=.0001f){FireCapitalTube(t,slot,1);t.SecondShot=0;}
                continue;
            }
            if(t.Reload>0)continue;
            if(!CapitalMuzzleClear(muzzle,t.Barrels.forward,distance)){CapitalState(t,slot,"own ship blocks muzzle");continue;}
            FireCapitalTube(t,slot,-1);t.SecondShot=.35f;t.Reload=NavalCapitalLayout.Reload;
        }
    }
    private void FireCapitalTube(CapitalMount mount,int slot,int tube)
    {
        Vector3 muzzle=mount.Barrels.TransformPoint(new Vector3(tube*.18f,0,CapitalMuzzleZ));
        Vector3 direction=Quaternion.Euler(Random.Range(-.6f,.6f),Random.Range(-.8f,.8f),0)*mount.Barrels.forward;
        if(!CapitalMuzzleClear(muzzle,direction,24f))return;
        GameObject shell=NavalProjectileVisual.CreateRifledShell();shell.name="NavalPrototype_305mmShell";shell.transform.SetParent(NavalTestWorld.Active!=null?NavalTestWorld.Active.transform:null,false);shell.transform.position=muzzle;shell.transform.rotation=Quaternion.LookRotation(direction);shell.transform.localScale=Vector3.one*2.2f;
        var collider=shell.GetComponent<Collider>();if(collider!=null){collider.enabled=false;Destroy(collider);}shell.GetComponent<Renderer>().sharedMaterial=shellMaterial;
        shell.AddComponent<NavalCannonball>().Initialize(direction*NavalCapitalLayout.Speed+FiringPlatformVelocity(),boat,hull,player,ocean,smoke,impactSpark,Time.time+18f,HostileToPlayer,null,NavalCapitalLayout.Power,2.25f,true);
        mount.Ammo--;LastCrewShotAt=Time.time;AudioSource.PlayClipAtPoint(sound,muzzle,1f);
        NavalShotPuff.Create(transform,muzzle,direction*4f,flash,.12f,.8f);
        for(int i=0;i<32;i++)NavalShotPuff.Create(transform,muzzle+Random.insideUnitSphere*.15f,direction*Random.Range(2f,5f)+Vector3.up+Random.insideUnitSphere,smoke,Random.Range(1.5f,2.8f),Random.Range(.2f,.4f));
        Debug.Log("[NavalPrototype] 305mm fired: ship="+boat.name+" turret="+NavalCapitalLayout.Name(slot)+" tube="+tube+" ammo="+mount.Ammo);
    }
    private void DrawCapitalReload(Camera camera)
    {
        if(camera==null)return;for(int slot=0;slot<capitalTurrets.Length;slot++){var t=capitalTurrets[slot];bool functional=MainBatterySlotFunctional(slot)&&t.Ammo>0;
            if(Time.time<t.Mechanism.ProgressUntil){
                NavalWorldProgressBar.Draw(camera,t.Root.TransformPoint(new Vector3(0,.65f,0)),t.Mechanism.DisplaySeconds/t.Mechanism.RepairSeconds,true,false,t.Mechanism.RepairStage+" "+Mathf.CeilToInt(t.Mechanism.RepairSeconds-t.Mechanism.DisplaySeconds)+" s",t.Root.GetInstanceID());continue;
            }
            NavalWorldProgressBar.Draw(camera,t.Root.TransformPoint(new Vector3(0,.65f,0)),functional?1f-t.Reload/NavalCapitalLayout.Reload:0f,functional,t.Reload<=0,"305 "+NavalCapitalLayout.Name(slot)+" / "+t.Ammo,t.Root.GetInstanceID());
        }
    }
}
