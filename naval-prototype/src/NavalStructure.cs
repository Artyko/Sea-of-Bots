using System.Collections.Generic;
using UnityEngine;

// A fixed attachment graph is built in the intact configuration. Gun aiming
// then moves its parts without tearing their original mechanical connections.
public class NavalStructure : MonoBehaviour
{
    private NavalHull hull;
    private readonly List<NavalHullVoxel> cells=new List<NavalHullVoxel>();
    private readonly List<Transform> parents=new List<Transform>();
    private readonly List<Vector3> positions=new List<Vector3>(), sizes=new List<Vector3>();
    private readonly List<Quaternion> rotations=new List<Quaternion>();
    private readonly List<bool> triggers=new List<bool>();
    private readonly List<GameObject> debris=new List<GameObject>();
    private List<int>[] edges;
    private bool dirty,initialCleanup=true;
    private bool[] absent;
    private NavalConnectivityWorkspace connectivity;
    private readonly Dictionary<NavalHullVoxel,int> cellIndices=new Dictionary<NavalHullVoxel,int>();
    public void MarkDirty(){dirty=true;}
    public void Initialize(NavalHull owner)
    {
        hull=owner;
        foreach(NavalHullVoxel cell in hull.Voxels)if(!cell.ManagedRig&&!cell.MovableEquipment)
        {
            cell.StructureOwner=this;cellIndices.Add(cell,cells.Count);
            cells.Add(cell); parents.Add(cell.transform.parent);
            positions.Add(cell.transform.localPosition); sizes.Add(cell.transform.localScale);
            rotations.Add(cell.transform.localRotation); triggers.Add(cell.PhysicsCollider.isTrigger);
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
        absent=new bool[cells.Count];connectivity=new NavalConnectivityWorkspace(cells.Count);dirty=true;
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
        int main=-1,largest=0;
        for(int i=0;i<components.Count;i++)if(components[i].Count>largest){main=i;largest=components[i].Count;}
        for(int i=0;i<components.Count;i++)if(i!=main){
            if(initialCleanup){Debug.Log("[NavalPrototype] Removed unsupported spawn detail: ship="+hull.Definition.Name+" cells="+components[i].Count+" at="+cells[components[i][0]].LocalCenter);foreach(int index in components[i])cells[index].SetMissing(true);}
            else Drop(components[i],shipVelocity);
        }
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
            cell.transform.SetParent(group.transform,true);cell.NotifyVisualChanged();cell.PhysicsCollider.isTrigger=false;
        }
        Rigidbody body=group.AddComponent<Rigidbody>();
        body.mass=Mathf.Clamp(component.Count*0.4f,0.5f,500f);
        body.linearVelocity=velocity;body.interpolation=RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
        debris.Add(group);
        hull.Revision++;
    }
    public void Restore()
    {
        foreach(GameObject group in debris)
        {Rigidbody body=group.GetComponent<Rigidbody>();body.isKinematic=true;body.detectCollisions=false;}
        for(int i=0;i<cells.Count;i++)
        {
            Transform cell=cells[i].transform;cell.SetParent(parents[i],false);
            cell.localPosition=positions[i];cell.localRotation=rotations[i];cell.localScale=sizes[i];
            cells[i].Detached=false;cells[i].NotifyVisualChanged();cells[i].PhysicsCollider.isTrigger=triggers[i];
            if(cells[i].Stream!=null)cells[i].Stream.gameObject.SetActive(false);
        }
        foreach(GameObject group in debris)Destroy(group);debris.Clear();
        hull.Revision++;dirty=true;
    }
    public void RestoreCell(NavalHullVoxel voxel)
    {
        if(voxel.RepairMechanism!=null&&(!voxel.RepairMechanism.CanRepair||
            voxel.RepairMechanism.IsCannon&&!voxel.RepairMechanism.RestoringCannon))return;
        int i;if(!cellIndices.TryGetValue(voxel,out i))return;
        Transform cell=voxel.transform;
        cell.SetParent(parents[i],false);cell.localPosition=positions[i];cell.localRotation=rotations[i];cell.localScale=sizes[i];
        voxel.Detached=false;voxel.NotifyVisualChanged();voxel.PhysicsCollider.isTrigger=triggers[i];
        voxel.SetMissing(false);voxel.ScorchStrength=0f;
        voxel.LastWetAt=float.NegativeInfinity;voxel.LastWetFlow=0f;
        cell.GetComponent<Renderer>().sharedMaterial=voxel.OriginalSurface;voxel.NotifyVisualChanged();
        if(voxel.Stream!=null)voxel.Stream.gameObject.SetActive(false);
        hull.Revision++;
    }
}
