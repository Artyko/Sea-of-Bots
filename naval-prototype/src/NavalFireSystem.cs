using System.Collections.Generic;
using UnityEngine;

public sealed class NavalFireTarget
{
    public NavalHullVoxel Cell;
    public NavalFireState Flame;
    public NavalPowderState Powder;
    public Vector3 PowderPoint;
    public NavalRepairSailor Claim;
    public bool Scorched;
    public AudioSource FuseSound;
    public List<NavalHullVoxel> PowderCells;
    public bool Active {get{return Powder!=null?Powder.Lit:Cell!=null&&!Cell.Missing&&!Cell.Detached&&Flame.Burning;}}
    public Vector3 Point {get{return Powder!=null?PowderPoint:Cell.Owner.Root.InverseTransformPoint(Cell.transform.position);}}
}
[DefaultExecutionOrder(80)]
public sealed partial class NavalFireSystem : MonoBehaviour
{
    public const int FireLimit=32;
    private NavalHull hull;
    private readonly List<NavalFireTarget> flames=new List<NavalFireTarget>(FireLimit),powder=new List<NavalFireTarget>();
    private readonly Dictionary<NavalHullVoxel,NavalFireTarget> history=new Dictionary<NavalHullVoxel,NavalFireTarget>();
    private readonly NavalPointIndex fuelIndex=new NavalPointIndex();
    private readonly List<int> nearby=new List<int>();
    private readonly List<NavalHullVoxel> fuels=new List<NavalHullVoxel>();
    private readonly Dictionary<Character,float> exposure=new Dictionary<Character,float>();
    private readonly List<Character> dead=new List<Character>();
    private Material orange,smoke,water,wood,woodLight,woodEdge,hoop;
    private AudioClip hiss,boom;
    private float clock,spreadClock;
    private NavalSmokeBatch effects;
    public float PlayerWater;
    public Vector3 PlayerWaterPoint;
    public NavalHull Ship {get{return hull;}}
    public int Count {get{return flames.Count;}}
    public int TargetCount {get{return powder.Count+flames.Count;}}
    public NavalFireTarget TargetAt(int i){return i<powder.Count?powder[i]:flames[i-powder.Count];}
    public bool HasFire {get{for(int i=0;i<TargetCount;i++)if(TargetAt(i).Active)return true;return false;}}
    private readonly Queue<Vector3> instantBlasts=new Queue<Vector3>();
    public bool CriticalFire {get{int lit=0;float shortest=999;foreach(var t in powder)if(t.Active){lit++;shortest=Mathf.Min(shortest,t.Powder.Fuse);}return lit>=2||lit>0&&shortest<5f||flames.Count>=16;}}
    public bool MayAssign(NavalRepairSailor worker)
    {
        int count=0;for(int i=0;i<TargetCount;i++){var t=TargetAt(i);if(t.Active&&t.Claim!=null&&t.Claim!=worker)count++;}
        return NavalFireResponseMath.MayJoin(count,CriticalFire);
    }
    public bool CanClaim(NavalFireTarget target,NavalRepairSailor worker)
    {
        if(target.Claim!=null&&target.Claim!=worker)return false;
        Vector3 point=hull.Root.TransformPoint(target.Point);
        for(int i=0;i<TargetCount;i++)
        {
            var other=TargetAt(i);if(other==target||!other.Active||other.Claim==null||other.Claim==worker)continue;
            if((hull.Root.TransformPoint(other.Point)-point).sqrMagnitude<2.2f*2.2f)return false;
        }
        return true;
    }
    public bool UnclaimedPowder {get{foreach(var t in powder)if(t.Active&&t.Claim==null)return true;return false;}}
    public bool Urgent {get{foreach(var target in powder)if(target.Active)return true;return flames.Count>=5;}}
    private Material Mat(Color color){var m=new Material(Shader.Find("Unlit/Color"));m.color=color;return m;}
    public void Initialize(NavalHull owner)
    {
        hull=owner;hull.Fire=this;
        effects=gameObject.GetComponent<NavalSmokeBatch>();if(effects==null)effects=gameObject.AddComponent<NavalSmokeBatch>();
        orange=Mat(new Color(1,.35f,.025f));smoke=Mat(new Color(.18f,.16f,.14f));water=Mat(new Color(.1f,.6f,.85f));
        wood=Mat(new Color(.49f,.30f,.14f));woodLight=Mat(new Color(.60f,.39f,.20f));woodEdge=Mat(new Color(.34f,.20f,.09f));
        hoop=Mat(new Color(.16f,.17f,.17f));hull.RegisterFireMaterial(wood);hull.RegisterFireMaterial(woodLight);hull.RegisterFireMaterial(woodEdge);
        hiss=AudioClip.Create("Powder fuse hiss",11025,1,22050,false);
        float[] samples=new float[11025];var noise=new System.Random(119);
        for(int i=0;i<samples.Length;i++)samples[i]=(float)(noise.NextDouble()*2-1)*.18f;
        hiss.SetData(samples,0);
        boom=AudioClip.Create("Powder barrel blast",17640,1,22050,false);samples=new float[17640];
        for(int i=0;i<samples.Length;i++)
        {float t=i/22050f;samples[i]=Mathf.Clamp((float)(noise.NextDouble()*2-1)*Mathf.Exp(-t*8f)+Mathf.Sin(t*220f)*Mathf.Exp(-t*5f),-1,1)*.7f;}
        boom.SetData(samples,0);
        int count=NavalPowderLayout.Count(hull.Definition);
        for(int barrel=0;barrel<count;barrel++)
        {
            Vector3 centre=new Vector3(NavalPowderLayout.X(hull.Definition,barrel),NavalPowderLayout.Floor(hull.Definition),NavalPowderLayout.Z(hull.Definition,barrel));
            int first=hull.Voxels.Count;float unit=NavalPowderLayout.CellSize/hull.WorldScale;
            float layer=NavalPowderLayout.LayerHeight/hull.WorldScale;
            for(int y=0;y<NavalPowderLayout.Layers;y++)for(int x=-2;x<=2;x++)for(int z=-2;z<=2;z++)
            {
                if(!NavalPowderLayout.Cell(x,y,z))continue;
                bool edge=!NavalPowderLayout.Cell(x+1,y,z)||!NavalPowderLayout.Cell(x-1,y,z)||
                    !NavalPowderLayout.Cell(x,y,z+1)||!NavalPowderLayout.Cell(x,y,z-1);
                bool band=edge&&(y==1||y==NavalPowderLayout.Layers-2);
                Material surface=band?hoop:(y==0||y==NavalPowderLayout.Layers-1)&&edge?woodEdge:
                    ((x+z)&1)==0?wood:woodLight;
                hull.AddVoxel(hull.Root,surface,centre+new Vector3(x*unit,(y+.5f)*layer,z*unit),new Vector3(unit,layer,unit),false,true);
            }
            Vector3 point=centre+Vector3.up*(NavalPowderLayout.Height*.5f/hull.WorldScale);
            GameObject warning=new GameObject("Powder fuse sound");warning.transform.SetParent(transform,false);warning.transform.localPosition=point;
            AudioSource audio=warning.AddComponent<AudioSource>();audio.clip=hiss;audio.loop=true;audio.playOnAwake=false;audio.spatialBlend=1;audio.minDistance=2;audio.maxDistance=35;audio.volume=.35f;
            powder.Add(new NavalFireTarget{Cell=hull.Voxels[first],Powder=new NavalPowderState(),PowderPoint=point,FuseSound=audio,
                PowderCells=hull.Voxels.GetRange(first,hull.Voxels.Count-first)});
        }
        foreach(NavalHullVoxel cell in hull.Voxels)if(cell.Flammable&&!cell.MovableEquipment&&!cell.RepairableEquipment)
        {int id=fuels.Count;fuels.Add(cell);Vector3 p=cell.LocalCenter;fuelIndex.Add(id,p.x,p.y,p.z);}
    }
    public bool Ignite(NavalHullVoxel cell)
    {
        if(cell==null||!cell.Flammable||cell.Missing||cell.Detached||cell.RepairableEquipment||cell.MovableEquipment||flames.Count>=FireLimit)return false;
        NavalFireTarget target;
        if(!history.TryGetValue(cell,out target))
        {target=new NavalFireTarget{Cell=cell,Flame=new NavalFireState(cell.RigFabric)};history.Add(cell,target);}
        if(!target.Flame.Ignite(Time.time))return false;
        if(!flames.Contains(target))flames.Add(target);return true;
    }
    public void Spark(NavalHullVoxel cell)
    {
        if(cell==null||Underwater(cell.transform.position))return;
        foreach(var target in powder)if((cell.LocalCenter-target.PowderPoint).magnitude<NavalPowderLayout.HitRadius/hull.WorldScale)
        {target.Powder.Ignite(0);return;}
        Ignite(cell);
    }
    public void Impact(Vector3 point,float radius,float power)
    {
        if(power<.9f)return; // Underwater hits still damage wood, but do not light dry powder.
        foreach(var target in powder)
        {
            if((point-target.PowderPoint).magnitude>radius+NavalPowderLayout.HitRadius/hull.WorldScale)continue;
            Vector3 p=hull.Root.TransformPoint(target.PowderPoint);
            if(!Underwater(p))
            {
                if(power>1.5f)target.Powder.Ignite(0);
                else if(target.Powder.Impact(Random.value))instantBlasts.Enqueue(p);
            }
        }
        if(power<=1.5f&&!NavalFireIgnition.FromCannon(Random.value))return;
        float reach=radius+1.2f/hull.WorldScale;fuelIndex.Query(point.x,point.y,point.z,reach,nearby);
        // The core cells will be removed by this impact; only ignite its surviving rim.
        foreach(int i in nearby)if((fuels[i].LocalCenter-point).sqrMagnitude>radius*radius&&
            (fuels[i].LocalCenter-point).sqrMagnitude<=reach*reach&&!Underwater(fuels[i].transform.position)&&Ignite(fuels[i]))break;
    }
    private bool Underwater(Vector3 point)
    {return WaterTouches(point);}
    public void Douse(Vector3 worldPoint,float volume)
    {
        if(volume<=0)return;
        const float range=2.2f;
        foreach(var target in powder)if(Vector3.Distance(hull.Root.TransformPoint(target.PowderPoint),worldPoint)<=range)target.Powder.Douse();
        foreach(var target in flames)if(Vector3.Distance(target.Cell.transform.position,worldPoint)<=range)target.Flame.Douse(Time.time);
        Vector3 local=hull.Root.InverseTransformPoint(worldPoint);
        // Water poured in the hold remains aboard; weather-deck runoff drains overboard.
        if(local.y<hull.Definition.WeatherDeck&&Mathf.Abs(local.x)<hull.Definition.HullHalfWidth(local.z)&&Mathf.Abs(local.z)<5.8f)
        {
            float remaining=volume;remaining-=hull.Flood.Add(hull.Flood.Index(local.x,local.z),remaining);
            for(int i=0;i<hull.Flood.Water.Length&&remaining>.0001f;i++)remaining-=hull.Flood.Add(i,remaining);
        }
        for(int i=0;i<8;i++)NavalShotPuff.Create(transform,worldPoint+Random.insideUnitSphere*.25f,Vector3.down+Random.insideUnitSphere,water,.5f,.1f);
    }
    public void ResetFire()
    {if(shipFlames!=null)shipFlames.Clear(false);instantBlasts.Clear();foreach(var target in flames){target.Flame.Douse(Time.time);target.Claim=null;}flames.Clear();history.Clear();foreach(var target in powder){target.Powder.Reset();target.Claim=null;if(target.FuseSound!=null)target.FuseSound.Stop();}exposure.Clear();}
    private void LateUpdate()
    {
        var world=NavalTestWorld.Active;if(world==null||!world.SimulationActive)return;
        // Never recurse into Hull.Damage while its shared hit buffers are being iterated.
        while(instantBlasts.Count>0)Explode(instantBlasts.Dequeue());
    }
    private void Update()
    {
        var world=NavalTestWorld.Active;if(world==null||!world.SimulationActive)return;
        TickSmokeMotion();
        TickFireWater();
        HideUnderwaterFlames();
        clock+=Mathf.Min(Time.deltaTime,.05f);if(clock<.25f)return;clock-=.25f;
        spreadClock+=.25f;bool spread=spreadClock>=1f;if(spread)spreadClock=0;
        int initial=flames.Count;
        for(int i=initial-1;i>=0;i--)
        {
            var target=flames[i];var cell=target.Cell;
            if(!target.Active){target.Claim=null;flames.RemoveAt(i);continue;}
            if(target.Flame.Step(.25f,Underwater(cell.transform.position),Time.time))cell.SetMissing(true);
            if(!target.Active){target.Claim=null;flames.RemoveAt(i);continue;}
            if(!target.Scorched&&target.Flame.Burned>target.Flame.Lifetime*.35f){hull.FireScorch(cell);target.Scorched=true;}
            Vector3 p=cell.transform.position;
            EmitFlame(p+Vector3.up*.2f);
            if(spread)
            {
                Vector3 local=hull.Root.InverseTransformPoint(p);float reach=1.2f/hull.WorldScale;fuelIndex.Query(local.x,local.y,local.z,reach,nearby);
                foreach(int index in nearby)if(Random.value<.18f&&(fuels[index].transform.position-p).sqrMagnitude<1.44f&&!Underwater(fuels[index].transform.position)&&Ignite(fuels[index]))break;
            }
        }
        EmitFireSmoke(.25f);
        foreach(var target in powder)
        {
            Vector3 centre=Vector3.zero;int intact=0;
            foreach(var cell in target.PowderCells)if(cell!=null&&!cell.Missing){centre+=cell.transform.position;intact++;}
            if(intact>0)target.PowderPoint=hull.Root.InverseTransformPoint(centre/intact);
            if(target.FuseSound!=null)target.FuseSound.transform.localPosition=target.PowderPoint;
            Vector3 p=hull.Root.TransformPoint(target.PowderPoint);bool near=false;
            foreach(var flame in flames)if(flame.Active&&(flame.Cell.transform.position-p).sqrMagnitude<2.25f){near=true;break;}
            bool exploded=target.Powder.Step(.25f,near,Underwater(p));
            if(target.FuseSound!=null)
            {if(target.Powder.Lit&&!target.FuseSound.isPlaying)target.FuseSound.Play();else if(!target.Powder.Lit&&target.FuseSound.isPlaying)target.FuseSound.Stop();}
            if(exploded){Explode(p);continue;}
            if(target.Powder.Lit)EmitFlame(p+Vector3.up*.6f);
        }
        if(spread)
        {
            dead.Clear();foreach(Character robot in NavalCharacters.Snapshot())
            {
                if(robot==null||!robot.IsAlive())continue;bool hot=false;
                foreach(var target in flames)if(target.Active&&(target.Cell.transform.position-(robot.transform.position+Vector3.up*.3f)).sqrMagnitude<1f){hot=true;break;}
                float seconds;exposure.TryGetValue(robot,out seconds);seconds=hot?seconds+1:Mathf.Max(0,seconds-2);
                if(seconds>=6){robot.Kill(null,DamageSourceType.ScriptedKill,-1);dead.Add(robot);}else if(seconds>0)exposure[robot]=seconds;else exposure.Remove(robot);
            }
            foreach(Character robot in exposure.Keys)if(robot==null||!robot.IsAlive())dead.Add(robot);
            foreach(Character robot in dead)exposure.Remove(robot);
        }
    }
    private void Explode(Vector3 point)
    {
        foreach(NavalSeaLadder ship in NavalSeaLadder.All)if(ship!=null&&ship.Hull!=null)
            ship.Hull.Damage(ship.transform.InverseTransformPoint(point),NavalPowderBlastMath.HullRadius/ship.Hull.WorldScale,true,null,NavalPowderBlastMath.Power);
        foreach(Character robot in NavalCharacters.Snapshot())if(robot!=null&&robot.IsAlive()&&NavalPowderBlastMath.HitsRobot((robot.transform.position-point).sqrMagnitude))robot.Kill(null,DamageSourceType.ScriptedKill,-1);
        GameObject blast=new GameObject("Naval powder explosion flash");
        blast.AddComponent<NavalPowderBlastVisual>().Initialize(effects,point,orange,smoke,wood);
        AudioSource.PlayClipAtPoint(boom,point,1f);
        Debug.Log("[NavalPrototype] Powder explosion: "+hull.Definition.Name);
    }
    private void OnGUI()
    {
        var world=NavalTestWorld.Active;Camera camera=world!=null?world.ViewCamera:null;if(camera==null)return;
        foreach(var target in powder)if(target.Powder.Lit)
        {Vector3 p=camera.WorldToScreenPoint(hull.Root.TransformPoint(target.PowderPoint)+Vector3.up);if(p.z>0&&p.z<80)GUI.Box(new Rect(p.x-80,Screen.height-p.y,160,25),"POWDER: "+Mathf.CeilToInt(target.Powder.Fuse)+" s");}
    }
    private void OnDestroy(){if(shipFlames!=null)Destroy(shipFlames.gameObject);if(hull!=null&&hull.Fire==this)hull.Fire=null;if(hiss!=null)Destroy(hiss);if(boom!=null)Destroy(boom);foreach(var m in new Material[]{orange,smoke,water,wood,woodLight,woodEdge,hoop})if(m!=null)Destroy(m);}
}
