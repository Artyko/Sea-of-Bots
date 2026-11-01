using System.Collections.Generic;
using UnityEngine;

public static class NavalMainBatteryMath
{
    public const float ReloadSeconds=20f,ProjectileSpeed=105f,ProjectilePower=5.2f,MaximumRange=650f,TraverseArc=70f;
    // At 65 degrees the target is simultaneously inside the main mount's
    // +/-70 degree sector and the casemate battery's +/-30 degree traverse.
    public const float JointBatteryBearing=65f,MainBatteryApproachBearing=65f,MainBatteryReadyBearing=45f,SteeringLeadSeconds=10f;
    public const int MuzzleSmokeCount=44;
    // Lowest axis that still clears the ironclad's bow rim at level fire.
    public const float MountHeight=.65f,TrunnionOffset=.18f,MuzzleZ=1.52f;
    public const float CrewX=.30f,CrewZ=-.25f,CrewApproachZ=-.66f,SideShieldX=.61f,SideShieldThickness=.10f;
    public const float FireproofHalfWidth=1.65f,FireproofAftZ=1.25f,FireproofForwardZ=5.35f;
    // The magazine is deliberately deep; the 20-second cycle, crew and hoist
    // still limit practical rate of fire.
    public const int Ammunition=100;
    public const int AimZoneCount=8;
    public const float AimArcReserve=.5f;
    public static bool WithinArc(float yaw){return Mathf.Abs(yaw)<=TraverseArc;}
    public static bool CanReload(int crew){return crew>=1;}
    public static bool CanTraverse(int crew,float reloadRemaining){return crew>=2||crew==1&&reloadRemaining<=0f;}
    public static bool SteeringPriority(bool functional,int ammunition,float reloadRemaining,int crew)
    {
        // Do not drop back to a 90-degree broadside immediately after every
        // heavy-gun shot. That binary 90 -> 45..58 -> 90 course change made
        // the ship weave once per 20-second reload. While loading, hold the
        // 65-degree overlap where both the casemates and main gun can bear;
        // then ease toward the main gun's reliable 45-degree ready bearing.
        return functional&&ammunition>0&&crew>0;
    }
    public static float JointBatteryCourse(float targetBearing,int side)
    {return targetBearing+(side==1?JointBatteryBearing:-JointBatteryBearing);}
    // When the heavy gun is nearly ready, favour a reliable shot over the
    // very narrow five-degree overlap with the casemate battery. The mount
    // receives twelve degrees of reserve for hull lag and target manoeuvres.
    public static float MainBatterySteeringBearing(float reloadRemaining)
    {
        // Gradually leave the broadside as the gun becomes ready. This avoids
        // a permanent 65-degree lock while keeping the manoeuvre smooth.
        float loading=Mathf.Clamp01(reloadRemaining/SteeringLeadSeconds);
        return Mathf.Lerp(MainBatteryReadyBearing,MainBatteryApproachBearing,loading);
    }
    public static float MainBatteryCourse(float predictedBearing,int side,float reloadRemaining)
    {
        float offset=MainBatterySteeringBearing(reloadRemaining);
        return predictedBearing+(side==1?offset:-offset);
    }
    public static int RecoverySide(float targetYaw,int preferredSide)
    {
        // Very close to the stern line the signed angle can alternate between
        // +180 and -180 every frame. Preserve the committed side there; away
        // from that seam, select the side on which the target actually lies.
        // A +/-20 degree dead band keeps this from alternating near the bow.
        if(Mathf.Abs(targetYaw)>=170f&&(preferredSide==1||preferredSide==2))return preferredSide;
        if(targetYaw>20f)return 2;
        if(targetYaw< -20f)return 1;
        if(preferredSide==1||preferredSide==2)return preferredSide;
        return targetYaw>0f?2:1;
    }
    public static float MainBatteryHelmCourse(float shipHeading,float predictedBearing,int preferredSide,float reloadRemaining)
    {
        float targetYaw=NavalBroadsideMath.Delta(shipHeading,predictedBearing);
        bool recovering=Mathf.Abs(targetYaw)>TraverseArc;
        int side=RecoverySide(targetYaw,preferredSide);
        // A course aimed at the 65-degree overlap was too weak for the real
        // rudder/controller dead band: ships could settle with the target at
        // 74..82 degrees while the physical stop remained 70. During recovery
        // demand the same 45-degree bearing used by a ready gun. This supplies
        // a full 25-degree capture margin and only returns to the smooth loading
        // course after the target is genuinely inside the traverse sector.
        float offset=recovering?MainBatteryReadyBearing:MainBatterySteeringBearing(reloadRemaining);
        return predictedBearing+(side==1?offset:-offset);
    }
    public static bool FireproofDeckCell(float x,float y,float z,float height,float weatherDeck)
    {
        float top=y+Mathf.Abs(height)*.5f;
        return Mathf.Abs(top-weatherDeck)<.04f&&Mathf.Abs(x)<=FireproofHalfWidth&&z>=FireproofAftZ&&z<=FireproofForwardZ;
    }

