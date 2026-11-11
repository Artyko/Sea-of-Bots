using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public static class NavalAirRaidMath
{
    public const float SupportDeadline=600f;
    public static float SupportDelay(float random){return 60f+180f*Mathf.Clamp01(random); }
    public static Vector3 Approach(int wave,bool support){return support?new Vector3(1f,0,1f).normalized:Quaternion.Euler(0,55f+wave*103f,0)*Vector3.forward;}
    public static Vector3 PairOffset(int index){int pair=index/2,wing=index%2;return new Vector3((pair%2==0?-1f:1f)*pair*90f+wing*28f,pair*18f+wing*4f,pair*280f+wing*85f);}
    public static Vector3 BombingOffset(int pair,int wing){return new Vector3(wing==0?-3f:3f,0f,(pair%3-1)*8f);}
    public const int Waves=3;
    public static int SupportWaveCount(int choice){return choice<=0?0:System.Math.Min(6,choice+1);}
    public static int AircraftInWave(int wave,int total){return wave<0||wave>=total?0:wave==0?5:6;}
    public const int BombsPerAircraft=4;
    public static int AircraftInWave(int wave){return wave==0?5:wave>0&&wave<Waves?6:0;}
    public static float FallTime(float height,float verticalSpeed)
    {
        return (verticalSpeed+Mathf.Sqrt(verticalSpeed*verticalSpeed+2f*9.81f*Mathf.Max(1f,height)))/9.81f;
    }
}

public partial class NavalTestWorld
{
    public bool AirRaidMission { get; private set; }
    internal NavalAirRaid AirRaid { get; private set; }

    private void StartAirRaid()
    {
        AirRaid=gameObject.AddComponent<NavalAirRaid>();
        AirRaid.Initialize(this,hull,ocean);
    }
}

[DefaultExecutionOrder(200)]
public sealed class NavalAirRaid : MonoBehaviour
{
    private sealed class PlanePart
    {
        public Transform Pose;
        public Renderer Renderer;
        public Bounds Bounds;
        public int Health,Kind;
    }
    private sealed class PlaneBatch
    {
        public Mesh Mesh;
        public MeshFilter Filter;
        public Material Material;
    }
    private List<PlanePart> buildingParts;
    private Transform buildingRoot;
    private sealed class Aircraft
    {
        public Transform Body,Propeller;
        public NavalHull Target;
        public Vector3 Course,Exit;
        public int Pair,Wing;public Vector3 AttackForward;
        public float Age,DiveAt,ReleaseAt;
        public List<PlanePart> Parts;
        public List<int>[] Links;public int Anchor;
        public readonly List<PlaneBatch> Batches=new List<PlaneBatch>();
        public float LeftDamage,RightDamage;
        public int EngineHealth=4;
        public int Bombs=NavalAirRaidMath.BombsPerAircraft,Health=13;
        public bool Leaving,Crashing;
        public float SmokeAt;
        public Vector3 CrashVelocity,FlightVelocity;
    }
    private struct Bomb { public Transform Body;public Vector3 Position,Velocity;public float Age; }
    private sealed class Trace { public float Speed;public Transform Body;public bool MachineGun;public TrailRenderer Trail;public float StartAt,Until,Length;public Vector3 Start,Direction; }
    private readonly List<Aircraft> aircraft=new List<Aircraft>(3);
    private readonly List<Bomb> bombs=new List<Bomb>(12);
    private readonly List<Trace> traces=new List<Trace>(32);
    private readonly List<NavalHull> ships=new List<NavalHull>(4);
    private readonly RaycastHit[] bombHits=new RaycastHit[64];
    private Collider[] aircraftContacts=new Collider[32];
    private int plannedWaves=NavalAirRaidMath.Waves;
    private NavalTestWorld world;
    private NavalHull flagship;
    private NavalWater ocean;
    private Material canvas,frame,metal,red,splash,marking,flash,smoke,sparkCore;
    private Transform nativeSpark,nativeBullet;
    private const float AARange=540f;
    private const float BulletSpeed=560f;
    private float pause=30f;
    private int wave,downed,dropped,impacts,shots,hits;
    private bool finished,won;
    private string summary;

