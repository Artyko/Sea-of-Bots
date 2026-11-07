using System.Collections.Generic;
using UnityEngine;
// Independent boiler/shaft groups; same steam state, damage and repair primitives as older plants.
public sealed partial class NavalSteamPlant {
 private NavalSteamState[] oilStates;
 private readonly List<List<NavalHullVoxel>> oilBoilers=new List<List<NavalHullVoxel>>(),oilTurbines=new List<List<NavalHullVoxel>>(),oilShafts=new List<List<NavalHullVoxel>>(),oilStacks=new List<List<NavalHullVoxel>>();
 private readonly List<List<NavalHullVoxel>> oilDriveLines=new List<List<NavalHullVoxel>>();
 public int OilEngineerCount;
 public Vector3 OilEngineerPost(int i){return new Vector3((i==0?-3.0f:3.0f)/hull.WorldScale,hull.Floor,-27f/hull.WorldScale);}
 private readonly List<List<NavalHullVoxel>> oilFuelTanks=new List<List<NavalHullVoxel>>(),oilFeeds=new List<List<NavalHullVoxel>>();
 private float oilEffectAt;
 private readonly float[] oilRuptureAt=new float[6];
 private float OilMean(bool pressure){float total=0;foreach(var v in oilStates)total+=pressure?v.Pressure:v.Coal;return total/oilStates.Length;}
 private bool OilDriveConnected(int i){foreach(var c in oilDriveLines[i])if(c==null||c.Missing||c.Detached)return false;return true;}
 private float OilPower {get{float total=0;for(int i=0;i<3;i++)total+=NavalOilDriveMath.Branch(oilStates[i*2].Pressure,oilStates[i*2+1].Pressure,Integrity(oilTurbines[i]),Integrity(oilShafts[i]),OilDriveConnected(i));return total/3*Mathf.Max(PropulsionMainIntegrity,PumpMainIntegrity);}}