    // Select once per loading cycle, not once per frame.  A deterministic
    // sequence makes both AI ships spread heavy shells across the belt,
    // machinery spaces and upper works without making the sight wander while
    // the gun is training.
    public static Vector3 AimPoint(NavalShipDefinition target,int sequence,int nearSide)
    {
        int zone=((sequence%AimZoneCount)+AimZoneCount)%AimZoneCount;
        float[] longitudinal={-3.55f,2.70f,-.85f,3.65f,-2.25f,1.20f,0f,4.35f};
        float[] heightWorld={-.10f,.12f,.32f,.72f,.08f,.48f,.24f,.56f};
        float z=longitudinal[zone];
        float side=nearSide<0?-1f:1f;
        float x=side*Mathf.Max(.08f,target.HullHalfWidth(z)-.10f/target.Scale);
        float y=target.DesignWaterline+heightWorld[zone]/target.Scale;
        return new Vector3(x,y,z);
    }
    public static string AimZoneName(int sequence)
    {
        string[] names={"aft-belt","forward-belt","machinery","forward-upper","aft-waterline","boiler-upper","mid-belt","bow-upper"};
        int zone=((sequence%AimZoneCount)+AimZoneCount)%AimZoneCount;return names[zone];
    }
    public static bool AimZoneAvailable(float yaw)
    {return !float.IsNaN(yaw)&&Mathf.Abs(yaw)<=TraverseArc-AimArcReserve;}
    public static int SelectAvailableAimZone(float[] yaws,int preferred,int current)
    {
        if(yaws==null||yaws.Length==0)return -1;
        if(current>=0&&current<yaws.Length&&AimZoneAvailable(yaws[current]))return current;
        preferred=((preferred%yaws.Length)+yaws.Length)%yaws.Length;
        if(AimZoneAvailable(yaws[preferred]))return preferred;
        int best=-1;float bestYaw=float.PositiveInfinity;
        for(int i=0;i<yaws.Length;i++)if(AimZoneAvailable(yaws[i])&&Mathf.Abs(yaws[i])<bestYaw)
        {best=i;bestYaw=Mathf.Abs(yaws[i]);}
        if(best>=0)return best;
        for(int i=0;i<yaws.Length;i++)if(!float.IsNaN(yaws[i])&&Mathf.Abs(yaws[i])<bestYaw)
        {best=i;bestYaw=Mathf.Abs(yaws[i]);}
        return best;
    }
}

public static class NavalMainBatteryCrewPolicy
{
    public static bool MansMount(bool functional){return functional;}
    public static int EmergencyDemand(bool criticalFire,bool criticalFlood,bool liveSteam,bool urgentPump,bool urgentFire)
    {return criticalFire||criticalFlood?2:liveSteam||urgentPump||urgentFire?1:0;}
    public static int ReleasedHands(int emergencyDemand,int livingGeneralCrew)
    {
        // Ordinary deck hands and broadside gunners respond first. Keep one of
        // them available for the helm; borrow the dedicated heavy-gun crew only
        // when casualties leave fewer responders than the emergency requires.
        int generalResponders=Mathf.Max(0,livingGeneralCrew-(livingGeneralCrew>0?1:0));
        return Mathf.Clamp(emergencyDemand-generalResponders,0,2);
    }
}

