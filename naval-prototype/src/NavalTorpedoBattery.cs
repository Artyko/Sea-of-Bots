using System.Collections.Generic;
using UnityEngine;
public sealed class NavalTorpedoBattery:MonoBehaviour {
 private sealed class Mount {public string Status="no operator";public float LastAim,PairSpread,PairDeadline;public Character ReloadOperator;public float OperatorSeen;public Transform PairTarget;public Vector3 PairVelocity;public Transform Pivot;public float Yaw=90,NextShot,PairAt;public Vector3 PairDirection;public Character PairOperator;public bool[] Loaded={true,true};public readonly NavalTorpedoWarheadState[] Charges={new NavalTorpedoWarheadState(),new NavalTorpedoWarheadState()};public readonly List<NavalHullVoxel>[] Tubes={new List<NavalHullVoxel>(),new List<NavalHullVoxel>()};public readonly List<NavalHullVoxel> Base=new List<NavalHullVoxel>();}
 private readonly Mount[] mounts=new Mount[4];private NavalHull hull;private NavalStations stations;private FirstPersonMover player;private NavalWater sea;private Material foam,steel,tubeMetal;private Character launchActor;private int active=-1,nearest=-1;private float inputYaw;
 private readonly NavalTorpedoReloadState reload=new NavalTorpedoReloadState();
 public int ReserveRounds {get{return reload.Reserve;}}
 public bool HasCombatPotential {get{for(int i=0;i<4;i++)if(Ready(i,0)||Ready(i,1)||CanReload(i))return true;return false;}}
 public void OperatorAt(int mount,Character actor){mounts[mount].ReloadOperator=actor;mounts[mount].OperatorSeen=Time.time;}
 private bool CanReload(int i){var m=mounts[i];return m!=null&&Intact(m.Base)&&(reload.Reserve>0||reload.Tube(i)>=0)&&((!m.Loaded[0]&&!m.Charges[0].Lit&&Intact(m.Tubes[0]))||(!m.Loaded[1]&&!m.Charges[1].Lit&&Intact(m.Tubes[1])));}
 public int SalvoStyle;public float PreferredLaunchDistance=700;
 public float LaunchWindowUntil;public float LastLaunch {get;private set;}public bool PlayerOperating {get{return active>=0;}}public Vector3 PlayerPosition {get{return hull.Root.TransformPoint(Post(active));}}
 public int Rounds {get{int n=0;for(int i=0;i<4;i++)for(int tube=0;tube<2;tube++)if(Ready(i,tube))n++;return n;}}
 private static readonly float[] mountZ={18,6,-3,-16};
 public Vector3 Post(int mount){return new Vector3(0,hull.Definition.WeatherDeck,(mountZ[mount]+(mount==1?3.05f:-3.05f))/hull.WorldScale);}
 private bool Intact(List<NavalHullVoxel> cells){foreach(var c in cells)if(c==null||c.Missing||c.Detached)return false;return cells.Count>0;}
 public bool Ready(int mount,int tube){return mounts[mount]!=null&&mounts[mount].Loaded[tube]&&!mounts[mount].Charges[tube].Spent&&!mounts[mount].Charges[tube].Lit&&Intact(mounts[mount].Base)&&Intact(mounts[mount].Tubes[tube]);}
 public bool Available(int mount){return mount>=0&&mount<4&&(Ready(mount,0)||Ready(mount,1)||CanReload(mount))&&mount!=active;}
 public void Initialize(NavalHull owner,NavalStations controls,FirstPersonMover actor,NavalWater ocean,Material metal,Material accent){hull=owner;stations=controls;player=actor;sea=ocean;steel=metal;tubeMetal=new Material(Shader.Find("Standard"));tubeMetal.color=new Color(.38f,.43f,.46f);foam=new Material(Shader.Find("Unlit/Color"));foam.color=new Color(.78f,.87f,.88f);float q=hull.WorldScale;float[] zs={18,6,-3,-16};
  for(int i=0;i<4;i++){var m=new Mount();mounts[i]=m;Vector3 position=new Vector3(0,hull.Definition.WeatherDeck,zs[i]/q);NavalDestroyerLayout.Box(hull,metal,position+Vector3.up*.6f/q,new Vector3(1.6f,1.2f,1.6f),false,true,m.Base);new NavalRepairableMechanism(hull,m.Base,false,false,"Основание торпедного аппарата");
   m.Pivot=new GameObject("Twin 450 mm torpedo mount "+(i+1)).transform;m.Pivot.SetParent(hull.Root,false);m.Pivot.localPosition=position+Vector3.up*NavalTorpedoMountGeometry.Axis/q;m.Pivot.localRotation=Quaternion.Euler(0,90,0);
   for(int tube=0;tube<2;tube++){int first=hull.Voxels.Count;float x=tube==0?-.40f:.40f;
    // Hollow octagonal steel launch tubes; the same cells receive damage.
    for(int wall=0;wall<8;wall++){float angle=wall*Mathf.PI/4f;float sx=Mathf.Cos(angle),sy=Mathf.Sin(angle);Vector3 size=wall%2==0?(Mathf.Abs(sx)>.5f?new Vector3(.09f,.36f,4.8f):new Vector3(.36f,.09f,4.8f)):new Vector3(.15f,.15f,4.8f);hull.AddDecorationBox(m.Pivot,tubeMetal,new Vector3(x+sx*.30f,sy*.30f,0)/q,size/q,true);}
    foreach(float z in new[]{-2.25f,0f,2.25f})for(int ring=0;ring<8;ring++){float angle=ring*Mathf.PI/4f;hull.AddDecorationBox(m.Pivot,accent,new Vector3(x+Mathf.Cos(angle)*.33f,Mathf.Sin(angle)*.33f,z)/q,new Vector3(.16f,.16f,.16f)/q,true);}
    hull.AddDecorationBox(m.Pivot,tubeMetal,new Vector3(x,0,-2.45f)/q,new Vector3(.63f,.63f,.14f)/q,true);

    for(int n=first;n<hull.Voxels.Count;n++){var c=hull.Voxels[n];c.Metallic=true;c.Flammable=false;c.MovableEquipment=true;m.Tubes[tube].Add(c);}new NavalRepairableMechanism(hull,m.Tubes[tube],false,false,"Труба торпедного аппарата "+(i+1)+"/"+(tube+1));
   }
  }
 }
 private Collider[] overlaps=new Collider[64];private RaycastHit[] sight=new RaycastHit[64];
 private bool Turn(int i,float desired,float dt){var m=mounts[i];float next=Mathf.MoveTowardsAngle(m.Yaw,desired,24*dt);if(Mathf.Abs(Mathf.DeltaAngle(m.Yaw,next))<.001f)return Mathf.Abs(Mathf.DeltaAngle(m.Yaw,desired))<1;Quaternion rotation=hull.Root.rotation*Quaternion.Euler(0,next,0);Vector3 centre=m.Pivot.position;int count=NavalOverlapQueries.Box(centre,new Vector3(.80f,NavalTorpedoMountGeometry.ClearanceHalfHeight,2.55f),rotation,~0,ref overlaps);
  if(count<0){m.Status="rotation query overflow";return false;}for(int n=0;n<count;n++)if(overlaps[n]!=null&&!overlaps[n].transform.IsChildOf(m.Pivot)){var obstacle=overlaps[n];
   if(!obstacle.transform.IsChildOf(hull.Root)&&obstacle.name=="FloorCollider")continue;
   if(obstacle.GetComponentInParent<Character>()!=null)continue;
   var cell=NavalVoxelHit.Resolve(obstacle,centre);if(cell==null||!m.Base.Contains(cell)){m.Status="rotation blocked by "+overlaps[n].name;return false;}}
  m.Yaw=next;m.Pivot.localRotation=Quaternion.Euler(0,next,0);foreach(var cells in m.Tubes)foreach(var c in cells)hull.NoteCellMoved(c);return Mathf.Abs(Mathf.DeltaAngle(next,desired))<1;
 }
 private bool Corridor(Vector3 a,Vector3 target){Vector3 d=target-a;int count=NavalRayQueries.Ray(a,d.normalized,d.magnitude,PhysicsManager.GetEnvironmentLayerMask(),ref sight);if(count<0)return false;for(int n=0;n<count;n++){var hit=sight[n];if(hit.collider==null)continue;if(hit.collider.transform.IsChildOf(hull.Root)&&hit.distance<10)continue;var voxel=NavalVoxelHit.Resolve(hit.collider,hit.point);if(voxel==null||voxel.Owner==hull)return false; // target contact is allowed, intervening land is not
   var world=NavalTestWorld.Active;bool enemy=world!=null&&(stations.EnemyControlled?voxel.Owner==world.PlayerHull:voxel.Owner.Root==world.EnemyBoat);if(!enemy)return false;
  }return true;
 }
 public void AimAI(int i,Transform target,Vector3 targetVelocity,float dt,Character actor){
  var m=mounts[i];m.LastAim=Time.time;
  if(!NavalTorpedoMath.CanAttack(Ready(i,0),Ready(i,1))&&reload.Tube(i)>=0){m.Status="loading torpedo";return;}
  if(m.PairAt>0){m.Status="preparing follow-up torpedo";return;}
  if(!Available(i)){m.Status="unavailable";return;}
  if(actor==null||!actor.IsAlive()){m.Status="no living operator";return;}
  if(target==null){m.Status="no target";return;}
  if(hull.CombatCritical||hull.Sunk){m.Status="damage control";return;}
  Vector3 start=m.Pivot.position,delta=target.position-start;float t;
  if(!NavalTorpedoMath.Lead(delta.x,delta.z,targetVelocity.x,targetVelocity.z,out t)){m.Status="no intercept in range";return;}
  Vector3 predicted=target.position+targetVelocity*t,direction=predicted-start;direction.y=0;float distance=direction.magnitude;
  if(distance<150||distance>1450){m.Status="distance outside launch limits";return;}
  var targetBody=target.GetComponent<NavalRamDamage>();float targetBeam=targetBody!=null?targetBody.Hull.Definition.HullBeam:20;
  float lane=NavalTorpedoSalvoMath.MountOffset(i)*Mathf.Sign(hull.Root.InverseTransformDirection(direction).x);
  float halfSpread=SalvoStyle==2&&Ready(i,0)&&Ready(i,1)?NavalTorpedoSalvoMath.FanHalfAngle(distance,targetBeam):0;
  direction=Quaternion.Euler(0,lane-halfSpread,0)*direction;
  Vector3 local=hull.Root.InverseTransformDirection(direction);float yaw=Mathf.Atan2(local.x,local.z)*Mathf.Rad2Deg;
  if(!NavalTorpedoMountGeometry.InSector(yaw)){m.Status="outside traverse sector";return;}
  m.Status="turning";if(!Turn(i,yaw,dt))return;
  if(!stations.AtHelm&&Time.time>LaunchWindowUntil){m.Status="waiting tactical window";return;}
  if(Time.time<m.NextShot){m.Status="launch interval";return;}
  Vector3 entry=LaunchEntry(i,direction);predicted.y=entry.y;
  if(!Corridor(entry,entry+direction.normalized*distance)){m.Status="corridor blocked";return;}
  launchActor=actor;if(!Fire(i,direction.normalized))return;
  Debug.Log("[NavalPrototype] Torpedo salvo: ship="+hull.Root.name+" style="+SalvoStyle+" intercept_seconds="+t.ToString("F1")+" distance="+distance.ToString("F0")+" fan_half_deg="+halfSpread.ToString("F1"));
  if(SalvoStyle!=0&&Ready(i,1)){m.PairAt=Time.time+.9f;m.PairDeadline=Time.time+3;m.PairTarget=target;m.PairVelocity=targetVelocity;m.PairSpread=lane+halfSpread;m.PairOperator=actor;}

 }
 public string Diagnostics {get{var report=new System.Text.StringBuilder();report.Append("ready=").Append(Rounds).Append(" reserve=").Append(reload.Reserve).Append(" salvo_style=").Append(SalvoStyle).Append(" preferred_distance=").Append(PreferredLaunchDistance.ToString("F0")).Append(" active_projectiles=").Append(NavalTorpedo.Active.Count);for(int i=0;i<4;i++){var m=mounts[i];if(m==null)continue;report.Append(" mount=").Append(i).Append(" loaded=").Append(m.Loaded[0]?1:0).Append(m.Loaded[1]?1:0).Append(" ready=").Append(Ready(i,0)?1:0).Append(Ready(i,1)?1:0).Append(" reload=").Append(reload.Progress(i).ToString("F2")).Append(" yaw=").Append(m.Yaw.ToString("F1")).Append(" status=").Append(Time.time-m.LastAim>2?"no operator at post":m.Status);}return report.ToString();}}

