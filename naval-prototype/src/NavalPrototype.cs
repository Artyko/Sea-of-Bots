using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using ModLibrary;
using UnityEngine;
using UnityEngine.SceneManagement;

[MainModClass]
public class NavalPrototypeMod : Mod
{
    private NavalTestWorld world;
    private Harmony swimmingPatch;
    private bool autoSuppressed,restartQueued;
    private float autoCheckAt;
    protected override void OnModEnabled()
    {
        swimmingPatch = new Harmony("SchoolNavalPrototype.SwimmingEntry");
        swimmingPatch.Patch(AccessTools.Method(typeof(FirstPersonMover), "applyMovement"),
            prefix: new HarmonyMethod(typeof(NavalPrototypeMod), "BeforeMovement"));
        swimmingPatch.Patch(AccessTools.Method(typeof(FirstPersonMover),"onDeath"),prefix:new HarmonyMethod(typeof(NavalPrototypeMod),"BeforePlayerDeath"));
        swimmingPatch.Patch(AccessTools.Method(typeof(FirstPersonMover),"ReparentCameraAndAnimateForDeath"),prefix:new HarmonyMethod(typeof(NavalPrototypeMod),"BeforeDeathCamera"));
        swimmingPatch.Patch(AccessTools.Method(typeof(GameFlowManager),"onCharacterKilled"),prefix:new HarmonyMethod(typeof(NavalPrototypeMod),"BeforeCharacterKilled"));
        swimmingPatch.Patch(AccessTools.Method(typeof(GameFlowManager),"LoseGameAndShowUI"),prefix:new HarmonyMethod(typeof(NavalPrototypeMod),"BeforeLoseGame"));
        swimmingPatch.Patch(AccessTools.Method(typeof(GameFlowManager),"RefreshIsLevelWon"),prefix:new HarmonyMethod(typeof(NavalPrototypeMod),"BeforeVictoryCheck"));
        foreach(string method in new string[]{"ConsciousnessTransferToKillerEnabled","ConsciousnessTransferToAlliesEnabled"})
            swimmingPatch.Patch(AccessTools.Method(typeof(GameModeManager),method),postfix:new HarmonyMethod(typeof(NavalPrototypeMod),"AfterTransferEnabled"));
    }
    private static void BeforePlayerDeath(FirstPersonMover __instance)
    {NavalTestWorld world=NavalTestWorld.Active;if(world!=null&&world.OwnsPlayer(__instance))world.BeginSpectating();}
    private static bool BeforeDeathCamera(FirstPersonMover __instance,ref bool __result)
    {NavalTestWorld world=NavalTestWorld.Active;if(world==null||!world.OwnsPlayer(__instance))return true;__result=false;return false;}
    private static bool BeforeCharacterKilled(Character __0)
    {NavalTestWorld world=NavalTestWorld.Active;if(world==null||!world.OwnsPlayer(__0))return true;world.BeginSpectating();return false;}
    private static bool BeforeLoseGame()
    {NavalTestWorld world=NavalTestWorld.Active;return world==null||!world.Spectating;}
    private static bool BeforeVictoryCheck()
    {NavalTestWorld world=NavalTestWorld.Active;return world==null||!world.Spectating;}
    private static void AfterTransferEnabled(ref bool __result)
    {NavalTestWorld world=NavalTestWorld.Active;if(world!=null&&world.Spectating)__result=false;}
    private static void BeforeMovement(FirstPersonMover __instance)
    {
        NavalTestWorld active = NavalTestWorld.Active;
        if (active != null) active.PrepareWaterEntry(__instance);
    }
    protected override void OnFirstPersonMoverSpawned(FirstPersonMover player)
    {
        if (player.IsMainPlayer()) {Clear();autoSuppressed=false;autoCheckAt=Time.time+1f;}
    }
    protected override void OnFirstPersonMoverUpdate(FirstPersonMover player)
    {
        if (!GameModeManager.IsSinglePlayer() || !player.IsMainPlayer() ||
            !player.IsAlive() || !player.IsPlayerInputEnabled() || Time.timeScale == 0f) return;
        if(world!=null&&world.RestartRequested)
        {world.ReturnToStart();Clear();restartQueued=true;return;}
        if(restartQueued){restartQueued=false;autoSuppressed=false;StartWorld(player);return;}
        if(world==null&&!autoSuppressed&&!Input.GetKeyDown(KeyCode.F6)&&Time.time>=autoCheckAt)
        {
            autoCheckAt=Time.time+1f;Vector3 sea;
            if(NavalWorldMap.TryGetSeaOrigin(out sea))StartWorld(player);
        }
        if (Input.GetKeyDown(KeyCode.F6))
        {
            if (world != null) { world.ReturnToStart(); Clear();autoSuppressed=true; }
            else {autoSuppressed=false;StartWorld(player);}
        }
        if (world != null && Input.GetKeyDown(KeyCode.F8)) world.ReturnToDeck();
    }
    protected override void OnModDeactivated()
    {
        if (swimmingPatch != null) { swimmingPatch.UnpatchAll("SchoolNavalPrototype.SwimmingEntry"); swimmingPatch = null; }
        if (world != null) world.ReturnToStart();
        Clear();
    }
    private void Clear()
    {
        if (world != null) UnityEngine.Object.Destroy(world.gameObject);
        world = null;restartQueued=false;
    }
    private void StartWorld(FirstPersonMover player)
    {
        GameObject root=new GameObject("NavalPrototype_World");world=root.AddComponent<NavalTestWorld>();
        try{world.Initialize(player);}catch(Exception error){Debug.LogError("[NavalPrototype] "+error);Clear();autoSuppressed=true;}
    }
}

