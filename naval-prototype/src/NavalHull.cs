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
    public bool ManagedRig;
    public bool MovableEquipment;
    public bool RigFabric;
    public bool MastTimber;
    public float CannonWear;
    public bool Detached;
    public bool Batched;
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
        Missing = value;
        LastWetAt=float.NegativeInfinity;LastWetFlow=0f;
        if (Owner != null) Owner.Revision++;
        GetComponent<Renderer>().enabled = !value && !Batched;
        GetComponent<Collider>().enabled = !value;
        if (!value && Stream != null) Stream.gameObject.SetActive(false);
    }
}

public class NavalHull
{
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
    public int HoleCount { get; private set; }
    public float SubmergedArea { get; private set; }
    public float Inflow { get; private set; }
    public readonly NavalShipDefinition Definition;
    public NavalBilgeSystem Bilge;
    public float Floor {get{return Definition.HoldFloor;}}
    public float WorldScale {get{return Definition.Scale;}}
    public float AreaScale {get{return Definition.AreaScale;}}
    public float VolumeScale {get{return Definition.VolumeScale;}}
    public readonly NavalFloodGrid Flood;
    public NavalHull():this(NavalShipDefinition.Sloop){}
    public NavalHull(NavalShipDefinition definition)
    {Definition=definition;Flood=new NavalFloodGrid(12,24,BoatDimensions.HoldWidth,BoatDimensions.HoldLength,definition.Scale,definition.RimY-definition.HoldFloor);}
    private readonly float[] floodFloors = new float[12*24];
    public float LeftWater { get { return SideWater(true); } }
    public float RightWater { get { return SideWater(false); } }
    private float SideWater(bool port){float total=Flood.Side(port);foreach(NavalRepairSailor worker in RepairWorkers)if(worker!=null&&(worker.CarriedSide<0f)==port)total+=worker.BucketWater;return total;}
    public float Volume { get { float total=Flood.Total;foreach(NavalRepairSailor worker in RepairWorkers)if(worker!=null)total+=worker.BucketWater;return total; } }
    public float WaterDepthAt(Vector3 local) { return Flood.Depth(Flood.Index(local.x,local.z)); }
    private readonly NavalCombatEmergency combatEmergency=new NavalCombatEmergency();
    public bool CombatCritical {get{return combatEmergency.Critical;}}
    public bool Sunk;
    public bool FloodEffectsHidden;
    public bool HasActiveLeaks;
    public float SinkDepth;
    private const float Cell = 0.25f;
    private Material streamMaterial;
    private Transform root;
    public Transform Root { get { return root; } }
    private NavalVoxelStyle style;

