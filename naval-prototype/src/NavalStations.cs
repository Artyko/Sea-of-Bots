using UnityEngine;

public partial class NavalStations : MonoBehaviour
{
    private Transform boat;
    private NavalHull hull;
    private FirstPersonMover player;
    private NavalWater ocean;
    private Transform helm;
    private NavalRepairableMechanism emergencyTiller;
    private bool previousEmergencySteering;
    public bool EmergencySteering {get{return helmMechanism!=null&&!helmMechanism.Functional;}}
    public float SteeringFactor {get{return EmergencySteering?1f/3f:1f;}}
    public Vector3 EmergencyPost {get{return TillerPost;}}
    // Keep the emergency steering post inboard of the tapered stern shell and
    // clear of the steam telegraph. The former port-side point intersected the
    // hull collider in the runtime access audit.
    private Vector3 TillerPost {get{return hull.Definition.EmergencySteeringPost;}}
    private NavalRepairableMechanism helmMechanism;
    private NavalRepairableMechanism[] cannonMechanisms;
    public bool HelmNeedsRepair {get{return helmMechanism!=null&&helmMechanism.NeedsRepair;}}
    private Transform[] guns;
    private Transform[] barrels;
    private float[] yaw;
    private float[] pitch;
    private float[] readyAt;
    private float[] shotAt;
    private System.Collections.Generic.List<NavalHullVoxel>[] mechanisms;
    private Material iron;
    private Material flash;
    private Material smoke;
    private Material wheelAccent;
    private Material shellMaterial;
    private Material impactSpark;
    private AudioClip sound;
    private readonly NavalStationState state = new NavalStationState();
    private int active { get { return state.Active; } } // 0 helm, 1 port, 2 starboard
    public NavalTorpedoBattery Torpedoes;
    public bool PlayerOperatingGunOrHelm {get{return active>=0||playerCapitalSlot>=0;}}
    private int nearest = -1;
    public bool EnemyControlled;
    public bool AlliedAI;
    public bool HostileToPlayer {get{return EnemyControlled&&!AlliedAI;}}
    public bool CrewRepairRetreat;
    private Character firingRobot;
    public bool IsFunctional(int station) { return Functional(station); }
    public int PlayerStation { get { return EnemyControlled?-1:active; } }
    public float LastCrewShotAt {get;private set;}
    public bool RecentlyFired(int gun) { return Time.time-shotAt[gun]<2f; }
    public bool CrewGunAssignable(int station)
    {
        if(station<=0||station>GunCount||PlayerStation==station||!Functional(station))return false;
        if(!NavalCasemateHeelMath.CanOperate(station-1,NavalHullStability.Roll(boat)))return false;
        if(hull.Cargo!=null&&hull.Cargo.AmmoAtGun(station-1)==0)return false;
        var repair=cannonMechanisms!=null?cannonMechanisms[station-1]:null;
        return repair==null||repair.RepairWorker==null&&Time.time>=repair.PlayerRepairUntil;
    }
    public bool CrewGunReady(int station)
    {return CrewGunAssignable(station)&&Time.time>=readyAt[station-1];}
    public int TargetSide(Vector3 point)
    {return boat.InverseTransformPoint(point).x<0f?1:2;}
    public int GunCount {get{return hull.Definition.GunCount;}}
    private int gunAvailabilityRevision=-1,portWorking,starboardWorking;
    private void UpdateGunAvailability()
    {
        if(gunAvailabilityRevision==hull.Revision)return;
        portWorking=starboardWorking=0;
        for(int station=1;station<=GunCount;station++)if(Functional(station))
        {if(hull.Definition.Destroyer){portWorking++;starboardWorking++;}else if(station%2==1)portWorking++;else starboardWorking++;}
        gunAvailabilityRevision=hull.Revision;
    }
    public float ShipScale {get{return hull.WorldScale;}}
    public float ProjectileSpeed {get{return hull.Definition.ProjectileSpeed;}}
    public int PortWorking {get{UpdateGunAvailability();return portWorking;}}
    public int StarboardWorking {get{UpdateGunAvailability();return starboardWorking;}}
    public int WorkingGuns {get{UpdateGunAvailability();return hull.Definition.Destroyer?portWorking:portWorking+starboardWorking;}}
    public int HeelInhibitedGuns {get{int count=0;float roll=NavalHullStability.Roll(boat);for(int gun=0;gun<GunCount;gun++)if(Functional(gun+1)&&!NavalCasemateHeelMath.CanOperate(gun,roll))count++;return count+HeavySideWorking(1);}}
    public int PortCombatWorking {get{int count=0;float roll=NavalHullStability.Roll(boat);for(int gun=0;gun<GunCount;gun+=hull.Definition.Destroyer?1:2)if(Functional(gun+1)&&(hull.Cargo==null||hull.Cargo.AmmoAtGun(gun)>0)&&NavalCasemateHeelMath.CanOperate(gun,roll))count++;return count+HeavySideWorking(1);}}
    public int StarboardCombatWorking {get{int count=0;float roll=NavalHullStability.Roll(boat);for(int gun=hull.Definition.Destroyer?0:1;gun<GunCount;gun+=hull.Definition.Destroyer?1:2)if(Functional(gun+1)&&(hull.Cargo==null||hull.Cargo.AmmoAtGun(gun)>0)&&NavalCasemateHeelMath.CanOperate(gun,roll))count++;return count+HeavySideWorking(2);}}
    public float CombatOrbitSign(float preferred)
    {UpdateGunAvailability();return NavalBroadsideMath.OrbitSign(portWorking,starboardWorking,preferred);}
    private struct BearingMemo {public int Frame,Revision,Station;public Vector3 Point,Muzzle;public Quaternion Rotation;public bool Result;}
    private const int BearingEntriesPerGun=4;
    private readonly BearingMemo[] bearingMemo=new BearingMemo[32*BearingEntriesPerGun];
    private readonly int[] bearingCursor=new int[32];
    public bool CanBearOn(int station,Vector3 point)
    {
        if(station<=0||station>GunCount||!Functional(station))return false;
        if(!NavalCasemateHeelMath.CanOperate(station-1,NavalHullStability.Roll(boat)))return false;
        Vector3 muzzle=barrels[station-1].TransformPoint(new Vector3(0,0,hull.Definition.GunMuzzleZ));Quaternion rotation=boat.rotation;
        int gun=station-1,first=gun*BearingEntriesPerGun;
        for(int i=first;i<first+BearingEntriesPerGun;i++){
            var m=bearingMemo[i];if(m.Frame==Time.frameCount&&m.Revision==hull.Revision&&m.Station==station&&m.Point.Equals(point)&&m.Muzzle.Equals(muzzle)&&m.Rotation.Equals(rotation))return m.Result;
        }
        float yawAngle,elevation;bool result=TryAimSolution(station-1,point,out yawAngle,out elevation);
        bearingMemo[first+bearingCursor[gun]]=new BearingMemo{Frame=Time.frameCount,Revision=hull.Revision,Station=station,Point=point,Muzzle=muzzle,Rotation=rotation,Result=result};bearingCursor[gun]=(bearingCursor[gun]+1)%BearingEntriesPerGun;
        return result;
    }
    public bool CanBearOnShip(int station,Transform target)
    {
        if(target==null)return false;
        NavalStations other=ForShip(target);
        Vector3 deck=other!=null?target.TransformPoint(new Vector3(0,other.hull.Definition.WeatherDeck+.3f/other.hull.WorldScale,0)):target.position;
        return CanBearOn(station,deck)||CanBearOn(station,target.position);
    }
    public int FiringSideToward(Vector3 point)
    {
        int port=0,starboard=0;
        for(int station=1;station<=GunCount;station++)if(CanBearOn(station,point))
        {if(hull.Definition.Destroyer){port++;starboard++;}else if(station%2==1)port++;else starboard++;}
        if(port==0&&starboard==0)return 0;
        return NavalBroadsideMath.CombatSide(TargetSide(point),port,starboard);
    }
    public int FiringSideAtShip(Transform target)
    {
        if(target==null)return 0;
        NavalStations other=ForShip(target);
        Vector3 deck=other!=null?target.TransformPoint(new Vector3(0,other.hull.Definition.WeatherDeck+.3f/other.hull.WorldScale,0)):target.position;
        int port=0,starboard=0;
        for(int station=1;station<=GunCount;station++)if(CanBearOn(station,deck)||CanBearOn(station,target.position))
        {if(hull.Definition.Destroyer){port++;starboard++;}else if(station%2==1)port++;else starboard++;}
        return port+starboard==0?0:NavalBroadsideMath.CombatSide(TargetSide(target.position),port,starboard);
    }
    public int CombatSideToward(Vector3 point)
    {
        UpdateGunAvailability();int firing=FiringSideToward(point);
        return firing!=0?firing:NavalBroadsideMath.CombatSide(TargetSide(point),portWorking,starboardWorking);
    }
    public void PauseInput(){ForwardInput=0f;TurnInput=0f;}
    public bool AllGunsBroken {get{return WorkingGuns==0&&!MainBatteryCombatAvailable&&(Torpedoes==null||Torpedoes.Rounds==0);}}
    public bool CanRestoreGun
    {
        get{if(MainBatteryCanRestore)return true;if(cannonMechanisms!=null)for(int i=0;i<cannonMechanisms.Length;i++)
            if(!Functional(i+1)&&cannonMechanisms[i].NeedsRepair&&cannonMechanisms[i].RecoverableWithParts)return true;return false;}
    }
    // One kit is usable per gun over its lifetime; no separate finite crate inventory exists.
    public int AvailableCannonKits
    {
        get{int count=MainBatteryCanRestore?1:0;if(cannonMechanisms!=null)foreach(var mechanism in cannonMechanisms)
            if(mechanism!=null&&!mechanism.PermanentlyDestroyed&&!mechanism.RepairUsed&&mechanism.RecoverableWithParts)count++;
            return count;}
    }
    public void AmmunitionDetonated(int gun)
    {
        if(cannonMechanisms==null||gun<0||gun>=cannonMechanisms.Length||cannonMechanisms[gun]==null)return;
        cannonMechanisms[gun].CatastrophicDestroy();gunAvailabilityRevision=-1;
    }
    public void MainMagazineDetonated(int slot)
    {
        if(capitalTurrets!=null&&slot>=0&&slot<capitalTurrets.Length){capitalTurrets[slot].Ammo=0;capitalTurrets[slot].SecondShot=0;capitalTurrets[slot].Mechanism.CatastrophicDestroy();}
        if(mainBatteryMechanism!=null)mainBatteryMechanism.CatastrophicDestroy();
    }
    // Steam captains withdraw on weapon loss; sail-era last-attack behavior remains available.
    public bool MustBoard {get{return !hull.Definition.SteamPowered&&AllGunsBroken&&!CanRestoreGun;}}
    public Vector3 Post(int station)
    {return station==SailStation?SailPost:station==0?(EmergencySteering?TillerPost:new Vector3(0f,hull.Definition.HelmDeck,NavalShipDefinition.HelmZ(hull.Definition))):hull.Definition.Destroyer&&openShields!=null&&openShields[station-1]!=null?hull.Definition.GunPosition(station-1)+openShields[station-1].localRotation*new Vector3(0,NavalGunStationLayout.OperatorHeightOffset,-NavalGunStationLayout.RearDistance)/hull.WorldScale:hull.Definition.GunOperator(station-1);}
    public Vector3 CrewPosition(int station){return boat.TransformPoint(Post(station));}
    public Vector3 CrewForward(int station) { return station==0||station==SailStation?boat.forward:barrels[station-1].forward; }
    public bool AimAndFire(int gun,Vector3 target,float dt,Character sailor)
    {
        if(PlayerStation==gun+1)return false;
        if(!Functional(gun+1)||sailor==null||!sailor.IsAlive())return false;
        if(!NavalCasemateHeelMath.CanOperate(gun,NavalHullStability.Roll(boat)))return false;
        float wantedYaw,angle;
        if(!TryAimSolution(gun,target,out wantedYaw,out angle))return false;
        yaw[gun]=Mathf.MoveTowards(yaw[gun],wantedYaw,28f*dt);
        pitch[gun]=Mathf.MoveTowards(pitch[gun],angle,24f*dt);Aim(gun);
        if(Time.time>=readyAt[gun]&&Mathf.Abs(yaw[gun]-wantedYaw)<0.6f&&Mathf.Abs(pitch[gun]-angle)<0.6f)
        { firingRobot=sailor;bool fired=Fire(gun);firingRobot=null;return fired; }
        return false;
    }
    public bool Operating { get { return active >= 0||playerCapitalSlot>=0||Torpedoes!=null&&Torpedoes.PlayerOperating; } }
    public bool AtHelm { get { return active == 0; } }
    public float ForwardInput { get; private set; }
    public float TurnInput { get; private set; }
    public Vector3 OperatorPosition {get{return playerCapitalSlot>=0?CapitalPlayerPosition(playerCapitalSlot):Torpedoes!=null&&Torpedoes.PlayerOperating?Torpedoes.PlayerPosition:CrewPosition(active);}}
    public void Initialize(Transform ship, NavalHull owner, FirstPersonMover robot, NavalWater sea,
        Material wood, Material trim, Material black)
    {
        boat = ship; RegisterShip(); hull = owner; player = robot; ocean = sea; iron = black;
        guns=new Transform[GunCount];barrels=new Transform[GunCount];yaw=new float[GunCount];pitch=new float[GunCount];readyAt=new float[GunCount];shotAt=new float[GunCount];
        for(int i=0;i<GunCount;i++)shotAt[i]=-100f;
        mechanisms=new System.Collections.Generic.List<NavalHullVoxel>[GunCount+1];InitializeGunnery(GunCount);
        flash = MakeMaterial(new Color(1f, 0.65f, 0.08f));
        smoke = MakeMaterial(new Color(0.55f, 0.58f, 0.60f));
        wheelAccent = MakeMaterial(new Color(0.58f, 0.405f, 0.225f));
        shellMaterial = MakeMaterial(new Color(0.30f,0.24f,0.10f));
        impactSpark = MakeMaterial(new Color(1f,.62f,.16f));
        Vector3 helmPost=Post(0);
        // Keep the spokes outside the raised helmsman's standing capsule.
        helm = Root("ShipWheel",hull.Definition.SteamPowered?helmPost+new Vector3(0,NavalSteamLayout.WheelHeight(hull.Definition),NavalSteamLayout.WheelForward(hull.Definition)):
            new Vector3(0f,hull.Definition.AboveDeckY(1.4f)+hull.Definition.UpperDeckOffset,NavalShipDefinition.HelmZ(hull.Definition)+.8f*hull.Definition.FixtureScale));
        helm.localScale=Vector3.one*hull.Definition.FixtureScale;
        int helmFirst=hull.Voxels.Count;Material helmMetal=hull.Definition.SteamPowered?iron:wood;
        hull.AddDecorationBox(helm,helmMetal,new Vector3(0f,-0.3f,0f),new Vector3(0.2f,0.6f,0.25f),true);
        for (int i = 0; i < 12; i++)
        {
            float angle = i * Mathf.PI / 6f;
            hull.AddDecorationBox(helm,hull.Definition.SteamPowered?iron:wheelAccent,new Vector3(Mathf.Sin(angle)*0.4f,Mathf.Cos(angle)*0.4f,0f),
                new Vector3(0.17f, 0.17f, 0.14f), true);
        }
        hull.AddDecorationBox(helm,helmMetal,Vector3.zero,new Vector3(0.75f,0.10f,0.12f),true);
        hull.AddDecorationBox(helm,helmMetal,Vector3.zero,new Vector3(0.10f,0.75f,0.12f),true);
        hull.AddDecorationBox(helm, wheelAccent, new Vector3(0f, 0f, -0.09f), new Vector3(0.22f, 0.22f, 0.12f), true);
        if(hull.Definition.SteamPowered)for(int cell=helmFirst;cell<hull.Voxels.Count;cell++)
        {hull.Voxels[cell].Metallic=true;hull.Voxels[cell].Flammable=false;}
        for (int i = 0; i < GunCount; i++)
        {
            float side = NavalShipDefinition.GunSide(i);
            float gunZ=NavalShipDefinition.GunZ(hull.Definition,i);
            guns[i] = Root((side<0f?"PortCannon":"StarboardCannon")+i, hull.Definition.GunPosition(i));
            guns[i].localScale=Vector3.one*hull.Definition.GunFixtureScale;
            barrels[i] = new GameObject("CannonBarrel").transform;
            barrels[i].SetParent(guns[i], false); barrels[i].localPosition = new Vector3(0f, hull.Definition.GunAxisHeight, 0f);
            if(hull.Definition.Destroyer)
            {
                hull.AddDecorationBox(guns[i],black,new Vector3(0,NavalGunSupportGeometry.Centre(hull.Definition.GunAxisHeight),0),new Vector3(.55f,NavalGunSupportGeometry.Height(hull.Definition.GunAxisHeight),.65f),true);
                CalibratedTube(barrels[i],black,trim,NavalGunDimensions.DestroyerCalibre,1f);
                hull.AddDecorationBox(barrels[i],trim,new Vector3(0,0,-.12f),new Vector3(.44f,.38f,.5f),true);
                BuildOpenShield(i,black,trim);
            }
            else if(hull.Definition.RifledWeapons)
            {
                // The old pedestal ended above the deck and the shield was a
                // child of the elevating barrel, so the whole casemate appeared
                // to float.  A fixed armoured trunk now carries the mounting
                // down to the weather deck.
                float fixture=hull.Definition.GunFixtureScale;
                float foundationTop=hull.Definition.GunMountY-.23f*fixture;
                float foundationHeight=Mathf.Max(.05f,foundationTop-hull.Definition.WeatherDeck+.02f);
                ArmoredDecoration(boat,black,new Vector3(side*hull.Definition.GunMountX,hull.Definition.WeatherDeck+foundationHeight*.5f-.01f,gunZ),
                    new Vector3(.42f,foundationHeight,.46f),false);
                // Iron pedestal, traversing ring, recoil slide and long jacketed
                // tube: no wooden truck carriage or muzzle-loading wheels.
                // The low plinth is visually solid but lies inside the only
                // weather-deck bypass. Its old box collider reached x=1.89
                // and repeatedly stopped full-size sailors at z=-2.79.
                // Shields, breech and barrel remain physical/damageable.
                hull.AddDecorationBox(guns[i],black,new Vector3(0,-.08f,0),new Vector3(.72f,.30f,.82f),false);
                hull.AddDecorationBox(guns[i],trim,new Vector3(0,.09f,0),new Vector3(.88f,.10f,.90f),false);
                CalibratedTube(barrels[i],black,trim,NavalGunDimensions.CasemateCalibre,hull.WorldScale*hull.Definition.GunFixtureScale);
                for(int recoil=-1;recoil<=1;recoil+=2)
                    hull.AddDecorationBox(barrels[i],trim,new Vector3(recoil*.30f,-.20f,.55f),new Vector3(.12f,.12f,1.05f),true);
                // Fixed open-backed casemate shield.  It is carried by the
                // mounting trunk, does not pitch with the barrel and reaches
                // the pedestal instead of reading as four window panes.
                ArmoredDecoration(guns[i],black,new Vector3(side*.46f,.20f,0),new Vector3(.14f,1.02f,1.48f),true);
                ArmoredDecoration(guns[i],black,new Vector3(side*.18f,.18f,-.66f),new Vector3(.58f,.86f,.14f),true);
                ArmoredDecoration(guns[i],black,new Vector3(side*.18f,.18f,.66f),new Vector3(.58f,.86f,.14f),true);
                ArmoredDecoration(guns[i],black,new Vector3(side*.20f,-.27f,0),new Vector3(.54f,.22f,1.48f),true);
                ArmoredDecoration(guns[i],trim,new Vector3(side*.19f,.72f,0),new Vector3(.70f,.12f,1.58f),true);
            }
            else
            {
                hull.AddDecorationBox(guns[i], wood, new Vector3(0f, -0.03f, 0f), new Vector3(0.75f, 0.3f, 0.9f), true);
                for (int wheel = -1; wheel <= 1; wheel += 2)
                    hull.AddDecorationBox(guns[i], black, new Vector3(wheel * 0.4f, -0.05f, 0f), new Vector3(0.15f, 0.35f, 0.65f), true);
                hull.AddDecorationBox(barrels[i], black, new Vector3(0f, 0f, 0.55f), new Vector3(0.38f, 0.38f, 1.3f), true);
                hull.AddDecorationBox(barrels[i], trim, new Vector3(0f, 0f, 0.9f), new Vector3(0.42f, 0.42f, 0.1f), true);
            }
            pitch[i] = 5f; Aim(i);
        }
        Vector3 cleat=SailPost+(SteamPowered?NavalSteamLayout.PedestalOffset(hull.WorldScale):new Vector3(NavalSailWorkLayout.CleatOffset,.45f,0)/hull.WorldScale);
        hull.AddDecorationBox(boat,SteamPowered?black:wood,cleat,new Vector3(SteamPowered?.24f:.16f,.9f,SteamPowered?.24f:.16f)/hull.WorldScale,!SteamPowered);
        hull.AddDecorationBox(boat,trim,cleat+Vector3.up*(.3f/hull.WorldScale),new Vector3(SteamPowered?.72f:.6f,SteamPowered?.26f:.14f,SteamPowered?.38f:.18f)/hull.WorldScale,!SteamPowered);
        if(SteamPowered)
        {
            // Visual-only telegraph: its former solid pedestal overlapped the
            // 0.8 m sailor capsule and made every valid route fail at the last step.
            hull.AddDecorationBox(boat,black,cleat+new Vector3(0,.42f,-.08f)/hull.WorldScale,new Vector3(.10f,.48f,.10f)/hull.WorldScale,false);
            hull.AddDecorationBox(boat,trim,cleat+new Vector3(0,.66f,-.08f)/hull.WorldScale,new Vector3(.22f,.16f,.16f)/hull.WorldScale,false);
        }
        sound = AudioClip.Create("NavalCannonBoom", 11025, 1, 22050, false);
        float[] samples = new float[11025]; System.Random random = new System.Random(14);
        for (int i = 0; i < samples.Length; i++)
        {
            float t = i / 22050f;
            samples[i] = ((float)random.NextDouble() * 2f - 1f) * Mathf.Exp(-t * 14f) * 0.5f +
                Mathf.Sin(t * 2f * Mathf.PI * (65f - t * 40f)) * Mathf.Exp(-t * 9f) * 0.45f;
        }
        sound.SetData(samples, 0);
        for (int i = 0; i <= GunCount; i++)
        {
            mechanisms[i] = new System.Collections.Generic.List<NavalHullVoxel>();
            Transform mechanism = i == 0 ? helm : barrels[i - 1];
            foreach (NavalHullVoxel voxel in hull.Voxels)
                if (voxel.transform.IsChildOf(mechanism)) mechanisms[i].Add(voxel);
        }
        helmMechanism=new NavalRepairableMechanism(hull,mechanisms[0],false);
        var tillerCells=new System.Collections.Generic.List<NavalHullVoxel>();
        int tillerFirst=hull.Voxels.Count;
        Vector3 tillerBase=TillerPost+new Vector3(0,0,-1.3f/hull.WorldScale);
        hull.AddDecorationBox(hull.Root,iron,tillerBase+Vector3.up*(.45f/hull.WorldScale),new Vector3(.22f,.9f,.22f)/hull.WorldScale,true);
        hull.AddDecorationBox(hull.Root,wood,tillerBase+Vector3.up*(.85f/hull.WorldScale),new Vector3(1.1f,.18f,.2f)/hull.WorldScale,true);
        for(int cell=tillerFirst;cell<hull.Voxels.Count;cell++)tillerCells.Add(hull.Voxels[cell]);
        emergencyTiller=new NavalRepairableMechanism(hull,tillerCells,false);
        cannonMechanisms=new NavalRepairableMechanism[GunCount];
        for(int gun=0;gun<GunCount;gun++)
        {
            var members=new System.Collections.Generic.List<NavalHullVoxel>();
            foreach(var cell in hull.Voxels)if(cell.transform.IsChildOf(guns[gun]))members.Add(cell);
            if(hull.Definition.RifledWeapons)foreach(var cell in members)cell.Metallic=true;
            var repair=new NavalRepairableMechanism(hull,members,false,true);cannonMechanisms[gun]=repair;
            repair.RepairPoint=hull.Definition.GunPosition(gun)+Vector3.up*((hull.Definition.GunAxisHeight)*hull.Definition.GunFixtureScale);
        }
        InitializeMainBattery(black,trim);
        if(hull.Definition.Destroyer){Torpedoes=boat.gameObject.AddComponent<NavalTorpedoBattery>();Torpedoes.Initialize(hull,this,player,ocean,black,trim);}
    }
    private Transform Root(string name, Vector3 position)
    {
        Transform root = new GameObject("NavalPrototype_" + name).transform;
        root.SetParent(boat, false); root.localPosition = position; return root;
    }
    public void Tick(float dt, float feetOffset, bool sunk, bool swimming)
    {
        if(active==0&&previousEmergencySteering!=EmergencySteering)Exit();
        previousEmergencySteering=EmergencySteering;
        if(HandleCapitalInput(dt,feetOffset,sunk||swimming)){ForwardInput=TurnInput=0;UpdateRecoil();return;}
        if(Torpedoes!=null&&Torpedoes.HandleInput(dt,feetOffset)){ForwardInput=TurnInput=0;UpdateRecoil();return;}
        if (sunk || swimming || (active >= 0 && !Functional(active))) Exit();
        nearest = -1; float distance = 3f;
        if (active < 0 && !sunk && !swimming)
        {
            Vector3 feet = player.transform.position - Vector3.up * feetOffset;
            for (int i = 0; i <= SailStation; i++)
            {
                Vector3 anchor = CrewPosition(i);
                float d = Vector3.Distance(feet, anchor);
                if (d < distance) { distance = d; nearest = i; }
            }
        }
        if (Input.GetKeyDown(KeyCode.E)&&!(hull.Bilge!=null&&hull.Bilge.PlayerInteracting))
        {
            state.PressInteract(nearest, sunk || swimming || (nearest >= 0 && !Functional(nearest)),SailStation);
            if(active==SailStation)sailControl.Adjust(0,0);
            if (active >= 0)
            {
                Vector3 facing = active == 0 || active == SailStation ? boat.forward : barrels[active - 1].forward;
                facing.y = 0f;
                if (facing.sqrMagnitude > 0.001f) player.transform.rotation = Quaternion.LookRotation(facing);
            }
        }
        ForwardInput = 0f; TurnInput = 0f;
        if (active == 0)
        {
            TurnInput = (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1f : 0f) -
                (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1f : 0f);
            if(SteamPowered)
            {
                float telegraphInput=(Input.GetKey(KeyCode.W)?1f:0f)-(Input.GetKey(KeyCode.S)?1f:0f);
                if(telegraphInput!=0)sailControl.Adjust(telegraphInput,dt);
                if(Input.GetKeyDown(KeyCode.C))sailControl.Auto();
            }
        }
        else if(active==SailStation)
        {
            float sailInput=(Input.GetKey(KeyCode.W)?1f:0f)-(Input.GetKey(KeyCode.S)?1f:0f);
            if(sailInput!=0)sailControl.Adjust(sailInput,dt);
            if(Input.GetKeyDown(KeyCode.C))sailControl.Auto();
        }
        else if (active > 0)
        {
            int gun = active - 1;
            yaw[gun] = Mathf.Clamp(yaw[gun] + ((Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1f : 0f) -
                (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1f : 0f)) * 28f * dt, -hull.Definition.GunTraverse, hull.Definition.GunTraverse);
            pitch[gun] = Mathf.Clamp(pitch[gun] + ((Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1f : 0f) -
                (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1f : 0f)) * 24f * dt, NavalGunneryMath.MinimumPitch, NavalGunneryMath.MaximumPitch);
            Aim(gun);
            if ((Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space)) && Time.time >= readyAt[gun]) Fire(gun);
        }
        UpdateRecoil();
    }
    public void UpdateRecoil()
    {
        for (int i = 0; i < GunCount; i++)
        {
            float recoil = Mathf.Clamp01(1f-(Time.time-shotAt[i])/0.35f) * 0.14f;
            barrels[i].localPosition = new Vector3(0f, hull.Definition.GunAxisHeight, 0f) - barrels[i].localRotation * Vector3.forward * recoil;
        }
    }
    private void Aim(int gun) {var rotation=Quaternion.Euler(-pitch[gun],hull.Definition.GunBaseYaw(gun)+yaw[gun],0);if(hull.Definition.Destroyer){ApplyOpenGunAim(gun,rotation);if(openShields!=null&&openShields[gun]!=null)openShields[gun].localRotation=Quaternion.Euler(0,hull.Definition.GunBaseYaw(gun)+yaw[gun],0);}else barrels[gun].localRotation=rotation;}
    private int functionalRevision=-1;
    private uint checkedStations,workingStations;

    private bool Functional(int station)
    {
        if(station==SailStation)return !hull.Sunk;
        if(station==0&&helmMechanism!=null)return helmMechanism.Functional||emergencyTiller!=null&&emergencyTiller.Functional;
        if (mechanisms==null||station<0||station>=mechanisms.Length||mechanisms[station] == null) return false;
        if(functionalRevision!=hull.Revision)
        {
            functionalRevision=hull.Revision;
            checkedStations=workingStations=0;
        }
        uint bit=1u<<station;
        if((checkedStations&bit)!=0)return (workingStations&bit)!=0;
        checkedStations|=bit;
        if(mechanisms[station].Count==0)return false;
        foreach(NavalHullVoxel voxel in mechanisms[station])
            if(voxel==null||voxel.Missing||voxel.Detached)return false;
        workingStations|=bit;
        return true;
    }
    private bool Fire(int gun)
    {
        if (!Functional(gun + 1)) return false;
        if(hull.Definition.Destroyer&&!ClearOwnGunLine(gun))return false;
        float reloadFactor=NavalCasemateHeelMath.ReloadFactor(gun,NavalHullStability.Roll(boat));if(reloadFactor<=0f){gunStatus[gun]="heel inhibits fire";return false;}
        if(hull.Cargo!=null&&!hull.Cargo.Fire(gun)){gunStatus[gun]="no ammunition at gun";return false;}
        shotAt[gun]=Time.time;
        readyAt[gun] = Time.time + NavalGunneryMath.ReloadSeconds/reloadFactor;
        if(firingRobot!=null)LastCrewShotAt=Time.time;
        Vector3 muzzle = barrels[gun].TransformPoint(new Vector3(0f, 0f, hull.Definition.GunMuzzleZ));
        Vector3 direction = ShotDirection(gun,barrels[gun].forward);
        float heelSpread=NavalCasemateHeelMath.AimSpread(NavalHullStability.Roll(boat));
        if(heelSpread>0f)
        {
            Vector2 error=Random.insideUnitCircle*heelSpread;Vector3 right=Vector3.Cross(Vector3.up,direction).normalized;
            direction=Quaternion.AngleAxis(error.x,Vector3.up)*Quaternion.AngleAxis(error.y,right)*direction;
        }
        GameObject ball = hull.Definition.RifledWeapons?NavalProjectileVisual.CreateRifledShell():GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ball.name = hull.Definition.RifledWeapons?"NavalPrototype_RifledShell":"NavalPrototype_Cannonball";
        // World-space ballistics must not inherit the firing vessel's later motion.
        ball.transform.SetParent(NavalTestWorld.Active!=null?NavalTestWorld.Active.transform:null, false);
        ball.transform.position = muzzle;ball.transform.rotation=Quaternion.LookRotation(direction);
        ball.transform.localScale = hull.Definition.RifledWeapons?Vector3.one:Vector3.one*.3f;
        Collider collider = ball.GetComponent<Collider>(); if(collider!=null){collider.enabled = false; Destroy(collider);}
        ball.GetComponent<Renderer>().sharedMaterial = hull.Definition.RifledWeapons?shellMaterial:iron;
        ball.AddComponent<NavalCannonball>().Initialize(direction * hull.Definition.ProjectileSpeed+FiringPlatformVelocity(), boat, hull, player, ocean, smoke,impactSpark, Time.time + 10f,HostileToPlayer,firingRobot,hull.Definition.ProjectilePower,1f,hull.Definition.RifledWeapons);
        AudioSource.PlayClipAtPoint(sound, muzzle, 0.7f);
        NavalShotPuff.Create(transform, muzzle, direction * 2f, flash, 0.07f, 0.38f);
        GameObject muzzleGlow=new GameObject("Naval muzzle flash");muzzleGlow.transform.position=muzzle+direction*.25f;
        Light lamp=muzzleGlow.AddComponent<Light>();lamp.type=LightType.Point;lamp.color=new Color(1f,.72f,.38f);lamp.intensity=.85f;lamp.range=4f;lamp.shadows=LightShadows.None;Destroy(muzzleGlow,.07f);
        for (int i = 0; i < 20; i++)
            NavalShotPuff.Create(transform, muzzle + Random.insideUnitSphere * 0.18f,
                direction * Random.Range(0.7f, 2.6f) + Vector3.up * Random.Range(0.6f, 1.3f) + Random.insideUnitSphere * 0.7f,
                smoke, Random.Range(1.1f, 2.1f), Random.Range(0.16f, 0.34f));
        Debug.Log("[NavalPrototype] Cannon fired: "+boat.name+" "+hull.Definition.Name+" gun="+(gun+1)+" "+(gun%2==0?"port":"starboard")+" pitch="+pitch[gun].ToString("F1"));
        return true;
    }
    public void Exit() { playerCapitalSlot=-1; if(Torpedoes!=null)Torpedoes.Exit();state.Exit(); ForwardInput = 0f; TurnInput = 0f; }
    private static Material MakeMaterial(Color color)
    {
        Shader shader = Shader.Find("Unlit/Color"); if (shader == null) shader = Shader.Find("Standard");
        Material material = new Material(shader); material.color = color; return material;
    }
    private void OnGUI()
    {
        DrawReloadBars();
        DrawRangefinder();
        if(DrawCapitalControls())return;
        if(Torpedoes!=null&&Torpedoes.PlayerOperating)return;
        if(EnemyControlled||NavalTestWorld.Active!=null&&NavalTestWorld.Active.Spectating)return;
        string text = active == 0 ? (EmergencySteering?"АВАРИЙНЫЙ РУМПЕЛЬ (поворот ×⅓): A/D":SteamPowered?"ШТУРВАЛ + ТЕЛЕГРАФ: A/D — курс; W/S — ход; C — авто":"ШТУРВАЛ: A/D — поворот")+"; E — выйти." :
            (active==SailStation?(SteamPowered?"МАШИННЫЙ ТЕЛЕГРАФ":"ПАРУСА")+": W — добавить ход; S — убрать / задний ход; C — авто; E — выйти\nПоложение: "+(SailSetting*100).ToString("F0")+"%":active > 0 ? "CANNON: A/D = aim +/-"+hull.Definition.GunTraverse+"; W/S = elevation; LMB/Space = fire; E = leave\nReload: " +
                Mathf.Max(0f, readyAt[active - 1] - Time.time).ToString("F1") + " s" :
                (nearest >= 0 ? "Press E: " + (nearest==SailStation?PropulsionName:nearest == 0 ? (EmergencySteering?"аварийный румпель в корме трюма":"ship wheel") : ((nearest-1)%2==0?"port cannon ":"starboard cannon ")+((nearest-1)/2+1)) :
                    "Approach a cannon or the stern wheel and press E"));
        if(active < 0 && nearest >= 0 && !Functional(nearest))
            text = (nearest == 0 ? "HELM DAMAGED — R: repair" : cannonMechanisms!=null&&nearest-1<cannonMechanisms.Length&&cannonMechanisms[nearest-1].PermanentlyDestroyed?
                "ОРУДИЕ УНИЧТОЖЕНО ВЗРЫВОМ БОЕЗАПАСА — ремонту не подлежит":"ПУШКА ПОВРЕЖДЕНА — R: ремонт 60 с, один раз");
        if(active<0&&nearest==0&&!Functional(0)&&helmMechanism!=null&&helmMechanism.Destroyed)
            text="Штурвал разрушен — восстановление невозможно";
        if(active<0&&nearest<0)return;
        GUI.Box(new Rect(Screen.width * 0.5f - 350f, Screen.height - 90f, 700f, 70f), text);
    }
    private void ArmoredDecoration(Transform parent,Material material,Vector3 centre,Vector3 size,bool solid)
    {
        int first=hull.Voxels.Count;hull.AddDecorationBox(parent,material,centre,size,solid);
        for(int i=first;i<hull.Voxels.Count;i++){hull.Voxels[i].Armored=true;hull.Voxels[i].Metallic=true;hull.Voxels[i].Flammable=false;}
    }
    private void OnDestroy()
    { UnregisterShip(); Exit(); if (flash != null) Destroy(flash); if (smoke != null) Destroy(smoke); if (wheelAccent != null) Destroy(wheelAccent); if(shellMaterial!=null)Destroy(shellMaterial);if(impactSpark!=null)Destroy(impactSpark); if (sound != null) Destroy(sound); }
}