public partial class NavalStations
{
    private Transform mainTurret,mainBarrel;
    private NavalRepairableMechanism mainBatteryMechanism;
    private float mainYaw,mainPitch,mainReloadRemaining,mainTargetYaw,mainTargetRange=-1f;
    private int mainAmmunition,mainCrewReady;
    private Transform mainAimTarget;
    private Vector3 mainAimLocal;
    private int mainAimSequence,mainAimZone=-1;
    private readonly float[] mainAimYaws=new float[NavalMainBatteryMath.AimZoneCount];
    private float mainAimReviewAt;
    private int mainAimAvailable;
    public int MainBatteryReleasedHands {get;set;}
    private bool mainTargetInArc,mainTargetSolution;
    public bool HasMainBattery {get{return mainTurret!=null;}}
    public int MainBatteryAmmunition {get{return mainAmmunition;}}
    public Vector3 MainBatteryCrewPost(int slot)
    {
        if(mainTurret==null)return new Vector3(slot==0?-NavalMainBatteryMath.CrewX:NavalMainBatteryMath.CrewX,hull.Definition.WeatherDeck+.08f,3.80f);
        // Both seats live inside the rotating shield, beside the breech. Their
        // ship-space destinations therefore follow the mount as it trains.
        return boat.InverseTransformPoint(mainTurret.TransformPoint(new Vector3(slot==0?-NavalMainBatteryMath.CrewX:NavalMainBatteryMath.CrewX,-.50f,NavalMainBatteryMath.CrewZ)));
    }
    public Vector3 MainBatteryCrewApproachPost(int slot)
    {
        float x=slot==0?-NavalMainBatteryMath.CrewX:NavalMainBatteryMath.CrewX;
        if(mainTurret==null)return new Vector3(x,hull.Definition.WeatherDeck,3.39f);
        // The mount is open at the rear. Always enter along that opening as
        // it trains; a direct route from the side intersects the cheek armour.
        return boat.InverseTransformPoint(mainTurret.TransformPoint(new Vector3(x,-NavalMainBatteryMath.MountHeight,NavalMainBatteryMath.CrewApproachZ)));
    }
    public Vector3 MainBatteryCrewForward {get{return mainBarrel!=null?mainBarrel.forward:boat.forward;}}
    public bool MainBatteryCanRestore {get{return mainBatteryMechanism!=null&&mainBatteryMechanism.NeedsRepair&&mainBatteryMechanism.RecoverableWithParts&&hull.Fire!=null&&hull.Fire.MainBatteryMagazineFunctional;}}
    public bool MainBatteryFunctional {get{return mainBatteryMechanism!=null&&mainBatteryMechanism.Functional&&hull.Fire!=null&&hull.Fire.MainBatteryMagazineFunctional;}}
    public bool MainBatteryCombatAvailable {get{return MainBatteryFunctional&&mainAmmunition>0;}}
    public bool MainBatteryHelmPriority {get{return HasMainBattery&&NavalMainBatteryMath.SteeringPriority(MainBatteryFunctional,mainAmmunition,mainReloadRemaining,mainCrewReady);}}
    public float MainBatteryLeadBearing(float shipHeading,float fallbackBearing)
    {
        // TickMainBattery predicts interception with relative ship velocity.
        // Steering must use that same world bearing or a moving target can sit
        // beyond the mount stop even while its geometric bearing looks valid.
        return mainTargetSolution?shipHeading+mainTargetYaw:fallbackBearing;
    }
    public float MainBatteryHelmCourse(float shipHeading,float fallbackBearing,int side)
    {return NavalMainBatteryMath.MainBatteryHelmCourse(shipHeading,MainBatteryLeadBearing(shipHeading,fallbackBearing),side,mainReloadRemaining);}
    public string MainBatteryDiagnostics {get{return "ammo="+mainAmmunition+" crew="+mainCrewReady+" released="+MainBatteryReleasedHands+" reload="+mainReloadRemaining.ToString("F1")+
        " mount="+(mainBatteryMechanism!=null&&mainBatteryMechanism.Functional)+" magazine="+(hull.Fire!=null&&hull.Fire.MainBatteryMagazineFunctional)+" range="+mainTargetRange.ToString("F1")+" yaw="+mainYaw.ToString("F1")+" targetYaw="+mainTargetYaw.ToString("F1")+
        " solution="+mainTargetSolution+" inArc="+mainTargetInArc+" aim="+(mainAimZone>=0?NavalMainBatteryMath.AimZoneName(mainAimZone):"none")+" availableZones="+mainAimAvailable+" helmAssist="+MainBatteryHelmPriority;}}