 private Vector3 LaunchEntry(int i,Vector3 direction){float side=Mathf.Sign(hull.Root.InverseTransformDirection(direction).x),z=mounts[i].Pivot.localPosition.z;Vector3 entry=hull.Root.TransformPoint(new Vector3(side*(hull.Definition.HullHalfWidth(z)+1f/hull.WorldScale),hull.Definition.DesignWaterline,z));entry.y=sea.Height(entry.x,entry.z)-NavalTorpedoMath.Depth;return entry;}
 public bool SalvoInProgress {get{foreach(var m in mounts)if(m!=null&&m.PairAt>0)return true;return Time.time-LastLaunch<1.5f;}}
 private bool Fire(int i,Vector3 direction){var m=mounts[i];if(Time.time<m.NextShot)return false;int tube=Ready(i,0)?0:Ready(i,1)?1:-1;if(tube<0)return false;
  Vector3 local=hull.Root.InverseTransformDirection(direction);if(Mathf.Abs(local.x)<.7f)return false;
  float side=Mathf.Sign(local.x),z=m.Pivot.localPosition.z;Vector3 start=hull.Root.TransformPoint(new Vector3(side*(hull.Definition.HullHalfWidth(z)+1f/hull.WorldScale),hull.Definition.DesignWaterline,z));start.y=sea.Height(start.x,start.z)-NavalTorpedoMath.Depth;
  if(!Corridor(start,start+direction*12)){m.Status="exit blocked";return false;}m.Status="launched";m.Loaded[tube]=false;m.NextShot=Time.time+4;LastLaunch=Time.time;
  var obj=GameObject.CreatePrimitive(PrimitiveType.Capsule);obj.name="450 mm torpedo";obj.transform.SetParent(NavalTestWorld.Active.transform,false);obj.transform.position=start;obj.transform.localScale=new Vector3(.45f,2f,.45f);obj.transform.rotation=Quaternion.LookRotation(direction)*Quaternion.Euler(90,0,0);var collider=obj.GetComponent<Collider>();collider.enabled=false;Destroy(collider);obj.GetComponent<Renderer>().sharedMaterial=steel;
  obj.AddComponent<NavalTorpedo>().Initialize(direction,hull.Root,sea,foam,launchActor!=null?launchActor:player);if(!stations.EnemyControlled)NavalShotPuff.Create(obj.transform.parent,new Vector3(start.x,start.y+NavalTorpedoMath.Depth,start.z),Vector3.up*2,foam,.9f,.65f);Debug.Log("[NavalPrototype] Torpedo launched: ship="+hull.Root.name+" mount="+i+" tube="+tube+" remaining="+Rounds);
  return true;
 }
 public bool HandleInput(float dt,float footOffset){if(stations.EnemyControlled)return false;if(hull.Sunk||player==null||!player.IsAlive()){Exit();return false;}
  nearest=-1;float best=2.5f;if(!stations.PlayerOperatingGunOrHelm){for(int i=0;i<4;i++){float d=Vector3.Distance(player.transform.position-Vector3.up*footOffset,hull.Root.TransformPoint(Post(i)));if(d<best&&Available(i)){best=d;nearest=i;}}}
  if(Input.GetKeyDown(KeyCode.E)){if(active>=0){Exit();return true;}if(nearest>=0&&!stations.PlayerOperatingGunOrHelm){active=nearest;inputYaw=mounts[active].Yaw;return true;}}
  if(active<0)return false;OperatorAt(active,player);launchActor=player;if(!AvailableIgnoringPlayer(active)){Exit();return true;}float input=(Input.GetKey(KeyCode.D)?1:0)-(Input.GetKey(KeyCode.A)?1:0);inputYaw=Mathf.Clamp(inputYaw+input*24*dt,-125,125);if(Mathf.Abs(inputYaw)<55)inputYaw=input<0?-55:55;Turn(active,inputYaw,dt);
  if(Input.GetKeyDown(KeyCode.Space)){mounts[active].PairAt=Time.time+.9f;mounts[active].PairDeadline=Time.time+3;mounts[active].PairTarget=null;mounts[active].PairDirection=hull.Root.TransformDirection(Quaternion.Euler(0,mounts[active].Yaw,0)*Vector3.forward);mounts[active].PairOperator=player;}
  if(Input.GetMouseButtonDown(0)||Input.GetKeyDown(KeyCode.Space))Fire(active,hull.Root.TransformDirection(Quaternion.Euler(0,mounts[active].Yaw,0)*Vector3.forward));return true;
 }
 private bool AvailableIgnoringPlayer(int i){return Ready(i,0)||Ready(i,1)||CanReload(i);}
 private void ClearPair(Mount m){m.PairAt=0;m.PairTarget=null;m.PairOperator=null;}
 private void Update(){
  var world=NavalTestWorld.Active;if(hull==null||hull.Sunk||world==null||!world.SimulationActive)return;
  TickReload(Time.deltaTime);
  for(int i=0;i<4;i++){
   var m=mounts[i];if(m==null||m.PairAt<=0||Time.time<m.PairAt)continue;
   bool atPost=m.PairOperator==player?active==i:m.PairOperator!=null&&m.PairOperator.GetComponent<NavalRepairSailor>()!=null&&m.PairOperator.GetComponent<NavalRepairSailor>().AtStation(Post(i));
   if(!atPost||m.PairOperator==null||!m.PairOperator.IsAlive()||hull.CombatCritical||Time.time>m.PairDeadline){ClearPair(m);continue;}
   Vector3 direction=m.PairDirection;
   if(m.PairTarget!=null){
    Vector3 velocity=m.PairTarget==world.PlayerHull.Root?world.ShipVelocity:m.PairTarget==world.EnemyBoat?world.EnemyVelocity:m.PairVelocity;
    Vector3 delta=m.PairTarget.position-m.Pivot.position;float t;
    if(!NavalTorpedoMath.Lead(delta.x,delta.z,velocity.x,velocity.z,out t)){ClearPair(m);continue;}
    direction=m.PairTarget.position+velocity*t-m.Pivot.position;direction.y=0;direction=(Quaternion.Euler(0,m.PairSpread,0)*direction).normalized;
    Vector3 local=hull.Root.InverseTransformDirection(direction);float yaw=Mathf.Atan2(local.x,local.z)*Mathf.Rad2Deg;
    if(!NavalTorpedoMountGeometry.InSector(yaw)){ClearPair(m);continue;}
    if(!Turn(i,yaw,Time.deltaTime))continue;
    if(!Corridor(LaunchEntry(i,direction),LaunchEntry(i,direction)+direction*delta.magnitude)){ClearPair(m);continue;}
   }else if(m.PairOperator!=player){ClearPair(m);continue;}
   m.NextShot=Time.time;launchActor=m.PairOperator;Fire(i,direction);ClearPair(m);
  }
 }