public static class NavalProjectileVisual
{
    private static Mesh rifledShell;
    public static GameObject CreateRifledShell()
    {
        if(rifledShell==null)BuildRifledShell();
        GameObject shell=new GameObject("NavalPrototype_RifledShell");
        shell.AddComponent<MeshFilter>().sharedMesh=rifledShell;shell.AddComponent<MeshRenderer>();
        return shell;
    }
    private static void BuildRifledShell()
    {
        const int sides=8,rings=3;float[] z={-.30f,.16f,.29f};float[] radius={.115f,.115f,.075f};
        Vector3[] vertices=new Vector3[2+rings*sides];int rear=0,nose=vertices.Length-1;vertices[rear]=new Vector3(0,0,z[0]);vertices[nose]=new Vector3(0,0,.43f);
        for(int ring=0;ring<rings;ring++)for(int side=0;side<sides;side++){
            float angle=side*Mathf.PI*2f/sides;vertices[1+ring*sides+side]=new Vector3(Mathf.Cos(angle)*radius[ring],Mathf.Sin(angle)*radius[ring],z[ring]);
        }
        int[] triangles=new int[sides*3*2+sides*(rings-1)*6];int t=0;
        for(int side=0;side<sides;side++){int next=(side+1)%sides;triangles[t++]=rear;triangles[t++]=1+next;triangles[t++]=1+side;}
        for(int ring=0;ring<rings-1;ring++)for(int side=0;side<sides;side++){
            int next=(side+1)%sides,a=1+ring*sides+side,b=1+ring*sides+next,c=1+(ring+1)*sides+side,d=1+(ring+1)*sides+next;
            triangles[t++]=a;triangles[t++]=b;triangles[t++]=d;triangles[t++]=a;triangles[t++]=d;triangles[t++]=c;
        }
        int last=1+(rings-1)*sides;for(int side=0;side<sides;side++){int next=(side+1)%sides;triangles[t++]=last+side;triangles[t++]=last+next;triangles[t++]=nose;}
        rifledShell=new Mesh();rifledShell.name="Naval rifled shell ogive";rifledShell.vertices=vertices;rifledShell.triangles=triangles;rifledShell.RecalculateNormals();rifledShell.RecalculateBounds();
    }
}