    private void InitializeMainBattery(Material armor,Material accent)
    {
        if(!hull.Definition.SteamPowered)return;
        // Compact low mount: the axis only just clears the bow rim. The hoist
        // joins a shallow barbette instead of lifting a tall box above deck.
        mainTurret=Root("ForwardMainBattery",new Vector3(0,hull.Definition.WeatherDeck+NavalMainBatteryMath.MountHeight,4.05f));
        int first=hull.Voxels.Count;
        // Semi-open naval mounting: shallow foundation, low floor, split
        // frontal shield and short cheeks; no roof, rear wall or top beam.
        ArmoredDecoration(mainTurret,armor,new Vector3(0,-.73f,0),new Vector3(.72f,.30f,.76f),true);
        ArmoredDecoration(mainTurret,accent,new Vector3(0,-.55f,-.02f),new Vector3(1.24f,.10f,.98f),true);
        for(int side=-1;side<=1;side+=2)
            ArmoredDecoration(mainTurret,armor,new Vector3(side*NavalMainBatteryMath.SideShieldX,-.08f,.04f),new Vector3(NavalMainBatteryMath.SideShieldThickness,.86f,.92f),true);
        // Split glacis leaves a narrow central embrasure for the elevating tube.
        for(int side=-1;side<=1;side+=2)
            ArmoredDecoration(mainTurret,armor,new Vector3(side*.39f,-.06f,.46f),new Vector3(.48f,.90f,.12f),true);
        // Two low seats and backrests are visual fittings, not route-blocking solids.
        for(int side=-1;side<=1;side+=2)
        {
            hull.AddDecorationBox(mainTurret,accent,new Vector3(side*NavalMainBatteryMath.CrewX,-.42f,-.27f),new Vector3(.28f,.09f,.28f),false);
            hull.AddDecorationBox(mainTurret,armor,new Vector3(side*NavalMainBatteryMath.CrewX,-.25f,-.40f),new Vector3(.28f,.30f,.07f),false);
        }
        mainBarrel=new GameObject("Main caliber barrel").transform;mainBarrel.SetParent(mainTurret,false);mainBarrel.localPosition=new Vector3(0,NavalMainBatteryMath.TrunnionOffset,.16f);
        ArmoredDecoration(mainBarrel,armor,new Vector3(0,0,.22f),new Vector3(.30f,.32f,.54f),true);
        ArmoredDecoration(mainBarrel,armor,new Vector3(0,0,.91f),new Vector3(.21f,.21f,1.34f),true);
        ArmoredDecoration(mainBarrel,accent,new Vector3(0,0,1.54f),new Vector3(.27f,.27f,.12f),true);
        List<NavalHullVoxel> members=new List<NavalHullVoxel>();
        for(int i=first;i<hull.Voxels.Count;i++){var cell=hull.Voxels[i];cell.Metallic=true;cell.Flammable=false;cell.Armored=true;members.Add(cell);}
        mainBatteryMechanism=new NavalRepairableMechanism(hull,members,false,true,"Орудие главного калибра",false,true);
        mainBatteryMechanism.RepairPoint=new Vector3(0,hull.Definition.WeatherDeck,3.42f);
        // The heavy mounting sits on an iron working apron. Retain the deck
        // cells for support and damage, but never let their old wood material
        // create an unreachable fire beneath the turntable.
        foreach(var cell in hull.Voxels)
            if(NavalMainBatteryMath.FireproofDeckCell(cell.LocalCenter.x,cell.LocalCenter.y,cell.LocalCenter.z,
                cell.transform.localScale.y,hull.Definition.WeatherDeck))
            {cell.Flammable=false;cell.Metallic=true;cell.Armored=true;}
        mainAmmunition=NavalMainBatteryMath.Ammunition;mainReloadRemaining=4f;
    }

