using UnityEngine;
using UnityEngine.Rendering;
[DefaultExecutionOrder(450)]
public class NavalFloodAppearance : MonoBehaviour
{
    private NavalWater ocean;
    private NavalHull hull;
    private Material material;
    private MeshRenderer renderer;
    private Mesh mesh;
    private Vector3[] vertices;
    private NavalFloodRenderState state;
    private float nextMeshAt;
    private bool hidden;
    // Four vertices per face preserve the original flat cube normals.
    private static readonly Vector3[] normals={Vector3.left,Vector3.right,Vector3.down,Vector3.up,Vector3.back,Vector3.forward};
    private static readonly Vector3[] corners={
        new Vector3(-.5f,-.5f,-.5f),new Vector3(-.5f,-.5f,.5f),new Vector3(-.5f,.5f,.5f),new Vector3(-.5f,.5f,-.5f),
        new Vector3(.5f,-.5f,.5f),new Vector3(.5f,-.5f,-.5f),new Vector3(.5f,.5f,-.5f),new Vector3(.5f,.5f,.5f),
        new Vector3(-.5f,-.5f,-.5f),new Vector3(.5f,-.5f,-.5f),new Vector3(.5f,-.5f,.5f),new Vector3(-.5f,-.5f,.5f),
        new Vector3(-.5f,.5f,.5f),new Vector3(.5f,.5f,.5f),new Vector3(.5f,.5f,-.5f),new Vector3(-.5f,.5f,-.5f),
        new Vector3(.5f,-.5f,-.5f),new Vector3(-.5f,-.5f,-.5f),new Vector3(-.5f,.5f,-.5f),new Vector3(.5f,.5f,-.5f),
        new Vector3(-.5f,-.5f,.5f),new Vector3(.5f,-.5f,.5f),new Vector3(.5f,.5f,.5f),new Vector3(-.5f,.5f,.5f)};
    private Color tint;
    private float previous=-1f;
    public void Initialize(NavalWater sea,NavalHull owner)
    {
        hull=owner;
        ocean=sea;material=new Material(sea.InteriorMaterial);tint=material.color;
        // Fade all shading, including specular highlights, rather than retaining
        // Standard transparent-mode reflections at zero diffuse alpha.
        if(material.shader.name=="Standard")
        {
            material.SetFloat("_Mode",2f);material.SetInt("_SrcBlend",(int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);material.SetInt("_ZWrite",0);
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");material.DisableKeyword("_ALPHATEST_ON");material.EnableKeyword("_ALPHABLEND_ON");
            material.SetOverrideTag("RenderType","Transparent");material.renderQueue=3000;
        }
        NavalFloodGrid grid=hull.Flood;
        state=new NavalFloodRenderState(grid.Water.Length);
        vertices=new Vector3[grid.Water.Length*24];
        Vector3[] surfaceNormals=new Vector3[vertices.Length];
        int[] triangles=new int[grid.Water.Length*36];
        for(int cell=0;cell<grid.Water.Length;cell++)
        {
            WriteCell(cell,0f);
            for(int face=0;face<6;face++)
            {
                int first=cell*24+face*4,index=cell*36+face*6;
                for(int v=0;v<4;v++)surfaceNormals[first+v]=normals[face];
                triangles[index]=first;triangles[index+1]=first+1;triangles[index+2]=first+2;
                triangles[index+3]=first;triangles[index+4]=first+2;triangles[index+5]=first+3;
            }
        }
        mesh=new Mesh();mesh.name="Batched compartment water";mesh.MarkDynamic();
        mesh.vertices=vertices;mesh.normals=surfaceNormals;mesh.triangles=triangles;
        // All hydraulic columns are clamped to the hull's upper water level.
        mesh.bounds=new Bounds(new Vector3(0f,(hull.Floor+hull.Definition.RimY)*.5f,0f),
            new Vector3(grid.Width,hull.Definition.RimY-hull.Floor,grid.Length));
        GameObject view=new GameObject("Compartment water mesh");view.transform.SetParent(transform,false);
        view.AddComponent<MeshFilter>().sharedMesh=mesh;renderer=view.AddComponent<MeshRenderer>();
        renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=true;renderer.enabled=false;
    }
    private void WriteCell(int cell,float depth)
    {
        NavalFloodGrid grid=hull.Flood;
        Vector3 centre=new Vector3(grid.X(cell),hull.Floor+depth*.5f,grid.Z(cell));
        Vector3 size=depth>0f?new Vector3(grid.CellWidth,depth,grid.CellLength):Vector3.zero;
        for(int v=0;v<24;v++)vertices[cell*24+v]=centre+Vector3.Scale(corners[v],size);
    }
    public static float Clearance(Transform boat,NavalWater sea)
    {
        float clearance=float.NegativeInfinity;
        NavalSeaLadder equipment=boat.GetComponent<NavalSeaLadder>();
        float top=equipment!=null?equipment.Hull.Definition.AboveDeckY(2.25f):2.25f;
        // The hull and rails, not the still-protruding mast, define full submergence.
        for(int x=-1;x<=1;x++)for(int z=-1;z<=1;z++)
        {
            Vector3 point=boat.TransformPoint(new Vector3(x*BoatDimensions.HalfWidth,top,z*BoatDimensions.HalfLength));
            clearance=Mathf.Max(clearance,point.y-sea.Height(point.x,point.z));
        }
        return clearance;
    }
    private void LateUpdate()
    {
        if(ocean==null||material==null)return;
        float clearance=Clearance(transform,ocean);
        float visibility=NavalFloodVisibilityMath.Visibility(clearance);
        if(Mathf.Abs(previous-visibility)>0.001f)
        {Color color=tint;color.a=tint.a*visibility;material.color=color;previous=visibility;}
        bool hide=NavalFloodVisibilityMath.HideEffects(clearance);
        hull.FloodEffectsHidden=hide;
        if(hide&&!hidden)foreach(NavalHullVoxel cell in hull.GetDamagedVoxels())
            if(cell.Stream!=null&&cell.Stream.gameObject.activeSelf)cell.Stream.gameObject.SetActive(false);
        hidden=hide;
        renderer.enabled=!hide&&state.WetCount>0;
        if(hide)return;
        if(Time.time<nextMeshAt)return;nextMeshAt=Time.time+1f/30f;
        if(state.Refresh(hull.Flood))
        {
            for(int changed=0;changed<state.ChangedCount;changed++)
            {int cell=state.ChangedCells[changed];WriteCell(cell,state.Depths[cell]);}
            mesh.vertices=vertices;
        }
        renderer.enabled=state.WetCount>0;
    }
    private void OnDestroy(){if(mesh!=null)Destroy(mesh);if(material!=null)Destroy(material);}
}
