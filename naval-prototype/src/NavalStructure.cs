using System.Collections.Generic;
using UnityEngine;

// A fixed attachment graph is built in the intact configuration. Gun aiming
// then moves its parts without tearing their original mechanical connections.
public class NavalStructure : MonoBehaviour
{
    private NavalHull hull;
    private sealed class MovingPose {public Transform Parent;public Vector3 Position,Scale;public Quaternion Rotation;public bool Trigger,NonPhysical;}
    private readonly Dictionary<NavalHullVoxel,MovingPose> movingPoses=new Dictionary<NavalHullVoxel,MovingPose>();

    private readonly List<NavalHullVoxel> cells=new List<NavalHullVoxel>();
    private readonly List<Transform> parents=new List<Transform>();
    private readonly List<Vector3> positions=new List<Vector3>(), sizes=new List<Vector3>();
    private readonly List<Quaternion> rotations=new List<Quaternion>();
    private readonly List<bool> triggers=new List<bool>();
    private readonly List<bool> nonPhysical=new List<bool>();
    private readonly List<GameObject> debris=new List<GameObject>();
    private List<int>[] edges;
    private bool dirty,initialCleanup=true;
    private bool[] absent,foundation;
    private int[] originalSection, retainedComponent, retainedFoundationCount;
    private NavalConnectivityWorkspace connectivity;
    private readonly Dictionary<NavalHullVoxel,int> cellIndices=new Dictionary<NavalHullVoxel,int>();
    public void MarkDirty(){dirty=true;}
    public void Initialize(NavalHull owner)
    {
        hull=owner;
        foreach(var cell in hull.Voxels)if(!cell.ManagedRig&&cell.MovableEquipment)movingPoses[cell]=new MovingPose{Parent=cell.transform.parent,Position=cell.transform.localPosition,Scale=cell.transform.localScale,Rotation=cell.transform.localRotation,Trigger=cell.PhysicsCollider.isTrigger,NonPhysical=cell.NonPhysicalDecoration};
        foreach(NavalHullVoxel cell in hull.Voxels)if(!cell.ManagedRig&&!cell.MovableEquipment)
        {
            cell.StructureOwner=this;cellIndices.Add(cell,cells.Count);
            cells.Add(cell); parents.Add(cell.transform.parent);
            positions.Add(cell.transform.localPosition); sizes.Add(cell.transform.localScale);
            rotations.Add(cell.transform.localRotation); triggers.Add(cell.PhysicsCollider.isTrigger);nonPhysical.Add(cell.NonPhysicalDecoration);
        }
        edges=new List<int>[cells.Count];
        Bounds[] bounds=new Bounds[cells.Count];
        Dictionary<Vector3Int,List<int>> buckets=new Dictionary<Vector3Int,List<int>>();
        for(int i=0;i<cells.Count;i++)
        {
            edges[i]=new List<int>();
            Matrix4x4 matrix=transform.worldToLocalMatrix*cells[i].transform.localToWorldMatrix;
            Bounds box=new Bounds(matrix.MultiplyPoint3x4(Vector3.zero),Vector3.zero);
            for(int corner=0;corner<8;corner++)
                box.Encapsulate(matrix.MultiplyPoint3x4(new Vector3((corner&1)==0?-0.5f:0.5f,(corner&2)==0?-0.5f:0.5f,(corner&4)==0?-0.5f:0.5f)));
            box.Expand(0.06f); bounds[i]=box;
            HashSet<int> tested=new HashSet<int>();
            Vector3Int min=Bucket(box.min),max=Bucket(box.max);
            for(int x=min.x;x<=max.x;x++)for(int y=min.y;y<=max.y;y++)for(int z=min.z;z<=max.z;z++)
            {
                Vector3Int key=new Vector3Int(x,y,z); List<int> nearby;
                if(!buckets.TryGetValue(key,out nearby)){nearby=new List<int>();buckets.Add(key,nearby);}
                foreach(int other in nearby)if(tested.Add(other)&&box.Intersects(bounds[other]))
                {edges[i].Add(other);edges[other].Add(i);}
                nearby.Add(i);
            }
        }
        // Pre-existing unconnected decorative islands are removed from the
        // hovering geometry on the first update as well as after damage.
        absent=new bool[cells.Count];foundation=new bool[cells.Count];
        int foundations=0;
        for(int i=0;i<cells.Count;i++)
        {
            // The low hydraulic shell and floor are the structural foundation.
            // Upper armour and fittings must retain a continuous attachment to
            // this band; being the largest disconnected object is not support.
            foundation[i]=cells[i].HydraulicShell&&cells[i].LocalCenter.y<=hull.Definition.HoldFloor+.10f;
            if(foundation[i])foundations++;
        }
        // Defensive fallback for an unforeseen profile with no low shell.  It
        // preserves the old hull instead of dropping an entire ship at spawn.
        if(foundations==0&&cells.Count>0)
        {
            float lowest=float.MaxValue;for(int i=0;i<cells.Count;i++)lowest=Mathf.Min(lowest,cells[i].LocalCenter.y);
            for(int i=0;i<cells.Count;i++)if(cells[i].HydraulicShell&&cells[i].LocalCenter.y<=lowest+.10f)foundation[i]=true;
        }
        connectivity=new NavalConnectivityWorkspace(cells.Count);dirty=true;
    }
    private static Vector3Int Bucket(Vector3 p)
    {return new Vector3Int(Mathf.FloorToInt(p.x*2f),Mathf.FloorToInt(p.y*2f),Mathf.FloorToInt(p.z*2f));}
    public void Refresh(Vector3 shipVelocity)
    {
        using(NavalPerformance.Measure(NavalPerfArea.Structure))
        {
        if(!dirty)return;
        dirty=false;
        for(int i=0;i<cells.Count;i++)absent[i]=cells[i].Missing||cells[i].Detached;
        List<List<int>> components=connectivity.Evaluate(edges,absent);
        if(initialCleanup)
        {
            originalSection=new int[cells.Count];
            retainedComponent=new int[components.Count];
            retainedFoundationCount=new int[components.Count];
            for(int i=0;i<components.Count;i++)
                foreach(int index in components[i])originalSection[index]=i;
        }
        System.Array.Clear(retainedFoundationCount,0,retainedFoundationCount.Length);
        for(int i=0;i<components.Count;i++)
        {
            int section=originalSection[components[i][0]],count=0;
            foreach(int index in components[i])if(foundation[index])count++;
            if(count<=retainedFoundationCount[section])continue;
            retainedFoundationCount[section]=count;
            retainedComponent[section]=i;
        }
        bool hullDetached=false;
        for(int i=0;i<components.Count;i++)
        {
            int section=originalSection[components[i][0]];
            if(retainedFoundationCount[section]>0&&retainedComponent[section]==i)continue;
            if(initialCleanup){Debug.Log("[NavalPrototype] Removed unsupported spawn detail: ship="+hull.Definition.Name+" cells="+components[i].Count+" at="+cells[components[i][0]].LocalCenter);foreach(int index in components[i])cells[index].SetMissing(true);}
            else{Debug.Log("[NavalPrototype] Unsupported structure falling: ship="+hull.Definition.Name+" cells="+components[i].Count+" at="+cells[components[i][0]].LocalCenter);hullDetached|=NavalStructuralSupport.HasFoundation(components[i],foundation);Drop(components[i],shipVelocity);}
        }
        if(initialCleanup){var guns=NavalStations.ForShip(hull.Root);if(guns!=null)Debug.Log("[NavalPrototype] Initial structure gun audit: ship="+hull.Root.name+" functional="+guns.WorkingGuns+"/"+guns.GunCount);}
        if(hullDetached)hull.RefreshFloodSections();
        initialCleanup=false;
            }
    }
    private void Drop(List<int> component,Vector3 velocity)
    {
        GameObject group=new GameObject("NavalPrototype_StructuralDebris");
        group.layer=PhysicsManager.GetEnvironmentLayer(); group.transform.SetParent(transform.parent,false);
        group.transform.position=cells[component[0]].transform.position;
        group.transform.rotation=transform.rotation;
        foreach(int index in component)
        {
            NavalHullVoxel cell=cells[index];cell.Detached=true;
            cell.Batched=false;cell.GetComponent<Renderer>().enabled=!cell.Missing;
            cell.transform.SetParent(group.transform,true);cell.NotifyVisualChanged();cell.SetNonPhysicalDecoration(false);cell.PhysicsCollider.isTrigger=false;
        }
        Rigidbody body=group.AddComponent<Rigidbody>();
        body.mass=Mathf.Clamp(component.Count*0.4f,0.5f,500f);
        body.linearVelocity=velocity;body.interpolation=RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
        debris.Add(group);
        hull.Revision++;
    }
    public void DropEquipment(IEnumerable<NavalHullVoxel> equipment,Vector3 velocity){
        GameObject group=new GameObject("NavalPrototype_EquipmentDebris");group.layer=PhysicsManager.GetEnvironmentLayer();
        group.transform.SetParent(transform.parent,false);group.transform.position=transform.position;group.transform.rotation=transform.rotation;
        int count=0;
        foreach(var cell in equipment){
            if(cell==null||cell.Missing||cell.Detached)continue;
            cell.Detached=true;cell.Batched=false;cell.GetComponent<Renderer>().enabled=true;
            cell.transform.SetParent(group.transform,true);cell.NotifyVisualChanged();cell.SetNonPhysicalDecoration(false);cell.PhysicsCollider.isTrigger=false;count++;
        }
        if(count==0){Destroy(group);return;}
        Rigidbody body=group.AddComponent<Rigidbody>();body.mass=Mathf.Clamp(count*.4f,.5f,500f);body.linearVelocity=velocity;
        body.interpolation=RigidbodyInterpolation.Interpolate;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
        debris.Add(group);hull.Revision++;
    }
    public void Restore()
    {
        foreach(GameObject group in debris)
        {Rigidbody body=group.GetComponent<Rigidbody>();body.isKinematic=true;body.detectCollisions=false;}
        for(int i=0;i<cells.Count;i++)
        {
            Transform cell=cells[i].transform;cell.SetParent(parents[i],false);
            cell.localPosition=positions[i];cell.localRotation=rotations[i];cell.localScale=sizes[i];
            cells[i].Detached=false;cells[i].NotifyVisualChanged();cells[i].PhysicsCollider.isTrigger=triggers[i];cells[i].SetNonPhysicalDecoration(nonPhysical[i]);
            if(cells[i].Stream!=null)cells[i].Stream.gameObject.SetActive(false);
        }
        foreach(var entry in movingPoses)RestoreMoving(entry.Key,entry.Value);
        foreach(GameObject group in debris)Destroy(group);debris.Clear();
        hull.RefreshFloodSections();
        hull.Revision++;dirty=true;
    }
    private void RestoreMoving(NavalHullVoxel voxel,MovingPose pose){if(voxel==null||pose.Parent==null)return;voxel.transform.SetParent(pose.Parent,false);voxel.transform.localPosition=pose.Position;voxel.transform.localRotation=pose.Rotation;voxel.transform.localScale=pose.Scale;voxel.Detached=false;voxel.SetNonPhysicalDecoration(pose.NonPhysical);voxel.PhysicsCollider.isTrigger=pose.Trigger;voxel.SetMissing(false);voxel.ScorchStrength=0;voxel.GetComponent<Renderer>().sharedMaterial=voxel.OriginalSurface;voxel.NotifyVisualChanged();hull.NoteCellMoved(voxel);hull.Revision++;}
    public bool CanRestoreCell(NavalHullVoxel voxel){
        return voxel!=null&&(cellIndices.ContainsKey(voxel)||movingPoses.ContainsKey(voxel))&&(voxel.RepairMechanism==null||voxel.RepairMechanism.CanRepair);
    }
    public void RestoreCell(NavalHullVoxel voxel)
    {
        if(voxel.RepairMechanism!=null&&(!voxel.RepairMechanism.CanRepair||
            voxel.RepairMechanism.IsCannon&&!voxel.RepairMechanism.RestoringCannon))return;
        int i;if(!cellIndices.TryGetValue(voxel,out i)){MovingPose pose;if(movingPoses.TryGetValue(voxel,out pose))RestoreMoving(voxel,pose);return;}
        Transform cell=voxel.transform;
        cell.SetParent(parents[i],false);cell.localPosition=positions[i];cell.localRotation=rotations[i];cell.localScale=sizes[i];
        voxel.Detached=false;voxel.NotifyVisualChanged();voxel.PhysicsCollider.isTrigger=triggers[i];voxel.SetNonPhysicalDecoration(nonPhysical[i]);
        voxel.SetMissing(false);voxel.ScorchStrength=0f;
        voxel.LastWetAt=float.NegativeInfinity;voxel.LastWetFlow=0f;
        cell.GetComponent<Renderer>().sharedMaterial=voxel.OriginalSurface;voxel.NotifyVisualChanged();
        if(voxel.Stream!=null)voxel.Stream.gameObject.SetActive(false);
        hull.Revision++;
    }
}