    public void TickMainBattery(Transform target,Vector3 targetVelocity,float dt,int crewReady)
    {
        mainCrewReady=crewReady;mainTargetInArc=false;mainTargetSolution=false;mainTargetRange=-1f;
        if(mainTurret==null||crewReady<1||!MainBatteryFunctional||mainAmmunition<=0)return;
        if(mainReloadRemaining>0f)
        {
            NavalSteamPlant steam=SteamPlant;
            float hoist=steam!=null?steam.AuxiliaryFactor:1f;
            mainReloadRemaining=Mathf.Max(0f,mainReloadRemaining-dt*hoist);
            // A lone operator works the hoist and breech with both hands; the
            // mount remains locked until loading is completely finished.
            if(!NavalMainBatteryMath.CanTraverse(crewReady,mainReloadRemaining))return;
        }
        if(target==null)return;
        Vector3 muzzle=mainBarrel.TransformPoint(new Vector3(0,0,NavalMainBatteryMath.MuzzleZ));
        NavalStations victim=ForShip(target);
        Vector3 relativeVelocity=targetVelocity-FiringPlatformVelocity();
        if(victim!=null&&(mainAimTarget!=target||mainAimZone<0||Time.time>=mainAimReviewAt))
            ReviewMainAim(target,victim,relativeVelocity,muzzle);
        Vector3 point=victim!=null?target.TransformPoint(mainAimLocal):target.position;
        Vector3 predicted=point;
        for(int i=0;i<3;i++)predicted=point+relativeVelocity*(Vector3.Distance(muzzle,predicted)/NavalMainBatteryMath.ProjectileSpeed);
        Vector3 delta=predicted-muzzle,flat=delta;flat.y=0;
        mainTargetRange=flat.magnitude;if(mainTargetRange>NavalMainBatteryMath.MaximumRange)return;
        float elevation;if(!NavalBattleMath.FiringAngle(flat.magnitude,delta.y,NavalMainBatteryMath.ProjectileSpeed,out elevation))return;
        Vector3 launch=flat.normalized*Mathf.Cos(elevation*Mathf.Deg2Rad)+Vector3.up*Mathf.Sin(elevation*Mathf.Deg2Rad);
        Vector3 local=boat.InverseTransformDirection(launch);
        float wantedYaw=Mathf.Atan2(local.x,local.z)*Mathf.Rad2Deg;mainTargetYaw=wantedYaw;mainTargetSolution=true;
        mainTargetInArc=NavalMainBatteryMath.WithinArc(wantedYaw);if(!mainTargetInArc)return;
        float wantedPitch=Mathf.Atan2(local.y,new Vector2(local.x,local.z).magnitude)*Mathf.Rad2Deg;
        mainYaw=Mathf.MoveTowards(mainYaw,wantedYaw,11f*dt);mainPitch=Mathf.MoveTowards(mainPitch,wantedPitch,8f*dt);
        mainTurret.localRotation=Quaternion.Euler(0,mainYaw,0);mainBarrel.localRotation=Quaternion.Euler(-mainPitch,0,0);
        if(mainReloadRemaining>0f||Mathf.Abs(mainYaw-wantedYaw)>.55f||Mathf.Abs(mainPitch-wantedPitch)>.4f)return;
        FireMainBattery();
    }

    private void ReviewMainAim(Transform target,NavalStations victim,Vector3 relativeVelocity,Vector3 muzzle)
    {
        int nearSide=target.InverseTransformPoint(muzzle).x<0f?-1:1;
        mainAimAvailable=0;
        for(int zone=0;zone<mainAimYaws.Length;zone++)
        {
            Vector3 point=target.TransformPoint(NavalMainBatteryMath.AimPoint(victim.hull.Definition,zone,nearSide));
            Vector3 predicted=point;
            for(int iteration=0;iteration<3;iteration++)predicted=point+relativeVelocity*(Vector3.Distance(muzzle,predicted)/NavalMainBatteryMath.ProjectileSpeed);
            Vector3 delta=predicted-muzzle,flat=delta;flat.y=0f;float elevation;
            if(flat.sqrMagnitude<.01f||flat.magnitude>NavalMainBatteryMath.MaximumRange||
                !NavalBattleMath.FiringAngle(flat.magnitude,delta.y,NavalMainBatteryMath.ProjectileSpeed,out elevation))
            {mainAimYaws[zone]=float.NaN;continue;}
            Vector3 launch=flat.normalized*Mathf.Cos(elevation*Mathf.Deg2Rad)+Vector3.up*Mathf.Sin(elevation*Mathf.Deg2Rad);
            Vector3 local=boat.InverseTransformDirection(launch);
            mainAimYaws[zone]=Mathf.Atan2(local.x,local.z)*Mathf.Rad2Deg;
            if(NavalMainBatteryMath.AimZoneAvailable(mainAimYaws[zone]))mainAimAvailable++;
        }
        int previous=mainAimZone;
        mainAimZone=NavalMainBatteryMath.SelectAvailableAimZone(mainAimYaws,mainAimSequence,mainAimTarget==target?mainAimZone:-1);
        if(mainAimZone>=0)mainAimLocal=NavalMainBatteryMath.AimPoint(victim.hull.Definition,mainAimZone,nearSide);
        mainAimTarget=target;mainAimReviewAt=Time.time+(mainAimAvailable>0?.65f:.20f);
        if(previous>=0&&mainAimZone!=previous)
            Debug.Log("[NavalPrototype] Main battery aim retarget: ship="+boat.name+" from="+NavalMainBatteryMath.AimZoneName(previous)+" to="+NavalMainBatteryMath.AimZoneName(mainAimZone)+" available="+mainAimAvailable);
    }

