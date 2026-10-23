using System.Collections.Generic;
using UnityEngine;

public class NavalHullVoxel : MonoBehaviour
{
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
    public int ColliderChunk=-1;
    public bool ColliderBatched;
    public NavalCargoCollision CargoCollision;
    public bool DamageIndexed;
    public void NotifyMaterialChanged(){if(BatchOwner!=null)BatchOwner.MarkDirty(this);if(RigBatchOwner!=null)RigBatchOwner.MarkDirty();}
    public void NotifyVisualChanged()
    {if(CargoCollision!=null)CargoCollision.SourceChanged();if(BallastOwner!=null){if(Missing||Detached)BallastOwner.Breach(this);else BallastOwner.ResealIfIntact();}if(ColliderBatchOwner!=null)ColliderBatchOwner.MarkDirty(this);if(Owner!=null){Owner.NoteCellMoved(this);if(Owner.PhysicsBody!=null)Owner.PhysicsBody.MarkDirty(this);}if(BatchOwner!=null)BatchOwner.MarkDirty(this);if(RigOwner!=null)RigOwner.MarkRigDirty();if(StructureOwner!=null)StructureOwner.MarkDirty();if(RigBatchOwner!=null)RigBatchOwner.MarkDirty();}
    private Collider cachedCollider;
    public Collider PhysicsCollider {get{if(cachedCollider==null)cachedCollider=GetComponent<Collider>();return cachedCollider;}}
    public NavalBallastUnit BallastOwner;
    public NavalHull Owner;
    public Material OriginalSurface;
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
        PhysicsCollider.enabled = !value&&!ColliderBatched;
        if (!value && Stream != null) Stream.gameObject.SetActive(false);
    }
}

