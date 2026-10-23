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
    private Vector3 TillerPost {get{return new Vector3(-1.2f,hull.Definition.MaintenanceFloor(hull.Definition.MaintenanceDeckCount-1),-5.3f);}}
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
    private AudioClip sound;
    private readonly NavalStationState state = new NavalStationState();
    private int active { get { return state.Active; } } // 0 helm, 1 port, 2 starboard
    private int nearest = -1;
    public bool EnemyControlled;
    public bool CrewRepairRetreat;
    private Character firingRobot;
    public bool IsFunctional(int station) { return Functional(station); }
    public int PlayerStation { get { return EnemyControlled?-1:active; } }
    public float LastCrewShotAt {get;private set;}
    public bool RecentlyFired(int gun) { return Time.time-shotAt[gun]<2f; }
    public bool CrewGunAssignable(int station)
    {
        if(station<=0||station>GunCount||PlayerStation==station||!Functional(station))return false;
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
        {if(station%2==1)portWorking++;else starboardWorking++;}
        gunAvailabilityRevision=hull.Revision;
    }
    public float ShipScale {get{return hull.WorldScale;}}
    public int PortWorking {get{UpdateGunAvailability();return portWorking;}}
    public int StarboardWorking {get{UpdateGunAvailability();return starboardWorking;}}
    public int WorkingGuns {get{UpdateGunAvailability();return portWorking+starboardWorking;}}
    public float CombatOrbitSign(float preferred)
    {UpdateGunAvailability();return NavalBroadsideMath.OrbitSign(portWorking,starboardWorking,preferred);}
    private struct BearingMemo {public int Frame,Revision,Station;public Vector3 Point,Muzzle;public Quaternion Rotation;public bool Result;}
    private readonly BearingMemo[] bearingMemo=new BearingMemo[24];private int bearingCursor;
    public bool CanBearOn(int station,Vector3 point)
    {
        if(station<=0||station>GunCount||!Functional(station))return false;
        Vector3 muzzle=barrels[station-1].TransformPoint(new Vector3(0,0,1.38f));Quaternion rotation=boat.rotation;
        for(int i=0;i<bearingMemo.Length;i++){
            var m=bearingMemo[i];if(m.Frame==Time.frameCount&&m.Revision==hull.Revision&&m.Station==station&&m.Point.Equals(point)&&m.Muzzle.Equals(muzzle)&&m.Rotation.Equals(rotation))return m.Result;
        }
        float yawAngle,elevation;bool result=TryAimSolution(station-1,point,out yawAngle,out elevation);
        bearingMemo[bearingCursor]=new BearingMemo{Frame=Time.frameCount,Revision=hull.Revision,Station=station,Point=point,Muzzle=muzzle,Rotation=rotation,Result=result};bearingCursor=(bearingCursor+1)%bearingMemo.Length;
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
        {if(station%2==1)port++;else starboard++;}
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
        {if(station%2==1)port++;else starboard++;}
        return port+starboard==0?0:NavalBroadsideMath.CombatSide(TargetSide(target.position),port,starboard);
    }
    public int CombatSideToward(Vector3 point)
    {
        UpdateGunAvailability();int firing=FiringSideToward(point);
        return firing!=0?firing:NavalBroadsideMath.CombatSide(TargetSide(point),portWorking,starboardWorking);
    }
    public void PauseInput(){ForwardInput=0f;TurnInput=0f;}
    public bool AllGunsBroken {get{return WorkingGuns==0;}}
    public bool CanRestoreGun
    {
        get{if(cannonMechanisms!=null)for(int i=0;i<cannonMechanisms.Length;i++)
            if(!Functional(i+1)&&cannonMechanisms[i].NeedsRepair&&cannonMechanisms[i].RecoverableWithParts)return true;return false;}
    }
    // One kit is usable per gun over its lifetime; no separate finite crate inventory exists.
    public int AvailableCannonKits
    {
        get{int count=0;if(cannonMechanisms!=null)foreach(var mechanism in cannonMechanisms)
            if(mechanism!=null&&!mechanism.RepairUsed&&mechanism.RecoverableWithParts)count++;
            return count;}
    }
    public bool MustBoard {get{return AllGunsBroken&&!CanRestoreGun;}}
    public Vector3 Post(int station)
    {return station==SailStation?SailPost:station==0?(EmergencySteering?TillerPost:new Vector3(0f,hull.Definition.HelmDeck,NavalShipDefinition.HelmZ(hull.Definition))):new Vector3(NavalShipDefinition.GunSide(station-1)*hull.Definition.GunPostX,hull.Definition.GunDeck,NavalShipDefinition.GunZ(hull.Definition,station-1));}
    public Vector3 CrewPosition(int station){return boat.TransformPoint(Post(station));}
    public Vector3 CrewForward(int station) { return station==0||station==SailStation?boat.forward:barrels[station-1].forward; }
    public bool AimAndFire(int gun,Vector3 target,float dt,Character sailor)
    {
        if(PlayerStation==gun+1)return false;
        if(!Functional(gun+1)||sailor==null||!sailor.IsAlive())return false;
        float wantedYaw,angle;
        if(!TryAimSolution(gun,target,out wantedYaw,out angle))return false;
        yaw[gun]=Mathf.MoveTowards(yaw[gun],wantedYaw,28f*dt);
        pitch[gun]=Mathf.MoveTowards(pitch[gun],angle,24f*dt);Aim(gun);
        if(Time.time>=readyAt[gun]&&Mathf.Abs(yaw[gun]-wantedYaw)<0.6f&&Mathf.Abs(pitch[gun]-angle)<0.6f)
        { firingRobot=sailor;Fire(gun);firingRobot=null;return true; }
        return false;
    }
    public bool Operating { get { return active >= 0; } }
    public bool AtHelm { get { return active == 0; } }
    public float ForwardInput { get; private set; }
    public float TurnInput { get; private set; }
    public Vector3 OperatorPosition {get{return CrewPosition(active);}}
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
        helm = Root("ShipWheel", new Vector3(0f, hull.Definition.AboveDeckY(1.4f)+hull.Definition.UpperDeckOffset, NavalShipDefinition.HelmZ(hull.Definition)+.8f*hull.Definition.FixtureScale));
        helm.localScale=Vector3.one*hull.Definition.FixtureScale;
        hull.AddDecorationBox(helm, wood, new Vector3(0f, -0.3f, 0f), new Vector3(0.2f, 0.6f, 0.25f), true);
        for (int i = 0; i < 12; i++)
        {
            float angle = i * Mathf.PI / 6f;
            hull.AddDecorationBox(helm, wheelAccent, new Vector3(Mathf.Sin(angle) * 0.4f, Mathf.Cos(angle) * 0.4f, 0f),
                new Vector3(0.17f, 0.17f, 0.14f), true);
        }
        hull.AddDecorationBox(helm, wood, Vector3.zero, new Vector3(0.75f, 0.10f, 0.12f), true);
        hull.AddDecorationBox(helm, wood, Vector3.zero, new Vector3(0.10f, 0.75f, 0.12f), true);
        hull.AddDecorationBox(helm, wheelAccent, new Vector3(0f, 0f, -0.09f), new Vector3(0.22f, 0.22f, 0.12f), true);
        for (int i = 0; i < GunCount; i++)
        {
            float side = NavalShipDefinition.GunSide(i);
            guns[i] = Root((side<0f?"PortCannon":"StarboardCannon")+i, new Vector3(side * hull.Definition.GunMountX, hull.Definition.GunMountY, NavalShipDefinition.GunZ(hull.Definition,i)));
            guns[i].localScale=Vector3.one*hull.Definition.GunFixtureScale;
            hull.AddDecorationBox(guns[i], wood, new Vector3(0f, -0.03f, 0f), new Vector3(0.75f, 0.3f, 0.9f), true);
            for (int wheel = -1; wheel <= 1; wheel += 2)
                hull.AddDecorationBox(guns[i], black, new Vector3(wheel * 0.4f, -0.05f, 0f), new Vector3(0.15f, 0.35f, 0.65f), true);
            barrels[i] = new GameObject("CannonBarrel").transform;
            barrels[i].SetParent(guns[i], false); barrels[i].localPosition = new Vector3(0f, 0.35f, 0f);
            hull.AddDecorationBox(barrels[i], black, new Vector3(0f, 0f, 0.55f), new Vector3(0.38f, 0.38f, 1.3f), true);
            hull.AddDecorationBox(barrels[i], trim, new Vector3(0f, 0f, 0.9f), new Vector3(0.42f, 0.42f, 0.1f), true);
            pitch[i] = 5f; Aim(i);
        }
        Vector3 cleat=SailPost+new Vector3(NavalSailWorkLayout.CleatOffset/hull.WorldScale,.45f/hull.WorldScale,0);
        hull.AddDecorationBox(boat,wood,cleat,new Vector3(.16f,.9f,.16f)/hull.WorldScale,true);
        hull.AddDecorationBox(boat,trim,cleat+Vector3.up*(.3f/hull.WorldScale),new Vector3(.6f,.14f,.18f)/hull.WorldScale,true);
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
            var repair=new NavalRepairableMechanism(hull,members,false,true);cannonMechanisms[gun]=repair;
            repair.RepairPoint=new Vector3(NavalShipDefinition.GunSide(gun)*hull.Definition.GunMountX,
                hull.Definition.GunMountY+.35f*hull.Definition.GunFixtureScale,NavalShipDefinition.GunZ(hull.Definition,gun));
        }
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
                (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1f : 0f)) * 28f * dt, -30f, 30f);
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
            barrels[i].localPosition = new Vector3(0f, 0.35f, 0f) - barrels[i].localRotation * Vector3.forward * recoil;
        }
    }
    private void Aim(int gun) { barrels[gun].localRotation = Quaternion.Euler(-pitch[gun], (gun % 2 == 0 ? -90f : 90f) + yaw[gun], 0f); }
    private bool Functional(int station)
    {
        if(station==SailStation)return !hull.Sunk;
        if(station==0&&helmMechanism!=null)return helmMechanism.Functional||emergencyTiller!=null&&emergencyTiller.Functional;
        if (mechanisms==null||station<0||station>=mechanisms.Length||mechanisms[station] == null) return false;
        if (mechanisms[station].Count == 0) return false;
        foreach (NavalHullVoxel voxel in mechanisms[station]) if (voxel == null || voxel.Missing || voxel.Detached) return false;
        return true;
    }
    private void Fire(int gun)
    {
        if (!Functional(gun + 1)) return;
        if(hull.Cargo!=null&&!hull.Cargo.Fire(gun))return;
        shotAt[gun]=Time.time;
        readyAt[gun] = Time.time + NavalGunneryMath.ReloadSeconds;
        if(firingRobot!=null)LastCrewShotAt=Time.time;
        Vector3 muzzle = barrels[gun].TransformPoint(new Vector3(0f, 0f, 1.38f));
        Vector3 direction = ShotDirection(gun,barrels[gun].forward);
        GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ball.name = "NavalPrototype_Cannonball";
        // World-space ballistics must not inherit the firing vessel's later motion.
        ball.transform.SetParent(NavalTestWorld.Active!=null?NavalTestWorld.Active.transform:null, false);
        ball.transform.position = muzzle; ball.transform.localScale = Vector3.one * 0.3f;
        Collider collider = ball.GetComponent<Collider>(); collider.enabled = false; Destroy(collider);
        ball.GetComponent<Renderer>().sharedMaterial = iron;
        ball.AddComponent<NavalCannonball>().Initialize(direction * 32f, boat, hull, player, ocean, smoke, Time.time + 10f,EnemyControlled,firingRobot);
        AudioSource.PlayClipAtPoint(sound, muzzle, 0.7f);
        NavalShotPuff.Create(transform, muzzle, direction * 2f, flash, 0.07f, 0.38f);
        GameObject muzzleGlow=new GameObject("Naval muzzle flash");muzzleGlow.transform.position=muzzle+direction*.25f;
        Light lamp=muzzleGlow.AddComponent<Light>();lamp.type=LightType.Point;lamp.color=new Color(1f,.72f,.38f);lamp.intensity=.85f;lamp.range=4f;lamp.shadows=LightShadows.None;Destroy(muzzleGlow,.07f);
        for (int i = 0; i < 20; i++)
            NavalShotPuff.Create(transform, muzzle + Random.insideUnitSphere * 0.18f,
                direction * Random.Range(0.7f, 2.6f) + Vector3.up * Random.Range(0.6f, 1.3f) + Random.insideUnitSphere * 0.7f,
                smoke, Random.Range(1.1f, 2.1f), Random.Range(0.16f, 0.34f));
        Debug.Log("[NavalPrototype] Cannon fired: "+boat.name+" "+hull.Definition.Name+" gun="+(gun+1)+" "+(gun%2==0?"port":"starboard")+" pitch="+pitch[gun].ToString("F1"));
    }
    public void Exit() { state.Exit(); ForwardInput = 0f; TurnInput = 0f; }
    private static Material MakeMaterial(Color color)
    {
        Shader shader = Shader.Find("Unlit/Color"); if (shader == null) shader = Shader.Find("Standard");
        Material material = new Material(shader); material.color = color; return material;
    }
    private void OnGUI()
    {
        DrawReloadBars();
        if(EnemyControlled||NavalTestWorld.Active!=null&&NavalTestWorld.Active.Spectating)return;
        string text = active == 0 ? (EmergencySteering?"АВАРИЙНЫЙ РУМПЕЛЬ (поворот ×⅓): A/D":"ШТУРВАЛ: A/D")+" — поворот; E — выйти. Ход задаётся парусами у мачты." :
            (active==SailStation?"ПАРУСА: W — добавить ход; S — убрать / задний ход; C — авто; E — выйти\nПоложение: "+(SailSetting*100).ToString("F0")+"%":active > 0 ? "CANNON: A/D or arrows = aim +/-30; W/S = elevation -25..45; LMB/Space = fire; E = leave\nReload: " +
                Mathf.Max(0f, readyAt[active - 1] - Time.time).ToString("F1") + " s" :
                (nearest >= 0 ? "Press E: " + (nearest==SailStation?"парусные снасти":nearest == 0 ? (EmergencySteering?"аварийный румпель в корме трюма":"ship wheel") : ((nearest-1)%2==0?"port cannon ":"starboard cannon ")+((nearest-1)/2+1)) :
                    "Approach a cannon or the stern wheel and press E"));
        if(active < 0 && nearest >= 0 && !Functional(nearest))
            text = (nearest == 0 ? "HELM DAMAGED — R: repair" : "ПУШКА ПОВРЕЖДЕНА — R: ремонт 60 с, один раз");
        if(active<0&&nearest==0&&!Functional(0)&&helmMechanism!=null&&helmMechanism.Destroyed)
            text="Штурвал разрушен — восстановление невозможно";
        GUI.Box(new Rect(Screen.width * 0.5f - 350f, Screen.height - 90f, 700f, 70f), text);
    }
    private void OnDestroy()
    { UnregisterShip(); Exit(); if (flash != null) Destroy(flash); if (smoke != null) Destroy(smoke); if (wheelAccent != null) Destroy(wheelAccent); if (sound != null) Destroy(sound); }
}