    public void Build(Transform boat, Material wood, Material waterMaterial, Material floorMaterial)
    {
        streamMaterial = waterMaterial;
        root = boat;
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
        for (int x = 0; x < 23; x++) for (int z = 0; z < 48; z++)
            Add(boat, floorMaterial, new Vector3(-2.75f + x * Cell, -2.35f, -5.875f + z * Cell));
    }
    private void Add(Transform boat, Material material, Vector3 center)
    {
        if (CannonPort(center.x, center.y, center.z, Cell * 0.5f, Cell * 0.5f, Cell * 0.5f)) return;
        bool bottom=center.y<BoatDimensions.HoldFloor;
        float low=Definition.HullY(center.y-Cell*.5f),high=Definition.HullY(center.y+Cell*.5f);
        center.y=(low+high)*.5f;
        NavalHullVoxel voxel=AddVoxel(boat, material, center, new Vector3(Cell,high-low,Cell), true, true);
        voxel.OpeningArea=bottom?Cell*Cell:Cell*(high-low);
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
        voxel.OriginalSurface = obj.GetComponent<Renderer>().sharedMaterial;
        voxel.LocalCenter = center; voxel.OpeningArea = Cell * Cell;
        voxel.HydraulicShell = shell;
        // Decorative cells still receive ray hits; sail cells do not block the robot.
        if (!solid) obj.GetComponent<Collider>().isTrigger = true;
        Voxels.Add(voxel);
        return voxel;
    }
    public bool CannonPort(float x,float y,float z,float halfX,float halfY,float halfZ)
    {
        if(BoatDimensions.IntersectsCannonPort(x,y,z,halfX,halfY,halfZ))return true;
        return Definition.GunCount>2&&BoatDimensions.IntersectsCannonPort(x,y,z-4.3f,halfX,halfY,halfZ);
    }
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
        int removed = 0;
        foreach (NavalHullVoxel voxel in Voxels)
        {
            if(voxel.Missing)continue;
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
                        style.Scorch(voxel.gameObject,voxel.ScorchStrength);
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
                {voxel.ScorchStrength=strength;style.Scorch(voxel.gameObject,strength);}
            }
        }
        return removed;
    }
    public void Repair()
    {
        foreach (NavalHullVoxel voxel in Voxels)
        {
            voxel.CannonWear=0f;
            if (voxel.Missing) voxel.SetMissing(false);
            if(voxel.ScorchStrength>0f)
            {voxel.GetComponent<Renderer>().sharedMaterial=voxel.OriginalSurface;voxel.ScorchStrength=0f;}
        }
    }
    public void Step(Transform boat, NavalWater ocean, float dt, bool pump)
    {
        FloodEffectsHidden=NavalFloodVisibilityMath.HideEffects(NavalFloodAppearance.Clearance(boat,ocean));
        HoleCount = 0; SubmergedArea = 0f; Inflow = 0f;HasActiveLeaks=false;
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
            // Partial submergence of a voxel opening changes its effective wet area.
            Vector3 cellSize=voxel.transform.lossyScale;
            float halfHeight=.5f*(Mathf.Abs(boat.right.y*cellSize.x)+Mathf.Abs(boat.up.y*cellSize.y)+Mathf.Abs(boat.forward.y*cellSize.z));
            float area;
            float flow = FloodMath.OpeningFlow(voxel.OpeningArea * AreaScale, sea, internalY, point.y, halfHeight, out area);
            SubmergedArea += area;
            float amount = Flood.Add(cellIndex,flow * dt);
            voxel.InflowRate=Mathf.Max(0f,amount/dt);
            if(voxel.InflowRate>0.00001f)HasActiveLeaks=true;
            if(area>0f&&flow>0.00001f){voxel.LastWetAt=Time.time;voxel.LastWetFlow=Mathf.Max(0.001f,flow);}
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
        for(int gun=0;gun<Definition.GunCount;gun++)
        {
            float x=NavalShipDefinition.GunSide(gun)*BoatDimensions.HalfWidth,z=NavalShipDefinition.GunZ(gun);
            Vector3 opening=boat.TransformPoint(new Vector3(x,Definition.AboveDeckY(BoatDimensions.CannonPortSill),z));
            float flow=Mathf.Max(0f,ocean.Height(opening.x,opening.z)-opening.y)*.2f*AreaScale;
            float accepted=Flood.Add(Flood.Index(x,z),flow*dt);Inflow+=accepted/dt;
        }
        for(int i=0;i<Flood.Water.Length;i++)
            floodFloors[i]=boat.TransformPoint(new Vector3(Flood.X(i),Floor,Flood.Z(i))).y;
        if(Bilge!=null)Bilge.UpdateBarriers();
        Flood.Spread(dt,floodFloors,boat.up.y);
        if(pump)Flood.Pump(BoatDimensions.PumpRate*AreaScale*dt);
        combatEmergency.Step(Volume/(BoatDimensions.SinkVolume*VolumeScale),Mathf.DeltaAngle(0f,boat.eulerAngles.z));
        if (Volume >= BoatDimensions.SinkVolume * VolumeScale) Sunk = true;
        if (Sunk) SinkDepth = Mathf.Min(5f, SinkDepth + dt * 0.2f);
    }
}
