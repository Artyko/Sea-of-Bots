using System.Collections.Generic;
using UnityEngine;

// Fixed local chunks of exactly merged boxes. Dirty chunks immediately fall back to source colliders.
[DefaultExecutionOrder(120)]
public sealed class NavalHullColliders : MonoBehaviour
{
    private sealed class Source
    {public NavalHullVoxel Cell;public BoxCollider Collider;public Vector3 Position,Scale,Center,Size;public bool Included,Trigger;public NavalMergedCollider Proxy;public int Layer;public PhysicsMaterial Material;public float ContactOffset;}
    private sealed class Chunk
    {
        public readonly List<Source> Sources=new List<Source>();
        public readonly List<NavalSolidBox> Bounds=new List<NavalSolidBox>(),Input=new List<NavalSolidBox>();
        public readonly List<NavalMergedCollider> Proxies=new List<NavalMergedCollider>();
        public readonly NavalBoxMerger Merger=new NavalBoxMerger();
        public Transform Parent;
        public bool Dirty,Merged;
        public int Active,Batched;
    }
    private static readonly List<NavalHullColliders> all=new List<NavalHullColliders>();
    private readonly List<Chunk> chunks=new List<Chunk>(),dirty=new List<Chunk>();
    private int lastBudgetFrame=-1;
    private NavalHull hull;
    public int SourceCount {get;private set;}
    public int ActiveCount {get;private set;}
    public int BatchedCount {get;private set;}
    public void Initialize(NavalHull owner)
    {
        hull=owner;all.Add(this);float chunkSize=NavalSpatialBatching.ColliderChunkSize(hull.Definition);bool centered=hull.Definition.SteamPowered;
        bool fleet=NavalTestWorld.Active!=null&&NavalTestWorld.Active.ConvoyMission;
        if(fleet)chunkSize=3f;
        Dictionary<string,Chunk> groups=new Dictionary<string,Chunk>();
        foreach(NavalHullVoxel cell in hull.Voxels)
        {
            BoxCollider collider=cell.PhysicsCollider as BoxCollider;
            Transform pose=cell.transform;
            Transform parent=pose.parent;
            if(collider==null||!collider.enabled||(!fleet&&collider.isTrigger)||cell.ManagedRig||
                cell.CargoCollision!=null||cell.Missing||cell.Detached||parent==null||
                (!fleet&&(cell.MovableEquipment||parent!=transform))||
                (parent!=transform&&!parent.IsChildOf(transform))||!AxisAligned(pose)||!Positive(pose.localScale))continue;
            Vector3 position=pose.localPosition,scale=pose.localScale;
            // Layer, physical material and contact offset must also agree. Built only at construction.
            float size= fleet&&parent==transform&&!collider.isTrigger?4f:chunkSize;
            string key=parent.GetInstanceID()+":"+NavalSpatialBatching.ColliderChunk(position.x,size,centered)+":"+NavalSpatialBatching.ColliderChunk(position.y,size,centered)+":"+NavalSpatialBatching.ColliderChunk(position.z,size,centered)+":"+
                cell.gameObject.layer+":"+collider.isTrigger+":"+(collider.sharedMaterial!=null?collider.sharedMaterial.GetInstanceID():0)+":"+collider.contactOffset.ToString("R",System.Globalization.CultureInfo.InvariantCulture);
            Chunk chunk;if(!groups.TryGetValue(key,out chunk)){chunk=new Chunk{Parent=parent};groups.Add(key,chunk);chunks.Add(chunk);}
            Vector3 center=position+Vector3.Scale(collider.center,scale),half=Vector3.Scale(collider.size,scale)*.5f;
            chunk.Bounds.Add(new NavalSolidBox{Id=chunk.Sources.Count,MinX=center.x-half.x,MaxX=center.x+half.x,MinY=center.y-half.y,MaxY=center.y+half.y,MinZ=center.z-half.z,MaxZ=center.z+half.z});
            chunk.Sources.Add(new Source{Cell=cell,Collider=collider,Position=position,Scale=scale,Center=collider.center,Size=collider.size,Layer=cell.gameObject.layer,Trigger=collider.isTrigger,Material=collider.sharedMaterial,ContactOffset=collider.contactOffset});
            cell.ColliderBatchOwner=this;cell.ColliderChunk=chunks.IndexOf(chunk);cell.ColliderSourceIndex=chunk.Sources.Count-1;SourceCount++;
        }
        foreach(Chunk chunk in chunks)Queue(chunk);
    }
    private static bool Positive(Vector3 p){return p.x>0f&&p.y>0f&&p.z>0f;}
    private static bool AxisAligned(Transform pose)
    {Quaternion q=pose.localRotation;return q.x==0f&&q.y==0f&&q.z==0f&&Mathf.Abs(q.w)==1f;}
    private bool Eligible(Source source,Transform parent)
    {
        NavalHullVoxel cell=source.Cell;Transform pose=cell.transform;
        return !cell.Missing&&!cell.Detached&&!cell.NonPhysicalDecoration&&pose.parent==parent&&AxisAligned(pose)&&
            pose.localPosition.Equals(source.Position)&&pose.localScale.Equals(source.Scale)&&
            source.Collider.center.Equals(source.Center)&&source.Collider.size.Equals(source.Size)&&source.Collider.isTrigger==source.Trigger&&
            cell.gameObject.layer==source.Layer&&source.Collider.sharedMaterial==source.Material&&source.Collider.contactOffset==source.ContactOffset;
    }
    private void Queue(Chunk chunk){if(!chunk.Dirty){chunk.Dirty=true;dirty.Add(chunk);}}
    public void MarkDirty(NavalHullVoxel cell)
    {
        if(cell.ColliderChunk<0||cell.ColliderChunk>=chunks.Count)return;
        Chunk chunk=chunks[cell.ColliderChunk];
        int sourceIndex=cell.ColliderSourceIndex;
        if(sourceIndex>=0&&sourceIndex<chunk.Sources.Count)
        {
            var proxy=chunk.Sources[sourceIndex].Proxy;
            if(proxy!=null&&proxy.Shape.enabled)
            {
                // Restore only the original boxes covered by this proxy.
                // Other exact shapes in the chunk remain valid until rebuilding.
                proxy.Shape.enabled=false;
                int count=proxy.Cells.Count;
                chunk.Active--;ActiveCount--;
                chunk.Batched-=count;BatchedCount-=count;
                foreach(var member in proxy.Cells)
                {
                    Source source=chunk.Sources[member.ColliderSourceIndex];
                    source.Proxy=null;
                    source.Included=false;
                    member.ColliderBatched=false;
                    source.Collider.enabled=!member.Missing&&!member.NonPhysicalDecoration;
                }
                chunk.Merged=chunk.Active>0;
            }
        }
        Queue(chunk);
    }
    public static void FlushAll(){for(int i=0;i<all.Count;i++)if(all[i]!=null)all[i].Flush(int.MaxValue);}
    // Damage already disables a stale merged proxy and restores the exact
    // source boxes synchronously. Re-merging is therefore an optimization, not
    // a correctness requirement, and may be spread across frames to avoid a
    // 100+ ms armour-impact hitch.
    public static void FlushFrame(){for(int i=0;i<all.Count;i++)if(all[i]!=null)all[i].FlushFrameBudget();}
    private void FlushFrameBudget()
    {
        if(lastBudgetFrame==Time.frameCount)return;
        lastBudgetFrame=Time.frameCount;Flush(1);
    }
    private void LateUpdate(){if(dirty.Count>0)NavalPerformance.SyncTransforms();}
    private void Flush(int budget)
    {
        if(dirty.Count==0)return;
        using(NavalPerformance.Measure(NavalPerfArea.ColliderChunks))
        {
            int count=Mathf.Min(Mathf.Max(1,budget),dirty.Count);
            for(int i=0;i<count;i++)
            {
                int index=dirty.Count-1;Chunk chunk=dirty[index];dirty.RemoveAt(index);Rebuild(chunk);
            }
        }
    }
    private void Rebuild(Chunk chunk)
    {
        ActiveCount-=chunk.Active;
        BatchedCount-=chunk.Batched;
        chunk.Input.Clear();
        for(int i=0;i<chunk.Sources.Count;i++)if(Eligible(chunk.Sources[i],chunk.Parent))chunk.Input.Add(chunk.Bounds[i]);
        List<NavalMergedBox> boxes=chunk.Merger.Build(chunk.Input);
        foreach(Source source in chunk.Sources){source.Included=false;source.Proxy=null;}
        int proxyIndex=0,membersCount=0;
        foreach(NavalMergedBox merged in boxes)
        {
            if(merged.Cells.Count<2)continue;
            foreach(int member in merged.Cells){chunk.Sources[member].Included=true;membersCount++;}
            int i=proxyIndex++;
            NavalMergedCollider proxy;
            if(i>=chunk.Proxies.Count)
            {
                GameObject obj=new GameObject("NavalPrototype_HullCollisionBox");obj.transform.SetParent(chunk.Parent,false);
                obj.layer=chunk.Sources[0].Layer;
                BoxCollider collider=obj.AddComponent<BoxCollider>();collider.enabled=false;collider.isTrigger=chunk.Sources[0].Trigger;
                collider.sharedMaterial=chunk.Sources[0].Material;collider.contactOffset=chunk.Sources[0].ContactOffset;
                proxy=obj.AddComponent<NavalMergedCollider>();proxy.Shape=collider;chunk.Proxies.Add(proxy);
            }
            else proxy=chunk.Proxies[i];
            NavalSolidBox box=merged.Box;
            proxy.Shape.center=new Vector3((box.MinX+box.MaxX)*.5f,(box.MinY+box.MaxY)*.5f,(box.MinZ+box.MaxZ)*.5f);
            proxy.Shape.size=new Vector3(box.MaxX-box.MinX,box.MaxY-box.MinY,box.MaxZ-box.MinZ);
            proxy.Initialize(hull,chunk.Bounds,merged.Cells);
            proxy.Cells.Clear();
            foreach(int member in merged.Cells)
            {
                var source=chunk.Sources[member];
                source.Proxy=proxy;
                proxy.Cells.Add(source.Cell);
            }
            proxy.Shape.enabled=true;
        }
        for(int i=proxyIndex;i<chunk.Proxies.Count;i++)chunk.Proxies[i].Shape.enabled=false;
        for(int i=0;i<chunk.Sources.Count;i++)
        {
            Source source=chunk.Sources[i];bool included=source.Included;
            source.Cell.ColliderBatched=included;source.Collider.enabled=!source.Cell.Missing&&!source.Cell.NonPhysicalDecoration&&!included;
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
    public bool IsFixedSupportFor(NavalHull owner)
    {
        return hull==owner&&transform.parent==owner.Root&&Shape.enabled&&!Shape.isTrigger;
    }
    public NavalHullVoxel Resolve(Vector3 point)
    {
        if(!Shape.enabled)return null;
        Vector3 local=transform.InverseTransformPoint(point);
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
        if(collider==null||!collider.enabled)return null;
        NavalHullVoxel direct=collider.GetComponent<NavalHullVoxel>();if(direct!=null)return direct.Missing?null:direct;
        NavalCargoProxy cargo=collider.GetComponent<NavalCargoProxy>();if(cargo!=null)return cargo.Resolve(point);
        NavalMergedCollider merged=collider.GetComponent<NavalMergedCollider>();return merged!=null?merged.Resolve(point):null;
    }
    public static NavalHullVoxel Resolve(Collider collider,Vector3 point,Vector3 surfacePoint)
    {
        NavalHullVoxel cell=Resolve(collider,point);
        // PhysX sweep contacts at initial overlaps and seams may not lie on the box.
        // Retry on that same shape's surface, without widening the voxel hit tolerance.
        return cell!=null?cell:Resolve(collider,surfacePoint);
    }
}