    public bool Completed {get{return finished;}}
    public bool HasAircraft
    {get{if(finished)return false;foreach(var plane in aircraft)if(!plane.Crashing&&plane.Health>0)return true;return false;}}
    public void Initialize(NavalTestWorld owner,NavalHull lead,NavalWater water,int waveCount=3)
    {
        world=owner;flagship=lead;ocean=water;
        plannedWaves=Mathf.Clamp(waveCount,2,6);
        foreach(var ship in owner.ConvoyHulls())if(!owner.ConvoyAirSupport||!ship.Definition.Destroyer)ships.Add(ship);
        if(owner.ConvoyAirSupport)pause=0f;
        canvas=Paint(new Color(.57f,.51f,.32f));frame=Paint(new Color(.26f,.14f,.08f));
        metal=Paint(new Color(.16f,.19f,.18f));
        marking=Paint(new Color(.75f,.18f,.10f));
        flash=new Material(Shader.Find("Unlit/Color")){color=new Color(1f,.57f,.11f)};
        smoke=Paint(new Color(.22f,.23f,.25f));
        red=new Material(Shader.Find("Unlit/Color"));red.color=new Color(2.8f,.12f,.075f);
        sparkCore=new Material(Shader.Find("Unlit/Color")){color=new Color(3.8f,1.1f,.70f)};
        splash=new Material(Shader.Find("Unlit/Color"));splash.color=new Color(.72f,.9f,1f);
    }
    private static Material Paint(Color color){return new Material(Shader.Find("Standard")){color=color};}
    private Transform Part(Transform parent,string name,Vector3 position,Vector3 size,Material material)
    {
        var part=GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name=name;part.transform.SetParent(parent,false);part.transform.localPosition=position;part.transform.localScale=size;
        var shape=part.GetComponent<Collider>();shape.enabled=false;Destroy(shape);
        var renderer=part.GetComponent<Renderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;
        if(buildingParts!=null&&parent==buildingRoot)
        {
            int kind=name=="Engine cowling"?3:name=="Pilot"?4:name.Contains("wing")||name.Contains("Wing")?
                (position.x<0?1:2):0;
            Bounds bounds=new Bounds(buildingRoot.InverseTransformPoint(part.transform.position),size);
            buildingParts.Add(new PlanePart{Pose=part.transform,Renderer=renderer,Bounds=bounds,Kind=kind,Health=kind==3?4:kind==4?1:kind==0?2:1});
        }
        return part.transform;
    }
    private void SpawnWave()
    {
        int count=NavalAirRaidMath.AircraftInWave(wave++,plannedWaves);
        Vector3 from=NavalAirRaidMath.Approach(wave,world.ConvoyAirSupport);
        Vector3 side=Vector3.Cross(Vector3.up,from);
        Vector3 anchor=flagship.Root.position;
        var pairTargets=new NavalHull[(count+1)/2];
        for(int i=0;i<count;i++)
        {
            if(i%2==0)pairTargets[i/2]=PickTarget(i/2);
            Vector3 offset=NavalAirRaidMath.PairOffset(i);
            var body=new GameObject("Biplane "+wave+"-"+(i+1)).transform;
            body.SetParent(transform,false);body.position=anchor+from*(3000f+offset.z)+side*offset.x+Vector3.up*(180f+offset.y);
            body.rotation=Quaternion.LookRotation(-from);
            buildingRoot=body;buildingParts=new List<PlanePart>();
            Transform propeller=BuildBiplane(body);
            var plane=new Aircraft{Body=body,Propeller=propeller,Parts=buildingParts,Pair=i/2,Wing=i%2,AttackForward=-from,Course=-from,Target=pairTargets[i/2],DiveAt=6f+i*1.15f};
            buildingParts=null;buildingRoot=null;
            ConnectAircraft(plane);
            BatchAircraft(plane);
            aircraft.Add(plane);
        }
        Debug.Log("[NavalPrototype] Air raid wave "+wave+" aircraft="+count+" formation=paired echelon pairs="+pairTargets.Length);
    }
    private Transform BuildBiplane(Transform body)
    {
        Part(body,"Forward fuselage",new Vector3(0,0,1.15f),new Vector3(1.65f,1.55f,4.3f),frame);
        Part(body,"Tapered tail boom",new Vector3(0,.10f,-2.30f),new Vector3(.85f,.85f,3.6f),frame);
        Part(body,"Pilot",new Vector3(0,1.07f,-.65f),new Vector3(.48f,.62f,.48f),metal);
        Part(body,"Cockpit rim",new Vector3(0,.79f,-.62f),new Vector3(1.08f,.23f,1.16f),metal);
        Part(body,"Windscreen",new Vector3(0,.97f,-.11f),new Vector3(.76f,.34f,.12f),metal);
        Part(body,"Tailplane",new Vector3(0,.24f,-3.79f),new Vector3(4.35f,.2f,1.35f),canvas);
        Part(body,"Tail fin",new Vector3(0,.86f,-3.82f),new Vector3(.24f,1.45f,1.1f),marking);
        Part(body,"Engine cowling",new Vector3(0,.01f,3.65f),new Vector3(1.55f,1.4f,.95f),metal);
        var propeller=new GameObject("Propeller hub").transform;
        propeller.SetParent(body,false);propeller.localPosition=new Vector3(0,0,4.23f);
        Part(propeller,"Wooden propeller",Vector3.zero,new Vector3(.23f,3.15f,.16f),canvas);
        Part(propeller,"Metal hub",Vector3.zero,new Vector3(.52f,.52f,.35f),metal);
        for(int layer=0;layer<2;layer++)
        {
            float y=layer==0?-.78f:1.95f,z=layer==0?.40f:1.02f;
            float span=layer==0?11.4f:13.3f;
            for(int tile=0;tile<14;tile++)for(int chord=0;chord<2;chord++)
                Part(body,"Fabric wing panel",new Vector3(-span*.5f+(tile+.5f)*span/14f,y,z+(chord==0?-.575f:.575f)),
                    new Vector3(span/14f,.30f,1.15f),canvas);
            for(int side=-1;side<=1;side+=2)
            {
                Part(body,"Stepped wingtip",new Vector3(side*(span*.5f+.32f),y,z+.14f),new Vector3(.64f,.24f,1.58f),canvas);
                Part(body,"Wing underside marking",new Vector3(side*span*.34f,y-.16f,z),new Vector3(1.13f,.045f,1.48f),marking);
            }
        }
        for(int side=-1;side<=1;side+=2)
        {
            for(int pair=0;pair<2;pair++)
            {
                Transform strut=Part(body,"Interplane strut",new Vector3(side*4.15f,.58f,.27f+pair*1.23f),new Vector3(.17f,2.86f,.17f),frame);
                strut.localRotation=Quaternion.Euler(12f,0,side*5f);
            }
            Part(body,"Landing gear leg",new Vector3(side*.87f,-1.08f,1.52f),new Vector3(.17f,1.11f,.19f),metal);
            Part(body,"Landing wheel",new Vector3(side*1.05f,-1.61f,1.52f),new Vector3(.37f,.87f,.88f),metal);
        }
        return propeller;
    }
    private void BatchAircraft(Aircraft plane)
    {
        foreach(var material in new[]{canvas,frame,metal,marking})
        {
            var parts=new List<CombineInstance>();
            foreach(var part in plane.Parts)
            {
                if(part.Health<=0||part.Renderer.sharedMaterial!=material)continue;
                parts.Add(new CombineInstance{mesh=part.Pose.GetComponent<MeshFilter>().sharedMesh,
                    transform=plane.Body.worldToLocalMatrix*part.Pose.localToWorldMatrix});
                part.Renderer.enabled=false;
            }
            PlaneBatch batch=plane.Batches.Find(entry=>entry.Material==material);
            if(batch==null)
            {
                var obj=new GameObject("Biplane panel batch");obj.transform.SetParent(plane.Body,false);
                batch=new PlaneBatch{Material=material,Filter=obj.AddComponent<MeshFilter>()};
                obj.AddComponent<MeshRenderer>().sharedMaterial=material;plane.Batches.Add(batch);
            }
            if(batch.Mesh!=null)Destroy(batch.Mesh);
            batch.Mesh=new Mesh();batch.Mesh.CombineMeshes(parts.ToArray(),true,true);batch.Filter.sharedMesh=batch.Mesh;
        }
    }
    private void DestroyAircraft(Aircraft plane)
    {
        foreach(var batch in plane.Batches)if(batch.Mesh!=null)Destroy(batch.Mesh);
        Destroy(plane.Body.gameObject);
    }
    private PlanePart AircraftHit(Aircraft plane,Vector3 origin,Vector3 direction,float max,out float distance)
    {
        Vector3 prediction=plane.Course*(Vector3.Distance(origin,plane.Body.position)/BulletSpeed)*(plane.Leaving?70f:62f);
        Vector3 start=plane.Body.InverseTransformPoint(origin-prediction);
        Vector3 rayDirection=plane.Body.InverseTransformDirection(direction);
        var ray=new Ray(start,rayDirection);
        PlanePart selected=null;distance=max;
        foreach(var part in plane.Parts)
        {
            if(part.Health<=0)continue;
            float contact;
            if(part.Bounds.IntersectRay(ray,out contact)&&contact>0f&&contact<distance)
            {selected=part;distance=contact;}
        }
        return selected;
    }
    private void DamageAircraft(Aircraft plane,PlanePart part)
    {
        part.Health--;
        if(part.Kind==3){plane.EngineHealth--;plane.Health=Mathf.Min(plane.Health,8);}
        if(part.Kind==4||plane.EngineHealth<=0)plane.Health=0;
        if(part.Health>0)return;
        Debug.Log("[NavalPrototype] Aircraft section destroyed: aircraft="+plane.Body.name+
            " part="+part.Pose.name+" kind="+part.Kind+" engine="+plane.EngineHealth);
        part.Renderer.enabled=false;
        if(part.Kind==1)plane.LeftDamage++;
        if(part.Kind==2)plane.RightDamage++;
        DropDisconnected(plane,part);
        if(plane.LeftDamage+plane.RightDamage>=22f)plane.Health=0;
        BatchAircraft(plane);
    }
    private void ConnectAircraft(Aircraft plane)
    {
        int n=plane.Parts.Count;plane.Links=new List<int>[n];
        for(int i=0;i<n;i++){
            plane.Links[i]=new List<int>();
            if(plane.Parts[i].Pose.name=="Forward fuselage")plane.Anchor=i;
            // Include rotated struts in both hit bounds and structural contacts.
            var pose=plane.Parts[i].Pose;Bounds b=new Bounds(plane.Body.InverseTransformPoint(pose.position),Vector3.zero);
            for(int c=0;c<8;c++)b.Encapsulate(plane.Body.InverseTransformPoint(pose.TransformPoint(new Vector3((c&1)==0?-.5f:.5f,(c&2)==0?-.5f:.5f,(c&4)==0?-.5f:.5f))));
            plane.Parts[i].Bounds=b;
        }
        for(int i=0;i<n;i++)for(int j=0;j<i;j++){
            Bounds b=plane.Parts[i].Bounds;b.Expand(.09f);
            if(!b.Intersects(plane.Parts[j].Bounds))continue;
            plane.Links[i].Add(j);plane.Links[j].Add(i);
        }
    }
    private void DropDisconnected(Aircraft plane,PlanePart destroyed)
    {
        var seen=new bool[plane.Parts.Count];var pending=new Queue<int>();
        if(plane.Parts[plane.Anchor].Health>0){seen[plane.Anchor]=true;pending.Enqueue(plane.Anchor);}
        while(pending.Count>0){int i=pending.Dequeue();foreach(int j in plane.Links[i])
            if(!seen[j]&&plane.Parts[j].Health>0){seen[j]=true;pending.Enqueue(j);}}
        var loose=new List<PlanePart>();loose.Add(destroyed);
        for(int i=0;i<seen.Length;i++)if(!seen[i]&&plane.Parts[i].Health>0){
            var part=plane.Parts[i];part.Health=0;loose.Add(part);
            if(part.Kind==1)plane.LeftDamage++;if(part.Kind==2)plane.RightDamage++;
            if(part.Kind==3||part.Kind==4)plane.Health=0;
        }
        // A single combined debris object per damage event avoids per-panel physics bodies.
        var root=new GameObject("Detached aircraft structure").transform;
        root.SetParent(transform,false);root.SetPositionAndRotation(plane.Body.position,plane.Body.rotation);
        var meshes=new List<Mesh>();
        foreach(var material in new[]{canvas,frame,metal,marking}){
            var combine=new List<CombineInstance>();
            foreach(var part in loose)if(part.Renderer.sharedMaterial==material)
                combine.Add(new CombineInstance{mesh=part.Pose.GetComponent<MeshFilter>().sharedMesh,transform=plane.Body.worldToLocalMatrix*part.Pose.localToWorldMatrix});
            if(combine.Count==0)continue;
            var mesh=new Mesh();mesh.CombineMeshes(combine.ToArray(),true,true);meshes.Add(mesh);
            var obj=new GameObject("Falling aircraft panels");obj.transform.SetParent(root,false);
            obj.AddComponent<MeshFilter>().sharedMesh=mesh;obj.AddComponent<MeshRenderer>().sharedMaterial=material;
        }
        var debris=root.gameObject.AddComponent<NavalAircraftDebris>();
        debris.Initialize(plane.Crashing?plane.CrashVelocity:plane.Course*(plane.Leaving?70f:62f),meshes,ocean);
        if(plane.Parts[plane.Anchor].Health<=0)plane.Health=0;
    }
    private NavalHull PickTarget(int index)
    {
        if(index%3!=1&&!flagship.Sunk)return flagship;
        for(int i=0;i<ships.Count;i++)
        {
            var ship=ships[(index+wave+i)%ships.Count];
            if(!ship.Sunk&&ship!=flagship)return ship;
        }
        return flagship.Sunk?null:flagship;
    }
    private void BeginExit(Aircraft plane)
    {
        plane.Leaving=true;
        Vector3 flat=Vector3.ProjectOnPlane(plane.Course,Vector3.up).normalized;
        plane.Exit=plane.Body.position+flat*1150f+Vector3.up*100f;
    }
    private void Fly(Aircraft plane,float dt,int index)
    {
        plane.Age+=dt;
        if(!plane.Leaving&&(plane.Age>85f||plane.Bombs==0))BeginExit(plane);
        if(!plane.Leaving&&(plane.Target==null||plane.Target.Sunk)){
            var target=PickTarget(plane.Pair);foreach(var mate in aircraft)if(mate.Pair==plane.Pair&&!mate.Leaving)mate.Target=target;
        }
        if(!plane.Leaving&&plane.Target==null)BeginExit(plane);
        Vector3 pos=plane.Body.position,goal;
        if(plane.Leaving)goal=plane.Exit;
        else
        {
            Transform target=plane.Target.Root;
            Vector3 targetPos=target.TransformPoint(Vector3.up*plane.Target.Definition.WeatherDeck);
            Vector3 attackForward=plane.AttackForward;
            Vector3 attackSide=Vector3.Cross(Vector3.up,attackForward);
            Vector3 aimOffset=NavalAirRaidMath.BombingOffset(plane.Pair,plane.Wing);
            Vector3 lead=targetPos+world.FleetVelocity(target)*2.4f+target.right*aimOffset.x+target.forward*aimOffset.z;
            float distance=Vector3.ProjectOnPlane(lead-pos,Vector3.up).magnitude;
            bool dive=plane.Age>=plane.DiveAt&&distance<340f;
            goal=lead+Vector3.up*(dive?10f:165f+plane.Pair*18f+plane.Wing*4f);
            if(!dive&&distance>500f){
                Aircraft leader=null;foreach(var mate in aircraft)if(mate.Pair==plane.Pair&&mate.Wing==0&&!mate.Crashing&&!mate.Leaving){leader=mate;break;}
                if(plane.Wing==1&&leader!=null)goal=leader.Body.position-attackForward*85f+attackSide*28f+Vector3.up*4f;
                else goal+=attackSide*((plane.Pair%2==0?-1:1)*plane.Pair*60f)*Mathf.Clamp01((distance-500f)/700f);
            }
            if(dive&&pos.y-targetPos.y<=12f){BeginExit(plane);goal=plane.Exit;}
            if(!plane.Leaving&&dive&&distance<125f&&pos.y-targetPos.y<95f&&pos.y-targetPos.y>12f&&Time.time>=plane.ReleaseAt)
            {
                DropBomb(plane,lead);plane.ReleaseAt=Time.time+.24f;
            }
        }
        Vector3 avoid=Vector3.zero;
        foreach(var other in aircraft){if(other==plane||other.Crashing)continue;Vector3 separation=pos-other.Body.position;float d=separation.sqrMagnitude;
            if(d>1f&&d<30f*30f)avoid+=separation.normalized*(1f-Mathf.Sqrt(d)/30f)*45f;
        }
        Vector3 direction=(goal+avoid-pos).normalized;
        if(direction.sqrMagnitude>.1f)plane.Course=Vector3.RotateTowards(plane.Course,direction,1.5f*dt,0f);
        if(plane.LeftDamage!=plane.RightDamage)
            plane.Course=Quaternion.AngleAxis(Mathf.Clamp(plane.LeftDamage-plane.RightDamage,-10f,10f)*dt*1.8f,Vector3.up)*plane.Course;
        plane.FlightVelocity=NavalAircraftCrashMath.FlightVelocity(plane.Course,plane.AttackForward,plane.Leaving?70f:62f);
        plane.Course=plane.FlightVelocity.normalized;plane.Body.position=pos+plane.FlightVelocity*dt;
        plane.Body.rotation=Quaternion.LookRotation(plane.Course,Vector3.up);
        plane.Propeller.Rotate(0f,0f,1250f*dt,Space.Self);
    }
    private void DropBomb(Aircraft plane,Vector3 target)
    {
        Vector3 position=plane.Body.position-Vector3.up*.9f;
        float descent=Mathf.Min(-3f,plane.Course.y*35f);
        float height=Mathf.Max(1f,position.y-target.y);
        float time=NavalAirRaidMath.FallTime(height,descent);
        Vector2 spread=Random.insideUnitCircle*36f;
        Vector3 scatter=new Vector3(spread.x,0f,spread.y);
        Vector3 velocity=Vector3.ClampMagnitude(Vector3.ProjectOnPlane(target-position+scatter,Vector3.up)/Mathf.Max(.25f,time),75f);
        velocity.y=descent;
        var visual=Part(transform,"50 kg bomb",Vector3.zero,new Vector3(.26f,.26f,1.1f),metal);visual.position=position;
        bombs.Add(new Bomb{Body=visual,Position=position,Velocity=velocity});
        plane.Bombs--;dropped++;
    }
    private bool StepBomb(ref Bomb bomb,float dt)
    {
        bomb.Age+=dt;
        Vector3 step=bomb.Velocity*dt+Vector3.down*(.5f*9.81f*dt*dt);
        bomb.Velocity+=Vector3.down*(9.81f*dt);
        if(TryAirImpact(bomb.Position,step,false))return true;
        bomb.Position+=step;bomb.Body.position=bomb.Position;
        bomb.Body.rotation=Quaternion.LookRotation(bomb.Velocity);
        if(bomb.Position.y<=ocean.Height(bomb.Position.x,bomb.Position.z))
        {
            Vector3 waterline=bomb.Position;waterline.y=ocean.Height(waterline.x,waterline.z);
            BombEffect(waterline,true);BombFragments(waterline,Vector3.up);return true;
        }
        return bomb.Age>12f;
    }