// Wave height field coupled to buoyancy and a conserved interior water volume.
public partial class NavalTestWorld : MonoBehaviour
{
    public static NavalTestWorld Active;
    public bool SimulationActive {get{return NavalSessionPolicy.Runs(Spectating,player!=null,player!=null&&player.IsAlive(),player!=null&&player.IsPlayerInputEnabled(),Time.timeScale==0f,scene==SceneManager.GetActiveScene().handle&&GameModeManager.IsSinglePlayer());}}
    public bool IsBoarding {get{return !Spectating&&boarding!=null&&boarding.Climbing;}}
    public NavalHull PlayerHull {get{return hull;}}
    public bool Retreating {get{return retreat.Active||stations!=null&&stations.CrewRepairRetreat;}}
    public float SpeedThroughWater {get;private set;}
    public Vector3 ShipVelocity { get { return boat!=null?boat.forward*speed+(ram!=null?ram.Drift:Vector3.zero):Vector3.zero; } }
    public Transform EnemyBoat { get { return enemy!=null&&!enemy.Hull.Sunk?enemy.Boat:null; } }
    public Vector3 EnemyVelocity { get { return enemy!=null?enemy.ShipVelocity:Vector3.zero; } }
    private NavalAlliedCrew allies;
    private FirstPersonMover player;
    private Transform boat;
    private Transform water;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private Vector3 waterCenter;
    private int scene;
    private float footOffset;
    private float speed;
    private NavalWater ocean;
    private NavalHull hull;
    private NavalShipVisuals rig;
    private NavalStructure structure;
    private NavalEnemyShip enemy;
    private NavalWorldMap seaMap;
    private float frameMilliseconds=16.7f;
    private bool rough = true;
    private readonly NavalHelmNavigator helmNavigator=new NavalHelmNavigator();
    private readonly NavalHelmDecision helmDecision=new NavalHelmDecision();
    private readonly NavalRetreatState retreat=new NavalRetreatState();
    private float heading;
    private float rudder;
    private NavalBoarding boarding;
    private NavalRamDamage ram;
    private float verticalVelocity;
    private bool swimming;
    private float savedMoveMultiplier;
    private float savedAcceleration;
    private Vector3 swimVelocity;
    private float neckOffset;
    private string damageMessage = "Aim at a ship part and press G";
    private float damageMessageUntil;
    private NavalStations stations;
    private NavalUnderwater underwater;
    private bool stationLocked;
    private float stationMoveMultiplier;
    private float stationAcceleration;
    private static readonly FieldInfo VelocityField = typeof(FirstPersonMover).GetField("_velocity", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo ControllerField = typeof(FirstPersonMover).GetField("_characterController", BindingFlags.Instance | BindingFlags.NonPublic);
    private const float HoldArea = BoatDimensions.HoldArea;
    private readonly List<Material> materials = new List<Material>();

    public void Initialize(FirstPersonMover owner)
    {
        NavalCharacters.Reset();NavalWorldProgressBar.Reset();NavalCharacters.Register(owner);
        player = owner;liveCamera=owner.GetPlayerCamera();RememberLiveCamera();
        scene = SceneManager.GetActiveScene().handle;
        startPosition = player.transform.position;
        startRotation = player.transform.rotation;
        float floor = startPosition.y;
        RaycastHit[] hits = Physics.RaycastAll(startPosition + Vector3.up * 2f,
            Vector3.down, 12f, ~0, QueryTriggerInteraction.Ignore);
        Array.Sort(hits, delegate(RaycastHit a, RaycastHit b) { return a.distance.CompareTo(b.distance); });
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.GetComponentInParent<Character>() != null) continue;
            floor = hit.point.y; break;
        }
        footOffset = Mathf.Clamp(startPosition.y - floor, 0f, 3f);
        CharacterModel model = player.GetCharacterModel();
        Transform head = player.GetHeadTransform();
        neckOffset = model != null && model.MouthSpawnPoint != null
            ? model.MouthSpawnPoint.position.y - startPosition.y - 0.18f
            : (head != null ? head.position.y - startPosition.y : 1.4f);
        // Raised above the existing arena: no original level objects are removed.
        if(!NavalWorldMap.TryGetSeaOrigin(out waterCenter))
            throw new InvalidOperationException("Load the separate Naval Ocean editor map first. F6 no longer replaces ordinary arenas.");
        GameObject mapRoot=new GameObject("NavalPrototype_SeaMap");mapRoot.transform.SetParent(transform,false);
        seaMap=mapRoot.AddComponent<NavalWorldMap>();seaMap.Initialize(player,waterCenter);
        Material wood = MakeMaterial(new Color(0.30f, 0.19f, 0.115f));
        Material lightWood = MakeMaterial(new Color(0.66f, 0.51f, 0.32f));
        Material deckWood = MakeMaterial(new Color(0.47f, 0.375f, 0.255f));
        Material alternatePlank = MakeMaterial(new Color(0.515f, 0.415f, 0.285f));
        Material blackMast = MakeMaterial(new Color(0.025f, 0.025f, 0.03f));
        water = new GameObject("NavalPrototype_WaveOcean").transform;
        water.SetParent(transform, false); water.position = waterCenter;
        ocean = water.gameObject.AddComponent<NavalWater>();
        ocean.Initialize();
        seaMap.SetOcean(ocean);
        ocean.Amplitude = 0.65f;
        boat = new GameObject("WoodenBoat").transform;
        boat.SetParent(transform, false);
        boat.localScale = Vector3.one * NavalSessionSettings.PlayerShip.Scale;
        boat.position = waterCenter;
        boat.rotation = Quaternion.Euler(0f, player.transform.eulerAngles.y, 0f);
        heading = boat.eulerAngles.y;
        hull = new NavalHull(NavalSessionSettings.PlayerShip); hull.Build(boat, wood, MakeMaterial(new Color(0.15f, 0.65f, 0.85f)), deckWood);
        boat.gameObject.AddComponent<NavalFloodAppearance>().Initialize(ocean,hull);
        rig = boat.gameObject.AddComponent<NavalShipVisuals>();
        rig.Initialize(hull, wood, lightWood, blackMast, MakeMaterial(new Color(0.91f, 0.90f, 0.84f)),
            MakeMaterial(new Color(0.12f, 0.085f, 0.05f)), deckWood, alternatePlank);
        stations = gameObject.AddComponent<NavalStations>();
        stations.Initialize(boat, hull, player, ocean, wood, lightWood, blackMast);
        boat.gameObject.AddComponent<NavalSeaLadder>().Initialize(hull,lightWood);
        boarding=new NavalBoarding(player,footOffset);
        if(hull.Definition.Compartments)boat.gameObject.AddComponent<NavalBilgeSystem>().Initialize(hull,player,wood,lightWood,blackMast);
        boat.gameObject.AddComponent<NavalCargo>().Initialize(hull,player,stations,false,wood,lightWood,blackMast);
        boat.gameObject.AddComponent<NavalFurnishings>().Initialize(hull,wood,lightWood,blackMast);
        boat.gameObject.AddComponent<NavalFireSystem>().Initialize(hull);
        structure=boat.gameObject.AddComponent<NavalStructure>();
        structure.Initialize(hull);
        ram=boat.gameObject.AddComponent<NavalRamDamage>();ram.Initialize(hull);
        gameObject.AddComponent<NavalPlayerRepair>().Initialize(player,hull,structure,stations);
        gameObject.AddComponent<NavalPlayerFire>().Initialize(player,hull,stations);
        boat.gameObject.AddComponent<NavalVoxelBatch>().Initialize(hull);
        hull.PhysicsBody.FinishConstruction();
        allies=gameObject.AddComponent<NavalAlliedCrew>();allies.Initialize(player,hull,structure,stations,footOffset);
        underwater = gameObject.AddComponent<NavalUnderwater>();
        underwater.Initialize(player, delegate(Vector3 point) { return VisualWaterLevelAt(point); });
        gameObject.AddComponent<NavalDrowning>().Initialize(player,delegate(Vector3 point){return WaterLevelAt(point);});
        NavalPerformance.SyncTransforms();
        ReturnToDeck();
        Active = this;
        if(GetComponent<NavalBattleTelemetry>()==null)gameObject.AddComponent<NavalBattleTelemetry>();
        ToggleEnemy();
        Debug.Log("[NavalPrototype] v227 ships and waves started on native Naval Ocean editor map.");
    }

    private Material MakeMaterial(Color color)
    {
        Shader shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) throw new InvalidOperationException("No supported material shader.");
        Material material = new Material(shader); material.color = color;
        materials.Add(material); return material;
    }
    private void ToggleEnemy()
    {
        if(enemy!=null){Destroy(enemy.gameObject);enemy=null;return;}
        GameObject root=new GameObject("NavalPrototype_Enemy");root.transform.SetParent(transform,false);
        enemy=root.AddComponent<NavalEnemyShip>();
        try{enemy.Initialize(player,boat,ocean,footOffset);}
        catch(Exception error){Debug.LogError("[NavalPrototype] Enemy ship: "+error);Destroy(root);enemy=null;}
    }
    private GameObject Part(Transform parent, string name, Vector3 position, Vector3 size, Material material, bool solid)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = "NavalPrototype_" + name;
        if (solid) obj.layer = PhysicsManager.GetEnvironmentLayer();
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = position; obj.transform.localScale = size;
        obj.GetComponent<Renderer>().sharedMaterial = material;
        if (!solid) { Collider collider = obj.GetComponent<Collider>(); collider.enabled = false; Destroy(collider); }
        return obj;
    }
    public void ReturnToDeck()
    {
        if (player == null || boat == null || (hull != null && hull.Sunk)) return;
        MovePlayer(boat.TransformPoint(new Vector3(0f, hull.Definition.WeatherDeck, -5f)) + Vector3.up * (footOffset + 0.08f));
    }
    public void ReturnToStart()
    {
        if (player == null || !player.IsAlive()) return;
        MovePlayer(startPosition + Vector3.up * 0.1f);
        player.transform.rotation = startRotation;
    }
    private void MovePlayer(Vector3 position)
    {
        if(boarding!=null)boarding.Cancel();
        if (stations != null) stations.Exit();
        SetStationLock(false);
        SetSwimming(false);
        CharacterController controller = GetController();
        bool enabled = controller != null && controller.enabled;
        if (enabled) controller.enabled = false;
        player.transform.position = position;
        if (enabled) controller.enabled = true;
        NavalPerformance.SyncTransforms();
    }
    private void LateUpdate()
    {
        if (!GameModeManager.IsSinglePlayer() || scene != SceneManager.GetActiveScene().handle) { Destroy(gameObject); return; }
        if(!Spectating&&(player==null||!player.IsAlive()))BeginSpectating();
        if(boat==null||!SimulationActive)return;
        if(Spectating)UpdateSpectator();else RememberLiveCamera();
        UpdateHudInput();
        if(replaceEnemy){replaceEnemy=false;if(enemy==null)ToggleEnemy();}
        float dt = Mathf.Min(Time.deltaTime, 0.05f);
        frameMilliseconds=Mathf.Lerp(frameMilliseconds,Time.unscaledDeltaTime*1000f,0.05f);
        if(!Spectating)boarding.Tick(dt,stations.Operating);
        if(!Spectating&&!ShipMenuOpen&&!boarding.Climbing)stations.Tick(dt, footOffset, hull.Sunk, swimming);
        if(!Spectating)SetStationLock(stations.Operating||boarding.Climbing||ShipMenuOpen);
        if(ShipMenuOpen)stations.PauseInput();
        if (Input.GetKeyDown(KeyCode.F7)) rough = !rough;
        ocean.Amplitude = rough ? 0.65f : 0.20f;
        if (Input.GetKeyDown(KeyCode.F9)) hull.Damage(new Vector3(-BoatDimensions.HalfWidth, -0.225f, 0f), 0.75f);
        if (Input.GetKeyDown(KeyCode.F11)) hull.Damage(new Vector3(BoatDimensions.HalfWidth, -0.225f, 0f), 0.75f);
        if(Input.GetKeyDown(KeyCode.F12))ToggleEnemy();
        if (!Spectating && Input.GetKeyDown(KeyCode.R) && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))) { structure.Restore(); hull.Repair(); rig.RestoreRig(); }
        if (!Spectating && Input.GetKeyDown(KeyCode.G))
            DamageAimedPart();
        structure.Refresh(boat.forward*speed+Vector3.up*verticalVelocity);
        hull.Step(boat, ocean, dt, Input.GetKey(KeyCode.F10));
        float depth = hull.Volume / (HoldArea * hull.AreaScale);
        retreat.Step(dt,allies!=null&&allies.NeedsRetreat,NavalCrewPriorities.ShouldWithdraw(hull.RepairRetreat,stations.AllGunsBroken,hull.HasActiveLeaks||hull.Bilge!=null&&(hull.Bilge.WantsOperator||!hull.Bilge.Functional)),EnemyBoat!=null&&!hull.Sunk&&!stations.AtHelm);
        float throttle = 0f;
        float turn = stations.AtHelm ? stations.TurnInput : 0f;
        if(!stations.AtHelm&&allies!=null&&allies.HasHelmsman&&stations.IsFunctional(0)&&EnemyBoat!=null&&!hull.Sunk)
        {
            Vector3 desired;
            if(helmNavigator.Reversing){helmDecision.Reset();desired=boat.forward;throttle=1;}
            else desired=helmDecision.Course(boat,hull,stations,EnemyBoat,EnemyVelocity,speed,dt,retreat.Active,enemy.Retreating,ram,ref throttle);
            helmNavigator.Steer(seaMap,boat,desired,speed,rig.SailPowerFactor,dt,ref throttle,out turn,!stations.MustBoard);
        }
        else {helmNavigator.Reset(boat.position);helmDecision.Reset();}
        if(stations.RescueStop){throttle=0f;turn=0f;helmNavigator.Reset(boat.position);helmDecision.Reset();}
        stations.ReverseManeuver=helmNavigator.Recovering&&!stations.RescueStop;
        throttle=stations.StepSails(dt,throttle,!stations.AtHelm&&allies!=null&&allies.HasHelmsman&&stations.IsFunctional(0)&&EnemyBoat!=null&&!hull.Sunk);
        rig.RefreshRig();
        ram.StepMomentum(dt,ref speed);
        speed = NavalRigMath.StepSpeed(speed, throttle, rig.SailPowerFactor, dt,hull.Definition.MaxSailingSpeed);
        // Carry only a rider supported by this deck. No parenting and no airborne snapping.
        bool aboard = !Spectating&&PlayerSupportedBy(boat);
        Vector3 riderFoot=!Spectating?PlayerFootPoint:Vector3.zero;
        Vector3 riderOffset=!Spectating?player.transform.position-riderFoot:Vector3.zero;
        Vector3 localRider=boat.InverseTransformPoint(riderFoot);
        rudder=stations.IsFunctional(0)?NavalRigMath.StepRudder(rudder,turn,dt):0f;
        heading += NavalRigMath.RudderRate(speed, rudder)*stations.SteeringFactor * dt / (1f + depth);
        Vector3 flatForward = Quaternion.Euler(0f, heading, 0f) * Vector3.forward;
        Vector3 flatRight = Quaternion.Euler(0f, heading, 0f) * Vector3.right;
        float bowHeight = ocean.Height(boat.position.x + flatForward.x * 9f, boat.position.z + flatForward.z * 9f);
        float sternHeight = ocean.Height(boat.position.x - flatForward.x * 9f, boat.position.z - flatForward.z * 9f);
        float rightHeight = ocean.Height(boat.position.x + flatRight.x * 5.4f, boat.position.z + flatRight.z * 5.4f);
        float leftHeight = ocean.Height(boat.position.x - flatRight.x * 5.4f, boat.position.z - flatRight.z * 5.4f);
        float pitch = -Mathf.Atan2(bowHeight - sternHeight, 18f) * Mathf.Rad2Deg;
        pitch += Mathf.Clamp(hull.WaterLongitudinalMoment / (BoatDimensions.HalfHoldArea * hull.VolumeScale) * 15f,-15f,15f);
        float roll = Mathf.Atan2(rightHeight - leftHeight, 10.8f) * Mathf.Rad2Deg;
        roll=hull.Stability.Step(dt,NavalHullStability.Roll(boat),roll);
        // Added interior water increases draft; spring/damper approximates vertical buoyancy.
        float targetY = (bowHeight + sternHeight + rightHeight + leftHeight) * 0.25f - hull.Stability.Waterline - hull.SinkDepth;
        verticalVelocity += ((targetY - boat.position.y) * 10f - verticalVelocity * 5f) * dt;
        Vector3 oldPosition=boat.position;Quaternion oldRotation=boat.rotation;
        Quaternion smoothed=Quaternion.Slerp(oldRotation,Quaternion.Euler(pitch,heading,oldRotation.eulerAngles.z),1f-Mathf.Exp(-4f*dt));
        Quaternion nextRotation=Quaternion.Euler(smoothed.eulerAngles.x,smoothed.eulerAngles.y,roll);
        Vector3 next = boat.position + Vector3.up*verticalVelocity*dt + (flatForward * speed / (1f + depth) + ram.Drift) * dt * (hull.Sunk ? 0f : 1f);
        ram.Velocity=(next-oldPosition)/Mathf.Max(dt,0.001f);ram.AngularRate=Mathf.DeltaAngle(oldRotation.eulerAngles.y,nextRotation.eulerAngles.y)/Mathf.Max(dt,0.001f);
        if(NavalShipContact.Constrain(oldPosition,oldRotation,ref next,ref nextRotation,enemy!=null?enemy.Boat:null,boat))
        {NavalRamDamage other=enemy!=null?enemy.Boat.GetComponent<NavalRamDamage>():null;ram.Contact(other);ram.PushContact(other);helmNavigator.ShipBlocked();rudder*=.7f;heading=nextRotation.eulerAngles.y;}
        float rigRetention=NavalRigContact.Resolve(ram,oldPosition,oldRotation,ref next,ref nextRotation,enemy!=null?enemy.Boat.GetComponent<NavalRamDamage>():null);
        if(rigRetention<1f){speed*=rigRetention;heading=nextRotation.eulerAngles.y;}
        if(seaMap!=null&&seaMap.ConstrainShore(boat,ref next,ref nextRotation))
        {if(new Vector2(next.x-boat.position.x,next.z-boat.position.z).sqrMagnitude<.00000001f){speed=0f;ram.StopMomentum();}heading=nextRotation.eulerAngles.y;rudder=0f;helmNavigator.LandBlocked();}
        SpeedThroughWater=NavalShipSpeedMath.Measure(next.x-oldPosition.x,next.z-oldPosition.z,dt);
        hull.PhysicsBody.SetPose(next,nextRotation);
        ocean.Focus = ViewCamera!=null?ViewCamera.transform.position:boat.position;
        ocean.RenderWaves(boat, speed);
        if (aboard)
        {
            // Apply the support's rigid transform directly. Sweeping against the already moved
            // floor can block upward carry or push the rider sideways into the hull.
            CharacterController controller = GetController();
            bool enabled = controller != null && controller.enabled;
            if (enabled) controller.enabled = false;
            player.transform.position = NavalRiderCarry.Position(boat,localRider,riderOffset);
            if (enabled) controller.enabled = true;
        }
        // Publish hull and supported rider together before any controller movement/query.
        NavalPerformance.SyncTransforms();
        if(!Spectating)
        {
        if(boarding.Climbing){SetSwimming(false);boarding.Apply(dt);if(VelocityField!=null)VelocityField.SetValue(player,Vector3.zero);}
        else UpdateSwimming(aboard, dt);
        if (stations.Operating)
        {
            CharacterController controller = GetController();
            bool enabled = controller != null && controller.enabled;
            if (enabled) controller.enabled = false;
            player.transform.position = stations.OperatorPosition + Vector3.up * footOffset;
            if (enabled) controller.enabled = true;
            if (VelocityField != null) VelocityField.SetValue(player, Vector3.zero);
        }
        }
    }
    public float OceanLevelAt(Vector3 point){return ocean.Height(point.x,point.z);}
    public float WaterLevelAt(Vector3 point)
    {
        float internalLevel;
        if(TryInternalWater(hull,point,out internalLevel))return internalLevel;
        if(enemy!=null&&TryInternalWater(enemy.Hull,point,out internalLevel))return internalLevel;
        return ocean.Height(point.x,point.z);
    }
    public float VisualWaterLevelAt(Vector3 point)
    {
        // Evaluate full hull submergence now, independent of the prior frame's
        // flood-cube visibility. A fully submerged interior uses ocean depth.
        for(int index=0;index<2;index++)
        {
            NavalHull ship=index==0?hull:enemy!=null?enemy.Hull:null;
            if(ship==null||ship.Root==null)continue;
            Vector3 local=ship.Root.InverseTransformPoint(point);
            if(Mathf.Abs(local.x)<ship.Definition.HullHalfWidth(local.z)&&Mathf.Abs(local.z)<BoatDimensions.HalfLength&&local.y>=ship.Floor-.3f&&
                NavalFloodAppearance.Clearance(ship.Root,ocean)<0f)return ocean.Height(point.x,point.z);
        }
        return WaterLevelAt(point);
    }
    private bool TryInternalWater(NavalHull ship,Vector3 point,out float level)
    {
        level=0f;if(ship==null||ship.Root==null||ship.FloodEffectsHidden)return false;
        Vector3 local=ship.Root.InverseTransformPoint(point);
        if(Mathf.Abs(local.x)>=ship.Definition.HullHalfWidth(local.z)-0.125f||Mathf.Abs(local.z)>=BoatDimensions.HalfLength-0.125f||
            local.y<ship.Floor-0.3f||local.y>=ship.Definition.RimY)return false;
        float depth=ship.WaterDepthAt(local);
        // An empty cell is dry even when the outside sea is above this deck.
        if(depth<0.002f){level=float.NegativeInfinity;return true;}
        float verticalLocal=ship.Root.InverseTransformVector(Vector3.up).y;
        level=NavalInteriorWaterMath.WorldLevel(point.y,local.y,ship.Floor+depth,verticalLocal);
        return true;
    }
    private bool WaterContact(bool aboard, out float sea)
    {
        Vector3 footPoint=player.transform.position-Vector3.up*footOffset;
        // The same local flood query serves swimming, sailors, breath and camera,
        // including a partially sunk ship and the enemy hold.
        sea=WaterLevelAt(footPoint);
        float feet=footPoint.y;
        // A submerged deck must not prevent a character from floating off a sinking ship.
        float bodyHeight = Mathf.Max(1f, neckOffset + footOffset);
        float immersion = sea - feet;
        // Feet/knee-deep contact preserves normal walking; swimming starts at waist depth.
        return NavalSwimMovement.InWater(immersion,bodyHeight,swimming);
    }
    private void DamageAimedPart()
    {
        Camera camera = player.GetPlayerCamera();
        if (camera == null) return;
        NavalPerformance.SyncTransforms();
        RaycastHit[] hits = Physics.RaycastAll(camera.transform.position, camera.transform.forward,
            40f, ~0, QueryTriggerInteraction.Collide);
        Array.Sort(hits, delegate(RaycastHit a, RaycastHit b) { return a.distance.CompareTo(b.distance); });
        damageMessage = "G: no ship part in sight (range 40 m)";
        foreach (RaycastHit hit in hits)
        {
            // Third-person cameras can hit the player's own body before reaching the ship.
            if (hit.collider.GetComponentInParent<Character>() == player || hit.collider.transform.IsChildOf(player.transform)) continue;
            NavalHullVoxel voxel = NavalVoxelHit.Resolve(hit.collider,hit.point);
            if (voxel != null && voxel.Owner != null)
            {
                NavalHull aimedHull=voxel.Owner;
                Vector3 aimed = aimedHull.Root.InverseTransformPoint(hit.point);
                int removed = aimedHull.Damage(aimed, 0.42f);
                // The billowing cloth sits in front of the black mast. A cut
                // through its centre must also reach the spar behind it.
                if(!voxel.HydraulicShell&&Mathf.Abs(aimed.x)<.45f&&aimed.y>3.7f&&aimed.y<7.8f)
                    foreach(float mastZ in aimedHull.Definition.MastZ)
                        if(aimed.z>mastZ-.1f&&aimed.z<mastZ+.7f)removed+=aimedHull.Damage(new Vector3(aimed.x,aimed.y,mastZ),.42f);
                // Always remove the selected cell, even at a distant corner of a long thin voxel.
                if (!voxel.Missing) { voxel.SetMissing(true); removed++; }
                NavalPerformance.SyncTransforms();
                damageMessage = "G: removed " + removed + " voxels / " + (voxel.HydraulicShell ? "hull" : "ship decoration");
                break;
            }
            if (hit.collider.isTrigger) continue;
            damageMessage = "G: view blocked by " + hit.collider.gameObject.name;
            break;
        }
        damageMessageUntil = Time.time + 5f;
        Debug.Log("[NavalPrototype] " + damageMessage);
    }
    public void PrepareWaterEntry(FirstPersonMover mover)
    {
        if(mover==player&&boarding!=null&&boarding.Climbing)
        {if(VelocityField!=null)VelocityField.SetValue(player,Vector3.zero);return;}
        if (mover != player || ocean == null || hull == null || !GameModeManager.IsSinglePlayer() ||
            !player.IsAlive() || !player.IsPlayerInputEnabled() || Time.timeScale == 0f) return;
        float sea;
        // Called before the game's movement step, not after a full frame of falling.
        bool supported=PlayerSupportedBy(boat)||(EnemyBoat!=null&&PlayerSupportedBy(EnemyBoat));
        SetSwimming(WaterContact(supported, out sea));
        if ((swimming || stationLocked) && VelocityField != null) VelocityField.SetValue(player, Vector3.zero);
    }
    private void UpdateSwimming(bool aboard, float dt)
    {
        float sea;
        SetSwimming(WaterContact(aboard, out sea));
        if (!swimming || stationLocked) return;
        Camera camera = player.GetPlayerCamera();
        Vector3 forward = camera != null ? camera.transform.forward : player.transform.forward;
        forward.y = 0f; forward.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, forward);
        float horizontal = (Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f);
        float longitudinal = (Input.GetKey(KeyCode.W) ? 1f : 0f) - (Input.GetKey(KeyCode.S) ? 1f : 0f);
        Vector3 direction = Vector3.ClampMagnitude(forward * longitudinal + right * horizontal, 1f);
        // Keep the neck at sea level, including when Space is held: never stand on the water.
        bool diving = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        float target = sea - neckOffset;
        float lift = Mathf.Clamp((target - player.transform.position.y) * 6f, -2f,
            Input.GetKey(KeyCode.Space) ? 6f : 4f);
        if (diving) lift = -2.8f;
        // Neutralise accumulated ground gravity while submerged; never disable normal simulation.
        if (VelocityField != null) VelocityField.SetValue(player, Vector3.zero);
        float speedLimit = NavalSwimMovement.SpeedLimit(player.GetBaseMoveSpeed());
        swimVelocity = Vector3.MoveTowards(swimVelocity, direction * speedLimit, dt * 3f);
        Vector3 movement = (swimVelocity + Vector3.up * lift) * dt;
        CharacterController controller = GetController();
        if (controller != null && controller.enabled) controller.Move(movement);
        else player.transform.position += movement;
    }
    public Vector3 PlayerFootPoint {get{
        CharacterController body=GetController();
        if(body!=null&&body.enabled){Bounds bounds=body.bounds;return new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);}
        return player.transform.position-Vector3.up*footOffset;
    }}
    public int PlayerSupportFallbacks {get;private set;}
    public bool PlayerSupportedBy(Transform ship){
        if(Spectating||player==null||ship==null)return false;
        float radius=PlayerSupportRadius;
        // Cast from just above the actual capsule sole, independent of model/root height.
        Vector3 sole=PlayerFootPoint;
        if(NavalPhysicsQueries.HasSupport(sole+Vector3.up*(radius+.25f),Vector3.down,.5f,ship,radius))return true;
        // A sphere starting against an edge may miss its top. Probe actual footprint points.
        float spread=radius*.7f;Vector3 origin=sole+Vector3.up*.35f;
        for(int i=0;i<5;i++){
            Vector3 offset=i==0?Vector3.zero:i==1?Vector3.right*spread:i==2?Vector3.left*spread:i==3?Vector3.forward*spread:Vector3.back*spread;
            if(NavalPhysicsQueries.HasSupport(origin+offset,Vector3.down,.6f,ship,0f)){PlayerSupportFallbacks++;return true;}
        }
        return false;
    }
    public float PlayerSupportRadius {get{
        CharacterController body=GetController();
        return body!=null?Mathf.Max(.12f,body.radius*Mathf.Max(Mathf.Abs(body.transform.lossyScale.x),Mathf.Abs(body.transform.lossyScale.z))):.35f;
    }}
    private CharacterController GetController()
    {
        if (player == null) return null;
        CharacterController controller = ControllerField != null ? ControllerField.GetValue(player) as CharacterController : null;
        return controller != null ? controller : player.GetComponent<CharacterController>();
    }
    private void SetSwimming(bool value)
    {
        if (value && stationLocked) { stations.Exit(); SetStationLock(false); }
        if (value == swimming) return;
        if (player != null)
        {
            if (value)
            {
                savedMoveMultiplier = player.MoveFromInputMultiplier;
                savedAcceleration = player.AccelerationFromMoveInput;
                player.MoveFromInputMultiplier = 0f;
                player.AccelerationFromMoveInput = 0f;
            }
            else
            {
                player.MoveFromInputMultiplier = savedMoveMultiplier;
                player.AccelerationFromMoveInput = savedAcceleration;
            }
        }
        swimVelocity = Vector3.zero;
        swimming = value;
    }
    private void SetStationLock(bool value)
    {
        if (value == stationLocked) return;
        if (value)
        {
            SetSwimming(false);
            stationMoveMultiplier = player.MoveFromInputMultiplier;
            stationAcceleration = player.AccelerationFromMoveInput;
            player.MoveFromInputMultiplier = 0f; player.AccelerationFromMoveInput = 0f;
        }
        else if (player != null)
        {
            player.MoveFromInputMultiplier = stationMoveMultiplier;
            player.AccelerationFromMoveInput = stationAcceleration;
        }
        stationLocked = value;
    }
    private void OnGUI()
    {
        if(boarding!=null&&player!=null&&player.IsAlive()&&player.IsPlayerInputEnabled())boarding.Draw();
        if(boat==null)return;
        DrawStructuredHud();
        GUI.Label(new Rect(Screen.width*.5f-5f,Screen.height*.5f-10f,20f,20f),"+");
        if(Time.time<damageMessageUntil)GUI.Box(new Rect(NavalHudLayout.ContextX(Screen.width,Screen.height,500f),Screen.height-285f,NavalHudLayout.ContextWidth(Screen.width,Screen.height,500f),28f),damageMessage);
    }
    private void OnDestroy()
    {
        CloseShipMenu();
        EndSpectating();
        if (Active == this){Active = null;NavalCharacters.Reset();NavalWorldProgressBar.Reset();}
        SetStationLock(false);
        SetSwimming(false);
        foreach (Material material in materials) if (material != null) Destroy(material);
    }
}
