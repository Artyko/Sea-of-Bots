using System.Collections.Generic;
using UnityEngine;

// All scenery is saved in the editor level. Only its water preview is replaced.
[DefaultExecutionOrder(200)]
public partial class NavalWorldMap : MonoBehaviour
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
    private readonly HashSet<Collider> knownShipShapes=new HashSet<Collider>();
    private readonly List<Collider> motionLand=new List<Collider>();
    private readonly List<float> motionDepth=new List<float>();
    private float horizonAt;
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
        foreach(ObjectPlacedInLevel obj in Object.FindObjectsByType<ObjectPlacedInLevel>(FindObjectsSortMode.None))RegisterIsland(obj);
        Debug.Log("[NavalPrototype] Island solid barriers: "+islandSolids.Count);
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
        if(ocean==null||focus==null||Time.time<horizonAt)return;horizonAt=Time.time+.1f;
        horizon.position=origin+new Vector3(Mathf.Floor((focus.position.x-origin.x)/40f)*40f,0f,Mathf.Floor((focus.position.z-origin.z)/40f)*40f);
        for(int i=0;i<border.Count;i++)for(int ring=0;ring<3;ring++)
        {Vector3 p=border[i]*(ring==0?1f:ring==1?4f:160f),world=horizon.position+p;p.y=ring==0?ocean.Height(world.x,world.z)-origin.y:0f;vertices[ring*border.Count+i]=p;}
        mesh.vertices=vertices;mesh.RecalculateNormals();mesh.RecalculateBounds();
    }
    private bool Land(Collider collider)
    {
        if(collider==null||knownShipShapes.Contains(collider)||collider.transform.IsChildOf(transform.parent))return false;
        if(knownLand.Contains(collider))return true;
        if(collider.GetComponent<NavalHullVoxel>()!=null||collider.GetComponent<NavalMergedCollider>()!=null||collider.GetComponent<NavalCargoProxy>()!=null){knownShipShapes.Add(collider);return false;}
        if(collider.GetComponentInParent<Character>()!=null)return false;
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
            count=Physics.OverlapBoxNonAlloc(point+offset,half,overlap,heading,~0,QueryTriggerInteraction.Ignore);
            if(count<overlap.Length)break;overlap=new Collider[overlap.Length*2];
        }
        for(int i=0;i<count;i++)if(Land(overlap[i])){cache.Clear=false;break;}
        if(cache.Clear){Vector3 o,h;ShoreVolume(ship,out o,out h);cache.Clear=IslandAdvance(point,heading,point,heading,o,h);}
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
    private struct ClearanceMemo {public Transform Ship;public Vector3 Point,Direction;public float Range,Result,Until;}
    private readonly ClearanceMemo[] clearances=new ClearanceMemo[32];private int clearanceCursor;
    // This short-lived cache is only for tactical look-ahead. CanAdvance never uses it.
    public float ShoreClearance(Vector3 point,Vector3 direction,Transform ship,float range)
    {
        direction.y=0;direction.Normalize();
        for(int i=0;i<clearances.Length;i++){
            var c=clearances[i];
            if(c.Ship==ship&&Time.time<c.Until&&c.Range==range&&(point-c.Point).sqrMagnitude<.0625f&&Vector3.Angle(direction,c.Direction)<1f)
                return Mathf.Max(0,c.Result-Vector3.Distance(point,c.Point)-range*Mathf.Sin(Vector3.Angle(direction,c.Direction)*Mathf.Deg2Rad));
        }
        float result=ShoreClearanceExact(point,direction,ship,range);
        clearances[clearanceCursor]=new ClearanceMemo{Ship=ship,Point=point,Direction=direction,Range=range,Result=result,Until=Time.time+.15f};clearanceCursor=(clearanceCursor+1)%clearances.Length;
        return result;
    }
    private float ShoreClearanceExact(Vector3 point,Vector3 direction,Transform ship,float range)
    {
        using(NavalPerformance.Measure(NavalPerfArea.ShoreNavigation))
        using(NavalPerformance.Measure(NavalPerfArea.Shore))
        {
        Vector3 offset,half;ShoreVolume(ship,out offset,out half);direction.y=0f;direction.Normalize();
        int count;Quaternion heading=Quaternion.LookRotation(direction);
        while(true)
        {
            count=Physics.BoxCastNonAlloc(point+offset,half,direction,casts,heading,range,~0,QueryTriggerInteraction.Ignore);
            if(count<casts.Length)break;casts=new RaycastHit[casts.Length*2];
        }
        float closest=range;
        for(int i=0;i<count;i++)if(Land(casts[i].collider))closest=Mathf.Min(closest,casts[i].distance);
        return IslandClearance(point,direction,heading,offset,half,closest);
            }
    }
    public bool CanAdvance(Vector3 from,Quaternion oldHeading,Vector3 to,Quaternion heading,Transform ship)
    {
        using(NavalPerformance.Measure(NavalPerfArea.ShoreMotion))
        using(NavalPerformance.Measure(NavalPerfArea.Shore))
        {
        // Safety queries must never reuse the navigation cache. Check the entire
        // motion, including rotation and draft, before committing a kinematic pose.
        Vector3 offset,half;ShoreVolume(ship,out offset,out half);
        if(!IslandAdvance(from,oldHeading,to,heading,offset,half))return false;
        if(shoreProbe==null)
        {
            GameObject probe=new GameObject("Naval shore clearance probe");probe.transform.SetParent(transform,false);
            shoreProbe=probe.AddComponent<BoxCollider>();shoreProbe.enabled=false;
        }
        shoreProbe.size=half*2f+Vector3.one*.12f;
        float angle=Quaternion.Angle(oldHeading,heading);
        Vector3 centerA=from+oldHeading*offset,centerB=to+heading*offset;
        Vector3 travel=centerB-centerA,local=Quaternion.Inverse(oldHeading)*travel;
        float radius=half.magnitude;
        float rotationMargin=2f*radius*Mathf.Sin(Mathf.Min(180f,angle)*Mathf.Deg2Rad*.5f);
        Vector3 sweptHalf=half+new Vector3(Mathf.Abs(local.x),Mathf.Abs(local.y),Mathf.Abs(local.z))*.5f+Vector3.one*(rotationMargin+.02f);
        int count;
        while(true)
        {
            count=Physics.OverlapBoxNonAlloc((centerA+centerB)*.5f,sweptHalf,overlap,oldHeading,~0,QueryTriggerInteraction.Ignore);
            if(count<overlap.Length)break;overlap=new Collider[overlap.Length*2];
        }
        motionLand.Clear();motionDepth.Clear();
        for(int i=0;i<count;i++)if(Land(overlap[i])){
            Collider land=overlap[i];Vector3 normal;float before;
            if(!Physics.ComputePenetration(shoreProbe,centerA,oldHeading,land,land.transform.position,land.transform.rotation,out normal,out before))before=0f;
            motionLand.Add(land);motionDepth.Add(before);
        }
        if(motionLand.Count==0)return true;
        int steps=NavalShoreSweepMath.Steps(travel.magnitude,angle,radius);
        for(int step=1;step<=steps;step++)
        {
            float t=(float)step/steps;
            Quaternion rotation=Quaternion.Slerp(oldHeading,heading,t);
            Vector3 center=Vector3.Lerp(from,to,t)+rotation*offset;
            NavalShoreEscape policy=new NavalShoreEscape();
            for(int i=0;i<motionLand.Count;i++)
            {
                Collider land=motionLand[i];Vector3 normal;float before=motionDepth[i],after;
                if(!Physics.ComputePenetration(shoreProbe,center,rotation,land,land.transform.position,land.transform.rotation,out normal,out after))after=0f;
                policy.Observe(before,after);
            }
            if(!policy.Allowed)return false;
        }
        return true;
            }
    }
    private float shoreLogAt;
    public bool ConstrainShore(Transform ship,ref Vector3 position,ref Quaternion rotation)
    {
        if(CanAdvance(ship.position,ship.rotation,position,rotation,ship))return false;
        if(Time.time>=shoreLogAt){shoreLogAt=Time.time+3f;Debug.Log("[NavalPrototype] Shore blocked: "+ship.name+" at "+ship.position+" next="+position+" island solids="+islandSolids.Count);}
        // A blocked rotation/vertical wave adjustment must not cancel a clear
        // horizontal retreat. Every fallback still sweeps the complete hull.
        Vector3 horizontal=new Vector3(position.x,ship.position.y,position.z);
        if((horizontal-ship.position).sqrMagnitude>.00000001f&&CanAdvance(ship.position,ship.rotation,horizontal,ship.rotation,ship))
        {position=horizontal;rotation=ship.rotation;return true;}
        if((position-ship.position).sqrMagnitude>.00000001f&&CanAdvance(ship.position,ship.rotation,position,ship.rotation,ship))
        {rotation=ship.rotation;return true;}
        position=ship.position;rotation=ship.rotation;return true;
    }
    private void OnDestroy()
    {
        if(Active==this)Active=null;
        foreach(Renderer r in preview)if(r!=null)r.enabled=true;
        if(camera!=null)camera.farClipPlane=previousFar;
        if(mesh!=null)Destroy(mesh);if(material!=null)Destroy(material);
    }
}
