using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Persistent vertex slots and triangle buffers; damage changes visibility/material, never the collision geometry.
[DefaultExecutionOrder(150)]
public class NavalVoxelBatch : MonoBehaviour
{
    private sealed class Geometry {public Vector3[] Vertices,Normals;public Vector2[] UV;public int[] Triangles;}
    private sealed class Source {public NavalHullVoxel Cell;public Transform Pose;public Renderer Renderer;public Geometry Shape;}
    private sealed class Chunk
    {
        public readonly List<Source> Sources=new List<Source>();
        public readonly Dictionary<Material,int> PaletteIds=new Dictionary<Material,int>();
        public readonly List<Material> Palette=new List<Material>(),ActiveMaterials=new List<Material>();
        public GameObject Object;public Mesh Mesh;public MeshRenderer Renderer;
        public NavalBatchTopology Topology;public Vector3[] Vertices,Normals;public bool Dirty;
    }
    private NavalHull hull;
    private readonly List<Chunk> chunkList=new List<Chunk>(),dirty=new List<Chunk>();
    private readonly Dictionary<Vector3Int,Chunk> chunks=new Dictionary<Vector3Int,Chunk>();
    public void Initialize(NavalHull owner)
    {
        hull=owner;var geometries=new Dictionary<Mesh,Geometry>();
        foreach(NavalHullVoxel cell in hull.Voxels)if(!cell.ManagedRig&&cell.transform.parent==transform)
        {
            Vector3 p=cell.transform.localPosition;Vector3Int key=new Vector3Int(Mathf.FloorToInt(p.x/2f),Mathf.FloorToInt(p.y/2f),Mathf.FloorToInt(p.z/2f));
            Chunk chunk;if(!chunks.TryGetValue(key,out chunk))
            {
                chunk=new Chunk();chunk.Object=new GameObject("HullRenderChunk");chunk.Object.transform.SetParent(transform,false);
                chunk.Object.AddComponent<MeshFilter>();chunk.Renderer=chunk.Object.AddComponent<MeshRenderer>();
                chunks.Add(key,chunk);chunkList.Add(chunk);
            }
            Mesh mesh=cell.GetComponent<MeshFilter>().sharedMesh;Geometry shape;
            if(!geometries.TryGetValue(mesh,out shape))
            {shape=new Geometry{Vertices=mesh.vertices,Normals=mesh.normals,UV=mesh.uv,Triangles=mesh.triangles};geometries.Add(mesh,shape);}
            chunk.Sources.Add(new Source{Cell=cell,Pose=cell.transform,Renderer=cell.GetComponent<Renderer>(),Shape=shape});
            cell.BatchOwner=this;cell.RenderChunk=chunkList.IndexOf(chunk);
        }
        foreach(Chunk chunk in chunkList)
        {
            int count=chunk.Sources.Count;int[] sizes=new int[count];int[][] triangles=new int[count][];
            for(int i=0;i<count;i++){sizes[i]=chunk.Sources[i].Shape.Vertices.Length;triangles[i]=chunk.Sources[i].Shape.Triangles;}
            chunk.Topology=new NavalBatchTopology(sizes,triangles);chunk.Vertices=new Vector3[chunk.Topology.VertexCount];chunk.Normals=new Vector3[chunk.Topology.VertexCount];
            Vector2[] uv=new Vector2[chunk.Topology.VertexCount];
            for(int i=0;i<count;i++)for(int v=0;v<chunk.Sources[i].Shape.UV.Length;v++)uv[chunk.Topology.Offset(i)+v]=chunk.Sources[i].Shape.UV[v];
            chunk.Mesh=new Mesh();chunk.Mesh.indexFormat=IndexFormat.UInt32;chunk.Mesh.vertices=chunk.Vertices;chunk.Mesh.uv=uv;
            chunk.Object.GetComponent<MeshFilter>().sharedMesh=chunk.Mesh;MarkDirty(chunk);
        }
        LateUpdate();
        Debug.Log("[NavalPrototype] Hull rendering uses "+chunks.Count+" persistent mesh buffers; individual damage retained.");
    }
    public void MarkDirty(NavalHullVoxel cell)
    {if(cell.RenderChunk>=0&&cell.RenderChunk<chunkList.Count)MarkDirty(chunkList[cell.RenderChunk]);}
    private void MarkDirty(Chunk chunk){if(!chunk.Dirty){chunk.Dirty=true;dirty.Add(chunk);}}
    private void LateUpdate()
    {
        if(hull==null||dirty.Count==0)return;
        using(NavalPerformance.Measure(NavalPerfArea.Chunks))
        {foreach(Chunk chunk in dirty){Rebuild(chunk);chunk.Dirty=false;}dirty.Clear();}
    }
    private void Rebuild(Chunk chunk)
    {
        for(int i=0;i<chunk.Sources.Count;i++)
        {
            Source source=chunk.Sources[i];NavalHullVoxel cell=source.Cell;
            bool attached=!cell.Detached&&source.Pose.parent==transform,visible=attached&&!cell.Missing;
            cell.Batched=attached;source.Renderer.enabled=!attached&&!cell.Missing;int id=-1;
            if(visible)
            {
                Material material=source.Renderer.sharedMaterial;
                if(!chunk.PaletteIds.TryGetValue(material,out id)){id=chunk.Palette.Count;chunk.PaletteIds.Add(material,id);chunk.Palette.Add(material);}
                Matrix4x4 matrix=Matrix4x4.TRS(source.Pose.localPosition,source.Pose.localRotation,source.Pose.localScale);
                Matrix4x4 normalMatrix=matrix.inverse.transpose;int offset=chunk.Topology.Offset(i);
                for(int v=0;v<source.Shape.Vertices.Length;v++)
                {chunk.Vertices[offset+v]=matrix.MultiplyPoint3x4(source.Shape.Vertices[v]);chunk.Normals[offset+v]=normalMatrix.MultiplyVector(source.Shape.Normals[v]).normalized;}
            }
            chunk.Topology.SetCell(i,id);
        }
        chunk.Topology.Rebuild();chunk.ActiveMaterials.Clear();
        foreach(int material in chunk.Topology.ActiveMaterials)chunk.ActiveMaterials.Add(chunk.Palette[material]);
        chunk.Mesh.vertices=chunk.Vertices;chunk.Mesh.normals=chunk.Normals;chunk.Mesh.subMeshCount=chunk.ActiveMaterials.Count;
        for(int sub=0;sub<chunk.ActiveMaterials.Count;sub++)chunk.Mesh.SetTriangles(chunk.Topology.Triangles(chunk.Topology.ActiveMaterials[sub]),sub,false);
        chunk.Mesh.RecalculateBounds();chunk.Renderer.SetSharedMaterials(chunk.ActiveMaterials);
        chunk.Object.SetActive(chunk.ActiveMaterials.Count>0);
    }
    private void OnDestroy(){foreach(Chunk chunk in chunkList)if(chunk.Mesh!=null)Destroy(chunk.Mesh);}
}
