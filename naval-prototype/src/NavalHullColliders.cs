using System.Collections.Generic;
using UnityEngine;

// Fixed local chunks of exactly merged boxes. Dirty chunks immediately fall back to source colliders.
[DefaultExecutionOrder(120)]
public sealed class NavalHullColliders : MonoBehaviour
{
    private sealed class Source
    {public NavalHullVoxel Cell;public BoxCollider Collider;public Vector3 Position,Scale,Center,Size;public bool Included;public int Layer;public PhysicsMaterial Material;public float ContactOffset;}
    private sealed class Chunk
    {
        public readonly List<Source> Sources=new List<Source>();
        public readonly List<NavalSolidBox> Bounds=new List<NavalSolidBox>(),Input=new List<NavalSolidBox>();
        public readonly List<NavalMergedCollider> Proxies=new List<NavalMergedCollider>();
        public readonly NavalBoxMerger Merger=new NavalBoxMerger();
        public bool Dirty,Merged;
        public int Active,Batched;
    }
    private static readonly List<NavalHullColliders> all=new List<NavalHullColliders>();
    private readonly List<Chunk> chunks=new List<Chunk>(),dirty=new List<Chunk>();
    private NavalHull hull;
    public int SourceCount {get;private set;}
    public int ActiveCount {get;private set;}
    public int BatchedCount {get;private set;}
    public void Initialize(NavalHull owner)
    {
        hull=owner;all.Add(this);
        Dictionary<string,Chunk> groups=new Dictionary<string,Chunk>();
        foreach(NavalHullVoxel cell in hull.Voxels)
        {
            BoxCollider collider=cell.PhysicsCollider as BoxCollider;
            Transform pose=cell.transform;
            if(collider==null||!collider.enabled||collider.isTrigger||cell.ManagedRig||cell.MovableEquipment||cell.RepairableEquipment||
                cell.Missing||cell.Detached||pose.parent!=transform||!AxisAligned(pose)||!Positive(pose.localScale))continue;
            Vector3 position=pose.localPosition,scale=pose.localScale;
            // Layer, physical material and contact offset must also agree. Built only at construction.
            string key=Mathf.FloorToInt(position.x/2f)+":"+Mathf.FloorToInt(position.y/2f)+":"+Mathf.FloorToInt(position.z/2f)+":"+
                cell.gameObject.layer+":"+(collider.sharedMaterial!=null?collider.sharedMaterial.GetInstanceID():0)+":"+collider.contactOffset.ToString("R",System.Globalization.CultureInfo.InvariantCulture);
            Chunk chunk;if(!groups.TryGetValue(key,out chunk)){chunk=new Chunk();groups.Add(key,chunk);chunks.Add(chunk);}
            Vector3 center=position+Vector3.Scale(collider.center,scale),half=Vector3.Scale(collider.size,scale)*.5f;
            chunk.Bounds.Add(new NavalSolidBox{Id=chunk.Sources.Count,MinX=center.x-half.x,MaxX=center.x+half.x,MinY=center.y-half.y,MaxY=center.y+half.y,MinZ=center.z-half.z,MaxZ=center.z+half.z});
            chunk.Sources.Add(new Source{Cell=cell,Collider=collider,Position=position,Scale=scale,Center=collider.center,Size=collider.size,Layer=cell.gameObject.layer,Material=collider.sharedMaterial,ContactOffset=collider.contactOffset});
            cell.ColliderBatchOwner=this;cell.ColliderChunk=chunks.IndexOf(chunk);SourceCount++;
        }
        foreach(Chunk chunk in chunks)Queue(chunk);
    }
    private static bool Positive(Vector3 p){return p.x>0f&&p.y>0f&&p.z>0f;}
    private static bool AxisAligned(Transform pose)
    {Quaternion q=pose.localRotation;return q.x==0f&&q.y==0f&&q.z==0f&&Mathf.Abs(q.w)==1f;}
    private bool Eligible(Source source)
    {
        NavalHullVoxel cell=source.Cell;Transform pose=cell.transform;
        return !cell.Missing&&!cell.Detached&&pose.parent==transform&&AxisAligned(pose)&&
            pose.localPosition.Equals(source.Position)&&pose.localScale.Equals(source.Scale)&&
            source.Collider.center.Equals(source.Center)&&source.Collider.size.Equals(source.Size)&&!source.Collider.isTrigger&&
            cell.gameObject.layer==source.Layer&&source.Collider.sharedMaterial==source.Material&&source.Collider.contactOffset==source.ContactOffset;
    }
    private void Queue(Chunk chunk){if(!chunk.Dirty){chunk.Dirty=true;dirty.Add(chunk);}}
    public void MarkDirty(NavalHullVoxel cell)
    {
        if(cell.ColliderChunk<0||cell.ColliderChunk>=chunks.Count)return;
        Chunk chunk=chunks[cell.ColliderChunk];
        if(chunk.Merged)
        {
            // Never leave an old solid proxy over a freshly opened hole, even before the next Flush.
            foreach(NavalMergedCollider proxy in chunk.Proxies)proxy.Shape.enabled=false;
            ActiveCount-=chunk.Active;BatchedCount-=chunk.Batched;chunk.Active=chunk.Batched=0;chunk.Merged=false;
            foreach(Source source in chunk.Sources)
            {
                source.Cell.ColliderBatched=false;source.Collider.enabled=!source.Cell.Missing;
                if(hull.PhysicsBody!=null)hull.PhysicsBody.MarkDirty(source.Cell);
            }
        }
        Queue(chunk);
    }
    public static void FlushAll(){for(int i=0;i<all.Count;i++)if(all[i]!=null)all[i].Flush();}
    private void LateUpdate(){if(dirty.Count>0)NavalPerformance.SyncTransforms();}
    private void Flush()
    {
        if(dirty.Count==0)return;
        using(NavalPerformance.Measure(NavalPerfArea.ColliderChunks))
        {
            foreach(Chunk chunk in dirty)Rebuild(chunk);dirty.Clear();
        }
    }
    private void Rebuild(Chunk chunk)
    {
        chunk.Input.Clear();
        for(int i=0;i<chunk.Sources.Count;i++)if(Eligible(chunk.Sources[i]))chunk.Input.Add(chunk.Bounds[i]);
        List<NavalMergedBox> boxes=chunk.Merger.Build(chunk.Input);
        foreach(Source source in chunk.Sources)source.Included=false;
        int proxyIndex=0,membersCount=0;
        foreach(NavalMergedBox merged in boxes)
        {
            if(merged.Cells.Count<2)continue;
            foreach(int member in merged.Cells){chunk.Sources[member].Included=true;membersCount++;}
            int i=proxyIndex++;
            NavalMergedCollider proxy;
            if(i>=chunk.Proxies.Count)
            {
                GameObject obj=new GameObject("NavalPrototype_HullCollisionBox");obj.transform.SetParent(transform,false);
                obj.layer=chunk.Sources[0].Layer;
                BoxCollider collider=obj.AddComponent<BoxCollider>();collider.enabled=false;
                collider.sharedMaterial=chunk.Sources[0].Material;collider.contactOffset=chunk.Sources[0].ContactOffset;
                proxy=obj.AddComponent<NavalMergedCollider>();proxy.Shape=collider;chunk.Proxies.Add(proxy);
            }
            else proxy=chunk.Proxies[i];
            NavalSolidBox box=merged.Box;
            proxy.Shape.center=new Vector3((box.MinX+box.MaxX)*.5f,(box.MinY+box.MaxY)*.5f,(box.MinZ+box.MaxZ)*.5f);
            proxy.Shape.size=new Vector3(box.MaxX-box.MinX,box.MaxY-box.MinY,box.MaxZ-box.MinZ);
            proxy.Initialize(hull,chunk.Bounds,merged.Cells);
            proxy.Cells.Clear();foreach(int member in merged.Cells)proxy.Cells.Add(chunk.Sources[member].Cell);
            proxy.Shape.enabled=true;
        }
        for(int i=proxyIndex;i<chunk.Proxies.Count;i++)chunk.Proxies[i].Shape.enabled=false;
        for(int i=0;i<chunk.Sources.Count;i++)
        {
            Source source=chunk.Sources[i];bool included=source.Included;
            source.Cell.ColliderBatched=included;source.Collider.enabled=!source.Cell.Missing&&!included;
            if(hull.PhysicsBody!=null)hull.PhysicsBody.MarkDirty(source.Cell);
        }
        chunk.Active=proxyIndex;chunk.Batched=membersCount;ActiveCount+=chunk.Active;BatchedCount+=membersCount;chunk.Merged=proxyIndex>0;chunk.Dirty=false;
    }
    private void OnDestroy()
    {
        all.Remove(this);
        foreach(Chunk chunk in chunks)
        {
            foreach(NavalMergedCollider proxy in chunk.Proxies)if(proxy!=null)Destroy(proxy.gameObject);
            foreach(Source source in chunk.Sources)if(source.Cell!=null)
            {source.Cell.ColliderBatchOwner=null;source.Cell.ColliderBatched=false;source.Collider.enabled=!source.Cell.Missing;}
        }
    }
}
public sealed class NavalMergedCollider : MonoBehaviour
{
    public BoxCollider Shape;
    public readonly List<NavalHullVoxel> Cells=new List<NavalHullVoxel>();
    private NavalHull hull;
    private List<NavalSolidBox> source;
    private List<int> members;
    public void Initialize(NavalHull owner,List<NavalSolidBox> bounds,List<int> indices){hull=owner;source=bounds;members=indices;}
    public NavalHullVoxel Resolve(Vector3 point)
    {
        if(!Shape.enabled)return null;
        Vector3 local=hull.Root.InverseTransformPoint(point);
        int member=NavalBoxHitResolver.Resolve(source,members,local.x,local.y,local.z,.001f);
        if(member<0)return null;
        for(int i=0;i<members.Count;i++)if(members[i]==member)return Cells[i];
        return null;
    }
}
public static class NavalVoxelHit
{
    public static NavalHullVoxel Resolve(Collider collider,Vector3 point)
    {
        NavalHullVoxel direct=collider.GetComponent<NavalHullVoxel>();if(direct!=null)return direct;
        NavalMergedCollider merged=collider.GetComponent<NavalMergedCollider>();return merged!=null?merged.Resolve(point):null;
    }
}
