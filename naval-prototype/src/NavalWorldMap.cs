using System.Collections.Generic;
using UnityEngine;

// All scenery is saved in the editor level. Only its water preview is replaced.
[DefaultExecutionOrder(200)]
public class NavalWorldMap : MonoBehaviour
{
    public const string WaterMarker="NAVAL_OCEAN_SURFACE";
    public static NavalWorldMap Active;
    private readonly List<Renderer> preview=new List<Renderer>();
    private readonly List<Vector3> border=new List<Vector3>();
    private NavalWater ocean;
    private Transform focus,horizon;
    private Vector3 origin;
    private Mesh mesh;
    private Material material;
    private Vector3[] vertices;
    private Camera camera;
    private float previousFar;
    private class QueryCache
    {public Vector3 Point,Avoid;public Quaternion Heading;public float SailAt,AvoidAt;public bool Clear;}
    private readonly Dictionary<int,QueryCache> queries=new Dictionary<int,QueryCache>();
    private Collider[] overlap=new Collider[2048];
    private RaycastHit[] casts=new RaycastHit[2048];
    private QueryCache Cache(Transform ship)
    {int id=ship.GetInstanceID();QueryCache cache;if(!queries.TryGetValue(id,out cache)){cache=new QueryCache();queries.Add(id,cache);}return cache;}
    public static bool TryGetSeaOrigin(out Vector3 point)
    {
        foreach(ObjectPlacedInLevel obj in Object.FindObjectsByType<ObjectPlacedInLevel>(FindObjectsSortMode.None))
            if(obj.name==WaterMarker)
            {point=obj.transform.position+Vector3.up*(obj.transform.lossyScale.y*0.5f);return true;}
        point=Vector3.zero;return false;
    }
    public void Initialize(FirstPersonMover player,Vector3 seaOrigin)
    {
        Active=this;origin=seaOrigin;focus=player.transform;camera=player.GetPlayerCamera();
        if(camera!=null){previousFar=camera.farClipPlane;camera.farClipPlane=12000f;}
        foreach(ObjectPlacedInLevel obj in Object.FindObjectsByType<ObjectPlacedInLevel>(FindObjectsSortMode.None))
            if(obj.name==WaterMarker)foreach(Renderer r in obj.GetComponentsInChildren<Renderer>())if(r.enabled){preview.Add(r);r.enabled=false;}
        for(int i=0;i<100;i++)border.Add(new Vector3(-150f+i*3f,0f,-150f));
        for(int i=0;i<100;i++)border.Add(new Vector3(150f,0f,-150f+i*3f));
        for(int i=0;i<100;i++)border.Add(new Vector3(150f-i*3f,0f,150f));
        for(int i=0;i<100;i++)border.Add(new Vector3(-150f,0f,150f-i*3f));
        vertices=new Vector3[border.Count*3];List<int> indices=new List<int>();
        for(int ring=0;ring<2;ring++)for(int i=0;i<border.Count;i++)
        {int a=ring*border.Count+i,b=ring*border.Count+(i+1)%border.Count,c=a+border.Count,d=b+border.Count;indices.Add(a);indices.Add(b);indices.Add(c);indices.Add(b);indices.Add(d);indices.Add(c);}
        mesh=new Mesh();mesh.name="EditorMapOceanHorizon";mesh.vertices=vertices;mesh.SetTriangles(indices,0);
        GameObject water=new GameObject("DistantOcean");water.transform.SetParent(transform,false);horizon=water.transform;
        water.AddComponent<MeshFilter>().sharedMesh=mesh;
        Shader shader=Shader.Find("Standard");if(shader==null)shader=Shader.Find("Unlit/Color");
        material=new Material(shader);material.color=new Color(0.045f,0.30f,0.39f);
        if(material.HasProperty("_Glossiness"))material.SetFloat("_Glossiness",0.8f);
        water.AddComponent<MeshRenderer>().sharedMaterial=material;
    }
    public void SetOcean(NavalWater water){ocean=water;horizon.GetComponent<Renderer>().sharedMaterial=water.SurfaceMaterial;}
    private void LateUpdate()
    {
        if(ocean==null||focus==null)return;
        horizon.position=origin+new Vector3(Mathf.Floor((focus.position.x-origin.x)/40f)*40f,0f,Mathf.Floor((focus.position.z-origin.z)/40f)*40f);
        for(int i=0;i<border.Count;i++)for(int ring=0;ring<3;ring++)
        {Vector3 p=border[i]*(ring==0?1f:ring==1?4f:160f),world=horizon.position+p;p.y=ring==0?ocean.Height(world.x,world.z)-origin.y:0f;vertices[ring*border.Count+i]=p;}
        mesh.vertices=vertices;mesh.RecalculateNormals();mesh.RecalculateBounds();
    }
    private bool Land(Collider collider)
    {return collider!=null&&collider.GetComponent<NavalHullVoxel>()==null&&collider.GetComponentInParent<Character>()==null&&!collider.transform.IsChildOf(transform.parent);}
    public bool CanSail(Vector3 point,Quaternion heading,Transform ship)
    {
        QueryCache cache=Cache(ship);
        if(Time.time<cache.SailAt&&(point-cache.Point).sqrMagnitude<0.25f&&Quaternion.Angle(heading,cache.Heading)<3f)return cache.Clear;
        cache.SailAt=Time.time+0.08f;cache.Point=point;cache.Heading=heading;cache.Clear=true;
        int count;
        while(true)
        {
            count=Physics.OverlapBoxNonAlloc(point+Vector3.down*1.5f,new Vector3(6.5f,1.8f,12.5f)*(Mathf.Abs(ship.lossyScale.x)/2f),overlap,heading,PhysicsManager.GetEnvironmentLayerMask(),QueryTriggerInteraction.Ignore);
            if(count<overlap.Length)break;overlap=new Collider[overlap.Length*2];
        }
        for(int i=0;i<count;i++)if(Land(overlap[i])){cache.Clear=false;break;}
        return cache.Clear;
    }
    public Vector3 AvoidLand(Vector3 point,Vector3 direction,Transform ship)
    {
        QueryCache cache=Cache(ship);if(Time.time<cache.AvoidAt)return direction+cache.Avoid;
        cache.AvoidAt=Time.time+0.2f;cache.Avoid=Vector3.zero;int count;
        while(true)
        {
            count=Physics.SphereCastNonAlloc(point+Vector3.down,8f*(Mathf.Abs(ship.lossyScale.x)/2f),direction.normalized,casts,65f,PhysicsManager.GetEnvironmentLayerMask(),QueryTriggerInteraction.Ignore);
            if(count<casts.Length)break;casts=new RaycastHit[casts.Length*2];
        }
        for(int i=0;i<count;i++)if(Land(casts[i].collider)){Vector3 normal=casts[i].normal;normal.y=0f;cache.Avoid=normal.normalized*2.5f;break;}
        return direction+cache.Avoid;
    }
    private void OnDestroy()
    {
        if(Active==this)Active=null;
        foreach(Renderer r in preview)if(r!=null)r.enabled=true;
        if(camera!=null)camera.farClipPlane=previousFar;
        if(mesh!=null)Destroy(mesh);if(material!=null)Destroy(material);
    }
}
