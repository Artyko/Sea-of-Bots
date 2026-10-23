using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Two renderers per mast: animated cloth and event-driven timber. Voxel colliders stay intact.
[DefaultExecutionOrder(200)]
public sealed class NavalRigRenderBatch : MonoBehaviour
{
    private sealed class Geometry {public Vector3[] Vertices,Normals;public Vector2[] UV;public int[] Triangles;}
    private NavalHullVoxel[] cells;
    private Transform root;
    private Transform[] poses;
    private Renderer[] originals;
    private Geometry[] geometry;
    private Vector3[] posePositions;
    private Quaternion[] poseRotations;
    private float[] clothScales;
    private Vector3[][] scaledVertices;
    private bool[] visible;
    private NavalBatchTopology topology;
    private readonly Dictionary<Material,int> paletteIds=new Dictionary<Material,int>();
    private readonly List<Material> palette=new List<Material>(),activeMaterials=new List<Material>();
    private Vector3[] vertices,normals;
    private Mesh mesh;
    private MeshRenderer renderer;
    private bool animated,dirty=true;
    private bool poseDirty;
    public static void Create(Transform parent,List<NavalHullVoxel> source,bool deforming)
    {
        if(source.Count==0)return;
        GameObject obj=new GameObject(deforming?"NavalPrototype_ClothBatch":"NavalPrototype_RigTimberBatch");
        obj.layer=source[0].gameObject.layer;obj.transform.SetParent(parent,false);
        obj.AddComponent<NavalRigRenderBatch>().Initialize(parent,source,deforming);
    }
    private void Initialize(Transform parent,List<NavalHullVoxel> source,bool deforming)
    {
        root=parent;animated=deforming;cells=source.ToArray();clothScales=new float[cells.Length];for(int i=0;i<clothScales.Length;i++)clothScales[i]=1f;
        poses=new Transform[cells.Length];originals=new Renderer[cells.Length];geometry=new Geometry[cells.Length];visible=new bool[cells.Length];
        posePositions=new Vector3[cells.Length];poseRotations=new Quaternion[cells.Length];scaledVertices=new Vector3[cells.Length][];
        int[] counts=new int[cells.Length];int[][] triangles=new int[cells.Length][];
        Dictionary<Mesh,Geometry> geometries=new Dictionary<Mesh,Geometry>();
        for(int i=0;i<cells.Length;i++)
        {
            poses[i]=cells[i].transform;originals[i]=cells[i].GetComponent<Renderer>();
            Mesh sourceMesh=cells[i].GetComponent<MeshFilter>().sharedMesh;Geometry shape;
            if(!geometries.TryGetValue(sourceMesh,out shape))
            {
                shape=new Geometry{Vertices=sourceMesh.vertices,Normals=sourceMesh.normals,UV=sourceMesh.uv,Triangles=sourceMesh.triangles};
                geometries.Add(sourceMesh,shape);
            }
            geometry[i]=shape;counts[i]=shape.Vertices.Length;triangles[i]=shape.Triangles;
            cells[i].RigBatchOwner=this;
            cells[i].RigBatchIndex=i;
            posePositions[i]=poses[i].localPosition;poseRotations[i]=poses[i].localRotation;
            scaledVertices[i]=new Vector3[shape.Vertices.Length];
            Vector3 scale=poses[i].localScale;
            for(int v=0;v<shape.Vertices.Length;v++)scaledVertices[i][v]=Vector3.Scale(shape.Vertices[v],scale);
        }
        topology=new NavalBatchTopology(counts,triangles);vertices=new Vector3[topology.VertexCount];normals=new Vector3[topology.VertexCount];
        Vector2[] uv=new Vector2[topology.VertexCount];
        for(int i=0;i<cells.Length;i++)
            for(int v=0;v<geometry[i].UV.Length&&v<counts[i];v++)uv[topology.Offset(i)+v]=geometry[i].UV[v];
        mesh=new Mesh();mesh.name=animated?"Naval moving cloth":"Naval rig timber";
        mesh.indexFormat=IndexFormat.UInt32;if(animated)mesh.MarkDynamic();
        mesh.vertices=vertices;mesh.normals=normals;mesh.uv=uv;
        gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;renderer=gameObject.AddComponent<MeshRenderer>();
        renderer.shadowCastingMode=originals[0].shadowCastingMode;renderer.receiveShadows=originals[0].receiveShadows;
        Refresh();
    }
    public void MarkDirty(){dirty=true;}
    public void SetAnimatedPose(int index,Vector3 position,Quaternion rotation,float clothScale=1f)
    {posePositions[index]=position;poseRotations[index]=rotation;clothScales[index]=clothScale;poseDirty=true;}
    private void LateUpdate(){if(mesh!=null&&(dirty||animated&&poseDirty))Refresh();}
    private void Refresh()
    {
        poseDirty=false;
        using(NavalPerformance.Measure(NavalPerfArea.RigRendering))
        {
            if(dirty)
            {
                for(int i=0;i<cells.Length;i++)
                {
                    NavalHullVoxel cell=cells[i];
                    bool attached=cell!=null&&!cell.Detached&&poses[i].parent==root;
                    visible[i]=attached&&!cell.Missing;
                    if(cell!=null)
                    {
                        cell.Batched=attached;originals[i].enabled=!attached&&!cell.Missing;
                    }
                    int id=-1;
                    if(visible[i])
                    {
                        Material material=originals[i].sharedMaterial;
                        if(!paletteIds.TryGetValue(material,out id)){id=palette.Count;paletteIds.Add(material,id);palette.Add(material);}
                    }
                    topology.SetCell(i,id);
                }
                topology.Rebuild();activeMaterials.Clear();
                foreach(int material in topology.ActiveMaterials)activeMaterials.Add(palette[material]);
                mesh.subMeshCount=activeMaterials.Count;
                for(int sub=0;sub<activeMaterials.Count;sub++)mesh.SetTriangles(topology.Triangles(topology.ActiveMaterials[sub]),sub,false);
                renderer.SetSharedMaterials(activeMaterials);renderer.enabled=activeMaterials.Count>0;dirty=false;
            }
            if(!renderer.enabled)return;
            for(int i=0;i<cells.Length;i++)if(visible[i])
            {
                Transform pose=poses[i];Quaternion rotation=animated?poseRotations[i]:pose.localRotation;
                Vector3 position=animated?posePositions[i]:pose.localPosition;
                Geometry shape=geometry[i];int offset=topology.Offset(i);
                Vector3 right=rotation*Vector3.right,up=rotation*Vector3.up,forward=rotation*Vector3.forward;
                for(int v=0;v<shape.Vertices.Length;v++)
                {
                    Vector3 scaled=scaledVertices[i][v];
                    vertices[offset+v]=position+right*scaled.x+up*(scaled.y*clothScales[i])+forward*scaled.z;
                    // Source cells are boxes: face normals lie on scale axes, so rotation preserves the exact normal.
                    Vector3 normal=shape.Normals[v];
                    normals[offset+v]=Mathf.Abs(normal.x)==1f?right*normal.x:Mathf.Abs(normal.y)==1f?up*normal.y:Mathf.Abs(normal.z)==1f?forward*normal.z:rotation*normal;
                }
            }
            mesh.vertices=vertices;mesh.normals=normals;mesh.RecalculateBounds();
        }
    }
    private void OnDestroy()
    {
        if(mesh!=null)Destroy(mesh);
        if(cells==null)return;
        for(int i=0;i<cells.Length;i++)if(cells[i]!=null)
        {cells[i].RigBatchOwner=null;cells[i].RigBatchIndex=-1;cells[i].Batched=false;if(originals[i]!=null)originals[i].enabled=!cells[i].Missing;}
    }
}
