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
    {public Vector3 Point;public Quaternion Heading;public float SailAt;public bool Clear;}
    private readonly Dictionary<int,QueryCache> queries=new Dictionary<int,QueryCache>();
    private Collider[] overlap=new Collider[2048];
    private RaycastHit[] casts=new RaycastHit[2048];
    private BoxCollider shoreProbe;
    private readonly HashSet<Collider> knownLand=new HashSet<Collider>();
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
    public void SetFocus(Transform target){focus=target;}
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
    {
        if(collider==null||collider.transform.IsChildOf(transform.parent))return false;
        if(knownLand.Contains(collider))return true;
        if(collider.GetComponent<NavalHullVoxel>()!=null||collider.GetComponentInParent<Character>()!=null)return false;
        knownLand.Add(collider);return true;
    }
    public bool CanSail(Vector3 point,Quaternion heading,Transform ship)
    {
        QueryCache cache=Cache(ship);
        if(Time.time<cache.SailAt&&(point-cache.Point).sqrMagnitude<0.25f&&Quaternion.Angle(heading,cache.Heading)<3f)return cache.Clear;
        cache.SailAt=Time.time+0.08f;cache.Point=point;cache.Heading=heading;cache.Clear=true;
        int count;
        while(true)
        {
            Vector3 offset,half;ShoreVolume(ship,out offset,out half);
            count=Physics.OverlapBoxNonAlloc(point+offset,half,overlap,heading,PhysicsManager.GetEnvironmentLayerMask(),QueryTriggerInteraction.Ignore);
            if(count<overlap.Length)break;overlap=new Collider[overlap.Length*2];
        }
        for(int i=0;i<count;i++)if(Land(overlap[i])){cache.Clear=false;break;}
        return cache.Clear;
    }
    private void ShoreVolume(Transform ship,out Vector3 offset,out Vector3 half)
    {
        float scale=Mathf.Abs(ship.lossyScale.x);NavalRamDamage equipment=ship.GetComponent<NavalRamDamage>();
        float bottom=equipment!=null?equipment.Hull.Floor*scale-.3f:-2.5f*scale;
        float top=equipment!=null?equipment.Hull.Definition.RimY*scale:2.25f*scale;
        offset=Vector3.up*((bottom+top)*.5f);
        half=new Vector3((BoatDimensions.HalfWidth+.2f)*scale,(top-bottom)*.5f,(BoatDimensions.HalfLength+.25f)*scale);
    }
    public float ShoreClearance(Vector3 point,Vector3 direction,Transform ship,float range)
    {
        Vector3 offset,half;ShoreVolume(ship,out offset,out half);direction.y=0f;direction.Normalize();
        int count;Quaternion heading=Quaternion.LookRotation(direction);
        while(true)
        {
            count=Physics.BoxCastNonAlloc(point+offset,half,direction,casts,heading,range,PhysicsManager.GetEnvironmentLayerMask(),QueryTriggerInteraction.Ignore);
            if(count<casts.Length)break;casts=new RaycastHit[casts.Length*2];
        }
        float closest=range;
        for(int i=0;i<count;i++)if(Land(casts[i].collider))closest=Mathf.Min(closest,casts[i].distance);
        return closest;
    }
    public bool CanAdvance(Vector3 from,Quaternion oldHeading,Vector3 to,Quaternion heading,Transform ship)
    {
        if(CanSail(to,heading,ship))return true;
        // Never push through shore. If already overlapping, only allow a move
        // which reduces penetration into every affected terrain collider.
        Vector3 offset,half;ShoreVolume(ship,out offset,out half);
        if(shoreProbe==null)
        {
            GameObject probe=new GameObject("Naval shore clearance probe");probe.transform.SetParent(transform,false);
            shoreProbe=probe.AddComponent<BoxCollider>();shoreProbe.enabled=false;
        }
        shoreProbe.size=half*2f;
        Vector3 travel=to-from,local=Quaternion.Inverse(heading)*travel;
        Vector3 sweptHalf=half+new Vector3(Mathf.Abs(local.x),Mathf.Abs(local.y),Mathf.Abs(local.z))*.5f;
        int count;
        while(true)
        {
            count=Physics.OverlapBoxNonAlloc((from+to)*.5f+offset,sweptHalf,overlap,heading,PhysicsManager.GetEnvironmentLayerMask(),QueryTriggerInteraction.Ignore);
            if(count<overlap.Length)break;overlap=new Collider[overlap.Length*2];
        }
        NavalShoreEscape policy=new NavalShoreEscape();
        for(int i=0;i<count;i++)if(Land(overlap[i]))
        {
            Collider land=overlap[i];Vector3 normal;float before,after;
            if(!Physics.ComputePenetration(shoreProbe,from+offset,oldHeading,land,land.transform.position,land.transform.rotation,out normal,out before))before=0f;
            if(!Physics.ComputePenetration(shoreProbe,to+offset,heading,land,land.transform.position,land.transform.rotation,out normal,out after))after=0f;
            policy.Observe(before,after);
        }
        return policy.Allowed;
    }
    private void OnDestroy()
    {
        if(Active==this)Active=null;
        foreach(Renderer r in preview)if(r!=null)r.enabled=true;
        if(camera!=null)camera.farClipPlane=previousFar;
        if(mesh!=null)Destroy(mesh);if(material!=null)Destroy(material);
    }
}