    private bool AircraftImpact(Aircraft plane,Vector3 position,Vector3 step)
    {
        int mask=PhysicsManager.GetEnvironmentLayerMask();
        if(!Physics.CheckSphere(position,9f+step.magnitude,mask,QueryTriggerInteraction.Ignore))return false;
        foreach(var part in plane.Parts){
            if(part.Health<=0)continue;
            Vector3 centre=position+plane.Body.rotation*part.Bounds.center;
            // A rotating wing may already overlap the deck before its translation sweep.
            int contacts=NavalOverlapQueries.Box(centre,part.Bounds.extents,plane.Body.rotation,mask,ref aircraftContacts);
            for(int i=0;i<contacts;i++){
                var shape=aircraftContacts[i];if(shape==null)continue;
                Vector3 point=shape.ClosestPoint(centre);
                var cell=NavalVoxelHit.Resolve(shape,point);
                if(cell==null||cell.Owner==null||cell.Owner.SimulationRetired)continue;
                Vector3 normal=centre-point;if(normal.sqrMagnitude<.00001f)normal=Vector3.up;else normal.Normalize();
                AirImpactEffect(point,normal,cell,true);return true;
            }
            if(TryAirImpact(centre,step,true,part.Bounds.extents,plane.Body.rotation))return true;
        }
        return false;
    }
    private bool TryAirImpact(Vector3 position,Vector3 step,bool aircraftCrash)
    {return TryAirImpact(position,step,aircraftCrash,Vector3.zero,Quaternion.identity);}
    private bool TryAirImpact(Vector3 position,Vector3 step,bool aircraftCrash,Vector3 halfExtents,Quaternion orientation)
    {
        if(step.sqrMagnitude<.000001f)return false;
        int mask=PhysicsManager.GetEnvironmentLayerMask();
        int count=halfExtents.sqrMagnitude>0?Physics.BoxCastNonAlloc(position,halfExtents,step.normalized,bombHits,orientation,step.magnitude,mask,QueryTriggerInteraction.Ignore):aircraftCrash?Physics.SphereCastNonAlloc(position,.85f,step.normalized,bombHits,step.magnitude,mask,QueryTriggerInteraction.Ignore):
            Physics.RaycastNonAlloc(position,step.normalized,bombHits,step.magnitude,mask,QueryTriggerInteraction.Ignore);
        int first=-1;float closest=float.PositiveInfinity;NavalHullVoxel cell=null;
        for(int i=0;i<count;i++)
        {
            var hit=bombHits[i];if(hit.collider==null||hit.distance>=closest)continue;
            var voxel=NavalVoxelHit.Resolve(hit.collider,hit.point,hit.collider.ClosestPoint(position));
            if(voxel==null&&(hit.collider.GetComponent<NavalMergedCollider>()!=null||hit.collider.GetComponent<NavalHullVoxel>()!=null))continue;
            first=i;closest=hit.distance;cell=voxel;
        }
        if(first>=0)
        {
            Vector3 point=bombHits[first].point;
            AirImpactEffect(point,bombHits[first].normal,cell,aircraftCrash);
            return true;
        }
        return false;
    }