public class NavalCannonball : MonoBehaviour
{
    private Vector3 velocity;
    private bool submerged;
    private float underwaterTravel;
    private float RadiusScale {get{return submerged?NavalProjectileWaterMath.DamageRadiusScale:1f;}}
    private int ImpactStrength {get{return submerged?Mathf.RoundToInt(4f*NavalProjectileWaterMath.DamagePower):4;}}
    private Transform boat;
    private NavalHull hull;
    private FirstPersonMover owner;
    private NavalWater ocean;
    private Material splash;
    private static int nextAttackId = 1800000000;
    private int attackId;
    private float expires;
    private float born;
    private bool hostile;
    private Character attacker;
    public void Initialize(Vector3 speed, Transform ship, NavalHull shell, FirstPersonMover player, NavalWater sea, Material material, float end,bool enemy=false,Character robot=null)
    { velocity = speed; boat = ship; hull = shell; owner = player; ocean = sea; splash = material; expires = end; born = Time.time; attackId = nextAttackId++; hostile=enemy;attacker=robot!=null?robot:player; }
    private void Update()
    {
        if (boat == null || Time.time >= expires) { Destroy(gameObject); return; }
        NavalTestWorld world=NavalTestWorld.Active;
        if (Time.timeScale == 0f || (world!=null?!world.SimulationActive:owner==null||!owner.IsPlayerInputEnabled())) return;
        float remaining=Mathf.Min(Time.deltaTime,0.05f);
        float clearance=submerged?0f:WaterClearance(transform.position);
        while(remaining>0.00001f)
        {
            float dt=Mathf.Min(remaining,0.008f);
            velocity+=Vector3.down*9.81f*dt;
            if(submerged)velocity*=NavalProjectileWaterMath.Damping(dt);
            Vector3 step=velocity*dt;
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
                transform.position=start+step*high;
                Vector3 surface=transform.position;surface.y=WaterLevel(surface);
                NavalWaterSplash.Create(transform.parent,surface,ocean);
                submerged=true;velocity*=NavalProjectileWaterMath.EntrySpeedScale;
                remaining-=dt*high;
                continue;
            }
            if(submerged)step=Vector3.ClampMagnitude(step,NavalProjectileWaterMath.MaxTravel-underwaterTravel);
            if(HitSolid(step))return;
            transform.position+=step;clearance=nextClearance;
            if(submerged)
            {
                underwaterTravel+=step.magnitude;
                if(underwaterTravel>=NavalProjectileWaterMath.MaxTravel-0.001f||velocity.magnitude<0.6f)
                {Destroy(gameObject);return;}
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
        // Shared scratch storage: projectile updates run sequentially on the Unity thread.
        // A full buffer must be grown and queried again: NonAlloc hits are unsorted.
        int count;
        while(true)
        {
            count=Physics.SphereCastNonAlloc(transform.position,.12f,step.normalized,impactHits,step.magnitude,~0,QueryTriggerInteraction.Collide);
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
            if (hit.collider.transform.IsChildOf(boat) && Time.time < born + 0.12f) continue;
            Vector3 sweepCentre=transform.position+step.normalized*Mathf.Max(0f,hit.distance);
            Vector3 surfacePoint=hit.collider.ClosestPoint(sweepCentre);
            NavalHullVoxel voxel = NavalVoxelHit.Resolve(hit.collider,hitPoint,surfacePoint);
            // A stale or unresolvable merged shape is not an impact on scenery.
            // Keep testing the ordered hits, and let the ball continue if nothing solid remains.
            if(voxel==null&&(hit.collider.GetComponent<NavalMergedCollider>()!=null||hit.collider.GetComponent<NavalCargoProxy>()!=null||hit.collider.GetComponent<NavalHullVoxel>()!=null))continue;
            if (voxel != null && voxel.Owner != null)
            {
                Vector3 local=voxel.transform.InverseTransformPoint(hitPoint);
                if(!NavalCannonImpactMath.ValidContact(hit.distance,local.x,local.y,local.z))
                    hitPoint=surfacePoint;
                // Initial sphere overlaps can report a zero/invalid contact. Recover the
                // actual surface, then place the compact damage sphere just inside the plank.
                Vector3 toCentre=voxel.transform.position-hitPoint;
                Vector3 damagePoint=hitPoint+toCentre*NavalCannonImpactMath.InsetFraction(toCentre.magnitude);
                voxel.Owner.Damage(voxel.Owner.Root.InverseTransformPoint(damagePoint), NavalRigImpactMath.CannonRadiusForScale(voxel.Owner.WorldScale)*RadiusScale,true,voxel,submerged?NavalProjectileWaterMath.DamagePower:1f);
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
            Burst(hitPoint); Destroy(gameObject); return true;
        }
        return false;
    }
    private void Burst(Vector3 point)
    {
        // The projectile also damages native robot body voxels near its impact,
        // including sailors struck beside a deck or cannon. A stable attack ID
        // prevents a body part receiving duplicate hits from several colliders.
        System.Collections.Generic.HashSet<BaseBodyPart> damaged=new System.Collections.Generic.HashSet<BaseBodyPart>();
        foreach(Collider collider in Physics.OverlapSphere(point,0.9f*RadiusScale,~0,QueryTriggerInteraction.Collide))
        {
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
        for (int i = 0; i < 16; i++)
            NavalShotPuff.Create(transform.parent, point + Random.insideUnitSphere * 0.15f,
                Random.insideUnitSphere * 1.7f + Vector3.up * 1.5f, splash,
                Random.Range(0.7f, 1.4f), Random.Range(0.14f, 0.3f));
    }
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
