using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public static class NavalAirRaidMath
{
    public const int Waves=3;
    public const int BombsPerAircraft=4;
    public static int AircraftInWave(int wave){return wave==0?2:wave>0&&wave<Waves?3:0;}
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
    private sealed class Aircraft
    {
        public Transform Body,Propeller;
        public NavalHull Target;
        public Vector3 Course,Exit;
        public float Age,DiveAt,ReleaseAt;
        public int Bombs=NavalAirRaidMath.BombsPerAircraft,Health=13;
        public bool Leaving;
    }
    private struct Bomb { public Transform Body;public Vector3 Position,Velocity;public float Age; }
    private sealed class Trace { public LineRenderer Line;public float StartAt,Until,Length;public Vector3 Start,Direction; }
    private readonly List<Aircraft> aircraft=new List<Aircraft>(3);
    private readonly List<Bomb> bombs=new List<Bomb>(12);
    private readonly List<Trace> traces=new List<Trace>(32);
    private readonly List<NavalHull> ships=new List<NavalHull>(4);
    private readonly RaycastHit[] bombHits=new RaycastHit[64];
    private NavalTestWorld world;
    private NavalHull flagship;
    private NavalWater ocean;
    private Material canvas,frame,metal,red,splash,marking,flash,smoke;
    private float pause=30f;
    private int wave,downed,dropped,impacts,shots,hits;
    private bool finished,won;
    private string summary;

    public bool NeedsAirDefense {get{return !finished;}}
    public bool HasAircraft {get{return !finished&&aircraft.Count>0;}}
    public void Initialize(NavalTestWorld owner,NavalHull lead,NavalWater water)
    {
        world=owner;flagship=lead;ocean=water;
        ships.AddRange(owner.ConvoyHulls());
        canvas=Paint(new Color(.57f,.51f,.32f));frame=Paint(new Color(.26f,.14f,.08f));
        metal=Paint(new Color(.16f,.19f,.18f));
        marking=Paint(new Color(.75f,.18f,.10f));
        flash=new Material(Shader.Find("Unlit/Color")){color=new Color(1f,.57f,.11f)};
        smoke=Paint(new Color(.22f,.23f,.25f));
        red=new Material(Shader.Find("Unlit/Color"));red.color=new Color(1f,.13f,.06f);
        splash=new Material(Shader.Find("Unlit/Color"));splash.color=new Color(.72f,.9f,1f);
    }
    private static Material Paint(Color color){return new Material(Shader.Find("Standard")){color=color};}
    private Transform Part(Transform parent,string name,Vector3 position,Vector3 size,Material material)
    {
        var part=GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name=name;part.transform.SetParent(parent,false);part.transform.localPosition=position;part.transform.localScale=size;
        var shape=part.GetComponent<Collider>();shape.enabled=false;Destroy(shape);
        var renderer=part.GetComponent<Renderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;
        return part.transform;
    }
    private void SpawnWave()
    {
        int count=NavalAirRaidMath.AircraftInWave(wave++);
        Vector3 from=Quaternion.Euler(0,55f+wave*103f,0)*Vector3.forward;
        Vector3 side=Vector3.Cross(Vector3.up,from);
        Vector3 anchor=flagship.Root.position;
        for(int i=0;i<count;i++)
        {
            var body=new GameObject("Biplane "+wave+"-"+(i+1)).transform;
            body.SetParent(transform,false);body.position=anchor+from*1150f+side*(i-(count-1)*.5f)*48f+Vector3.up*(105f+i*13f);
            body.rotation=Quaternion.LookRotation(-from);
            Transform propeller=BuildBiplane(body);
            aircraft.Add(new Aircraft{Body=body,Propeller=propeller,Course=-from,Target=PickTarget(i),DiveAt=6f+i*1.15f});
        }
        Debug.Log("[NavalPrototype] Air raid wave "+wave+" aircraft="+count);
    }
    private Transform BuildBiplane(Transform body)
    {
        Part(body,"Forward fuselage",new Vector3(0,0,1.15f),new Vector3(1.65f,1.55f,4.3f),frame);
        Part(body,"Tapered tail boom",new Vector3(0,.10f,-2.30f),new Vector3(.85f,.85f,3.6f),frame);
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
            Part(body,"Fabric wing",new Vector3(0,y,z),new Vector3(span,.30f,2.3f),canvas);
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
        if(!plane.Leaving&&(plane.Age>42f||plane.Bombs==0))BeginExit(plane);
        if(!plane.Leaving&&(plane.Target==null||plane.Target.Sunk))plane.Target=PickTarget(index);
        if(!plane.Leaving&&plane.Target==null)BeginExit(plane);
        Vector3 pos=plane.Body.position,goal;
        if(plane.Leaving)goal=plane.Exit;
        else
        {
            Transform target=plane.Target.Root;
            Vector3 targetPos=target.TransformPoint(Vector3.up*plane.Target.Definition.WeatherDeck);
            Vector3 lead=targetPos+world.FleetVelocity(target)*2.4f;
            float distance=Vector3.ProjectOnPlane(lead-pos,Vector3.up).magnitude;
            bool dive=plane.Age>=plane.DiveAt&&distance<220f;
            goal=lead+Vector3.up*(dive?10f:81f);
            if(dive&&pos.y-targetPos.y<=12f){BeginExit(plane);goal=plane.Exit;}
            if(!plane.Leaving&&dive&&distance<125f&&pos.y-targetPos.y<95f&&pos.y-targetPos.y>12f&&Time.time>=plane.ReleaseAt)
            {
                DropBomb(plane,lead);plane.ReleaseAt=Time.time+.24f;
            }
        }
        Vector3 direction=(goal-pos).normalized;
        if(direction.sqrMagnitude>.1f)plane.Course=Vector3.RotateTowards(plane.Course,direction,1.5f*dt,0f);
        plane.Body.position=pos+plane.Course*(plane.Leaving?70f:62f)*dt;
        plane.Body.rotation=Quaternion.LookRotation(plane.Course,Vector3.up);
        plane.Propeller.Rotate(0f,0f,1250f*dt,Space.Self);
    }
    private void DropBomb(Aircraft plane,Vector3 target)
    {
        Vector3 position=plane.Body.position-Vector3.up*.9f;
        float descent=Mathf.Min(-3f,plane.Course.y*35f);
        float height=Mathf.Max(1f,position.y-target.y);
        float time=NavalAirRaidMath.FallTime(height,descent);
        Vector2 spread=Random.insideUnitCircle*22f;
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
        int count=Physics.RaycastNonAlloc(bomb.Position,step.normalized,bombHits,step.magnitude,
            PhysicsManager.GetEnvironmentLayerMask(),QueryTriggerInteraction.Ignore);
        int first=-1;float closest=float.PositiveInfinity;NavalHullVoxel cell=null;
        for(int i=0;i<count;i++)
        {
            var hit=bombHits[i];if(hit.collider==null||hit.distance>=closest)continue;
            var voxel=NavalVoxelHit.Resolve(hit.collider,hit.point);
            if(voxel==null&&(hit.collider.GetComponent<NavalMergedCollider>()!=null||hit.collider.GetComponent<NavalHullVoxel>()!=null))continue;
            first=i;closest=hit.distance;cell=voxel;
        }
        if(first>=0)
        {
            Vector3 point=bombHits[first].point;
            if(cell!=null&&cell.Owner!=null&&!cell.Owner.SimulationRetired)
            {
                cell.Owner.Damage(cell.Owner.Root.InverseTransformPoint(point),3.1f,true,cell,5.2f,4.2f,true);impacts++;
            }
            BombEffect(point,false);
            return true;
        }
        bomb.Position+=step;bomb.Body.position=bomb.Position;
        bomb.Body.rotation=Quaternion.LookRotation(bomb.Velocity);
        if(bomb.Position.y<=ocean.Height(bomb.Position.x,bomb.Position.z))
        {
            BombEffect(bomb.Position,true);return true;
        }
        return bomb.Age>12f;
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
            else NavalShotPuff.Create(transform,point+scatter*1.3f,scatter*7f+Vector3.up*5f,
                i<5?flash:smoke,i<5?.45f:2.4f,i<5?1.25f:2.4f);
        }
    }
    internal bool AimAtAircraft(Transform ship,Vector3 muzzle,float baseYaw,out Vector3 aim)
    {
        aim=Vector3.zero;float best=280f*280f;
        foreach(var plane in aircraft)
        {
            if(plane.Health<=0)continue;
            float distance=(plane.Body.position-muzzle).sqrMagnitude;
            if(distance>=best)continue;
            Vector3 predicted=plane.Body.position+plane.Course*(Mathf.Sqrt(distance)/560f)*(plane.Leaving?70f:62f);
            Vector3 local=ship.InverseTransformDirection(predicted-muzzle);
            float yaw=Mathf.Atan2(local.x,local.z)*Mathf.Rad2Deg;
            float pitch=Mathf.Atan2(local.y,new Vector2(local.x,local.z).magnitude)*Mathf.Rad2Deg;
            if(Mathf.Abs(Mathf.DeltaAngle(baseYaw,yaw))>115f||pitch< -8f||pitch>85f)continue;
            best=distance;aim=predicted;
        }
        return best<280f*280f;
    }
    internal void FireAA(Vector3 muzzle,Vector3 direction)
    {
        shots++;
        float range=280f;
        // Start outside the gun assembly. The ship still blocks fire through its deck or bridge.
        RaycastHit hit;
        if(Physics.Raycast(muzzle,direction,out hit,range,PhysicsManager.GetEnvironmentLayerMask(),QueryTriggerInteraction.Ignore))
            range=hit.distance;
        Aircraft target=null;float nearest=range;
        foreach(var plane in aircraft)
        {
            if(plane.Health<=0)continue;
            float along=Vector3.Dot(plane.Body.position-muzzle,direction);
            if(along<=0f||along>=nearest)continue;
            Vector3 predicted=plane.Body.position+plane.Course*(along/560f)*(plane.Leaving?70f:62f);
            along=Vector3.Dot(predicted-muzzle,direction);
            if(along<=0f||along>=nearest||(predicted-muzzle-direction*along).sqrMagnitude>3.4f*3.4f)continue;
            target=plane;nearest=along;
        }
        if(target!=null){hits++;target.Health--;}
        EmitTrace(muzzle,muzzle+direction*nearest);
    }
    private void EmitTrace(Vector3 start,Vector3 end)
    {
        Trace trace=null;
        foreach(var entry in traces)if(entry.Until<=Time.time){trace=entry;break;}
        if(trace==null)
        {
            if(traces.Count>=48)return;
            var obj=new GameObject("Red AA tracer");obj.transform.SetParent(transform,false);
            var line=obj.AddComponent<LineRenderer>();line.sharedMaterial=red;line.positionCount=2;line.startWidth=.085f;line.endWidth=.035f;
            line.shadowCastingMode=ShadowCastingMode.Off;line.receiveShadows=false;
            trace=new Trace{Line=line};traces.Add(trace);
        }
        Vector3 delta=end-start;
        trace.Start=start;trace.Direction=delta.normalized;trace.Length=delta.magnitude;
        trace.StartAt=Time.time;trace.Until=Time.time+trace.Length/560f+.035f;
        trace.Line.SetPosition(0,start);trace.Line.SetPosition(1,start);
        trace.Line.enabled=true;
    }

    private void LateUpdate()
    {
        if(world==null||!world.SimulationActive)return;
        using(NavalPerformance.Measure(NavalPerfArea.AirRaid))Tick();
    }
    private void Tick()
    {
        foreach(var trace in traces)
        {
            if(!trace.Line.enabled)continue;
            if(Time.time>=trace.Until){trace.Line.enabled=false;continue;}
            float travel=(Time.time-trace.StartAt)*560f;
            trace.Line.SetPosition(0,trace.Start+trace.Direction*Mathf.Clamp(travel-18f,0f,trace.Length));
            trace.Line.SetPosition(1,trace.Start+trace.Direction*Mathf.Min(travel,trace.Length));
        }
        if(finished)return;
        float dt=Mathf.Min(Time.deltaTime,.05f);
        for(int i=bombs.Count-1;i>=0;i--)
        {
            Bomb bomb=bombs[i];
            if(StepBomb(ref bomb,dt)){Destroy(bomb.Body.gameObject);bombs.RemoveAt(i);}else bombs[i]=bomb;
        }
        for(int i=aircraft.Count-1;i>=0;i--)
        {
            var plane=aircraft[i];
            if(plane.Health<=0)
            {
                downed++;NavalShotPuff.Create(transform,plane.Body.position,Vector3.up*3f,frame,1.4f,2.8f);
            }
            else
            {
                Fly(plane,dt,i);
                if(!plane.Leaving||((plane.Body.position-plane.Exit).sqrMagnitude>45f*45f&&plane.Age<75f))continue;
            }
            Destroy(plane.Body.gameObject);aircraft.RemoveAt(i);
        }
        if(flagship.Sunk){Finish(false);return;}
        if(aircraft.Count>0||bombs.Count>0)return;
        if(wave==NavalAirRaidMath.Waves){Finish(true);return;}
        pause-=dt;
        if(pause<=0f){SpawnWave();pause=20f;}
    }
    private void Finish(bool success)
    {
        finished=true;won=success;
        foreach(var plane in aircraft)Destroy(plane.Body.gameObject);aircraft.Clear();
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
        GUI.Box(new Rect((Screen.width-width)*.5f,14f,width,finished?79f:63f),"\u0410\u0432\u0438\u0430\u043d\u0430\u043b\u0451\u0442 \u043d\u0430 \u043a\u043e\u043d\u0432\u043e\u0439\n"+status);
        Camera camera=world.ViewCamera;
        if(camera==null||Event.current.type!=EventType.Repaint)return;
        foreach(var plane in aircraft)
        {
            Vector3 screen=camera.WorldToScreenPoint(plane.Body.position+Vector3.up*4f);
            if(screen.z<=0f||screen.x<60f||screen.x>Screen.width-60f||screen.y<25f||screen.y>Screen.height-85f)continue;
            float distance=Vector3.Distance(camera.transform.position,plane.Body.position);
            float y=Screen.height-screen.y;
            if(distance>420f)
            {
                Color original=GUI.color;GUI.color=new Color(.95f,.26f,.16f,1f);
                GUI.DrawTexture(new Rect(screen.x-2f,y-2f,5f,5f),Texture2D.whiteTexture);
                GUI.color=original;
            }
            else GUI.Box(new Rect(screen.x-58f,y-22f,116f,21f),"\u0411\u0438\u043f\u043b\u0430\u043d · "+Mathf.RoundToInt(distance)+" \u043c");
        }
    }
    private void OnDestroy(){foreach(var material in new[]{canvas,frame,metal,red,splash,marking,flash,smoke})if(material!=null)Destroy(material);}
}
