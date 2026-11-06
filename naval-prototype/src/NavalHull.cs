using System.Collections.Generic;
using UnityEngine;

public class NavalHullVoxel : MonoBehaviour
{
    internal int AimSampleStamp;
    internal float AimWidthSlope;
    internal bool AimRootCell;
    internal Vector3 AimSampleWorld;
    internal float AimSampleDepth;
    public Vector3 LocalCenter;
    public float OpeningArea;
    public bool Missing;
    public bool HydraulicShell = true;
    public bool InternalBarrier;
    public bool RepairableEquipment;
    public NavalRepairableMechanism RepairMechanism;
    public bool ManagedRig;
    public bool MovableEquipment;
    public bool RigFabric;
    public bool Flammable;
    public bool Armored;
    // Metal equipment is not necessarily part of the armoured hydraulic shell.
    // Keep its impact treatment independent so gun damage never receives wood scars.
    public bool Metallic;
    public bool MastTimber;
    public float CannonWear;
    public bool Detached;
    public bool Batched;
    public NavalVoxelBatch BatchOwner;
    public int RenderChunk=-1;
    public NavalShipVisuals RigOwner;
    public NavalRigRenderBatch RigBatchOwner;
    public int RigBatchIndex=-1;
    public NavalStructure StructureOwner;
    public NavalHullColliders ColliderBatchOwner;
    public int ColliderChunk=-1,ColliderSourceIndex=-1;
    public bool ColliderBatched;
    // Fixed visual trim participates in rendering/structural support but does
    // not need an attached PhysX trigger. Falling structure promotes it to a
    // normal debris collider.
    public bool NonPhysicalDecoration;
    public NavalCargoCollision CargoCollision;
    public bool DamageIndexed;
    internal Vector3 DamageLocalCenter;
    public void NotifyMaterialChanged(){if(BatchOwner!=null)BatchOwner.MarkDirty(this);if(RigBatchOwner!=null)RigBatchOwner.MarkDirty();}
    public void NotifyVisualChanged()
    {if(CargoCollision!=null)CargoCollision.SourceChanged();if(BallastOwner!=null){if(Missing||Detached)BallastOwner.Breach(this);else BallastOwner.ResealIfIntact();}if(ColliderBatchOwner!=null)ColliderBatchOwner.MarkDirty(this);if(Owner!=null){Owner.NoteCellMoved(this);if(Owner.PhysicsBody!=null)Owner.PhysicsBody.MarkDirty(this);}if(BatchOwner!=null)BatchOwner.MarkDirty(this);if(RigOwner!=null)RigOwner.MarkRigDirty();if(StructureOwner!=null)StructureOwner.MarkDirty();if(RigBatchOwner!=null)RigBatchOwner.MarkDirty();}
    private Collider cachedCollider;
    public Collider PhysicsCollider {get{if(cachedCollider==null)cachedCollider=GetComponent<Collider>();return cachedCollider;}}
    public NavalBallastUnit BallastOwner;
    public NavalHull Owner;
    public Material OriginalSurface;
    public bool WoodenPatch;
    public bool PatchOriginalArmored,PatchOriginalMetallic,PatchOriginalFlammable;
    public float ScorchStrength;
    public float InflowRate;
    public float LastWetAt=float.NegativeInfinity;
    public float LastWetFlow;
    public NavalRepairSailor RepairClaim;
    public NavalLeakStream Stream;
    public void SetMissing(bool value)
    {
        if(!value){CannonWear=0f;InflowRate=0f;if(Stream!=null)Stream.gameObject.SetActive(false);}
        if (Missing == value) return;
        Missing = value;NotifyVisualChanged();
        LastWetAt=float.NegativeInfinity;LastWetFlow=0f;
        if (Owner != null) Owner.Revision++;
        GetComponent<Renderer>().enabled = !value && !Batched;
        PhysicsCollider.enabled = !value&&!ColliderBatched&&!NonPhysicalDecoration;
        if (!value && Stream != null) Stream.gameObject.SetActive(false);
    }
    public void SetNonPhysicalDecoration(bool value)
    {NonPhysicalDecoration=value;PhysicsCollider.enabled=!Missing&&!ColliderBatched&&!value;}
}

public class NavalHull
{
    public NavalFireSystem Fire;
    public NavalBoilerProtection BoilerProtection;
    public NavalSteamPlant SteamMachinery;
    private readonly HashSet<Material> fireMaterials=new HashSet<Material>();
    public void RegisterFireMaterial(Material material){if(material!=null)fireMaterials.Add(material);}
    public void FireScorch(NavalHullVoxel cell)
    {cell.ScorchStrength=Mathf.Max(cell.ScorchStrength,.8f);style.Scorch(cell.gameObject,cell.ScorchStrength);cell.NotifyMaterialChanged();}
    public int Revision;
    public readonly List<NavalRepairSailor> RepairWorkers=new List<NavalRepairSailor>();
    public readonly List<NavalHullVoxel> Voxels = new List<NavalHullVoxel>();
    private int damagedRevision=-1;
    private readonly List<NavalHullVoxel> damagedVoxels=new List<NavalHullVoxel>();
    private readonly Dictionary<NavalHullVoxel,float> unavailableRepairLogAfter=new Dictionary<NavalHullVoxel,float>();
    public bool ShouldReportUnavailableRepair(NavalHullVoxel cell,float now)
    {
        if(cell==null)return false;
        float after;if(unavailableRepairLogAfter.TryGetValue(cell,out after)&&now<after)return false;
        unavailableRepairLogAfter[cell]=now+10f;return true;
    }
    private int deckGapRevision=-1;
    private readonly List<NavalHullVoxel> deckGaps=new List<NavalHullVoxel>();
    private struct DeckGapShape {public Vector3 Centre,Size;}
    private readonly List<DeckGapShape> deckGapShapes=new List<DeckGapShape>();
    private readonly NavalPointIndex deckGapIndex=new NavalPointIndex();
    private readonly List<int> nearbyDeckGaps=new List<int>();
    private float deckGapExtent;