public class NavalCannonball : MonoBehaviour
{
    public static readonly System.Collections.Generic.List<NavalCannonball> Active=new System.Collections.Generic.List<NavalCannonball>();
    public Vector3 ObservedVelocity {get{return velocity;}}public Transform Source {get{return boat;}}public bool InWater {get{return submerged;}}
    private Vector3 velocity;
    private bool submerged;
    private float underwaterTravel;
    private float UnderwaterPowerScale {get{return NavalProjectileWaterMath.ImpactPowerScale(projectilePower);}}
    private float RadiusScale {get{return projectileRadiusScale*(submerged?NavalProjectileWaterMath.ImpactRadiusScale(projectilePower):1f);}}
    private int ImpactStrength {get{return Mathf.RoundToInt(4f*projectilePower*(submerged?UnderwaterPowerScale:1f));}}
    private Transform boat;
    private NavalHull hull;
    private FirstPersonMover owner;
    private NavalWater ocean;
    private Material splash;
    private Material metalSpark;
    private static int nextAttackId = 1800000000;
    private int attackId;
    private float expires;
    private bool hostile;
    private Character attacker;
    private float projectilePower=1f;
    private float projectileRadiusScale=1f;
    private bool internalArmorPiercingBurst;
    private bool firstMetalContact,firstMetalShell;
    private NavalHull firstMetalHull;
    private Vector3 firstMetalPoint,firstMetalNormal;
    private float firstMetalTravel;