public class NavalHull
{
    public NavalFireSystem Fire;
    private readonly HashSet<Material> fireMaterials=new HashSet<Material>();
    public void RegisterFireMaterial(Material material){if(material!=null)fireMaterials.Add(material);}
    public void FireScorch(NavalHullVoxel cell)
    {cell.ScorchStrength=Mathf.Max(cell.ScorchStrength,.8f);style.Scorch(cell.gameObject,cell.ScorchStrength);cell.NotifyMaterialChanged();}
    public int Revision;
    public readonly List<NavalRepairSailor> RepairWorkers=new List<NavalRepairSailor>();
    public readonly List<NavalHullVoxel> Voxels = new List<NavalHullVoxel>();
    private int damagedRevision=-1;
    private readonly List<NavalHullVoxel> damagedVoxels=new List<NavalHullVoxel>();
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
                    Vector3 p=root.InverseTransformPoint(cell.transform.position);damageIndex.Add(i,p.x,p.y,p.z);
                }
                else dynamicDamageCells.Add(cell);
            }
        }
        damageCandidates.Clear();
        damageIndex.Query(point.x,point.y,point.z,Mathf.Max(0f,radius)+.571f,damageQuery);
        foreach(int index in damageQuery)if(Voxels[index].DamageIndexed)damageCandidates.Add(Voxels[index]);
        damageCandidates.AddRange(dynamicDamageCells);
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
    {Definition=definition;Flood=new NavalFloodGrid(12,24,BoatDimensions.HoldWidth,BoatDimensions.HoldLength,definition.Scale,definition.RimY-definition.HoldFloor,definition.DeckCount>2);Flood.ConfigureStorage(definition);Stability=new NavalHullStability(this);}
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
    public bool FloodEffectsHidden;
    public bool HasActiveLeaks;
    public readonly List<NavalCannonSupply> CannonSupplies=new List<NavalCannonSupply>();
    public readonly List<NavalPlankSupply> PlankSupplies=new List<NavalPlankSupply>();
    public bool HasCannonStock {get{foreach(var stock in CannonSupplies)if(stock.Available)return true;return false;}}
    public bool HasPlankStock {get{foreach(var stock in PlankSupplies)if(stock.Available)return true;return false;}}
    public NavalCargo Cargo;
    public NavalCannonSupply CannonSupply;
    public NavalPlankSupply PlankSupply;
    public bool CrewCannonRepairAllowed=true;
    public bool HasRepairDemand;
    public float SinkDepth;
    private const float Cell = 0.25f;
    private Material streamMaterial;
    private Transform root;
    public Transform Root { get { return root; } }
    private NavalVoxelStyle style;
    public NavalShipPhysics PhysicsBody {get;private set;}

    public void Build(Transform boat, Material wood, Material waterMaterial, Material floorMaterial)
    {
        RegisterFireMaterial(wood);RegisterFireMaterial(floorMaterial);
        streamMaterial = waterMaterial;
        root = boat;
        PhysicsBody=boat.gameObject.AddComponent<NavalShipPhysics>();PhysicsBody.Initialize(this);
        style = boat.gameObject.AddComponent<NavalVoxelStyle>();
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
                    float px=-2.75f+x*Cell;if(Mathf.Abs(px)>Definition.HullHalfWidth(side*BoatDimensions.HalfLength)-Cell*.5f)continue;
                    var cell=AddVoxel(boat,wood,new Vector3(px,y,side*BoatDimensions.HalfLength),new Vector3(Cell,height,Cell),true,true);cell.OpeningArea=Cell*height;
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
            if(Mathf.Abs(center.z)>5.99f&&Mathf.Abs(center.x)>Definition.HullHalfWidth(center.z)-Cell*.5f)return;
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
    public int Damage(Vector3 localPoint, float radius,bool cannon=false,NavalHullVoxel direct=null,float cannonPower=1f)
    {
        using(NavalPerformance.Measure(NavalPerfArea.HullDamage))
        {
        if(Fire!=null&&cannon)Fire.Impact(localPoint,radius,cannonPower);
        int removed = 0;
        foreach (NavalHullVoxel voxel in DamageCandidates(localPoint,radius,direct))
        {
            if(voxel==null||voxel.Missing)continue;
            Vector3 position=root.InverseTransformPoint(voxel.transform.position);
            float distance=(position-localPoint).magnitude;
            if(distance<=radius||voxel==direct)
            {
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
            else if(distance<radius+0.57f)
            {
                // Stable irregular, voxel-shaped rim. It colours the actual
                // surviving cells rather than leaving floating decal planes.
                float noise=Mathf.Repeat(Mathf.Sin(position.x*12.9898f+position.y*78.233f+position.z*37.719f)*43758.5453f,1f);
                float band=0.35f+noise*0.22f;
                float strength=Mathf.Clamp01(1f-(distance-radius)/band);
                if(strength>0.05f&&strength>voxel.ScorchStrength)
                {voxel.ScorchStrength=strength;style.Scorch(voxel.gameObject,strength);voxel.NotifyMaterialChanged();}
            }
        }
        return removed;
            }
    }
    public void Repair()
    {
        if(Fire!=null)Fire.ResetFire();
        if(Cargo!=null)Cargo.ResetCargo();
        foreach(var stock in CannonSupplies)stock.ResetStock();
        HashSet<NavalRepairableMechanism> equipment=new HashSet<NavalRepairableMechanism>();
        foreach (NavalHullVoxel voxel in Voxels)
        {
            if(voxel.RepairMechanism!=null)equipment.Add(voxel.RepairMechanism);
            voxel.CannonWear=0f;
            if (voxel.Missing) voxel.SetMissing(false);
            if(voxel.ScorchStrength>0f)
            {voxel.GetComponent<Renderer>().sharedMaterial=voxel.OriginalSurface;voxel.ScorchStrength=0f;voxel.NotifyVisualChanged();}
        }
        if(Bilge!=null)Bilge.ResetBallastSeals();
        foreach(NavalRepairableMechanism mechanism in equipment)mechanism.Reset(); // Explicit debug full restoration only.
    }
    private void ObserveRimPressure(Transform boat,NavalWater ocean,float x,float z,float width,float dt,Vector3 inward)
    {
        Vector3 lip=boat.TransformPoint(new Vector3(x,Definition.RimY,z));int index=Flood.Index(x,z);
        float inside=NavalDownfloodMath.InsideAtOpening(lip.y,Definition.RimY,Floor,Flood.Depth(index),boat.up.y,WorldScale);
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
        FloodEffectsHidden=NavalFloodVisibilityMath.HideEffects(NavalFloodAppearance.Clearance(boat,ocean));
        HoleCount = 0; SubmergedArea = 0f; Inflow = 0f;OpeningInflow=0f;openingPressure=0f;SubmergedRimHead=0f;HasActiveLeaks=false;HasRepairDemand=false;
        int visibleStreams = 0;
        foreach (NavalHullVoxel voxel in GetDamagedVoxels())
        {
            voxel.InflowRate=0f;
            if (!voxel.Missing && !voxel.Detached) continue;
            HoleCount++;
            if (!voxel.HydraulicShell) continue;
            Vector3 point = boat.TransformPoint(voxel.LocalCenter);
            int cellIndex = Flood.Index(voxel.LocalCenter.x,voxel.LocalCenter.z);
            float internalY = boat.TransformPoint(new Vector3(Flood.X(cellIndex),
                Floor + Flood.Depth(cellIndex),Flood.Z(cellIndex))).y;
            float sea = ocean.Height(point.x, point.z);
            NavalBallastUnit connected=Bilge!=null?Bilge.SeaConnection(voxel):null;
            if(connected!=null&&!connected.Tank.InnerBroken)
                internalY=boat.TransformPoint(new Vector3(connected.Tank.X,connected.Floor+NavalBallastLayout.Height/WorldScale*connected.Tank.FillFraction,connected.Tank.Z)).y;
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
                Vector3 inward=Mathf.Abs(voxel.LocalCenter.x)>2.9f?new Vector3(-Mathf.Sign(voxel.LocalCenter.x),0,0):new Vector3(0,0,-Mathf.Sign(voxel.LocalCenter.z));
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
                Vector3 inward = voxel.LocalCenter.y < Floor ? Vector3.up :
                    (Mathf.Abs(voxel.LocalCenter.x) > 2.9f ? new Vector3(-Mathf.Sign(voxel.LocalCenter.x), 0f, 0f) :
                    new Vector3(0f, 0f, -Mathf.Sign(voxel.LocalCenter.z)));
                voxel.Stream.Animate(boat.InverseTransformPoint(origin), inward, amount / dt, Time.time);
            }
            else if (voxel.Stream != null) voxel.Stream.gameObject.SetActive(false);
        }
        // Open gunports are the lowest intact openings above the deck.
        for(int gun=0;gun<Definition.GunCount+(Definition.DeckCount>2?2:0);gun++)
        {
            bool boarding=gun>=Definition.GunCount;
            float x=NavalShipDefinition.GunSide(gun)*BoatDimensions.HalfWidth,z=boarding?Definition.LadderZ:NavalShipDefinition.GunZ(Definition,gun);
            float lip=boarding?Definition.GunDeck:Definition.GunPortSill;
            Vector3 opening=boat.TransformPoint(new Vector3(x,lip,z));
            int index=Flood.Index(x,z);
            float inside=NavalDownfloodMath.InsideAtOpening(opening.y,lip,Floor,Flood.Depth(index),boat.up.y,WorldScale);
            float width=(boarding?1.3f:2.2f)*WorldScale;
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
                for(int gun=0;gun<Definition.GunCount;gun+=2)
                    width-=NavalRimFlowMath.Overlap(z,1f,NavalShipDefinition.GunZ(Definition,gun),2.2f);
                if(Definition.DeckCount>2)
                    width-=NavalRimFlowMath.UncoveredLadder(z,Definition);
                width=Mathf.Max(0,width);if(width<=.001f)continue;
                float x=side*Definition.HullHalfWidth(z);
                ObserveRimPressure(boat,ocean,x,z,width*WorldScale,dt,new Vector3(-side,0,0));
            }
            float endHalf=Definition.HullHalfWidth(side*BoatDimensions.HalfLength);
            int pieces=Mathf.Max(1,Mathf.CeilToInt(endHalf*2));float span=endHalf*2/pieces;
            for(int segment=0;segment<pieces;segment++)
                ObserveRimPressure(boat,ocean,-endHalf+(segment+.5f)*span,side*BoatDimensions.HalfLength,span*WorldScale,dt,new Vector3(0,0,-side));
        }
        Matrix4x4 floodPose=boat.localToWorldMatrix;
        Flood.FillFloorHeights(floodFloors,floodPose.m10,floodPose.m12,floodPose.m11*Floor,floodPose.m13);
        if(Bilge!=null)Bilge.UpdateBarriers();
        Flood.Spread(dt,floodFloors,boat.up.y);
        if(pump)Flood.Pump(BoatDimensions.PumpRate*AreaScale*dt);
        float floodFraction=Volume/Definition.SinkingVolume,heel=NavalHullStability.Roll(boat);
        downfloodEmergency.Step(dt,HasActiveLeaks,openingPressure,NavalBroadsideMath.Delta(Stability.WaveRoll,heel),Mathf.Atan2(-boat.forward.y,boat.up.y)*Mathf.Rad2Deg);
        combatEmergency.Step(floodFraction,heel,HasActiveLeaks&&Inflow>NavalMaintenanceMath.PumpOutRate&&Mathf.Abs(heel)>=22f);
        maintenanceEscalation.Step(dt,floodFraction,heel,CombatCritical,HasActiveLeaks&&Inflow>NavalMaintenanceMath.PumpOutRate&&Mathf.Abs(heel)>=12f);
        if (Volume >= Definition.SinkingVolume) Sunk = true;
        if (Sunk) SinkDepth = Mathf.Min(5f, SinkDepth + dt * 0.2f);
            }
    }
}