    public List<NavalHullVoxel> GetDamagedVoxels()
    {
        if(damagedRevision!=Revision)
        {
            damagedVoxels.Clear();
            foreach(NavalHullVoxel cell in Voxels)if(cell.Missing||cell.Detached)damagedVoxels.Add(cell);
            damagedRevision=Revision;
        }
        return damagedVoxels;
    }
    private void RefreshDeckGaps()
    {
        if(deckGapRevision!=Revision)
        {
            deckGaps.Clear();deckGapShapes.Clear();deckGapIndex.Clear();deckGapExtent=0;
            foreach(var cell in GetDamagedVoxels())
            {
                if(cell==null||cell.ManagedRig||cell.MovableEquipment||cell.RepairableEquipment||cell.NonPhysicalDecoration)continue;
                Vector3 size=cell.transform.localScale;
                if(Mathf.Abs(size.y)<=.35f&&Mathf.Abs(size.x)>=.10f&&Mathf.Abs(size.z)>=.10f&&NavalDeckGapMath.IsDeckLayer(cell.LocalCenter.y,Mathf.Abs(size.y),Definition)){
                    Vector3 centre=cell.LocalCenter,extent=new Vector3(Mathf.Abs(size.x),Mathf.Abs(size.y),Mathf.Abs(size.z));
                    deckGapIndex.Add(deckGapShapes.Count,centre.x,centre.y,centre.z);
                    deckGapShapes.Add(new DeckGapShape{Centre=centre,Size=extent});deckGaps.Add(cell);
                    deckGapExtent=Mathf.Max(deckGapExtent,Mathf.Max(extent.x,Mathf.Max(extent.y,extent.z))*.5f);
                }
            }
            deckGapRevision=Revision;
        }
    }
    public bool DeckGapAt(Vector3 point,float radiusWorld)
    {
        RefreshDeckGaps();
        if(deckGapShapes.Count==0)return false;
        // Broad phase is conservative; the original exact footprint test remains final.
        float search=deckGapExtent+Mathf.Max(.08f,.45f/WorldScale)+Mathf.Max(.05f,radiusWorld)/WorldScale;
        deckGapIndex.Query(point.x,point.y,point.z,search,nearbyDeckGaps);
        for(int i=0;i<nearbyDeckGaps.Count;i++)
        {
            DeckGapShape shape=deckGapShapes[nearbyDeckGaps[i]];Vector3 size=shape.Size,p=shape.Centre;
            if(NavalDeckGapMath.Overlaps(point.x,point.y,point.z,radiusWorld,WorldScale,p.x,p.y,p.z,size.x,size.y,size.z))return true;
        }
        return false;
    }
    public void DeckGapsAlongStep(Vector3 start,Vector3 end,float radiusWorld,out bool currentGap,out bool proposedGap)
    {
        currentGap=proposedGap=false;
        RefreshDeckGaps();
        if(deckGapShapes.Count==0)return;
        float scale=WorldScale;
        Vector3 midpoint=(start+end)*.5f,delta=end-start;
        float reach=.5f*Mathf.Max(Mathf.Abs(delta.x),Mathf.Max(Mathf.Abs(delta.y),Mathf.Abs(delta.z)));
        float search=deckGapExtent+Mathf.Max(.08f,.45f/scale)+Mathf.Max(.05f,radiusWorld)/scale+reach+.0001f;
        deckGapIndex.Query(midpoint.x,midpoint.y,midpoint.z,search,nearbyDeckGaps);
        foreach(int index in nearbyDeckGaps)
        {
            DeckGapShape shape=deckGapShapes[index];Vector3 size=shape.Size,p=shape.Centre;
            if(!currentGap)currentGap=NavalDeckGapMath.Overlaps(start.x,start.y,start.z,radiusWorld,scale,p.x,p.y,p.z,size.x,size.y,size.z);
            if(!proposedGap)proposedGap=NavalDeckGapMath.Overlaps(end.x,end.y,end.z,radiusWorld,scale,p.x,p.y,p.z,size.x,size.y,size.z);
            if(currentGap&&proposedGap)return;
        }
    }
    public bool TryNearestDeckGap(Vector3 point,float deckY,float maxWorldDistance,out Vector3 gap)
    {
        RefreshDeckGaps();gap=Vector3.zero;float best=maxWorldDistance*maxWorldDistance;
        for(int i=0;i<deckGaps.Count;i++)
        {
            NavalHullVoxel cell=deckGaps[i];if(cell==null)continue;
            Vector3 p=cell.LocalCenter,size=cell.transform.localScale;
            // A deck voxel is centred below its walking surface. Reject fallen
            // horizontal fittings and holes belonging to another deck.
            float top=p.y+Mathf.Abs(size.y)*.5f;
            if(Mathf.Abs(top-deckY)*WorldScale>.45f)continue;
            float dx=(p.x-point.x)*WorldScale,dz=(p.z-point.z)*WorldScale,d=dx*dx+dz*dz;
            if(d>=best)continue;best=d;gap=new Vector3(p.x,deckY,p.z);
        }
        return best<maxWorldDistance*maxWorldDistance;
    }
    private readonly NavalPointIndex damageIndex=new NavalPointIndex();
    private readonly List<int> damageQuery=new List<int>();
    private readonly List<NavalHullVoxel> dynamicDamageCells=new List<NavalHullVoxel>(),damageCandidates=new List<NavalHullVoxel>();
    private int damageIndexCount=-1;
    public void NoteCellMoved(NavalHullVoxel cell)
    {
        if(cell.DamageIndexed&&cell.transform.parent!=root)
        {cell.DamageIndexed=false;dynamicDamageCells.Add(cell);}
    }
    private List<NavalHullVoxel> DamageCandidates(Vector3 point,float radius,NavalHullVoxel direct)
    {
        if(damageIndexCount!=Voxels.Count)
        {
            damageIndexCount=Voxels.Count;damageIndex.Clear();dynamicDamageCells.Clear();
            for(int i=0;i<Voxels.Count;i++)
            {
                NavalHullVoxel cell=Voxels[i];
                cell.DamageIndexed=cell.transform.parent==root&&!cell.ManagedRig&&!cell.MovableEquipment&&!cell.Detached;
                if(cell.DamageIndexed)
                {
                    Vector3 p=cell.transform.localPosition;cell.DamageLocalCenter=p;damageIndex.Add(i,p.x,p.y,p.z);
                }
                else dynamicDamageCells.Add(cell);
            }
        }
        damageCandidates.Clear();
        damageIndex.Query(point.x,point.y,point.z,Mathf.Max(0f,radius)+.571f,damageQuery);
        foreach(int index in damageQuery)if(Voxels[index].DamageIndexed)damageCandidates.Add(Voxels[index]);
        // Detached wreckage and remote moving fittings used to be appended to
        // every impact query. Late in a battle that made one armour hit scan a
        // large part of both broken ships. Cull them in world space before the
        // more expensive local-space damage loop.
        Vector3 worldPoint=root.TransformPoint(point);
        float worldRadius=(Mathf.Max(0f,radius)+.571f)*WorldScale,worldRadiusSq=worldRadius*worldRadius;
        foreach(NavalHullVoxel cell in dynamicDamageCells)
            if(cell!=null&&!cell.Missing&&!cell.Detached&&(cell.transform.position-worldPoint).sqrMagnitude<=worldRadiusSq)
                damageCandidates.Add(cell);
        if(direct!=null&&direct.Owner==this&&!damageCandidates.Contains(direct))damageCandidates.Add(direct);
        return damageCandidates;
    }
    public int HoleCount { get; private set; }
    public float SubmergedArea { get; private set; }
    public float Inflow { get; private set; }
    public float OpeningInflow {get;private set;}
    private float openingPressure;
    public float OpeningPressure {get{return openingPressure;}}
    public float SubmergedRimHead {get;private set;}
    private readonly NavalDownfloodEmergency downfloodEmergency=new NavalDownfloodEmergency();
    public bool UrgentTrim {get{return !Sunk&&downfloodEmergency.Active;}}
    public readonly NavalShipDefinition Definition;
    public NavalBilgeSystem Bilge;
    public float Floor {get{return Definition.HoldFloor;}}
    public float WorldScale {get{return Definition.Scale;}}
    public float AreaScale {get{return Definition.AreaScale;}}
    public float VolumeScale {get{return Definition.VolumeScale;}}
    public readonly NavalFloodGrid Flood;
    public readonly NavalHullStability Stability;
    public NavalHull():this(NavalShipDefinition.Sloop){}
    public NavalHull(NavalShipDefinition definition)
    {Definition=definition;Flood=new NavalFloodGrid(12,24,definition.Destroyer?definition.HullBeam/definition.Scale:BoatDimensions.HoldWidth,BoatDimensions.HoldLength,definition.Scale,definition.RimY-definition.HoldFloor,definition.DeckCount>2,definition.SteamPowered,definition.Destroyer?definition:null);Flood.ConfigureStorage(definition);Stability=new NavalHullStability(this);}
    private readonly float[] floodFloors = new float[12*24];
    public float LeftWater { get { return SideWater(true); } }
    public float RightWater { get { return SideWater(false); } }
    private float SideWater(bool port){float total=Flood.Side(port)+(Bilge!=null?Bilge.BallastSide(port):0f)+(Fire!=null&&(Fire.PlayerWaterPoint.x<0)==port?Fire.PlayerWater:0f);foreach(NavalRepairSailor worker in RepairWorkers)if(worker!=null&&(worker.CarriedSide<0f)==port)total+=worker.BucketWater;return total;}
    public float Volume { get { float total=Flood.Total+(Bilge!=null?Bilge.BallastVolume:0f)+(Fire!=null?Fire.PlayerWater:0f);foreach(NavalRepairSailor worker in RepairWorkers)if(worker!=null)total+=worker.BucketWater;return total; } }
    public float WaterLongitudinalMoment {get{return Flood.LongitudinalMoment+(Bilge!=null?Bilge.BallastLongitudinalMoment:0f);}}
    public Vector3 RepairAccess(NavalHullVoxel hole,Vector3 stand)
    {
        if(hole.RepairMechanism!=null&&hole.RepairMechanism.IsCannon)return hole.RepairMechanism.RepairPoint;
        NavalBallastUnit tank=hole.BallastOwner!=null?hole.BallastOwner:Bilge!=null?Bilge.BallastCover(hole):null;
        return tank!=null?tank.RepairAccess(hole.LocalCenter,stand):hole.LocalCenter;
    }
    public float WaterDepthAt(Vector3 local) { return Flood.Depth(Flood.Index(local.x,local.z)); }
    private readonly NavalCombatEmergency combatEmergency=new NavalCombatEmergency();
    private readonly NavalMaintenanceEscalation maintenanceEscalation=new NavalMaintenanceEscalation();
    public bool ParallelMaintenance {get{return maintenanceEscalation.Parallel||UrgentTrim;}}
    public bool CombatCritical {get{return combatEmergency.Critical;}}
    public bool RepairRetreat {get{return combatEmergency.RepairRetreat;}}
    public bool Sunk;
    public NavalCrewController CrewController;
    public float RescuePickupUntil;
    public bool SinkingDanger {get;private set;}
    private float capsizeSeconds,submergedHullSeconds;
    public bool SimulationRetired {get;internal set;}
    public bool FloodEffectsHidden;
    public bool HasActiveLeaks;
    public readonly List<NavalCannonSupply> CannonSupplies=new List<NavalCannonSupply>();
    public readonly List<NavalPlankSupply> PlankSupplies=new List<NavalPlankSupply>();
    public bool HasCannonStock {get{foreach(var stock in CannonSupplies)if(stock.Available)return true;return false;}}
    public bool HasPlankStock {get{foreach(var stock in PlankSupplies)if(stock.Available)return true;return false;}}
    public NavalWatertightBulkheads Watertight;
    public NavalCargo Cargo;
    public NavalCannonSupply CannonSupply;
    public NavalPlankSupply PlankSupply;
    public bool CrewCannonRepairAllowed=true;
    public bool HasRepairDemand;
    public float SinkDepth;
    private const float Cell = 0.25f;
    private Material streamMaterial;
    private Material repairPatchMaterial;
    private Transform root;
    public Transform Root { get { return root; } }
    private NavalVoxelStyle style;
    public NavalShipPhysics PhysicsBody {get;private set;}

