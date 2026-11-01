using System.Collections.Generic;
using UnityEngine;

public sealed class NavalFireTarget
{
    public NavalHullVoxel Cell;
    public NavalFireState Flame;
    public INavalExplosiveStore Powder;
    public Vector3 PowderPoint;
    public NavalRepairSailor Claim;
    public bool Scorched;
    public AudioSource FuseSound;
    public List<NavalHullVoxel> PowderCells;
    public List<NavalHullVoxel> SupportCells;
    public float ExplosiveHeight;
    public float ExplosiveRadius;
    public bool Collapsed;
    public int ReadyGun=-1;
    public bool Magazine {get{return Powder is NavalMagazineState;}}
    public bool ReadyUse {get{return ReadyGun>=0;}}
    public bool MainMagazine;
    public NavalCargoCrate CargoCrate;
    public bool Active {get{return Powder!=null?Powder.Lit:Cell!=null&&!Cell.Missing&&!Cell.Detached&&Flame.Burning;}}
    public Vector3 Point {get{return Powder!=null?PowderPoint:Cell.Owner.Root.InverseTransformPoint(Cell.transform.position);}}
}
[DefaultExecutionOrder(80)]
public sealed partial class NavalFireSystem : MonoBehaviour
{
    // Keep the number of independently simulated/destructible fire cells low.
    // In v250 two ships reached 30+ cells each: the particle batch stayed
    // cheap, but burned voxels repeatedly rebuilt hull collider chunks and
    // the extra firefighter routes congested every damage-control passage.
    public const int FireLimit=12;
    private NavalHull hull;
    private readonly List<NavalFireTarget> flames=new List<NavalFireTarget>(FireLimit),powder=new List<NavalFireTarget>();
    private readonly Dictionary<NavalHullVoxel,NavalFireTarget> history=new Dictionary<NavalHullVoxel,NavalFireTarget>();
    private readonly NavalPointIndex fuelIndex=new NavalPointIndex();
    private readonly List<int> nearby=new List<int>();
    private readonly List<NavalHullVoxel> fuels=new List<NavalHullVoxel>();
    private readonly Dictionary<Character,float> exposure=new Dictionary<Character,float>();
    private readonly List<Character> dead=new List<Character>();
    private Material orange,smoke,water,wood,woodLight,woodEdge,hoop,charred;
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
    public string VisualDiagnostics {get{
        int burning=0;float intensity=0;for(int i=0;i<TargetCount;i++){var t=TargetAt(i);if(!t.Active)continue;burning++;if(t.Flame!=null)intensity+=t.Flame.Intensity;}
        return "burning="+burning+" intensity="+intensity.ToString("F1")+" smoke="+(effects!=null?effects.ActiveCount:0)+" fire_cubes="+(effects!=null?effects.SolidFireCount:0)+" palettes="+(effects!=null?effects.PaletteCount:0);
    }}
    private readonly Queue<NavalFireTarget> instantBlasts=new Queue<NavalFireTarget>();
    public bool CriticalFire {get{int lit=0;float shortest=999;foreach(var t in powder)if(t.Active){lit++;shortest=Mathf.Min(shortest,t.Powder.Fuse);}return lit>=2||lit>0&&shortest<5f||flames.Count>=8;}}
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
        hoop=Mat(new Color(.16f,.17f,.17f));charred=Mat(new Color(.055f,.045f,.035f));hull.RegisterFireMaterial(wood);hull.RegisterFireMaterial(woodLight);hull.RegisterFireMaterial(woodEdge);
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
                ExplosiveHeight=NavalPowderLayout.Height,ExplosiveRadius=NavalPowderLayout.HitRadius,PowderCells=hull.Voxels.GetRange(first,hull.Voxels.Count-first)});
        }
        BuildMagazines();
        BuildReadyUseAmmunition();
        BuildMainBatteryMagazine();
        BuildCargoAmmunition();
        foreach(NavalHullVoxel cell in hull.Voxels)if(cell.Flammable&&!cell.MovableEquipment&&!cell.RepairableEquipment)
        {int id=fuels.Count;fuels.Add(cell);Vector3 p=cell.LocalCenter;fuelIndex.Add(id,p.x,p.y,p.z);}
    }
    private void BuildMagazines()
    {
        int count=NavalMagazineLayout.Count(hull.Definition);if(count==0)return;
        Material armor=Mat(new Color(.28f,.31f,.33f)),edge=Mat(new Color(.13f,.16f,.18f)),marking=Mat(new Color(.62f,.54f,.25f));
        // Materials are owned by this component and disposed with the other fire-system palette.
        magazineMaterials=new Material[]{armor,edge,marking};
        for(int index=0;index<count;index++)
        {
            Vector3 floor=new Vector3(NavalMagazineLayout.X(index),NavalMagazineLayout.Floor(hull.Definition),NavalMagazineLayout.Z(index));
            float scale=hull.WorldScale,w=NavalMagazineLayout.Width/scale,h=NavalMagazineLayout.Height/scale,d=NavalMagazineLayout.Depth/scale;
            List<NavalHullVoxel> supports=MagazineSupports(floor,w,d);
            int first=hull.Voxels.Count;
            // The explosive volume lies behind the starboard longitudinal
            // bulkhead. Only recessed double doors and their riveted frame are
            // visible in the hold, so no free-standing box can overlap cargo.
            float wallX=NavalCompartmentMath.WallX+.025f;
            for(int door=-1;door<=1;door+=2)
                MagazineBox(armor,new Vector3(wallX,floor.y+h*.50f,floor.z+door*d*.245f),new Vector3(.10f/scale,h*.82f,d*.46f));
            MagazineBox(edge,new Vector3(wallX-.01f,floor.y+.07f/scale,floor.z),new Vector3(.14f/scale,.14f/scale,d));
            MagazineBox(edge,new Vector3(wallX-.01f,floor.y+h-.07f/scale,floor.z),new Vector3(.14f/scale,.14f/scale,d));
            for(int side=-1;side<=1;side+=2)MagazineBox(edge,new Vector3(wallX-.01f,floor.y+h*.5f,floor.z+side*(d*.5f-.06f/scale)),new Vector3(.14f/scale,h,.12f/scale));
            MagazineBox(edge,new Vector3(wallX-.035f,floor.y+h*.5f,floor.z),new Vector3(.08f/scale,h*.82f,.09f/scale));
            MagazineBox(marking,new Vector3(wallX-.065f,floor.y+h*.69f,floor.z),new Vector3(.05f/scale,.14f/scale,d*.58f));
            Vector3 point=floor+Vector3.up*(h*.5f);
            GameObject warning=new GameObject("Ammunition magazine warning");warning.transform.SetParent(transform,false);warning.transform.localPosition=point;
            AudioSource audio=warning.AddComponent<AudioSource>();audio.clip=hiss;audio.loop=true;audio.playOnAwake=false;audio.spatialBlend=1;audio.minDistance=2;audio.maxDistance=35;audio.volume=.28f;
            powder.Add(new NavalFireTarget{Cell=hull.Voxels[first],Powder=new NavalMagazineState(),PowderPoint=point,FuseSound=audio,
                ExplosiveHeight=NavalMagazineLayout.Height,ExplosiveRadius=NavalMagazineLayout.HitRadius,PowderCells=hull.Voxels.GetRange(first,hull.Voxels.Count-first),SupportCells=supports});
        }
    }
    private Material[] magazineMaterials;
    private void BuildCargoAmmunition()
    {
        if(hull.Cargo==null)return;
        foreach(var crate in hull.Cargo.Crates)
        {
            if(crate==null||!crate.ExplosiveAmmunition)continue;
            List<NavalHullVoxel> cells=crate.ExplosiveCells();if(cells.Count==0)continue;
            Vector3 point=crate.ExplosivePoint;
            GameObject warning=new GameObject("Ammunition crate fuse sound");warning.transform.SetParent(transform,false);warning.transform.localPosition=point;
            AudioSource audio=warning.AddComponent<AudioSource>();audio.clip=hiss;audio.loop=true;audio.playOnAwake=false;audio.spatialBlend=1;audio.minDistance=2;audio.maxDistance=30;audio.volume=.30f;
            powder.Add(new NavalFireTarget{Cell=cells[0],Powder=new NavalPowderState(),PowderPoint=point,FuseSound=audio,
                ExplosiveHeight=1.15f,ExplosiveRadius=.85f,PowderCells=cells,CargoCrate=crate});
        }
    }
    private void BuildReadyUseAmmunition()
    {
        int count=NavalReadyAmmoLayout.Count(hull.Definition);if(count==0)return;
        Material armor=magazineMaterials[0],edge=magazineMaterials[1],brass=magazineMaterials[2];
        float scale=hull.WorldScale,w=NavalReadyAmmoLayout.Width/scale,h=NavalReadyAmmoLayout.Height/scale,d=NavalReadyAmmoLayout.Depth/scale,t=.10f/scale;
        for(int gun=0;gun<count;gun++)
        {
            float side=NavalShipDefinition.GunSide(gun);
            Vector3 floor=new Vector3(NavalReadyAmmoLayout.X(hull.Definition,gun),NavalReadyAmmoLayout.Floor(hull.Definition),NavalReadyAmmoLayout.Z(hull.Definition,gun));
            int first=hull.Voxels.Count;
            // Low armoured ready-use locker bolted to the outboard side of the
            // fixed mounting.  The open inboard face exposes individual brass
            // shell cases without placing a loose crate in the crew passage.
            ReadyAmmoBox(armor,floor+new Vector3(side*(w*.5f-t*.5f),h*.5f,0),new Vector3(t,h,d),true,true);
            ReadyAmmoBox(edge,floor+new Vector3(0,t*.5f,0),new Vector3(w,t,d),true,true);
            ReadyAmmoBox(edge,floor+new Vector3(0,h-t*.5f,0),new Vector3(w,t,d),true,true);
            for(int end=-1;end<=1;end+=2)ReadyAmmoBox(edge,floor+new Vector3(0,h*.5f,end*(d*.5f-t*.5f)),new Vector3(w,h,t),true,true);
            for(int row=-1;row<=1;row+=2)for(int column=-1;column<=1;column+=2)
                ReadyAmmoBox(brass,floor+new Vector3(-side*w*.12f,h*.43f,column*d*.22f),new Vector3(.11f/scale,h*.68f,.11f/scale),false,false);
            Vector3 point=floor+Vector3.up*(h*.5f);
            GameObject warning=new GameObject("Ready ammunition fuse sound");warning.transform.SetParent(transform,false);warning.transform.localPosition=point;
            AudioSource audio=warning.AddComponent<AudioSource>();audio.clip=hiss;audio.loop=true;audio.playOnAwake=false;audio.spatialBlend=1;audio.minDistance=2;audio.maxDistance=35;audio.volume=.32f;
            powder.Add(new NavalFireTarget{Cell=hull.Voxels[first],Powder=new NavalPowderState(),PowderPoint=point,FuseSound=audio,
                ExplosiveHeight=NavalReadyAmmoLayout.Height,ExplosiveRadius=NavalReadyAmmoLayout.HitRadius,ReadyGun=gun,
                PowderCells=hull.Voxels.GetRange(first,hull.Voxels.Count-first)});
        }
    }
    private void ReadyAmmoBox(Material material,Vector3 centre,Vector3 size,bool solid,bool armored)
    {
        int first=hull.Voxels.Count;hull.AddDecorationBox(hull.Root,material,centre,size,solid);
        for(int i=first;i<hull.Voxels.Count;i++)
        {
            hull.Voxels[i].Flammable=false;hull.Voxels[i].Metallic=true;hull.Voxels[i].Armored=armored;
            if(!solid)hull.Voxels[i].MovableEquipment=true;
        }
    }
    private void MagazineBox(Material material,Vector3 centre,Vector3 size)
    {
        int first=hull.Voxels.Count;hull.AddDecorationBox(hull.Root,material,centre,size,true);
        for(int i=first;i<hull.Voxels.Count;i++){hull.Voxels[i].Flammable=false;hull.Voxels[i].Armored=true;}
    }
    private List<NavalHullVoxel> MagazineSupports(Vector3 floor,float width,float depth)
    {
        var result=new List<NavalHullVoxel>();
        foreach(var cell in hull.Voxels)
        {
            if(cell==null||cell.Missing||cell.Detached||cell.MovableEquipment||cell.RepairableEquipment)continue;
            Vector3 size=cell.transform.localScale,centre=cell.LocalCenter;
            float top=centre.y+Mathf.Abs(size.y)*.5f;
            if(Mathf.Abs(top-floor.y)>.10f||Mathf.Abs(centre.x-floor.x)>width*.48f+Mathf.Abs(size.x)*.5f||
               Mathf.Abs(centre.z-floor.z)>depth*.48f+Mathf.Abs(size.z)*.5f)continue;
            result.Add(cell);
        }
        return result;
    }
    private void CollapseMagazine(NavalFireTarget target)
    {
        target.Collapsed=true;target.Powder.Douse();target.Claim=null;
        if(target.FuseSound!=null)target.FuseSound.Stop();
        foreach(var cell in target.PowderCells)if(cell!=null&&!cell.Missing)cell.SetMissing(true);
        if(hull.Cargo!=null)
        {
            int stores=1;foreach(var other in powder)if(other.Magazine&&other!=target&&!other.Collapsed&&!other.Powder.Spent)stores++;
            hull.Cargo.LoseMagazineAmmo(stores);
        }
        Debug.Log("[NavalPrototype] Unsupported ammunition magazine collapsed: "+hull.Root.name+" at "+target.PowderPoint);
    }
    public bool Ignite(NavalHullVoxel cell){return Ignite(cell,.30f);}
    public bool Ignite(NavalHullVoxel cell,float strength)
    {
        if(cell==null||!cell.Flammable||cell.Missing||cell.Detached||cell.RepairableEquipment||cell.MovableEquipment||flames.Count>=FireLimit)return false;
        NavalFireTarget target;
        if(!history.TryGetValue(cell,out target))
        {target=new NavalFireTarget{Cell=cell,Flame=new NavalFireState(cell.RigFabric)};history.Add(cell,target);}
        if(!target.Flame.Ignite(Time.time,strength))return false;
        if(!flames.Contains(target))flames.Add(target);return true;
    }
    public void Spark(NavalHullVoxel cell)
    {
        if(cell==null||Underwater(cell.transform.position))return;
        foreach(var target in powder)if((cell.LocalCenter-target.PowderPoint).magnitude<target.ExplosiveRadius/hull.WorldScale)
        {target.Powder.Ignite(0);return;}
        Ignite(cell);
    }
    public void Impact(Vector3 point,float radius,float power,bool penetrated=true,NavalHullVoxel direct=null)
    {
        if(power<.9f)return; // Underwater hits still damage wood, but do not light dry powder.
        foreach(var target in powder)
        {
            if((point-target.PowderPoint).magnitude>radius+target.ExplosiveRadius/hull.WorldScale)continue;
            bool directStore=direct!=null&&target.PowderCells!=null&&target.PowderCells.Contains(direct);
            if(target.Magazine&&!penetrated&&!directStore)continue;
            Vector3 p=hull.Root.TransformPoint(target.PowderPoint);
            if(!Underwater(p))
            {
                if(directStore&&(target.ReadyUse||penetrated))
                {
                    if(NavalExplosiveImpact.DirectHit(target.Powder,Random.value))instantBlasts.Enqueue(target);
                    Debug.Log("[NavalPrototype] Direct projectile hit on ammunition: ship="+hull.Root.name+" kind="+(target.ReadyUse?"ready gun "+(target.ReadyGun+1):target.MainMagazine?"main magazine":target.CargoCrate!=null?"ammunition crate":"magazine")+" fuse="+target.Powder.Fuse.ToString("F1"));
                }
                else if(power>2.1f)target.Powder.Ignite(0);
                else if(target.Powder.Impact(Random.value))instantBlasts.Enqueue(target);
            }
        }
        if(!NavalFireIgnition.FromImpact(power,penetrated,Random.value))return;
        float reach=radius+1.2f/hull.WorldScale;fuelIndex.Query(point.x,point.y,point.z,reach,nearby);
        // The core cells will be removed by this impact; only ignite its surviving rim.
        foreach(int i in nearby)if((fuels[i].LocalCenter-point).sqrMagnitude>radius*radius&&
            (fuels[i].LocalCenter-point).sqrMagnitude<=reach*reach&&!Underwater(fuels[i].transform.position)&&Ignite(fuels[i]))
        {
            Debug.Log("[NavalPrototype] Impact ignited fire: ship="+hull.Root.name+" power="+power.ToString("F2")+" penetrated="+penetrated+" point="+point);
            break;
        }
    }
    // Called only after an AP fuze has armed beyond the outer plate. This is
    // deliberately separate from Impact(): a successful internal burst heats
    // one compartment, not an arbitrary exterior plank around the entry hole.
    public int InternalBurst(Vector3 point,float radius,float basePower,float residualPower)
    {
        float chance=NavalArmorMath.InternalFireChance(basePower,residualPower);
        if(Random.value>=chance)return 0;
        float reach=radius+1.45f/hull.WorldScale;fuelIndex.Query(point.x,point.y,point.z,reach,nearby);
        int limit=NavalArmorMath.InternalFireLimit(basePower,residualPower),started=0;
        float strength=Mathf.Clamp(.38f+basePower*.055f+residualPower*.045f,.42f,.92f);
        // Prefer fuel actually inside the burst, then let at most a few hot
        // fragments seed separate localized fires. FireLimit remains global.
        nearby.Sort((a,b)=>(fuels[a].LocalCenter-point).sqrMagnitude.CompareTo((fuels[b].LocalCenter-point).sqrMagnitude));
        foreach(int i in nearby)
        {
            var fuel=fuels[i];float d=(fuel.LocalCenter-point).magnitude;if(d>reach||Underwater(fuel.transform.position))continue;
            if(Ignite(fuel,strength)){started++;if(started>=limit)break;}
        }
        if(started>0)Debug.Log("[NavalPrototype] AP internal fire: ship="+hull.Root.name+" fires="+started+" chance="+chance.ToString("F2")+" intensity="+strength.ToString("F2")+" point="+point);
        return started;
    }
    private bool Underwater(Vector3 point)
    {return WaterTouches(point);}
    public void Douse(Vector3 worldPoint,float volume)
    {
        if(volume<=0)return;
        const float range=2.2f;
        foreach(var target in powder)if(Vector3.Distance(hull.Root.TransformPoint(target.PowderPoint),worldPoint)<=range)target.Powder.Douse();
        foreach(var target in flames)if(Vector3.Distance(target.Cell.transform.position,worldPoint)<=range)target.Flame.Cool(Time.time,volume);
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
    {if(shipFlames!=null)shipFlames.Clear(false);instantBlasts.Clear();foreach(var target in flames){target.Flame.Douse(Time.time);target.Claim=null;}flames.Clear();history.Clear();foreach(var target in powder){target.Powder.Reset();target.Claim=null;target.Collapsed=false;if(target.FuseSound!=null)target.FuseSound.Stop();}exposure.Clear();}
    private void LateUpdate()
    {
        var world=NavalTestWorld.Active;if(world==null||!world.SimulationActive)return;
        // Never recurse into Hull.Damage while its shared hit buffers are being iterated.
        while(instantBlasts.Count>0){var target=instantBlasts.Dequeue();Explode(hull.Root.TransformPoint(target.PowderPoint),target);}
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
                float spreadChance=target.Flame.SpreadChance;
                foreach(int index in nearby)if(Random.value<spreadChance&&(fuels[index].transform.position-p).sqrMagnitude<1.44f&&!Underwater(fuels[index].transform.position)&&Ignite(fuels[index],Mathf.Max(.22f,target.Flame.Intensity*.48f)))break;
            }
        }
        EmitFireSmoke(.25f);
        foreach(var target in powder)
        {
            if(target.Magazine&&!target.Collapsed&&target.SupportCells!=null)
            {
                int support=0;foreach(var cell in target.SupportCells)if(cell!=null&&!cell.Missing&&!cell.Detached)support++;
                if(!NavalMagazineSupport.Stable(support,target.SupportCells.Count)){CollapseMagazine(target);continue;}
            }
            if(target.Collapsed)continue;
            Vector3 centre=Vector3.zero;int intact=0;
            foreach(var cell in target.PowderCells)if(cell!=null&&!cell.Missing){centre+=cell.transform.position;intact++;}
            if(intact>0)target.PowderPoint=hull.Root.InverseTransformPoint(centre/intact);
            if(target.FuseSound!=null)target.FuseSound.transform.localPosition=target.PowderPoint;
            Vector3 p=hull.Root.TransformPoint(target.PowderPoint);bool near=false;
            if(NavalFireProtection.AllowsNearbyHeat(target.MainMagazine))
                foreach(var flame in flames)if(flame.Active&&(flame.Cell.transform.position-p).sqrMagnitude<2.25f){near=true;break;}
            bool wasLit=target.Powder.Lit;
            bool exploded=target.Powder.Step(.25f,near,Underwater(p));
            if(!wasLit&&target.Powder.Lit)
                Debug.Log("[NavalPrototype] Ammunition ignited by nearby fire: ship="+hull.Root.name+" kind="+
                    (target.ReadyUse?"ready gun "+(target.ReadyGun+1):target.MainMagazine?"main magazine":target.CargoCrate!=null?"ammunition crate":"magazine")+
                    " fuse="+target.Powder.Fuse.ToString("F1"));
            if(target.FuseSound!=null)
            {if(target.Powder.Lit&&!target.FuseSound.isPlaying)target.FuseSound.Play();else if(!target.Powder.Lit&&target.FuseSound.isPlaying)target.FuseSound.Stop();}
            if(exploded){Explode(p,target);continue;}
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
    private void Explode(Vector3 point,NavalFireTarget source=null)
    {
        NavalStations controls=source!=null?NavalStations.ForShip(hull.Root):null;
        if(source!=null&&source.ReadyUse)
        {
            if(hull.Cargo!=null)hull.Cargo.LoseReadyAmmo(source.ReadyGun);
            if(controls!=null)controls.AmmunitionDetonated(source.ReadyGun);
        }
        if(source!=null&&source.MainMagazine&&controls!=null)controls.MainMagazineDetonated();
        if(source!=null&&source.CargoCrate!=null)source.CargoCrate.DestroyAmmunition();
        if(source!=null&&source.Magazine&&hull.Cargo!=null)
        {
            int stores=1;foreach(var target in powder)if(target.Magazine&&!target.Powder.Spent)stores++;
            hull.Cargo.LoseMagazineAmmo(stores);
        }
        bool ready=source!=null&&source.ReadyUse,magazine=source!=null&&source.Magazine,main=source!=null&&source.MainMagazine;
        float radius=NavalAmmunitionBlastMath.HullRadius(ready,magazine,main);
        float power=NavalAmmunitionBlastMath.Power(ready,magazine,main);
        foreach(NavalSeaLadder ship in NavalSeaLadder.All)if(ship!=null&&ship.Hull!=null)
            ship.Hull.Damage(ship.transform.InverseTransformPoint(point),radius/ship.Hull.WorldScale,true,null,power,power,true);
        foreach(Character robot in NavalCharacters.Snapshot())if(robot!=null&&robot.IsAlive()&&
            NavalAmmunitionBlastMath.HitsRobot((robot.transform.position-point).sqrMagnitude,ready,magazine,main))robot.Kill(null,DamageSourceType.ScriptedKill,-1);
        GameObject blast=new GameObject("Naval powder explosion flash");
        blast.AddComponent<NavalPowderBlastVisual>().Initialize(effects,point,orange,smoke,charred,NavalAmmunitionBlastMath.VisualScale(ready,magazine,main));
        AudioSource.PlayClipAtPoint(boom,point,1f);
        string kind=source!=null&&source.MainMagazine?"Main battery magazine detonation: ":source!=null&&source.ReadyUse?"Ready ammunition explosion: ":source!=null&&source.Magazine?"Ammunition magazine explosion: ":source!=null&&source.CargoCrate!=null?"Ammunition crate explosion: ":"Powder explosion: ";
        Debug.Log("[NavalPrototype] "+kind+hull.Definition.Name+(source!=null&&source.ReadyUse?" gun="+(source.ReadyGun+1):""));
    }
    private void OnGUI()
    {
        var world=NavalTestWorld.Active;Camera camera=world!=null?world.ViewCamera:null;if(camera==null)return;
        foreach(var target in powder)if(target.Powder.Lit)
        {Vector3 p=camera.WorldToScreenPoint(hull.Root.TransformPoint(target.PowderPoint)+Vector3.up);if(p.z>0&&p.z<80)GUI.Box(new Rect(p.x-90,Screen.height-p.y,180,25),(target.MainMagazine?"MAIN MAGAZINE: ":target.ReadyUse?"READY AMMO: ":target.Magazine?"MAGAZINE: ":"POWDER: ")+Mathf.CeilToInt(target.Powder.Fuse)+" s");}
    }
    private void OnDestroy(){if(shipFlames!=null)Destroy(shipFlames.gameObject);if(hull!=null&&hull.Fire==this)hull.Fire=null;if(hiss!=null)Destroy(hiss);if(boom!=null)Destroy(boom);foreach(var m in new Material[]{orange,smoke,water,wood,woodLight,woodEdge,hoop,charred})if(m!=null)Destroy(m);if(magazineMaterials!=null)foreach(var m in magazineMaterials)if(m!=null)Destroy(m);}
}