    private void AirImpactEffect(Vector3 point,Vector3 normal,NavalHullVoxel cell,bool aircraftCrash)
    {
        if(cell!=null&&cell.Owner!=null&&!cell.Owner.SimulationRetired){
            cell.Owner.Damage(cell.Owner.Root.InverseTransformPoint(point),3.1f,true,cell,5.2f,4.2f,true);
            if(aircraftCrash)Debug.Log("[NavalPrototype] Aircraft crash impact: ship="+cell.Owner.Root.name+" equivalent=50kg bomb");
            else impacts++;
        }
        BombEffect(point,false);BombFragments(point,normal);
    }

    private void BombFragments(Vector3 point,Vector3 surfaceNormal)
    {
        int mask=PhysicsManager.GetEnvironmentLayerMask();
        Vector3 origin=point+surfaceNormal*.12f;
        for(int i=0;i<24;i++)
        {
            Vector3 direction=Random.onUnitSphere;
            if(Vector3.Dot(direction,surfaceNormal)<0f)direction=-direction;
            float distance=Random.Range(12f,32f);
            RaycastHit hit;
            if(Physics.Raycast(origin,direction,out hit,distance,mask,QueryTriggerInteraction.Ignore))
            {
                distance=hit.distance;
                var cell=NavalVoxelHit.Resolve(hit.collider,hit.point);
                if(cell!=null&&cell.Owner!=null&&!cell.Owner.SimulationRetired&&!cell.Missing)
                    cell.Owner.Damage(cell.Owner.Root.InverseTransformPoint(hit.point),0f,true,cell,.55f,.22f);
            }
            // Crew body parts use their own layers; the environment hit limits penetration.
            RaycastHit[] crewHits=Physics.RaycastAll(origin,direction,distance,~0,QueryTriggerInteraction.Collide);
            System.Array.Sort(crewHits,(a,b)=>a.distance.CompareTo(b.distance));
            foreach(var contact in crewHits){
                var body=contact.collider.GetComponentInParent<BaseBodyPart>();
                var actor=body!=null?body.GetOwner():null;
                if(actor==null||!actor.IsAlive()||actor.IsImmuneToDamage()||!body.CanBeDamaged)continue;
                body.TryStabVolume(contact.point-direction*.35f,contact.point+direction*.25f,GetInstanceID(),12,null,FireType.None,false,DamageSourceType.None);
                distance=contact.distance;break;
            }
            EmitTrace(origin,origin+direction*distance,false,160f);
        }
    }
    private void BombEffect(Vector3 point,bool water)
    {
        if(!water)NavalShotPuff.Create(transform,point+Vector3.up*.5f,Vector3.up*3f,flash,.23f,4.6f);
        int count=water?16:12;
        for(int i=0;i<count;i++)
        {
            Vector3 scatter=Random.insideUnitSphere;
            if(water)
            {
                scatter.y=0f;
                NavalShotPuff.Create(transform,point+scatter*1.65f,scatter*5f+Vector3.up*Random.Range(6f,16f),
                    splash,Random.Range(.55f,1.1f),Random.Range(.55f,1.55f));
            }
            else NavalShotPuff.Create(transform,point+scatter*1.3f,scatter*3f+Vector3.up*Random.Range(7f,10f),
                i<5?flash:smoke,i<5?.35f:6f,i<5?1.25f:1.5f);
        }
    }
    internal bool AimAtAircraft(Transform ship,Vector3 muzzle,float baseYaw,out Vector3 aim,out float range)
    {
        aim=Vector3.zero;float best=AARange*AARange;
        foreach(var plane in aircraft)
        {
            if(plane.Health<=0||plane.Crashing)continue;
            float distance=(plane.Body.position-muzzle).sqrMagnitude;
            if(distance>=best)continue;
            Vector3 predicted=plane.Body.position+plane.Course*(Mathf.Sqrt(distance)/560f)*(plane.Leaving?70f:62f);
            Vector3 local=ship.InverseTransformDirection(predicted-muzzle);
            float yaw=Mathf.Atan2(local.x,local.z)*Mathf.Rad2Deg;
            float pitch=Mathf.Atan2(local.y,new Vector2(local.x,local.z).magnitude)*Mathf.Rad2Deg;
            if(Mathf.Abs(Mathf.DeltaAngle(baseYaw,yaw))>115f||pitch< -8f||pitch>85f)continue;
            best=distance;aim=predicted;
        }
        range=Mathf.Sqrt(best);return best<AARange*AARange;
    }
    internal void FireAA(Vector3 muzzle,Vector3 direction)
    {
        shots++;
        float range=AARange;
        // Start outside the gun assembly. The ship still blocks fire through its deck or bridge.
        RaycastHit hit;
        if(Physics.Raycast(muzzle,direction,out hit,range,PhysicsManager.GetEnvironmentLayerMask(),QueryTriggerInteraction.Ignore))
            range=hit.distance;
        Aircraft target=null;PlanePart targetPart=null;float nearest=range;
        foreach(var plane in aircraft)
        {
            if(plane.Health<=0||plane.Crashing)continue;
            float distance;
            PlanePart part=AircraftHit(plane,muzzle,direction,nearest,out distance);
            if(part==null)continue;
            target=plane;targetPart=part;nearest=distance;
        }
        if(target!=null){hits++;DamageAircraft(target,targetPart);}
        EmitTrace(muzzle,muzzle+direction*nearest,true);
    }
    private Trace CreateTrace(bool machineGun)
    {
        if(nativeSpark==null)
        {
            var manager=Singleton<ProjectileManager>.Instance;
            var pool=manager!=null?manager.MortarExplosionShrapnelPool:null;
            if(pool!=null)nativeSpark=pool.Prefab!=null?pool.Prefab:
                pool.PooledObjectPrefab!=null?pool.PooledObjectPrefab.transform:null;
        }
        if(machineGun&&nativeBullet==null)
        {
            var manager=Singleton<ProjectileManager>.Instance;
            var pool=manager!=null?manager.SpidertronBulletPool:null;
            if(pool!=null)nativeBullet=pool.Prefab!=null?pool.Prefab:
                pool.PooledObjectPrefab!=null?pool.PooledObjectPrefab.transform:null;
        }
        Transform sourceVisual=machineGun&&nativeBullet!=null?nativeBullet:nativeSpark;
        var root=new GameObject("Spidertron projectile visual").transform;
        root.SetParent(transform,false);
        TrailRenderer trail=null;
        if(sourceVisual!=null)
        {
            foreach(var filter in sourceVisual.GetComponentsInChildren<MeshFilter>(true))
            {
                var renderer=filter.GetComponent<MeshRenderer>();
                if(renderer==null||filter.sharedMesh==null)continue;
                var part=new GameObject("Native shrapnel mesh").transform;
                part.SetParent(root,false);
                part.localPosition=sourceVisual.InverseTransformPoint(filter.transform.position);
                part.localRotation=Quaternion.Inverse(sourceVisual.rotation)*filter.transform.rotation;
                part.localScale=filter.transform.lossyScale;
                part.gameObject.AddComponent<MeshFilter>().sharedMesh=filter.sharedMesh;
                var copy=part.gameObject.AddComponent<MeshRenderer>();
                copy.sharedMaterials=renderer.sharedMaterials;copy.shadowCastingMode=ShadowCastingMode.Off;
            }
            var source=sourceVisual.GetComponentInChildren<TrailRenderer>(true);
            if(source!=null)
            {
                trail=root.gameObject.AddComponent<TrailRenderer>();
                trail.sharedMaterials=source.sharedMaterials;
                trail.colorGradient=source.colorGradient;trail.widthCurve=source.widthCurve;
                trail.widthMultiplier=source.widthMultiplier;
                trail.time=Mathf.Min(source.time,.012f);trail.minVertexDistance=.12f;
                trail.shadowCastingMode=ShadowCastingMode.Off;
            }
        }
        Part(root,"Neon red spark",Vector3.zero,new Vector3(.23f,.23f,.62f),red);
        Part(root,"Hot spark core",new Vector3(0f,0f,.28f),new Vector3(.12f,.12f,.25f),sparkCore);
        root.gameObject.SetActive(false);
        return new Trace{Body=root,Trail=trail,MachineGun=machineGun};
    }
    private void EmitTrace(Vector3 start,Vector3 end,bool machineGun=false,float speed=BulletSpeed)
    {
        Trace trace=null;
        foreach(var entry in traces)if(entry.Until<=Time.time&&entry.MachineGun==machineGun){trace=entry;break;}
        if(trace==null)
        {
            if(traces.Count>=96)return;
            trace=CreateTrace(machineGun);traces.Add(trace);
        }
        Vector3 delta=end-start;
        if(delta.sqrMagnitude<.0001f)return;
        trace.Start=start;trace.Direction=delta.normalized;trace.Length=delta.magnitude;
        trace.Speed=speed;trace.StartAt=Time.time;trace.Until=Time.time+trace.Length/speed+.025f;
        trace.Body.SetPositionAndRotation(start,Quaternion.LookRotation(trace.Direction));
        trace.Body.gameObject.SetActive(true);
        if(trace.Trail!=null)trace.Trail.Clear();
    }