 private void OilBox(List<NavalHullVoxel> group,Vector3 p,Vector3 size,Material m){int start=hull.Voxels.Count;NavalDestroyerLayout.Box(hull,m,p,size,false,true,group);for(int i=start;i<hull.Voxels.Count;i++){var c=hull.Voxels[i];c.RepairableEquipment=true;}}
 private void BuildOilPlant(Material iron,Material dark,Material accent){
  oilStates=new NavalSteamState[6];var s=hull.Definition;float q=s.Scale;
  smokeMaterial=new Material(Shader.Find("Unlit/Color"));smokeMaterial.color=new Color(.20f,.22f,.24f);steamMaterial=new Material(Shader.Find("Unlit/Color"));steamMaterial.color=Color.white;
  smoke=gameObject.AddComponent<NavalSmokeBatch>();var jets=new GameObject("White oil-plant steam");jets.transform.SetParent(transform,false);steamClouds=jets.AddComponent<NavalSmokeBatch>();
  for(int side=0;side<2;side++){
   var tank=new List<NavalHullVoxel>();oilFuelTanks.Add(tank);var feed=new List<NavalHullVoxel>();oilFeeds.Add(feed);float x=(side==0?-1.2f:1.2f)/q;
   OilBox(tank,new Vector3(x,s.HoldFloor+.65f/q,-18.5f/q),new Vector3(2f,1.3f,1.8f),iron);new NavalRepairableMechanism(hull,tank,false,false,"Нефтяная цистерна "+(side+1));
   OilBox(feed,new Vector3((side==0?-2.0f:2.0f)/q,s.HoldFloor+.4f/q,-19.8f/q),new Vector3(.7f,.8f,.9f),dark);
   Pipe(feed,new Vector3(x,s.HoldFloor+.65f/q,-18.5f/q),new Vector3(x,s.HoldFloor-.4f/q,-18.5f/q),.16f/q,accent);
   Pipe(feed,new Vector3(x,s.HoldFloor-.4f/q,-19.8f/q),new Vector3((side==0?-2.0f:2.0f)/q,s.HoldFloor+.4f/q,-19.8f/q),.16f/q,accent);
   Pipe(feed,new Vector3(x,s.HoldFloor-.4f/q,26f/q),new Vector3(x,s.HoldFloor-.4f/q,-19.8f/q),.16f/q,accent);new NavalRepairableMechanism(hull,feed,false,false,"Топливный насос и подача "+(side+1),true);
  }
  hull.Watertight=gameObject.AddComponent<NavalWatertightBulkheads>();hull.Watertight.Initialize(hull,iron);
  hull.BoilerProtection=new NavalBoilerProtection(hull,iron);
  for(int i=0;i<6;i++){oilStates[i]=new NavalSteamState();var cells=new List<NavalHullVoxel>();oilBoilers.Add(cells);float z=26-i*8;
   OilBox(cells,new Vector3(0,s.HoldFloor+1.1f/q,z/q),new Vector3(3.2f,2.2f,3),iron);boiler.AddRange(cells);new NavalRepairableMechanism(hull,cells,false,false,"Нефтяной котёл "+(i+1));
   // Each drum connects to both parallel steam headers. No path crosses the side aisles.
   foreach(float side in new[]{-1f,1f})Pipe(side<0?propulsionMain:pumpMain,new Vector3(0,s.HoldFloor+2.3f/q,z/q),new Vector3(side*1.8f/q,s.HoldFloor+3.6f/q,z/q),.18f/q,accent);
  }
  for(int i=0;i<4;i++){var cells=new List<NavalHullVoxel>();oilStacks.Add(cells);float z=22-i*10;
   OilBox(cells,new Vector3(0,(s.HoldFloor+s.WeatherDeck)*.5f,z/q),new Vector3(1.2f,(s.WeatherDeck-s.HoldFloor)*q,1.5f),dark);
   OilBox(cells,new Vector3(0,s.WeatherDeck+NavalOilStackLayout.Height*.5f/q,z/q),new Vector3(1.4f,NavalOilStackLayout.Height,2),dark);funnel.AddRange(cells);new NavalRepairableMechanism(hull,cells,false,false,"Дымоход "+(i+1));
   FunnelMouth(0,z/q,s.WeatherDeck+NavalOilStackLayout.Height/q,1.4f/q,2f/q,dark);
  }
  for(int i=0;i<6;i++){float z=26-i*8;int stack=NavalOilStackLayout.ForBoiler(i);var cells=oilStacks[stack];int begin=cells.Count;
   Vector3 from=new Vector3(0,s.HoldFloor+2.3f/q,z/q),elbow=new Vector3(0,s.HoldFloor+4.8f/q,z/q),to=new Vector3(0,s.HoldFloor+4.8f/q,(22-stack*10)/q);
   Pipe(cells,from,elbow,.35f/q,dark);Pipe(cells,elbow,to,.35f/q,dark);for(int c=begin;c<cells.Count;c++)funnel.Add(cells[c]);
   int fuel=i%2;Vector3 feed=new Vector3((fuel==0?-1.2f:1.2f)/q,s.HoldFloor-.4f/q,z/q);Pipe(oilFeeds[fuel],feed,feed+Vector3.up*.9f/q,.16f/q,accent);
  }
  for(int i=0;i<4;i++)new NavalRepairableMechanism(hull,oilStacks[i],false,false,"Дымоход "+(i+1));
  for(int i=0;i<2;i++)new NavalRepairableMechanism(hull,oilFeeds[i],false,false,"Топливный насос и подача "+(i+1),true);
  for(int i=0;i<3;i++){float x=(i-1)*1.05f;var turbine=new List<NavalHullVoxel>();oilTurbines.Add(turbine);var shaft=new List<NavalHullVoxel>();oilShafts.Add(shaft);
   OilBox(turbine,new Vector3(x/q,s.HoldFloor+.8f/q,-25f/q),new Vector3(.9f,1.6f,8),iron);engine.AddRange(turbine);new NavalRepairableMechanism(hull,turbine,false,false,"Турбина "+(i+1));
   Vector3 from=new Vector3(x/q,s.HoldFloor-.35f/q,-29f/q),to=new Vector3(x*1.5f/q,s.HoldFloor-.55f/q,-51.8f/q);
   Pipe(shaft,new Vector3(x/q,s.HoldFloor+.15f/q,-28.8f/q),from,.22f/q,iron);Pipe(shaft,from,to,.22f/q,iron);oilDriveLines.Add(new List<NavalHullVoxel>(shaft));OilBox(shaft,to,new Vector3(.25f,1.2f,.22f),dark);OilBox(shaft,to,new Vector3(1.2f,.25f,.22f),dark);new NavalRepairableMechanism(hull,shaft,false,false,"Вал и винт "+(i+1),false,false,false);
   // Engine feeds from both headers retain power when one branch is lost.
   foreach(float side in new[]{-1f,1f})Pipe(side<0?propulsionMain:pumpMain,new Vector3(side*1.8f/q,s.HoldFloor+3.6f/q,-21f/q),new Vector3(x/q,s.HoldFloor+1.6f/q,-21f/q),.18f/q,accent);
  }
  Pipe(propulsionMain,new Vector3(-1.8f/q,s.HoldFloor+3.6f/q,26f/q),new Vector3(-1.8f/q,s.HoldFloor+3.6f/q,-21f/q),.18f/q,accent);
  Pipe(pumpMain,new Vector3(1.8f/q,s.HoldFloor+3.6f/q,26f/q),new Vector3(1.8f/q,s.HoldFloor+3.6f/q,-21f/q),.18f/q,accent);
  Pipe(auxiliaryMain,new Vector3(0,s.HoldFloor+4.6f/q,-21f/q),new Vector3(0,s.HoldFloor+4.6f/q,-46f/q),.14f/q,accent);
  BuildOilValves(accent);
  new NavalRepairableMechanism(hull,propulsionMain,false,false,"Левая паровая магистраль",true);new NavalRepairableMechanism(hull,pumpMain,false,false,"Правая паровая магистраль",true);new NavalRepairableMechanism(hull,auxiliaryMain,false,false,"Вспомогательная магистраль",true);
 }
 private bool TryOilSmokeOutlet(int stack,out Vector3 point){point=Vector3.zero;float highest=float.NegativeInfinity;bool found=false;foreach(var cell in oilStacks[stack])if(cell!=null&&NavalSteamSmokeMath.IsExternalOutlet(cell.Missing,cell.Detached,cell.LocalCenter.y,hull.Definition.WeatherDeck)){Vector3 top=cell.transform.TransformPoint(new Vector3(0,.5f,0));float y=hull.Root.InverseTransformPoint(top).y;if(y>highest){highest=y;point=top;found=true;}}return found;}
 private void EmitOilSmoke(int stack,Vector3 velocity){float burn=0,pressure=0,combustion=0;int count=0;for(int i=0;i<6;i++)if(NavalOilStackLayout.ForBoiler(i)==stack){burn+=oilStates[i].LastBurnRate;pressure+=oilStates[i].Pressure;combustion+=oilStates[i].Combustion;count++;}if(count==0)return;burn/=count;pressure/=count;combustion/=count;
  if(!NavalSteamSmokeMath.Emits(burn,1-combustion,Integrity(oilStacks[stack])))return;
  Vector3 outlet;if(!TryOilSmokeOutlet(stack,out outlet))return;float density=NavalSteamSmokeMath.Density(burn,pressure);
  smoke.EmitPlume(outlet,velocity*.12f+Vector3.up*Mathf.Lerp(1.8f,3,density),smokeMaterial,Mathf.Lerp(.16f,.29f,density),Mathf.Lerp(4.8f,6.6f,density),1.15f,3.15f);
 }
 private float StepOilPlant(float dt,float command,bool engineer,Vector3 velocity){
  isolation.Step(dt,Pressure,Integrity(propulsionMain),Integrity(pumpMain),Integrity(auxiliaryMain),OilEngineerCount>0);
  setting=Mathf.Clamp(command,-1,1);float maxDepth=0,meanFlood=0;
  for(int i=0;i<6;i++){float z=(26-i*8)/hull.WorldScale,water=hull.WaterDepthAt(new Vector3(0,hull.Floor,z))*hull.WorldScale;maxDepth=Mathf.Max(maxDepth,water);float flood=NavalBoilerFloodMath.Flooding(Mathf.Max(0,water-.5f));meanFlood+=flood;
   oilStates[i].LoseFuel((1-Integrity(oilFuelTanks[i%2]))*dt*2);
   if(oilStates[i].StepFlooded(dt,setting,OilEngineerCount>0,Integrity(oilBoilers[i])*Integrity(oilFeeds[i%2]),Integrity(oilStacks[NavalOilStackLayout.ForBoiler(i)]),flood,true)){
    oilRuptureAt[i]=Time.time;NavalHullVoxel direct=null;foreach(var c in oilBoilers[i])if(!c.Missing&&!c.Detached){direct=c;break;}
    Vector3 centre=new Vector3(0,hull.Floor+1.1f/hull.WorldScale,z);hull.Damage(centre,.8f,true,direct,.6f);
    Debug.Log("[NavalPrototype] Oil boiler rupture: ship="+hull.Root.name+" boiler="+i);
   }
   if(oilStates[i].Ruptured&&Time.time>oilRuptureAt[i]+3&&Integrity(oilBoilers[i])>.95f&&water<.2f)oilStates[i].RepairRupture();
  }
  ConsumeSteam(NavalSteamMainMath.LeakDraw(dt,Pressure,SteamLeakSeverity));
  boilerFlooding=meanFlood/6;earlyRetreat.Step(dt,maxDepth,maxDepth,hull.HasActiveLeaks,Pressure,BoilerIntegrity);pumpAssist=NavalSteamMainMath.PumpMultiplier(Pressure,PumpMainIntegrity);
  if(Time.time>=oilEffectAt){oilEffectAt=Time.time+.4f;
   for(int i=0;i<4;i++)EmitOilSmoke(i,velocity);

  }
  TickSteamLeaks(dt,velocity);
  return oilStates[0].OutputThrottle(setting);
 }
}