 private void TickReload(float dt){for(int i=0;i<4;i++){
  var m=mounts[i];bool operatorPresent=m.ReloadOperator!=null&&m.ReloadOperator.IsAlive()&&Time.time-m.OperatorSeen<.3f;
  if(reload.Tube(i)<0&&m.PairAt<=0&&Time.time>=m.NextShot&&operatorPresent&&CanReload(i)){int tube=!m.Loaded[0]&&Intact(m.Tubes[0])&&!m.Charges[0].Lit?0:1;if(reload.Begin(i,tube))Debug.Log("[NavalPrototype] Torpedo loading started: ship="+hull.Root.name+" mount="+i+" tube="+tube+" reserve="+reload.Reserve);}
  int pending=reload.Tube(i);if(pending<0)continue;
  if(reload.Step(i,dt,operatorPresent,Intact(m.Base)&&Intact(m.Tubes[pending])&&!m.Charges[pending].Lit)){m.Loaded[pending]=true;m.Charges[pending].Reset();Debug.Log("[NavalPrototype] Torpedo loaded: ship="+hull.Root.name+" mount="+i+" tube="+pending+" reserve="+reload.Reserve);}
 }}
 public void RegisterWarheads(NavalFireSystem fire){for(int i=0;i<4;i++)for(int tube=0;tube<2;tube++){int m=i,t=tube;var mount=mounts[i];fire.RegisterExplosive(new NavalFireTarget{Cell=mount.Tubes[tube][0],Powder=mount.Charges[tube],PowderCells=mount.Tubes[tube],PowderPoint=hull.Root.InverseTransformPoint(mount.Pivot.position),ExplosiveHeight=.5f,ExplosiveRadius=.7f,BlastRadiusOverride=4f,BlastPowerOverride=3.2f,HasStock=delegate{return mounts[m].Loaded[t];},Detonated=delegate{mounts[m].Loaded[t]=false;}});}}
 public void ResetLoaded(){reload.Reset();foreach(var m in mounts){m.Loaded[0]=m.Loaded[1]=true;m.Charges[0].Reset();m.Charges[1].Reset();m.PairAt=m.NextShot=0;m.PairOperator=null;m.ReloadOperator=null;}}
 public void Exit(){active=-1;}
 private void OnGUI(){DrawReloadBars();if(stations==null||stations.EnemyControlled||NavalTestWorld.Active!=null&&NavalTestWorld.Active.Spectating)return;if(active>=0)GUI.Box(new Rect(Screen.width*.5f-300,Screen.height-110,600,85),"ТОРПЕДНЫЙ АППАРАТ "+(active+1)+" · A/D — наводка · ЛКМ — одиночный · Space — пара · E — выйти\nТорпед осталось: "+Rounds+" · взведение после 70 м · глубина 1,5 м");else if(nearest>=0)GUI.Box(new Rect(Screen.width*.5f-200,Screen.height-130,400,30),"E — торпедный аппарат "+(nearest+1));}
 private void DrawReloadBars(){var world=NavalTestWorld.Active;if(world==null||world.Spectating||stations==null||stations.EnemyControlled)return;for(int i=0;i<4;i++)if(mounts[i]!=null){int ready=(Ready(i,0)?1:0)+(Ready(i,1)?1:0);bool loading=reload.Tube(i)>=0;float progress=loading?reload.Progress(i):ready>0?1:0;NavalWorldProgressBar.Draw(world.ViewCamera,mounts[i].Pivot.position+Vector3.up*.9f,progress,Intact(mounts[i].Base),!loading&&ready>0,"ТА "+(i+1)+": "+ready+"/2"+(loading?" · "+((1-progress)*NavalTorpedoReloadState.Seconds).ToString("F0")+"с":""),mounts[i].Pivot.GetInstanceID());}}
 private void OnDestroy(){if(foam!=null)Destroy(foam);if(tubeMetal!=null)Destroy(tubeMetal);}
}
