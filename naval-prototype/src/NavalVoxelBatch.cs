using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Keep per-voxel colliders/damage, draw static hull cells as spatial chunks.
[DefaultExecutionOrder(150)]
public class NavalVoxelBatch : MonoBehaviour
{
    private class Chunk
    {public readonly List<NavalHullVoxel> Cells=new List<NavalHullVoxel>();public GameObject Object;public Mesh Mesh;public bool Dirty;}
    private NavalHull hull;
    private readonly List<Chunk> chunkList=new List<Chunk>(),dirty=new List<Chunk>();
    private readonly List<Mesh> scratch=new List<Mesh>();
    private readonly Dictionary<Vector3Int,Chunk> chunks=new Dictionary<Vector3Int,Chunk>();
    public void Initialize(NavalHull owner)
    {
        hull=owner;
        foreach(NavalHullVoxel cell in hull.Voxels)if(!cell.ManagedRig&&cell.transform.parent==transform)
        {
            Vector3 p=cell.transform.localPosition;Vector3Int key=new Vector3Int(Mathf.FloorToInt(p.x/2f),Mathf.FloorToInt(p.y/2f),Mathf.FloorToInt(p.z/2f));
            Chunk chunk;if(!chunks.TryGetValue(key,out chunk))
            {chunk=new Chunk();chunk.Object=new GameObject("HullRenderChunk");chunk.Object.transform.SetParent(transform,false);chunk.Object.AddComponent<MeshFilter>();chunk.Object.AddComponent<MeshRenderer>();chunks.Add(key,chunk);chunkList.Add(chunk);}
            chunk.Cells.Add(cell);cell.BatchOwner=this;
            cell.RenderChunk=chunkList.IndexOf(chunk);
        }
        foreach(Chunk chunk in chunkList)MarkDirty(chunk);
        LateUpdate();
        Debug.Log("[NavalPrototype] Hull rendering batched into "+chunks.Count+" spatial chunks; individual damage retained.");
    }
    public void MarkDirty(NavalHullVoxel cell)
    {if(cell.RenderChunk>=0&&cell.RenderChunk<chunkList.Count)MarkDirty(chunkList[cell.RenderChunk]);}
    private void MarkDirty(Chunk chunk){if(!chunk.Dirty){chunk.Dirty=true;dirty.Add(chunk);}}
    private void LateUpdate()
    {
        if(hull==null||dirty.Count==0)return;
        using(NavalPerformance.Measure(NavalPerfArea.Chunks))
        {
            for(int i=0;i<dirty.Count;i++)
            {
                Chunk chunk=dirty[i];chunk.Dirty=false;
                foreach(NavalHullVoxel cell in chunk.Cells)
                {
                    bool attached=!cell.Detached&&cell.transform.parent==transform;
                    cell.Batched=attached;cell.GetComponent<Renderer>().enabled=!attached&&!cell.Missing;
                }
                Rebuild(chunk);
            }
            dirty.Clear();
        }
    }
    private void Rebuild(Chunk chunk)
    {
        Dictionary<Material,List<CombineInstance>> groups=new Dictionary<Material,List<CombineInstance>>();
        foreach(NavalHullVoxel cell in chunk.Cells)if(!cell.Missing&&!cell.Detached&&cell.transform.parent==transform)
        {
            Material material=cell.GetComponent<Renderer>().sharedMaterial;List<CombineInstance> list;
            if(!groups.TryGetValue(material,out list)){list=new List<CombineInstance>();groups.Add(material,list);}
            CombineInstance part=new CombineInstance();part.mesh=cell.GetComponent<MeshFilter>().sharedMesh;
            part.transform=Matrix4x4.TRS(cell.transform.localPosition,cell.transform.localRotation,cell.transform.localScale);list.Add(part);
        }
        List<Material> materials=new List<Material>();List<CombineInstance> combined=new List<CombineInstance>();int scratchIndex=0;
        foreach(KeyValuePair<Material,List<CombineInstance>> pair in groups)
        {
            if(scratchIndex==scratch.Count){Mesh temporary=new Mesh();temporary.indexFormat=IndexFormat.UInt32;scratch.Add(temporary);}
            Mesh mesh=scratch[scratchIndex++];mesh.Clear();mesh.CombineMeshes(pair.Value.ToArray(),true,true);materials.Add(pair.Key);
            CombineInstance part=new CombineInstance();part.mesh=mesh;part.transform=Matrix4x4.identity;combined.Add(part);
        }
        if(chunk.Mesh==null){chunk.Mesh=new Mesh();chunk.Mesh.indexFormat=IndexFormat.UInt32;}else chunk.Mesh.Clear();
        if(combined.Count>0)chunk.Mesh.CombineMeshes(combined.ToArray(),false,true);
        chunk.Object.GetComponent<MeshFilter>().sharedMesh=chunk.Mesh;chunk.Object.GetComponent<Renderer>().sharedMaterials=materials.ToArray();
        chunk.Object.SetActive(combined.Count>0);
    }
    private void OnDestroy(){foreach(Chunk chunk in chunks.Values)if(chunk.Mesh!=null)Destroy(chunk.Mesh);foreach(Mesh mesh in scratch)Destroy(mesh);}
}