    public void Build(Transform boat, Material wood, Material waterMaterial, Material floorMaterial,Material patchMaterial=null)
    {
        if(!Definition.Armored)RegisterFireMaterial(wood);RegisterFireMaterial(floorMaterial);
        streamMaterial = waterMaterial;
        repairPatchMaterial=patchMaterial!=null?patchMaterial:wood;
        root = boat;
        PhysicsBody=boat.gameObject.AddComponent<NavalShipPhysics>();PhysicsBody.Initialize(this);
        style = boat.gameObject.AddComponent<NavalVoxelStyle>();
        if(Definition.Destroyer){NavalDestroyerLayout.BuildHull(this,wood,floorMaterial);return;}
        // One cell through the shell: a removed cell is an actual through-hole.
        for (int z = 0; z < 48; z++) for (int y = 0; y < 16; y++)
        {
            Add(boat, wood, new Vector3(-BoatDimensions.HalfWidth, -2.1f + y * Cell, -5.875f + z * Cell));
            Add(boat, wood, new Vector3(BoatDimensions.HalfWidth, -2.1f + y * Cell, -5.875f + z * Cell));
        }
        for (int x = 0; x < 23; x++) for (int y = 0; y < 16; y++)
        {
            Add(boat, wood, new Vector3(-2.75f + x * Cell, -2.1f + y * Cell, -BoatDimensions.HalfLength));
            Add(boat, wood, new Vector3(-2.75f + x * Cell, -2.1f + y * Cell, BoatDimensions.HalfLength));
        }
        if(Definition.DeckCount>2)
        {
            int rows=Mathf.CeilToInt(Definition.UpperDeckOffset/Cell);float height=Definition.UpperDeckOffset/rows;
            for(int row=0;row<rows;row++)
            {
                float y=Definition.MiddleRimY+(row+.5f)*height;
                for(int z=0;z<48;z++)for(int side=-1;side<=1;side+=2)
                {
                    float pz=-5.875f+z*Cell;
                    float half=Definition.HullHalfWidth(pz),thickness=Cell+Mathf.Abs(Definition.HullHalfWidth(pz+Cell*.5f)-Definition.HullHalfWidth(pz-Cell*.5f));
                    if(CannonPort(side*half,y,pz,thickness*.5f,height*.5f,Cell*.5f))continue;
                    var cell=AddVoxel(boat,wood,new Vector3(side*half,y,pz),new Vector3(thickness,height,Cell),true,true);cell.OpeningArea=Cell*height;
                }
                for(int x=0;x<23;x++)for(int side=-1;side<=1;side+=2)
                {
                    float px=-2.75f+x*Cell;float cx,width;
                    if(!NavalHullOutline.ClipEndCell(px,Definition.HullHalfWidth(side*BoatDimensions.HalfLength),Cell,out cx,out width))continue;
                    var cell=AddVoxel(boat,wood,new Vector3(cx,y,side*BoatDimensions.HalfLength),new Vector3(width,height,Cell),true,true);cell.OpeningArea=width*height;
                }
            }
        }
        for (int x = 0; x < 23; x++) for (int z = 0; z < 48; z++)
            Add(boat, floorMaterial, new Vector3(-2.75f + x * Cell, -2.35f, -5.875f + z * Cell));
    }
    private void Add(Transform boat, Material material, Vector3 center)
    {
        if (Definition.DeckCount<3&&CannonPort(center.x, center.y, center.z, Cell * 0.5f, Cell * 0.5f, Cell * 0.5f)) return;
        bool bottom=center.y<BoatDimensions.HoldFloor;
        if(Definition.DeckCount>2)
        {
            if(Mathf.Abs(center.z)>5.99f&&Mathf.Abs(center.x)-Cell*.5f>=Definition.HullHalfWidth(center.z))return;
            if(!bottom&&Mathf.Abs(center.x)>2.99f)center.x=Mathf.Sign(center.x)*Definition.HullHalfWidth(center.z);
        }
        float low=Definition.HullY(center.y-Cell*.5f),high=Definition.HullY(center.y+Cell*.5f);
        center.y=(low+high)*.5f;
        if(Definition.DeckCount>2&&CannonPort(center.x,center.y,center.z,Cell*.5f,(high-low)*.5f,Cell*.5f))return;
        float thickness=Definition.DeckCount>2&&!bottom&&Mathf.Abs(center.z)<5.99f?
            Cell+Mathf.Abs(Definition.HullHalfWidth(center.z+Cell*.5f)-Definition.HullHalfWidth(center.z-Cell*.5f)):Cell;
        if(bottom&&Definition.DeckCount>2)
        {
            float half=Definition.HullHalfWidth(center.z)-Cell*.5f;
            if(center.x-Cell*.5f>=half||center.x+Cell*.5f<=-half)return;
            float left=Mathf.Max(-half,center.x-Cell*.5f),right=Mathf.Min(half,center.x+Cell*.5f);thickness=right-left;center.x=(left+right)*.5f;
        }
        if(Definition.DeckCount>2&&!bottom&&Mathf.Abs(center.z)>5.99f){
            float cx,width;if(!NavalHullOutline.ClipEndCell(center.x,Definition.HullHalfWidth(center.z),Cell,out cx,out width))return;
            thickness=width;center.x=cx;
        }
        NavalHullVoxel voxel=AddVoxel(boat, material, center, new Vector3(thickness,high-low,Cell), true, true);
        voxel.OpeningArea=bottom?thickness*Cell:Cell*(high-low);
    }
    public NavalHullVoxel AddVoxel(Transform boat, Material material, Vector3 center, Vector3 size, bool shell, bool solid)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = "NavalPrototype_HullVoxel";
        obj.layer = PhysicsManager.GetEnvironmentLayer();
        obj.transform.SetParent(boat, false); obj.transform.localPosition = center;
        obj.transform.localScale = size;
        obj.GetComponent<Renderer>().sharedMaterial = material;
        style.Apply(obj, material);
        NavalHullVoxel voxel = obj.AddComponent<NavalHullVoxel>();
        voxel.Owner = this;
        voxel.Flammable=fireMaterials.Contains(material);
        voxel.Armored=shell&&Definition.Armored;
        voxel.OriginalSurface = obj.GetComponent<Renderer>().sharedMaterial;
        voxel.LocalCenter = center; voxel.OpeningArea = Cell * Cell;
        voxel.HydraulicShell = shell;
        // Decorative cells still receive ray hits; sail cells do not block the robot.
        if (!solid) voxel.PhysicsCollider.isTrigger = true;
        Voxels.Add(voxel);
        return voxel;
    }
    public bool CannonPort(float x,float y,float z,float halfX,float halfY,float halfZ)
    {return Definition.IsOpening(x,y,z,halfX,halfY,halfZ);}
    public void AddDecorationBox(Transform boat, Material material, Vector3 center, Vector3 size, bool solid, bool cutCannonPorts = false)
    {
        int nx = Mathf.CeilToInt(size.x / Cell), ny = Mathf.CeilToInt(size.y / Cell), nz = Mathf.CeilToInt(size.z / Cell);
        Vector3 cell = new Vector3(size.x / nx, size.y / ny, size.z / nz);
        for (int x = 0; x < nx; x++) for (int y = 0; y < ny; y++) for (int z = 0; z < nz; z++)
        {
            Vector3 point = center + new Vector3((x + 0.5f) * cell.x - size.x * 0.5f,
                (y + 0.5f) * cell.y - size.y * 0.5f, (z + 0.5f) * cell.z - size.z * 0.5f);
            if (cutCannonPorts && CannonPort(point.x, point.y, point.z, cell.x * 0.5f, cell.y * 0.5f, cell.z * 0.5f)) continue;
            AddVoxel(boat, material, point, cell, false, solid);
        }
    }
    // The center is in hull coordinates. The radius is always in world metres.
    public int Damage(Vector3 localPoint, float radiusMetres,bool cannon=false,NavalHullVoxel direct=null,float cannonPower=1f,float armorPower=-1f,bool blastArmor=false)
    {
        using(NavalImpactTiming.Measure("Hull.Damage"))
        using(NavalPerformance.Measure(NavalPerfArea.HullDamage))
        {
        float damageRadius=Mathf.Max(0f,radiusMetres)/WorldScale;
        if(armorPower<0f)armorPower=cannonPower;
        bool penetrated=direct==null||!direct.Armored||NavalArmorMath.Penetrates(direct.CannonWear,armorPower);
        if(Fire!=null&&cannon)Fire.Impact(localPoint,damageRadius,cannonPower,penetrated,direct);
        int removed = 0,blastArmorHit=0,blastArmorBroken=0;
        Matrix4x4 toLocal=root.worldToLocalMatrix;
        foreach (NavalHullVoxel voxel in DamageCandidates(localPoint,damageRadius,direct))
        {
            if(voxel==null||voxel.Missing)continue;
            Vector3 position=voxel.DamageIndexed?voxel.DamageLocalCenter:toLocal.MultiplyPoint3x4(voxel.transform.position);
            float distance=(position-localPoint).magnitude;
            if(distance<=damageRadius||voxel==direct)
            {
                if(cannon&&voxel.Armored)
                {
                    if(voxel!=direct)
                    {
                        if(!blastArmor)
                        {
                            if(voxel.ScorchStrength<.25f){voxel.ScorchStrength=.25f;style.Impact(voxel.gameObject,voxel.ScorchStrength,true);voxel.NotifyMaterialChanged();}
                            continue;
                        }
                        float blastWear=NavalArmorMath.ExplosionArmorWear(armorPower,distance,damageRadius)/(Definition.CapitalShip?1.6f:1f);
                        if(blastWear<=.001f)continue;
                        blastArmorHit++;voxel.CannonWear=NavalArmorMath.AddWear(voxel.CannonWear,blastWear);
                        if(!NavalArmorMath.Broken(voxel.CannonWear))
                        {
                            voxel.ScorchStrength=Mathf.Max(voxel.ScorchStrength,Mathf.Clamp(.25f+blastWear*.16f,.25f,.72f));
                            style.Impact(voxel.gameObject,voxel.ScorchStrength,true);voxel.NotifyMaterialChanged();continue;
                        }
                        blastArmorBroken++;voxel.SetMissing(true);removed++;continue;
                    }
                    voxel.CannonWear=NavalArmorMath.AddWear(voxel.CannonWear,armorPower);
                    if(!NavalArmorMath.Broken(voxel.CannonWear))
                    {
                        if(voxel.ScorchStrength<.45f){voxel.ScorchStrength=.45f;style.Impact(voxel.gameObject,voxel.ScorchStrength,true);voxel.NotifyMaterialChanged();}
                        continue;
                    }
                }
                if(cannon&&voxel.MastTimber)
                {
                    voxel.CannonWear=NavalMastDurability.Accumulate(voxel.CannonWear,cannonPower);
                    if(!NavalMastDurability.Broken(voxel.CannonWear))
                    {
                        voxel.ScorchStrength=Mathf.Max(voxel.ScorchStrength,.45f);
                        style.Scorch(voxel.gameObject,voxel.ScorchStrength);voxel.NotifyMaterialChanged();
                        continue;
                    }
                }
                voxel.SetMissing(true);removed++;
            }
            else if(distance<damageRadius+0.57f*Mathf.Min(1f,3f/WorldScale))
            {
                // Stable irregular, voxel-shaped rim. It colours the actual
                // surviving cells rather than leaving floating decal planes.
                float noise=Mathf.Repeat(Mathf.Sin(position.x*12.9898f+position.y*78.233f+position.z*37.719f)*43758.5453f,1f);
                float band=(0.35f+noise*0.22f)*Mathf.Min(1f,3f/WorldScale);
                float strength=Mathf.Clamp01(1f-(distance-damageRadius)/band);
                if(strength>0.05f&&strength>voxel.ScorchStrength)
                {voxel.ScorchStrength=strength;style.Impact(voxel.gameObject,strength,NavalImpactMaterial.IsMetal(voxel.Armored,voxel.Metallic,false));voxel.NotifyMaterialChanged();}
            }
        }
        if(blastArmor&&blastArmorHit>0)Debug.Log("[NavalPrototype] Explosion pressure damaged armor: ship="+Root.name+" plates="+blastArmorHit+" broken="+blastArmorBroken+" power="+armorPower.ToString("F2"));
        return removed;
            }
    }
    public void ApplyWoodPatch(NavalHullVoxel voxel)
    {
        if(voxel==null||repairPatchMaterial==null||!NavalRepairMaterials.ReceivesWoodPatch(voxel))return;
        if(!voxel.WoodenPatch)
        {
            voxel.PatchOriginalArmored=voxel.Armored;voxel.PatchOriginalMetallic=voxel.Metallic;
            voxel.PatchOriginalFlammable=voxel.Flammable;voxel.WoodenPatch=true;
        }
        voxel.Armored=false;voxel.Metallic=false;voxel.Flammable=true;voxel.ScorchStrength=0f;
        style.Apply(voxel.gameObject,repairPatchMaterial);voxel.NotifyMaterialChanged();
    }
    private void ClearWoodPatch(NavalHullVoxel voxel)
    {
        if(voxel==null||!voxel.WoodenPatch)return;
        voxel.Armored=voxel.PatchOriginalArmored;voxel.Metallic=voxel.PatchOriginalMetallic;
        voxel.Flammable=voxel.PatchOriginalFlammable;voxel.WoodenPatch=false;
        voxel.GetComponent<Renderer>().sharedMaterial=voxel.OriginalSurface;voxel.NotifyMaterialChanged();
    }
    // Brittle iron plate can throw fragments into the ship even when it keeps
    // the sea out. Spall is deliberately restricted to internal structure and
    // equipment: it can create repair work, fire or casualties, but it cannot
    // manufacture a hydraulic opening through an intact shell cell.
    public int InternalSpall(Vector3 localPoint,float radius,float power)
    {
        using(NavalImpactTiming.Measure("Hull.InternalSpall")){
        if(radius<=0f||power<=0f)return 0;
        if(Fire!=null)Fire.Impact(localPoint,radius*.55f,power,true);
        int removed=0;
        foreach(NavalHullVoxel voxel in DamageCandidates(localPoint,radius,null))
        {
            if(voxel==null||voxel.Missing||voxel.Detached||voxel.HydraulicShell||voxel.Armored)continue;
            if(!voxel.RepairableEquipment&&!voxel.InternalBarrier&&!voxel.MovableEquipment)continue;
            Vector3 position=root.InverseTransformPoint(voxel.transform.position);
            float distance=(position-localPoint).magnitude;if(distance>radius)continue;
            float noise=Mathf.Repeat(Mathf.Sin(position.x*19.17f+position.y*43.71f+position.z*91.13f)*15731.743f,1f);
            float chance=Mathf.Clamp01((1f-distance/Mathf.Max(.01f,radius))*.70f+power*.10f);
            if(noise>chance)continue;
            voxel.SetMissing(true);removed++;
        }
        if(removed>0)Debug.Log("[NavalPrototype] Internal armor spall damaged equipment: ship="+Root.name+" cells="+removed+" power="+power.ToString("F2")+" point="+localPoint);
        return removed;
        }
    }
    public void Repair()
    {
        if(Fire!=null)Fire.ResetFire();
        if(Cargo!=null)Cargo.ResetCargo();
        var controls=NavalStations.ForShip(root);if(controls!=null&&controls.Torpedoes!=null)controls.Torpedoes.ResetLoaded();
        foreach(var stock in CannonSupplies)stock.ResetStock();
        HashSet<NavalRepairableMechanism> equipment=new HashSet<NavalRepairableMechanism>();
        foreach (NavalHullVoxel voxel in Voxels)
        {
            if(voxel.RepairMechanism!=null)equipment.Add(voxel.RepairMechanism);
            voxel.CannonWear=0f;
            if (voxel.Missing) voxel.SetMissing(false);
            ClearWoodPatch(voxel);
            if(voxel.ScorchStrength>0f)
            {voxel.GetComponent<Renderer>().sharedMaterial=voxel.OriginalSurface;voxel.ScorchStrength=0f;voxel.NotifyVisualChanged();}
        }
        if(Bilge!=null)Bilge.ResetBallastSeals();
        foreach(NavalRepairableMechanism mechanism in equipment)mechanism.Reset(); // Explicit debug full restoration only.
    }
    public void RefreshFloodSections()
    {
        var removed=new bool[Flood.Rows];
        var attached=new bool[Flood.Rows];
        foreach(var cell in Voxels)
        {
            if(!cell.HydraulicShell||cell.LocalCenter.y>Floor+.10f)continue;
            int row=Flood.RowAt(cell.LocalCenter.z);
            if(cell.Detached)removed[row]=true;
            else if(!cell.Missing)attached[row]=true;
        }
        for(int row=0;row<removed.Length;row++)removed[row]&=!attached[row];
        Flood.SetDetachedRows(removed);
    }
    private void ObserveRimPressure(Transform boat,NavalWater ocean,float x,float z,float width,float dt,Vector3 inward)
    {
        float rim=Definition.RimAt(z);Vector3 lip=boat.TransformPoint(new Vector3(x,rim,z));int index=Flood.Index(x,z);
        if(!Flood.IsActive(index))return;
        float inside=NavalDownfloodMath.InsideAtOpening(lip.y,rim,Floor,Flood.Depth(index),boat.up.y,WorldScale);
        SubmergedRimHead=Mathf.Max(SubmergedRimHead,ocean.Height(lip.x,lip.z)-lip.y);
        float flow=NavalDownfloodMath.OverflowRate(ocean.Height(lip.x,lip.z),lip.y,inside,width);
        openingPressure+=flow;
        float accepted=Flood.Add(index,flow*dt);Inflow+=accepted/dt;OpeningInflow+=accepted/dt;
        if(Fire!=null&&accepted>.00001f)Fire.Wash(lip,boat.TransformVector(inward),accepted/dt);
    }
    public void Step(Transform boat, NavalWater ocean, float dt, bool pump)
    {
        using(NavalPerformance.Measure(NavalPerfArea.Flood))
        {
        if(Sunk)
        {
            SinkDepth=Mathf.Min(5f,SinkDepth+dt*.2f);
            return;
        }
        FloodEffectsHidden=NavalFloodVisibilityMath.HideEffects(NavalFloodAppearance.Clearance(boat,ocean));
        HoleCount = 0; SubmergedArea = 0f; Inflow = 0f;OpeningInflow=0f;openingPressure=0f;SubmergedRimHead=0f;HasActiveLeaks=false;HasRepairDemand=false;
        int visibleStreams = 0;
        foreach (NavalHullVoxel voxel in GetDamagedVoxels())
        {
            voxel.InflowRate=0f;
            if(voxel.Detached||Flood.RowDetached(Flood.RowAt(voxel.LocalCenter.z)))
            {
                voxel.LastWetAt=float.NegativeInfinity;
                voxel.LastWetFlow=0f;
                if(voxel.Stream!=null&&voxel.Stream.gameObject.activeSelf)voxel.Stream.gameObject.SetActive(false);
                continue;
            }
            if (!voxel.Missing) continue;
            HoleCount++;
            if (!voxel.HydraulicShell) continue;
            Vector3 point = boat.TransformPoint(voxel.LocalCenter);
            int cellIndex = Flood.Index(voxel.LocalCenter.x,voxel.LocalCenter.z);
            float internalY = boat.TransformPoint(new Vector3(Flood.X(cellIndex),
                Floor + Flood.Depth(cellIndex),Flood.Z(cellIndex))).y;
            float sea = ocean.Height(point.x, point.z);
            NavalBallastUnit connected=Bilge!=null?Bilge.SeaConnection(voxel):null;
            if(connected!=null&&!connected.Tank.InnerBroken)
                internalY=boat.TransformPoint(new Vector3(connected.Tank.X,connected.Floor+NavalBallastLayout.For(Definition).Height/WorldScale*connected.Tank.FillFraction,connected.Tank.Z)).y;
            // Partial submergence of a voxel opening changes its effective wet area.
            Vector3 cellSize=voxel.transform.lossyScale;
            float halfHeight=.5f*(Mathf.Abs(boat.right.y*cellSize.x)+Mathf.Abs(boat.up.y*cellSize.y)+Mathf.Abs(boat.forward.y*cellSize.z));
            float area;
            float flow = FloodMath.OpeningFlow(voxel.OpeningArea * AreaScale, sea, internalY, point.y, halfHeight, out area);
            SubmergedArea += area;
            float amount=connected!=null&&!connected.Tank.InnerBroken?
                (sea>point.y-halfHeight?connected.Tank.FloodFromSea(connected.SeaTarget(boat,sea)):0f):Flood.Add(cellIndex,flow*dt);
            voxel.InflowRate=Mathf.Max(0f,amount/dt);
            if(voxel.InflowRate>0.00001f)HasActiveLeaks=true;
            if(area>0f&&flow>0.00001f){voxel.LastWetAt=Time.time;voxel.LastWetFlow=Mathf.Max(0.001f,flow);}
            if(NavalLeakPriority.Score(voxel.InflowRate,voxel.LastWetAt,voxel.LastWetFlow,Time.time)>0f)HasRepairDemand=true;
            if(Fire!=null&&amount>.00001f&&(connected==null||connected.Tank.InnerBroken))
            {
                Vector3 inward=NavalLeakVisualMath.Inward(voxel.LocalCenter,Definition);
                Fire.Wash(point,boat.TransformVector(inward),amount/dt);
            }
            Inflow += amount / dt;
            bool show = !FloodEffectsHidden && amount > 0.00001f && area > 0f && visibleStreams < 32;
            if (show)
            {
                visibleStreams++;
                if (voxel.Stream == null)
                {
                    GameObject obj = new GameObject("NavalPrototype_IncomingWater");
                    obj.transform.SetParent(boat, false);
                    voxel.Stream = obj.AddComponent<NavalLeakStream>();
                    voxel.Stream.Initialize(streamMaterial);
                }
                voxel.Stream.gameObject.SetActive(true);
                Vector3 origin = point; origin.y = Mathf.Min(point.y, sea - 0.02f);
                Vector3 inward = voxel.LocalCenter.y < Floor ? Vector3.up : NavalLeakVisualMath.Inward(voxel.LocalCenter,Definition);
                voxel.Stream.Animate(boat.InverseTransformPoint(origin), inward, amount / dt, Time.time);
            }
            else if (voxel.Stream != null) voxel.Stream.gameObject.SetActive(false);
        }
        // Open gunports are the lowest intact openings above the deck.
        for(int gun=0;!Definition.RifledWeapons&&gun<Definition.GunCount;gun++)
        {
            float x=NavalShipDefinition.GunSide(gun)*BoatDimensions.HalfWidth,z=NavalShipDefinition.GunZ(Definition,gun);
            float lip=Definition.GunPortSill;
            Vector3 opening=boat.TransformPoint(new Vector3(x,lip,z));
            int index=Flood.Index(x,z);
            if(!Flood.IsActive(index))continue;
            float inside=NavalDownfloodMath.InsideAtOpening(opening.y,lip,Floor,Flood.Depth(index),boat.up.y,WorldScale);
            float width=2.2f*WorldScale;
            float flow=NavalDownfloodMath.OverflowRate(ocean.Height(opening.x,opening.z),opening.y,inside,width);
            openingPressure+=flow;
            float accepted=Flood.Add(Flood.Index(x,z),flow*dt);Inflow+=accepted/dt;OpeningInflow+=accepted/dt;
            if(Fire!=null&&flow>.001f)Fire.Wash(opening,boat.TransformVector(new Vector3(-Mathf.Sign(x),0,0)),flow);
        }
        // Water over intact hull edges is an actual source, not just a danger signal.
        for(int side=-1;side<=1;side+=2)
        {
            for(int segment=0;segment<12;segment++)
            {
                float z=-5.5f+segment,width=1f;
                // The gunport/ladder source above already accounts for these strips.
                for(int gun=0;!Definition.RifledWeapons&&gun<Definition.GunCount;gun+=2)
                    width-=NavalRimFlowMath.Overlap(z,1f,NavalShipDefinition.GunZ(Definition,gun),2.2f);
                width=Mathf.Max(0,width);if(width<=.001f)continue;
                float x=side*Definition.HullHalfWidth(z);
                ObserveRimPressure(boat,ocean,x,z,width*WorldScale,dt,new Vector3(-side,0,0));
            }
            float endHalf=Definition.HullHalfWidth(side*BoatDimensions.HalfLength);
            int pieces=Mathf.Max(1,Mathf.CeilToInt(endHalf*2));float span=endHalf*2/pieces;
            for(int segment=0;segment<pieces;segment++)
                ObserveRimPressure(boat,ocean,-endHalf+(segment+.5f)*span,side*BoatDimensions.HalfLength,span*WorldScale,dt,new Vector3(0,0,-side));
        }
        if(Flood.AttachedFraction<1f)
        for(int row=0;row<Flood.Rows;row++)
        {
            if(Flood.RowDetached(row))continue;
            for(int side=-1;side<=1;side+=2)
            {
                if(!Flood.RowDetached(row+side))continue;
                for(int col=0;col<Flood.Columns;col++)
                {
                    int index=row*Flood.Columns+col;
                    if(!Flood.IsActive(index))continue;
                    float height=(Definition.RimAt(Flood.Z(index))-Floor)*.5f;
                    Vector3 edge=boat.TransformPoint(new Vector3(Flood.X(index),Floor+height,Flood.Z(index)+side*Flood.CellLength*.5f));
                    float inside=boat.TransformPoint(new Vector3(Flood.X(index),Floor+Flood.Depth(index),Flood.Z(index))).y;
                    float area;
                    float flow=FloodMath.OpeningFlow(Flood.CellWidth*height*2f*WorldScale*WorldScale,ocean.Height(edge.x,edge.z),inside,edge.y,height*WorldScale,out area);
                    float accepted=Flood.Add(index,flow*dt);
                    Inflow+=accepted/dt;
                    OpeningInflow+=accepted/dt;
                    openingPressure+=flow;
                }
            }
        }
        Matrix4x4 floodPose=boat.localToWorldMatrix;
        Flood.FillFloorHeights(floodFloors,floodPose.m10,floodPose.m12,floodPose.m11*Floor,floodPose.m13);
        if(BoilerProtection!=null)BoilerProtection.RestoreSills();
        if(Watertight!=null)Watertight.RestoreSills();
        if(Bilge!=null)Bilge.UpdateBarriers();
        if(Watertight!=null)Watertight.ApplySills();
        if(BoilerProtection!=null)BoilerProtection.ApplySills();
        Flood.Spread(dt,floodFloors,boat.up.y);
        if(pump)Flood.Pump(BoatDimensions.PumpRate*AreaScale*dt);
        float remainingCapacity=Definition.SinkingVolume*Mathf.Max(.01f,Flood.AttachedFraction);
        float floodFraction=Volume/remainingCapacity,heel=NavalHullStability.Roll(boat);
        downfloodEmergency.Step(dt,HasActiveLeaks,openingPressure,NavalBroadsideMath.Delta(Stability.WaveRoll,heel),Mathf.Atan2(-boat.forward.y,boat.up.y)*Mathf.Rad2Deg);
        combatEmergency.Step(floodFraction,heel,HasActiveLeaks&&Inflow>NavalMaintenanceMath.PumpOutRate&&Mathf.Abs(heel)>=22f,Definition.Destroyer);
        if(Definition.Destroyer)
        {
            float absoluteHeel=Mathf.Abs(heel);
            SinkingDanger=absoluteHeel>=50f||floodFraction>=.65f&&SubmergedRimHead>.15f;
            capsizeSeconds=absoluteHeel>=65f?capsizeSeconds+dt:0f;
            if(capsizeSeconds>=4f)Sunk=true;
        }
        maintenanceEscalation.Step(dt,floodFraction,heel,CombatCritical,HasActiveLeaks&&Inflow>NavalMaintenanceMath.PumpOutRate&&Mathf.Abs(heel)>=12f);
        if (Volume >= remainingCapacity) Sunk = true;
        if(!Sunk)
        {
            // Test the exposed hull rather than waiting for every flood cell
            // to fill. A passing crest alone must not retire a ship.
            float rim=Mathf.Max(Definition.WeatherDeck,Mathf.Max(Definition.RimAt(-BoatDimensions.HalfLength),Definition.RimAt(BoatDimensions.HalfLength)));
            Vector3 center=boat.TransformPoint(Vector3.up*rim);
            bool submerged=center.y<ocean.Height(center.x,center.z)-.25f;
            for(int x=-1;x<=1&&submerged;x+=2)for(int z=-1;z<=1&&submerged;z+=2)
            {
                Vector3 corner=boat.TransformPoint(new Vector3(x*BoatDimensions.HalfWidth,rim,z*BoatDimensions.HalfLength));
                submerged=corner.y<ocean.Height(corner.x,corner.z)-.25f;
            }
            submergedHullSeconds=submerged?submergedHullSeconds+dt:0f;
            if(submergedHullSeconds>=1.5f)
            {
                Sunk=true;
                Debug.Log("[NavalPrototype] Hull fully submerged: ship="+boat.name+" target retired");
            }
        }
        if (Sunk) SinkDepth = Mathf.Min(5f, SinkDepth + dt * 0.2f);
            }
    }
}