    private void FireMainBattery()
    {
        Vector3 direction=mainBarrel.forward;
        Vector3 muzzle=mainBarrel.TransformPoint(new Vector3(0,0,NavalMainBatteryMath.MuzzleZ));
        GameObject shell=NavalProjectileVisual.CreateRifledShell();shell.name="NavalPrototype_MainCaliberShell";
        shell.transform.SetParent(NavalTestWorld.Active!=null?NavalTestWorld.Active.transform:null,false);
        shell.transform.position=muzzle;shell.transform.rotation=Quaternion.LookRotation(direction);shell.transform.localScale=Vector3.one*1.8f;
        Collider collider=shell.GetComponent<Collider>();if(collider!=null){collider.enabled=false;Destroy(collider);}
        shell.GetComponent<Renderer>().sharedMaterial=shellMaterial;
        shell.AddComponent<NavalCannonball>().Initialize(direction*NavalMainBatteryMath.ProjectileSpeed+FiringPlatformVelocity(),boat,hull,player,ocean,smoke,impactSpark,
            Time.time+14f,EnemyControlled,null,NavalMainBatteryMath.ProjectilePower,2f,true);
        mainAmmunition--;mainReloadRemaining=NavalMainBatteryMath.ReloadSeconds;
        int firedZone=mainAimZone;mainAimSequence++;mainAimZone=-1;mainAimReviewAt=0f;
        LastCrewShotAt=Time.time;
        AudioSource.PlayClipAtPoint(sound,muzzle,1f);
        NavalShotPuff.Create(transform,muzzle,direction*3.5f,flash,.09f,.58f);
        for(int i=0;i<NavalMainBatteryMath.MuzzleSmokeCount;i++)NavalShotPuff.Create(transform,muzzle+Random.insideUnitSphere*.13f,
            direction*Random.Range(1.0f,4.2f)+Vector3.up*Random.Range(.6f,1.7f)+Random.insideUnitSphere*.70f,smoke,Random.Range(1.7f,3.0f),Random.Range(.17f,.36f));
        Debug.Log("[NavalPrototype] Main battery fired: "+boat.name+" rounds="+mainAmmunition+" aim="+NavalMainBatteryMath.AimZoneName(firedZone)+" local="+mainAimLocal+" reload="+NavalMainBatteryMath.ReloadSeconds.ToString("F0")+"s");
    }

    private void DrawMainBatteryReloadBar(Camera camera)
    {
        if(mainTurret==null||camera==null)return;
        bool mechanism=MainBatteryFunctional;
        bool hasAmmunition=mainAmmunition>0;
        float remaining=Mathf.Max(0f,mainReloadRemaining);
        float progress=mechanism&&hasAmmunition?Mathf.Clamp01(1f-remaining/NavalMainBatteryMath.ReloadSeconds):0f;
        bool ready=mechanism&&hasAmmunition&&remaining<=0f;
        string text=!mechanism?"ГК ПОВРЕЖДЁН":!hasAmmunition?"ГК: НЕТ СНАРЯДОВ":ready?"ГК ГОТОВ":"ГК "+remaining.ToString("F1")+" с";
        // Keep the indicator attached to the training mount and above the
        // open shield, using exactly the same world-space widget as casemate guns.
        NavalWorldProgressBar.Draw(camera,mainTurret.TransformPoint(new Vector3(0f,.62f,0f)),progress,mechanism&&hasAmmunition,ready,text,mainTurret.GetInstanceID());
    }
}
