using System.Collections.Generic;
using System.Reflection;
using ModLibrary;
using UnityEngine;

public class NavalEnemyShip : MonoBehaviour
{
    public Transform Boat { get; private set; }
    public NavalHull Hull { get; private set; }
    public NavalStations Controls {get{return stations;}}
    public float SailPower {get{return rig!=null?rig.SailPowerFactor:0f;}}
    public int LivingCrew {get{int n=0;if(crew!=null)foreach(Character sailor in crew)if(sailor!=null&&sailor.IsAlive())n++;return n;}}
    public float SpeedThroughWater {get;private set;}
    public Vector3 ShipVelocity { get { return Boat!=null?Boat.forward*speed+(ram!=null?ram.Drift:Vector3.zero):Vector3.zero; } }
    private readonly NavalCrewController crewRoles=new NavalCrewController();
    private NavalRamDamage ram;
    private Transform target;
    private FirstPersonMover player;
    private NavalWater ocean;
    private NavalStations stations;
    private NavalShipVisuals rig;
    private NavalStructure structure;
    private Character[] crew;
    private bool[] recruited;
    private NavalRepairSailor[] repairers;
    private readonly List<Material> materials=new List<Material>();
    private float heading,speed,verticalSpeed,footOffset,retryCrew,rudder;
    private readonly NavalHelmNavigator helmNavigator=new NavalHelmNavigator();
    private readonly NavalHelmDecision helmDecision=new NavalHelmDecision();
    private readonly NavalRetreatState retreat=new NavalRetreatState();
    public bool Reversing {get{return helmNavigator.Reversing;}}
    public bool Retreating {get{return retreat.Active||stations!=null&&stations.CrewRepairRetreat;}}
    private bool helmUseful=true;
    private static readonly FieldInfo Velocity=typeof(FirstPersonMover).GetField("_velocity",BindingFlags.Instance|BindingFlags.NonPublic);
    public void Initialize(FirstPersonMover observer,Transform opponent,NavalWater sea,float feet)
    {
        player=observer;target=opponent;ocean=sea;footOffset=feet;
        Boat=new GameObject("EnemyWoodenShip").transform;Boat.SetParent(transform,false);
        Boat.localScale=Vector3.one*NavalSessionSettings.EnemyShip.Scale;
        Vector3 facing=target.forward;facing.y=0f;facing.Normalize();
        Boat.position=target.position+facing*NavalSessionSettings.EnemySpawnDistance;
        Boat.rotation=Quaternion.LookRotation(-facing);heading=Boat.eulerAngles.y;
        Material wood=Mat(new Color(0.30f,0.19f,0.115f)),trim=Mat(new Color(0.66f,0.51f,0.32f));
        Material deck=Mat(new Color(0.47f,0.375f,0.255f)),plank=Mat(new Color(0.515f,0.415f,0.285f));
        Material iron=Mat(new Color(0.025f,0.025f,0.03f));
        Hull=new NavalHull(NavalSessionSettings.EnemyShip);
        crew=new Character[Hull.Definition.CrewCount];repairers=new NavalRepairSailor[crew.Length];recruited=new bool[crew.Length];
        Hull.Build(Boat,wood,ocean.InteriorMaterial,deck);
        rig=Boat.gameObject.AddComponent<NavalShipVisuals>();
        rig.Initialize(Hull,wood,trim,iron,Mat(new Color(0.48f,0.15f,0.12f)),Mat(new Color(0.12f,0.085f,0.05f)),deck,plank);
        stations=gameObject.AddComponent<NavalStations>();stations.EnemyControlled=true;
        stations.Initialize(Boat,Hull,player,ocean,wood,trim,iron);
        Boat.gameObject.AddComponent<NavalSeaLadder>().Initialize(Hull,trim);
        if(Hull.Definition.Compartments)Boat.gameObject.AddComponent<NavalBilgeSystem>().Initialize(Hull,player,wood,trim,iron);
        Boat.gameObject.AddComponent<NavalCargo>().Initialize(Hull,player,stations,true,wood,trim,iron);
        Boat.gameObject.AddComponent<NavalFurnishings>().Initialize(Hull,wood,trim,iron);
        Boat.gameObject.AddComponent<NavalFireSystem>().Initialize(Hull);
        structure=Boat.gameObject.AddComponent<NavalStructure>();structure.Initialize(Hull);
        ram=Boat.gameObject.AddComponent<NavalRamDamage>();ram.Initialize(Hull);
        Boat.gameObject.AddComponent<NavalVoxelBatch>().Initialize(Hull);
        Hull.PhysicsBody.FinishConstruction();
        crewRoles.Initialize(Hull,stations,crew,repairers);
        Boat.gameObject.AddComponent<NavalFloodAppearance>().Initialize(ocean,Hull);
        ocean.AdditionalHull=Boat;
        RecruitCrew();
    }
    private Material Mat(Color color)
    {
        Shader shader=Shader.Find("Unlit/Color");if(shader==null)shader=Shader.Find("Standard");
        Material material=new Material(shader);material.color=color;materials.Add(material);return material;
    }
    private void RecruitCrew()
    {
        EnemyFactory factory=Singleton<EnemyFactory>.Instance;if(factory==null)return;
        for(int i=0;i<Hull.Definition.CrewCount;i++)if(!recruited[i])
        {
            Vector3 initialPost=i<=Hull.Definition.GunCount?stations.Post(i):
                new Vector3(NavalShipDefinition.StandbyX(i),Hull.Definition.WeatherDeck,NavalShipDefinition.StandbyZ(i));
            Vector3 position=Boat.TransformPoint(initialPost)+Vector3.up*footOffset;
            Transform spawned=factory.SpawnEnemy(i==0?EnemyType.Spear2:EnemyType.Hammer1,position,position+Boat.forward,null);
            if(spawned==null)continue;
            Character sailor=spawned.GetComponent<Character>();
            if(sailor==null){Destroy(spawned.gameObject);continue;}
            spawned.SetParent(transform,true);spawned.localScale=Vector3.one;
            sailor.IsPlayerTeam=false;sailor.SetIgnoreForVictoryCount(true);sailor.DisableAI();
            crew[i]=sailor;recruited[i]=true;
            repairers[i]=spawned.gameObject.AddComponent<NavalRepairSailor>();
            repairers[i].Initialize(sailor,Hull,structure,initialPost,footOffset);
            spawned.gameObject.AddComponent<NavalSailorRescue>().Initialize(sailor,repairers[i],Hull);
        }
    }
    private bool Alive(int i){return crew[i]!=null&&crew[i].IsAlive();}
    private bool CanHelm(){return helmUseful&&crewRoles.HasHelmsman;}
    private void LateUpdate()
    {
        NavalTestWorld world=NavalTestWorld.Active;
        if(target==null||world==null||!world.SimulationActive)return;
        float dt=Mathf.Min(Time.deltaTime,0.05f);
        bool rider=!world.Spectating&&player!=null&&!world.IsBoarding&&world.PlayerSupportedBy(Boat);
        Vector3 riderFoot=!world.Spectating&&player!=null?world.PlayerFootPoint:Vector3.zero;
        Vector3 riderOffset=!world.Spectating&&player!=null?player.transform.position-riderFoot:Vector3.zero;
        Vector3 riderLocal=Boat.InverseTransformPoint(riderFoot);
        if(Time.time>=retryCrew){retryCrew=Time.time+2f;RecruitCrew();}
        structure.Refresh(Boat.forward*speed+Vector3.up*verticalSpeed);
        Hull.Step(Boat,ocean,dt,false);rig.RefreshRig();
        helmUseful=NavalCrewRoles.HelmUseful(rig.SailPowerFactor,speed);
        retreat.Step(dt,crewRoles.NeedsRetreat,NavalCrewPriorities.ShouldWithdraw(Hull.RepairRetreat,stations.AllGunsBroken,Hull.HasActiveLeaks||Hull.Bilge!=null&&(Hull.Bilge.WantsOperator||!Hull.Bilge.Functional)),target!=null&&!Hull.Sunk);
        float throttle=0f,turn=0f;
        if(!Hull.Sunk&&CanHelm()&&stations.IsFunctional(0))
        {
            Vector3 desired;
            if(helmNavigator.Reversing){helmDecision.Reset();desired=Boat.forward;throttle=1;}
            else desired=helmDecision.Course(Boat,Hull,stations,target,world.ShipVelocity,speed,dt,retreat.Active,world.Retreating,ram,ref throttle);
            helmNavigator.Steer(NavalWorldMap.Active,Boat,desired,speed,rig.SailPowerFactor,dt,ref throttle,out turn,!stations.MustBoard);
        }
        else {helmNavigator.Reset(Boat.position);helmDecision.Reset();}
        if(stations.RescueStop){throttle=0f;turn=0f;helmNavigator.Reset(Boat.position);helmDecision.Reset();}
        stations.ReverseManeuver=helmNavigator.Recovering&&!stations.RescueStop;
        throttle=stations.StepSails(dt,throttle,!Hull.Sunk&&CanHelm()&&stations.IsFunctional(0));
        ram.StepMomentum(dt,ref speed);
        speed=NavalRigMath.StepSpeed(speed,throttle,rig.SailPowerFactor,dt,Hull.Definition.MaxSailingSpeed);
        float depth=Hull.Volume/(BoatDimensions.HoldArea*Hull.AreaScale);
        rudder=stations.IsFunctional(0)?NavalRigMath.StepRudder(rudder,turn,dt):0f;
        heading+=NavalRigMath.RudderRate(speed,rudder)*stations.SteeringFactor*dt/(1f+depth);
        Quaternion flat=Quaternion.Euler(0f,heading,0f);Vector3 forward=flat*Vector3.forward,right=flat*Vector3.right;
        Vector3 p=Boat.position;
        float bow=ocean.Height(p.x+forward.x*9f,p.z+forward.z*9f),stern=ocean.Height(p.x-forward.x*9f,p.z-forward.z*9f);
        float starboard=ocean.Height(p.x+right.x*5.4f,p.z+right.z*5.4f),port=ocean.Height(p.x-right.x*5.4f,p.z-right.z*5.4f);
        float pitch=-Mathf.Atan2(bow-stern,18f)*Mathf.Rad2Deg+Mathf.Clamp(Hull.WaterLongitudinalMoment/(BoatDimensions.HalfHoldArea*Hull.VolumeScale)*15f,-15f,15f);
        float roll=Hull.Stability.Step(dt,NavalHullStability.Roll(Boat),Mathf.Atan2(starboard-port,10.8f)*Mathf.Rad2Deg);
        float height=(bow+stern+starboard+port)*0.25f-Hull.Stability.Waterline-Hull.SinkDepth;
        verticalSpeed+=((height-p.y)*10f-verticalSpeed*5f)*dt;
        Vector3 next=Boat.position+(forward*speed/(1f+depth)+ram.Drift)*dt*(Hull.Sunk?0f:1f);
        next+=Vector3.up*verticalSpeed*dt;
        Quaternion smoothed=Quaternion.Slerp(Boat.rotation,Quaternion.Euler(pitch,heading,Boat.eulerAngles.z),1f-Mathf.Exp(-4f*dt));
        Quaternion nextRotation=Quaternion.Euler(smoothed.eulerAngles.x,smoothed.eulerAngles.y,roll);
        ram.Velocity=(next-Boat.position)/Mathf.Max(dt,0.001f);ram.AngularRate=Mathf.DeltaAngle(Boat.eulerAngles.y,nextRotation.eulerAngles.y)/Mathf.Max(dt,0.001f);
        if(NavalShipContact.Constrain(Boat.position,Boat.rotation,ref next,ref nextRotation,target,Boat))
        {NavalRamDamage other=target.GetComponent<NavalRamDamage>();ram.Contact(other);ram.PushContact(other);helmNavigator.ShipBlocked();rudder*=.7f;heading=nextRotation.eulerAngles.y;}
        float rigRetention=NavalRigContact.Resolve(ram,Boat.position,Boat.rotation,ref next,ref nextRotation,target.GetComponent<NavalRamDamage>());
        if(rigRetention<1f){speed*=rigRetention;heading=nextRotation.eulerAngles.y;}
        if(NavalWorldMap.Active!=null&&NavalWorldMap.Active.ConstrainShore(Boat,ref next,ref nextRotation))
        {if(new Vector2(next.x-Boat.position.x,next.z-Boat.position.z).sqrMagnitude<.00000001f){speed=0f;ram.StopMomentum();}heading=nextRotation.eulerAngles.y;rudder=0f;helmNavigator.LandBlocked();}
        SpeedThroughWater=NavalShipSpeedMath.Measure(next.x-Boat.position.x,next.z-Boat.position.z,dt);
        Hull.PhysicsBody.SetPose(next,nextRotation);
        if(rider)
        {
            CharacterController body=player.GetComponentInChildren<CharacterController>();
            bool enabled=body!=null&&body.enabled;if(enabled)body.enabled=false;
            player.transform.position=NavalRiderCarry.Position(Boat,riderLocal,riderOffset);
            if(enabled)body.enabled=true;
        }
        NavalPerformance.SyncTransforms();
        crewRoles.Tick(dt,target,NavalTestWorld.Active!=null?NavalTestWorld.Active.ShipVelocity:Vector3.zero,helmUseful,footOffset);
        stations.UpdateRecoil();
    }
    private void OnGUI()
    {
        if(Boat==null)return;
        Camera camera=NavalTestWorld.Active!=null?NavalTestWorld.Active.ViewCamera:null;
        if(camera!=null)for(int i=0;i<Hull.Definition.CrewCount;i++)if(Alive(i))
        {
            Vector3 screen=camera.WorldToScreenPoint(crew[i].transform.position+Vector3.up*2.2f);
            if(screen.z>0f)GUI.Label(new Rect(screen.x-85f,Screen.height-screen.y-20f,190f,25f),crewRoles.Role(i));
        }
    }
    private void OnDestroy()
    {
        if(ocean!=null&&ocean.AdditionalHull==Boat)ocean.AdditionalHull=null;
        foreach(Character sailor in crew)if(sailor!=null)Destroy(sailor.gameObject);
        foreach(Material material in materials)if(material!=null)Destroy(material);
    }
}