    private float travelled;
    private NavalHull armedHull;
    private bool waterStopped;
    private float armedDetonateAt,armedResidual,armedTravelled,armedStepOffset;
    private readonly System.Collections.Generic.HashSet<NavalHullVoxel> penetratedCells=new System.Collections.Generic.HashSet<NavalHullVoxel>();
    private bool convoyHitRecorded;
    public void Initialize(Vector3 speed, Transform ship, NavalHull shell, FirstPersonMover player, NavalWater sea, Material material,Material sparks, float end,bool enemy=false,Character robot=null,float power=1f,float radiusScale=1f,bool armorPiercingBurst=false)
    { var mission=NavalTestWorld.Active;if(mission!=null)mission.RecordConvoyShot(ship,false,false); Active.Add(this);velocity = speed; boat = ship; hull = shell; owner = player; ocean = sea; splash = material;metalSpark=sparks; expires = end; attackId = nextAttackId++; hostile=enemy;attacker=robot!=null?robot:player;projectilePower=Mathf.Max(.1f,power);projectileRadiusScale=Mathf.Clamp(radiusScale,1f,3f);internalArmorPiercingBurst=armorPiercingBurst; }
    private void Update()
    {
        if (boat == null) { Destroy(gameObject); return; }
        if(armedHull!=null&&Time.time>=armedDetonateAt){DetonateArmed(transform.position,"two-second fuse");return;}
        if(armedHull==null&&Time.time>=expires)
        {
            Destroy(gameObject);
            return;
        }
        NavalTestWorld world=NavalTestWorld.Active;
        if (Time.timeScale == 0f || (world!=null?!world.SimulationActive:owner==null||!owner.IsPlayerInputEnabled())) return;
        if(waterStopped)return;
        float remaining=Mathf.Min(Time.deltaTime,0.05f);
        float clearance=submerged?0f:WaterClearance(transform.position);
        while(remaining>0.00001f)
        {
            float dt=Mathf.Min(remaining,0.008f);
            velocity+=Vector3.down*9.81f*dt;
            if(submerged)velocity*=NavalProjectileWaterMath.Damping(dt,projectilePower);
            if(velocity.sqrMagnitude>.01f)transform.rotation=Quaternion.LookRotation(velocity.normalized,Vector3.up);
            Vector3 step=velocity*dt;
            bool armedAtStepStart=armedHull!=null;
            Vector3 start=transform.position,next=start+step;
            float nextClearance=submerged?0f:WaterClearance(next);
            if(!submerged&&NavalProjectileWaterMath.EntersWater(clearance,nextClearance))
            {
                float low=0f,high=1f;
                if(clearance>0f)
                    for(int i=0;i<10;i++)
                    {
                        float middle=(low+high)*0.5f;Vector3 p=start+step*middle;
                        if(WaterClearance(p)>0f)low=middle;else high=middle;
                    }
                else high=0f;
                if(HitSolid(step*high))return;
                travelled+=step.magnitude*high;
                transform.position=start+step*high;
                Vector3 surface=transform.position;surface.y=WaterLevel(surface);
                NavalWaterSplash.Create(transform.parent,surface,ocean);
                submerged=true;velocity*=NavalProjectileWaterMath.EntrySpeedScale;
                if(projectilePower>1.05f)Debug.Log("[NavalPrototype] Projectile entered water: power="+projectilePower.ToString("F2")+" speed="+velocity.magnitude.ToString("F1")+" limit="+NavalProjectileWaterMath.TravelLimit(projectilePower).ToString("F1"));
                remaining-=dt*high;
                continue;
            }
            float underwaterLimit=NavalProjectileWaterMath.TravelLimit(projectilePower);
            if(submerged)step=Vector3.ClampMagnitude(step,underwaterLimit-underwaterTravel);
            if(HitSolid(step))return;
            transform.position+=step;travelled+=step.magnitude;clearance=nextClearance;
            if(armedHull!=null)
            {
                float internalStep=armedAtStepStart?step.magnitude:Mathf.Max(0f,step.magnitude-armedStepOffset);
                armedStepOffset=0f;armedTravelled+=internalStep;
            }
            if(submerged)
            {
                underwaterTravel+=step.magnitude;
                if(underwaterTravel>=underwaterLimit-0.001f||velocity.magnitude<0.6f)
                {
                    if(projectilePower>1.05f)Debug.Log("[NavalPrototype] Underwater projectile spent: power="+projectilePower.ToString("F2")+" distance="+underwaterTravel.ToString("F2")+" reason="+(underwaterTravel>=underwaterLimit-0.001f?"range":"drag"));
                    // An armed shell stops in water but retains its two-second
                    // fuze. Water range is never a detonation distance.
                    if(armedHull!=null){velocity=Vector3.zero;waterStopped=true;}else Destroy(gameObject);
                    return;
                }
            }
            remaining-=dt;
        }
    }
    private float WaterLevel(Vector3 point)
    {
        // The ocean surface does not extend through a dry hull. Use the same
        // local flood column as swimming, including heel and full submergence.
        NavalTestWorld world=NavalTestWorld.Active;
        return world!=null?world.WaterLevelAt(point):ocean.Height(point.x,point.z);
    }
    private float WaterClearance(Vector3 point){return point.y-WaterLevel(point);}
    private static RaycastHit[] impactHits=new RaycastHit[256];
    private sealed class ImpactOrder:System.Collections.Generic.IComparer<RaycastHit>
    {public int Compare(RaycastHit a,RaycastHit b){return a.distance.CompareTo(b.distance);}}
    private static readonly ImpactOrder impactOrder=new ImpactOrder();
    private bool HitSolid(Vector3 step)
    {
        if(step.sqrMagnitude<0.00000001f)return false;
        // Keep a fixed base for every hit returned by this sweep. Otherwise
        // several sorted contacts in one SphereCast add the entry distance
        // repeatedly and make the fuze appear to travel farther than it did.
        float sweepArmedTravelled=armedTravelled;
        // Shared scratch storage: projectile updates run sequentially on the Unity thread.
        // A full buffer must be grown and queried again: NonAlloc hits are unsorted.
        int count;
        while(true)
        {
            count=Physics.SphereCastNonAlloc(transform.position,.12f*projectileRadiusScale,step.normalized,impactHits,step.magnitude,~0,QueryTriggerInteraction.Collide);
            if(count<impactHits.Length)break;
            System.Array.Resize(ref impactHits,impactHits.Length*2);
        }
        System.Array.Sort(impactHits,0,count,impactOrder);
        for(int index=0;index<count;index++)
        {
            RaycastHit hit=impactHits[index];
            if(hit.collider==null||!hit.collider.enabled)continue;
            Vector3 hitPoint=hit.distance<=0.0001f?hit.collider.ClosestPoint(transform.position):hit.point;
            Character hitRobot=hit.collider.GetComponentInParent<Character>();
            // A destroyed attacker compares equal to null in Unity. A plain deck has
            // no Character either, but must still receive a projectile already in flight.
            if (hitRobot != null && (hitRobot == attacker || (!hostile && hitRobot == owner))) continue;
            // A projectile never damages its own firing platform. The old
            // short grace period let a trained heavy gun clip the tapered bow.
            if (hit.collider.transform.IsChildOf(boat)) continue;
            Vector3 sweepCentre=transform.position+step.normalized*Mathf.Max(0f,hit.distance);
            Vector3 surfacePoint=hit.collider.ClosestPoint(sweepCentre);
            NavalHullVoxel voxel = NavalVoxelHit.Resolve(hit.collider,hitPoint,surfacePoint);
            // A stale or unresolvable merged shape is not an impact on scenery.
            // Keep testing the ordered hits, and let the ball continue if nothing solid remains.
            if(voxel==null&&(hit.collider.GetComponent<NavalMergedCollider>()!=null||hit.collider.GetComponent<NavalCargoProxy>()!=null||hit.collider.GetComponent<NavalHullVoxel>()!=null))continue;
            // Trigger-only trim, rails and visual fittings are not physical
            // armour. Let the blast behind a real plate damage them, but never
            // arm an AP fuze on an invisible/non-solid contact.
            if(voxel!=null&&hit.collider.isTrigger)continue;
            bool metalImpact=voxel!=null&&NavalImpactMaterial.IsMetal(voxel.Armored,voxel.Metallic,voxel.RepairableEquipment);
            if(voxel!=null&&penetratedCells.Contains(voxel))continue;
            if(!convoyHitRecorded&&voxel!=null&&voxel.Owner!=null&&voxel.Owner!=hull)
            {
                convoyHitRecorded=true;
                var mission=NavalTestWorld.Active;if(mission!=null)mission.RecordConvoyShot(boat,false,true);
            }
            if (voxel != null && voxel.Owner != null)
            {
                Vector3 local=voxel.transform.InverseTransformPoint(hitPoint);
                if(!NavalCannonImpactMath.ValidContact(hit.distance,local.x,local.y,local.z))
                    hitPoint=surfacePoint;
                // Initial sphere overlaps can report a zero/invalid contact. Recover the
                // actual surface, then place the compact damage sphere just inside the plank.
                Vector3 toCentre=voxel.transform.position-hitPoint;
                Vector3 damagePoint=hitPoint+toCentre*NavalCannonImpactMath.InsetFraction(toCentre.magnitude);
                float impactTravel=travelled+Mathf.Max(0f,hit.distance);
                Vector3 impactNormal=hit.normal.sqrMagnitude>.01f?hit.normal.normalized:-velocity.normalized;
                float impactCosine=Mathf.Abs(Vector3.Dot(-velocity.normalized,impactNormal));
                float straightPower=NavalArmorMath.PenetrationPower(projectilePower,impactTravel)*(submerged?UnderwaterPowerScale:1f);
                float armorPower=NavalArmorMath.AngledPower(straightPower,impactCosine)/(voxel.Armored&&voxel.Owner.Definition.CapitalShip?1.6f:1f);
                if(submerged&&projectilePower>1.05f)Debug.Log("[NavalPrototype] Underwater projectile impact: target="+voxel.Owner.Root.name+" distance="+underwaterTravel.ToString("F2")+" retained="+armorPower.ToString("F2")+" armored="+voxel.Armored);
                if(projectilePower>2f||voxel.Owner==hull)Debug.Log("[NavalPrototype] Heavy projectile impact: source="+hull.Root.name+" target="+voxel.Owner.Root.name+" self="+(voxel.Owner==hull)+" travel="+impactTravel.ToString("F1")+" power="+projectilePower.ToString("F1")+" point="+voxel.Owner.Root.InverseTransformPoint(damagePoint));
                float baseRadius=1.2f;
                NavalPenetrationMaterial penetrationMaterial=NavalArmorMath.Material(voxel.Armored,voxel.Metallic,voxel.RepairableEquipment);
                bool mainBatteryHit=voxel.RepairMechanism!=null&&voxel.RepairMechanism.IsMainBattery;
                armorPower=NavalArmorMath.MechanismArmorPower(armorPower,mainBatteryHit);
                if(mainBatteryHit)Debug.Log("[NavalPrototype] Main battery direct hit: ship="+voxel.Owner.Root.name+" retained="+armorPower.ToString("F2")+" wear="+voxel.CannonWear.ToString("F2"));
                bool ricochet=NavalArmorMath.Ricochets(voxel.Armored,impactCosine,armorPower);
                float appliedArmorPower=ricochet?NavalArmorMath.RicochetWear(armorPower):armorPower;
                float residual=NavalArmorMath.ResidualAfterWear(armorPower,voxel.CannonWear,penetrationMaterial);
                bool perforates=voxel.Armored?NavalArmorMath.Penetrates(voxel.CannonWear,appliedArmorPower):residual>.05f;
                bool narrowApContact=internalArmorPiercingBurst&&voxel.Owner.Definition.Destroyer&&voxel.HydraulicShell&&!voxel.InternalBarrier;
                if(armedHull!=null)
                {
                    float encounter=NavalArmorMath.InternalEncounterDistance(sweepArmedTravelled,hit.distance,armedStepOffset);
                    if(voxel.Owner==armedHull&&NavalArmorMath.IsEntryPlateSeam(encounter,voxel.HydraulicShell,voxel.InternalBarrier))
                    {
                        // The projectile sphere overlaps a neighbouring voxel
                        // of the already perforated outer plate. Do not turn
                        // that lateral seam into an instant internal burst or
                        // run a second hull-damage pass in the impact frame.
                        penetratedCells.Add(voxel);
                        if(projectilePower>2f)Debug.Log("[NavalPrototype] Heavy AP cleared entry-plate seam: target="+voxel.Owner.Root.name+" internalTravel="+encounter.ToString("F2"));
                        continue;
                    }
                    armedHull=voxel.Owner;
                    float internalPower=Mathf.Max(.35f,armedResidual-NavalArmorMath.Resistance(penetrationMaterial)*.45f);
                    armedTravelled=encounter;
                    voxel.Owner.Damage(voxel.Owner.Root.InverseTransformPoint(damagePoint),narrowApContact?0f:baseRadius*.72f,true,voxel,internalPower,internalPower);
                    Vector3 burstPoint=hitPoint+step.normalized*.18f;
                    Debug.Log("[NavalPrototype] Heavy AP armed obstruction burst: target="+voxel.Owner.Root.name+" material="+penetrationMaterial+" internalTravel="+armedTravelled.ToString("F2")+" point="+voxel.Owner.Root.InverseTransformPoint(burstPoint));
                    Burst(hitPoint,metalImpact,impactNormal);DetonateArmed(burstPoint,"obstruction");return true;
                }
                NavalPenetrationMaterial fuseMaterial=penetrationMaterial;
                if(fuseMaterial==NavalPenetrationMaterial.Machinery&&!voxel.Metallic&&!voxel.Armored)fuseMaterial=NavalPenetrationMaterial.Wood;
                bool separateMetal=firstMetalContact&&(voxel.Owner!=firstMetalHull||NavalShellFuse.SeparateMetal(
                    Mathf.Abs(Vector3.Dot(hitPoint-firstMetalPoint,firstMetalNormal)),impactTravel-firstMetalTravel,
                    firstMetalShell&&voxel.HydraulicShell&&!voxel.InternalBarrier));
                bool armHere=NavalShellFuse.Arms(projectilePower>2f,fuseMaterial,residual,separateMetal);
                // Unarmed AP crosses wood and the first light metal obstacle.
                if(internalArmorPiercingBurst&&armedHull==null&&perforates&&!ricochet&&!armHere)
                {
                    if(fuseMaterial!=NavalPenetrationMaterial.Wood&&!firstMetalContact){
                        firstMetalContact=true;firstMetalHull=voxel.Owner;firstMetalPoint=hitPoint;firstMetalNormal=impactNormal;
                        firstMetalTravel=impactTravel;firstMetalShell=voxel.HydraulicShell&&!voxel.InternalBarrier;
                    }
                    voxel.Owner.Damage(voxel.Owner.Root.InverseTransformPoint(damagePoint),narrowApContact?0f:baseRadius*.65f,true,voxel,projectilePower*.45f,appliedArmorPower);
                    penetratedCells.Add(voxel);
                    velocity*=NavalArmorMath.ExitVelocityFactor(armorPower,residual,penetrationMaterial);
                    continue;
                }
                if(internalArmorPiercingBurst&&perforates&&!ricochet&&armHere)
                {
                    voxel.Owner.Damage(voxel.Owner.Root.InverseTransformPoint(damagePoint),narrowApContact?0f:baseRadius*.65f,true,voxel,projectilePower*.45f,appliedArmorPower);
                    armedHull=voxel.Owner;armedResidual=residual;armedTravelled=0f;armedStepOffset=Mathf.Max(0f,hit.distance);
                    sweepArmedTravelled=0f;
                    armedDetonateAt=Time.time+NavalShellFuse.Seconds;penetratedCells.Add(voxel);
                    velocity*=NavalArmorMath.ExitVelocityFactor(armorPower,residual,penetrationMaterial);
                    Debug.Log("[NavalPrototype] Heavy AP fuse armed: target="+voxel.Owner.Root.name+" caliberPower="+projectilePower.ToString("F2")+" travel="+impactTravel.ToString("F1")+" material="+penetrationMaterial+" retained="+armorPower.ToString("F2")+" resistance="+NavalArmorMath.Resistance(penetrationMaterial).ToString("F2")+" residual="+residual.ToString("F2")+" fuseSeconds="+NavalShellFuse.Seconds.ToString("F2"));
                    Vector3 entryNormal=hit.normal.sqrMagnitude>.01f?hit.normal:-velocity.normalized;
                    Burst(hitPoint,metalImpact,entryNormal);
                    // Continue through the already sorted hits from this same
                    // sweep. Returning here could advance a fast shell past a
                    // thin internal bulkhead before the next sub-step.
                    continue;
                }
                voxel.Owner.Damage(voxel.Owner.Root.InverseTransformPoint(damagePoint),narrowApContact?0f:baseRadius*RadiusScale,true,voxel,projectilePower*(submerged?UnderwaterPowerScale:1f),appliedArmorPower);
                if(voxel.Armored&&!perforates&&!submerged)
                {
                    float spallPower=NavalArmorMath.SpallPower(armorPower,impactCosine,ricochet);
                    if(spallPower>0f)
                    {
                        Vector3 inside=hitPoint+velocity.normalized*Mathf.Max(.7f,NavalArmorMath.SpallRadius(spallPower)*.55f);
                        voxel.Owner.InternalSpall(voxel.Owner.Root.InverseTransformPoint(inside),NavalArmorMath.SpallRadius(spallPower)/voxel.Owner.WorldScale,spallPower);
                        SpallBurst(inside,spallPower);
                    }
                    Debug.Log("[NavalPrototype] Armor stopped shell: target="+voxel.Owner.Root.name+" caliberPower="+projectilePower.ToString("F2")+" travel="+impactTravel.ToString("F1")+" ricochet="+ricochet+" cosine="+impactCosine.ToString("F2")+" plateWear="+voxel.CannonWear.ToString("F2")+" spall="+spallPower.ToString("F2"));
                }
            }
            else
            {
                BaseBodyPart part = hit.collider.GetComponentInParent<BaseBodyPart>();
                if(part==null&&hitRobot!=null)continue;
                Character target = part != null ? part.GetOwner() : null;
                if (attacker != null && part != null && target != null && target != attacker && target.IsAlive() &&
                    NavalBattleMath.CanDamageCrew(hostile,target.IsPlayerTeam,target.IsNeutral) && !target.IsImmuneToDamage() && part.CanBeDamaged)
                    part.TryStabVolume(transform.position, hitPoint + step.normalized * 0.3f*RadiusScale, attackId, ImpactStrength, attacker, FireType.None, false, DamageSourceType.None);
                else if (hit.collider.isTrigger) continue;
            }
            if(armedHull!=null){armedTravelled=NavalArmorMath.InternalEncounterDistance(armedTravelled,hit.distance,armedStepOffset);DetonateArmed(hitPoint,"obstruction");return true;}
            Vector3 normal=hit.normal.sqrMagnitude>.01f?hit.normal:-velocity.normalized;
            Burst(hitPoint,metalImpact,normal); Destroy(gameObject); return true;
        }
        return false;
    }
    private static Collider[] impactBodies=new Collider[128];
    private readonly System.Collections.Generic.HashSet<BaseBodyPart> impactParts=new System.Collections.Generic.HashSet<BaseBodyPart>();
    private static int ImpactBodies(Vector3 point,float radius){
        int count;while(true){count=Physics.OverlapSphereNonAlloc(point,radius,impactBodies,~0,QueryTriggerInteraction.Collide);
            if(count<impactBodies.Length)return count;System.Array.Resize(ref impactBodies,impactBodies.Length*2);}
    }
    private void Burst(Vector3 point,bool metal,Vector3 normal)
    {
        // The projectile also damages native robot body voxels near its impact,
        // including sailors struck beside a deck or cannon. A stable attack ID
        // prevents a body part receiving duplicate hits from several colliders.
        var damaged=impactParts;damaged.Clear();
        int bodyCount=ImpactBodies(point,0.9f*RadiusScale);
        for(int bodyIndex=0;bodyIndex<bodyCount;bodyIndex++)
        {
            Collider collider=impactBodies[bodyIndex];
            BaseBodyPart part=collider.GetComponentInParent<BaseBodyPart>();
            Character target=part!=null?part.GetOwner():null;
            if(attacker!=null&&part!=null&&damaged.Add(part)&&target!=null&&target!=attacker&&target.IsAlive()&&
                NavalBattleMath.CanDamageCrew(hostile,target.IsPlayerTeam,target.IsNeutral)&&!target.IsImmuneToDamage()&&part.CanBeDamaged)
            {
                Vector3 hit=collider.ClosestPoint(point);
                if(part.TryStabVolume(transform.position,hit+velocity.normalized*0.35f*RadiusScale,attackId,ImpactStrength,attacker,FireType.None,false,DamageSourceType.None))
                    Debug.Log("[NavalPrototype] Cannon impact damaged robot body: "+target.name);
            }
        }
        if(submerged)return;
        if(metal)
        {
            Vector3 reflected=Vector3.Reflect(velocity.normalized,normal.normalized);
            for(int i=0;i<18;i++)NavalShotPuff.Create(transform.parent,point+Random.insideUnitSphere*.06f,
                reflected*Random.Range(2.5f,6.5f)+Random.insideUnitSphere*3.2f+Vector3.up*Random.Range(.4f,2.2f),metalSpark,
                Random.Range(.16f,.38f),Random.Range(.025f,.065f));
        }
        else for (int i = 0; i < 16; i++)
            NavalShotPuff.Create(transform.parent, point + Random.insideUnitSphere * 0.15f,
                Random.insideUnitSphere * 1.7f + Vector3.up * 1.5f, splash,
                Random.Range(0.7f, 1.4f), Random.Range(0.14f, 0.3f));
    }
    private void SpallBurst(Vector3 point,float power)
    {
        float radius=NavalArmorMath.SpallRadius(power);
        var damaged=impactParts;damaged.Clear();
        int bodyCount=ImpactBodies(point,radius);
        for(int bodyIndex=0;bodyIndex<bodyCount;bodyIndex++)
        {
            Collider collider=impactBodies[bodyIndex];
            BaseBodyPart part=collider.GetComponentInParent<BaseBodyPart>();Character target=part!=null?part.GetOwner():null;
            if(attacker!=null&&part!=null&&damaged.Add(part)&&target!=null&&target!=attacker&&target.IsAlive()&&
                NavalBattleMath.CanDamageCrew(hostile,target.IsPlayerTeam,target.IsNeutral)&&!target.IsImmuneToDamage()&&part.CanBeDamaged)
                part.TryStabVolume(point-velocity.normalized*.2f,collider.ClosestPoint(point)+velocity.normalized*.12f,attackId,
                    Mathf.Max(1,Mathf.RoundToInt(power*1.5f)),attacker,FireType.None,false,DamageSourceType.None);
        }
        for(int i=0;i<14;i++)NavalShotPuff.Create(transform.parent,point+Random.insideUnitSphere*.06f,
            velocity.normalized*Random.Range(.4f,2.0f)+Random.insideUnitSphere*3.0f,metalSpark,Random.Range(.16f,.34f),Random.Range(.025f,.065f));
    }
    private void InternalDetonation(Vector3 point)
    {
        Burst(point,false,-velocity.normalized);
        for(int i=0;i<28;i++)NavalShotPuff.Create(transform.parent,point+Random.insideUnitSphere*.12f,
            Random.insideUnitSphere*2.8f+Vector3.up*Random.Range(.4f,2.4f),splash,Random.Range(1.0f,2.1f),Random.Range(.16f,.34f));
        for(int i=0;i<12;i++)NavalShotPuff.Create(transform.parent,point+Random.insideUnitSphere*.08f,
            Random.insideUnitSphere*4.2f,metalSpark,Random.Range(.18f,.42f),Random.Range(.04f,.09f));
    }
    private void DetonateArmed(Vector3 point,string cause)
    {
        if(armedHull!=null)
        {
            float burstPower=NavalArmorMath.InternalBlastPower(projectilePower,armedResidual);
            float radiusMetres=NavalArmorMath.InternalBlastWorldRadius(projectilePower,armedResidual);
            float radius=radiusMetres/armedHull.WorldScale;
            Vector3 local=armedHull.Root.InverseTransformPoint(point);
            // A delayed AP burst loads the surrounding armour from the inside,
            // just like a magazine explosion.  Distance falloff in Hull.Damage
            // keeps the wear local and prevents the whole belt failing at once.
            int removed=armedHull.Damage(local,radiusMetres,true,null,burstPower,armedResidual,true);
            int fragments=armedHull.InternalSpall(local,radius*.90f,burstPower);
            int fires=armedHull.Fire!=null?armedHull.Fire.InternalBurst(local,radius,projectilePower,armedResidual):0;
            Debug.Log("[NavalPrototype] Heavy AP internal burst: target="+armedHull.Root.name+" cause="+cause+" internalTravel="+armedTravelled.ToString("F2")+" residual="+armedResidual.ToString("F2")+" blast="+burstPower.ToString("F2")+" world_radius_m="+radiusMetres.ToString("F2")+" removed="+removed+" fragments="+fragments+" fires="+fires+" point="+local);
        }
        InternalDetonation(point);Destroy(gameObject);
    }
    private void OnDestroy(){Active.Remove(this);}
}

// One bounded mesh batch per effect parent, shared by muzzle smoke and impacts.
public static class NavalShotPuff
{
    public static void Create(Transform parent,Vector3 position,Vector3 speed,Material material,float duration,float radius)
    {
        if(parent==null||material==null||duration<=0f)return;
        NavalSmokeBatch batch=parent.GetComponent<NavalSmokeBatch>();
        if(batch==null)batch=parent.gameObject.AddComponent<NavalSmokeBatch>();
        batch.Emit(position,speed,material,duration,radius);
    }
}