    private bool StepCrash(Aircraft plane,float dt)
    {
        Vector3 position=plane.Body.position;
        plane.CrashVelocity+=Vector3.down*(9.81f*dt);
        Vector3 step=plane.CrashVelocity*dt;
        if(AircraftImpact(plane,position,step))return true;
        position+=step;
        plane.Body.position=position;
        plane.Body.rotation=Quaternion.LookRotation(plane.CrashVelocity)*Quaternion.Euler(0f,0f,plane.Age*105f);
        if(position.y<=ocean.Height(position.x,position.z)){BombEffect(position,true);return true;}
        return false;
    }
    private void EmitAircraftSmoke(Aircraft plane)
    {
        if(plane.Health>8||Time.time<plane.SmokeAt)return;
        plane.SmokeAt=Time.time+(plane.Crashing?.08f:.16f);
        NavalShotPuff.Create(transform,plane.Body.position-plane.Body.forward*2.4f,
            Vector3.up*1.1f-plane.Body.forward*2f,smoke,1.7f,plane.Crashing?1.9f:1.15f);
    }

    private void LateUpdate()
    {
        if(world==null||!world.SimulationActive)return;
        if(world.ConvoyAirSupport&&world.ConvoyBattleFinished){if(!finished)Finish(!flagship.Sunk);return;}
        using(NavalPerformance.Measure(NavalPerfArea.AirRaid))Tick();
    }
    private void Tick()
    {
        foreach(var trace in traces)
        {
            if(!trace.Body.gameObject.activeSelf)continue;
            if(Time.time>=trace.Until){trace.Body.gameObject.SetActive(false);continue;}
            float travel=(Time.time-trace.StartAt)*trace.Speed;
            trace.Body.position=trace.Start+trace.Direction*Mathf.Min(travel,trace.Length);
        }
        if(finished)return;
        float dt=NavalAircraftDebrisMath.Elapsed(Time.deltaTime);
        for(int i=bombs.Count-1;i>=0;i--)
        {
            Bomb bomb=bombs[i];
            if(StepBomb(ref bomb,dt)){Destroy(bomb.Body.gameObject);bombs.RemoveAt(i);}else bombs[i]=bomb;
        }
        for(int i=aircraft.Count-1;i>=0;i--)
        {
            var plane=aircraft[i];
            if(plane.Health<=0&&!plane.Crashing)
            {
                plane.Crashing=true;downed++;
                plane.CrashVelocity=plane.FlightVelocity.sqrMagnitude>1f?plane.FlightVelocity:NavalAircraftCrashMath.FlightVelocity(plane.Course,plane.AttackForward,plane.Leaving?70f:62f);
                Debug.Log("[NavalPrototype] Biplane shot down: "+plane.Body.name+" inherited_velocity="+plane.CrashVelocity);
            }
            EmitAircraftSmoke(plane);
            if(plane.Crashing)
            {
                float remaining=Mathf.Max(0f,Time.deltaTime);bool impacted=false;
                while(remaining>0f){float stepTime=Mathf.Min(.05f,remaining);remaining-=stepTime;
                    plane.Age+=stepTime;if(StepCrash(plane,stepTime)){impacted=true;break;}}
                if(!impacted)continue;
            }
            else
            {
                Vector3 before=plane.Body.position;Fly(plane,dt,i);
                if(AircraftImpact(plane,before,plane.Body.position-before)){DestroyAircraft(plane);aircraft.RemoveAt(i);continue;}
                if(!plane.Leaving||((plane.Body.position-plane.Exit).sqrMagnitude>45f*45f&&plane.Age<120f))continue;
            }
            DestroyAircraft(plane);aircraft.RemoveAt(i);
        }
        if(flagship.Sunk){Finish(false);return;}
        if(aircraft.Count>0||bombs.Count>0)return;
        if(wave>=plannedWaves){Finish(true);return;}
        pause-=dt;
        if(pause<=0f){SpawnWave();pause=20f;}
    }
    private void Finish(bool success)
    {
        finished=true;won=success;
        foreach(var plane in aircraft)DestroyAircraft(plane);aircraft.Clear();
        foreach(var bomb in bombs)Destroy(bomb.Body.gameObject);bombs.Clear();
        int lost=0,dead=0;world.AirRaidLosses(out lost,out dead);
        summary="\u0421\u0431\u0438\u0442\u043e "+downed+" \u00b7 \u0411\u043e\u043c\u0431\u044b "+impacts+"/"+dropped+" \u00b7 \u0422\u043e\u0447\u043d\u043e\u0441\u0442\u044c \u041f\u0412\u041e "+(shots>0?(100f*hits/shots).ToString("F1")+"%":"\u2014")+
            "\n\u041f\u043e\u0442\u0435\u0440\u044f\u043d\u043e \u043a\u043e\u0440\u0430\u0431\u043b\u0435\u0439 "+lost+" \u00b7 \u041f\u043e\u0433\u0438\u0431\u043b\u043e \u043c\u0430\u0442\u0440\u043e\u0441\u043e\u0432 "+dead;
        Debug.Log("[NavalPrototype] Air raid result: "+won+" | "+summary.Replace('\n',' '));
    }
    public void Draw()
    {
        string direction="";
        if(aircraft.Count>0)
        {
            Vector3 delta=aircraft[0].Body.position-flagship.Root.position;
            float bearing=Mathf.Repeat(Mathf.Atan2(delta.x,delta.z)*Mathf.Rad2Deg,360f);
            direction=" · \u0410\u0437\u0438\u043c\u0443\u0442 "+Mathf.RoundToInt(bearing)+"°";
        }
        string status=finished?(won?"\u041c\u0438\u0441\u0441\u0438\u044f \u0432\u044b\u043f\u043e\u043b\u043d\u0435\u043d\u0430":"\u041c\u0438\u0441\u0441\u0438\u044f \u043f\u0440\u043e\u0432\u0430\u043b\u0435\u043d\u0430")+"\n"+summary:
            "\u0412\u043e\u043b\u043d\u0430 "+wave+"/3 \u00b7 \u0421\u0430\u043c\u043e\u043b\u0451\u0442\u044b \u0432 \u0432\u043e\u0437\u0434\u0443\u0445\u0435 "+aircraft.Count+direction+
            (aircraft.Count==0&&bombs.Count==0?" \u00b7 \u0421\u043b\u0435\u0434\u0443\u044e\u0449\u0430\u044f \u0447\u0435\u0440\u0435\u0437 "+Mathf.CeilToInt(pause)+" \u0441":"")+"\n\u0417\u0430\u0449\u0438\u0442\u0438\u0442\u0435 \u0434\u0440\u0435\u0434\u043d\u043e\u0443\u0442 \u00b7 F5 \u2014 \u0437\u0430\u043d\u043e\u0432\u043e";
        float width=Mathf.Min(620f,Screen.width-24f);
        GUI.Box(new Rect((Screen.width-width)*.5f,62f,width,finished?79f:63f),"\u0410\u0432\u0438\u0430\u043d\u0430\u043b\u0451\u0442 \u043d\u0430 \u043a\u043e\u043d\u0432\u043e\u0439\n"+status);
    }
    private void OnDestroy(){foreach(var plane in aircraft)foreach(var batch in plane.Batches)if(batch.Mesh!=null)Destroy(batch.Mesh);foreach(var material in new[]{canvas,frame,metal,red,splash,marking,flash,smoke,sparkCore})if(material!=null)Destroy(material);}
}
