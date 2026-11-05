using UnityEngine;
using System.Collections.Generic;
public static class NavalAmmoDispatch {
 public static bool BetterCandidate(int rounds,int gun,int least,int bestGun,bool functional,bool reserved)
 {return functional&&!reserved&&rounds<=15&&(rounds<least||rounds==least&&(bestGun<0||gun<bestGun));}
}
public sealed class NavalCargo : MonoBehaviour {
 public readonly List<NavalCargoCrate> Crates=new List<NavalCargoCrate>();private NavalHull hull;private FirstPersonMover player;private NavalStations stations;private bool hostile;private int carried;public int PlayerBoards {get;private set;}private float scan;
 private int[] directRounds;private NavalCargoCrate[] deckCrates;
 public int AmmoAtGun(int gun){var c=Deck(gun);return c==null?0:c.Stock.Destroyed?directRounds[gun]:c.Count;}
 public int Deliver(int gun,int amount){var c=Deck(gun);if(c==null)return 0;c.Refresh();if(!c.Stock.Destroyed)return c.Put(amount);int n=Mathf.Min(20-directRounds[gun],amount);directRounds[gun]+=n;return n;}
 private readonly HashSet<NavalRepairSailor> runners=new HashSet<NavalRepairSailor>();
 private readonly HashSet<int> reservedGuns=new HashSet<int>();
 public const int MaxAmmoRunners=2;
 public bool CanAssignRunner {get{return runners.Count<MaxAmmoRunners;}}
 public bool AssignRunner(NavalRepairSailor sailor,NavalCargoCrate target)
 {if(sailor==null||target==null||!CanAssignRunner||reservedGuns.Contains(target.Gun))return false;runners.Add(sailor);reservedGuns.Add(target.Gun);return true;}
 public void ReleaseRunner(NavalRepairSailor sailor,NavalCargoCrate target)
 {if(sailor!=null)runners.Remove(sailor);if(target!=null)reservedGuns.Remove(target.Gun);}
 public int Carried {get{return carried;}}
 public void Initialize(NavalHull owner,FirstPersonMover observer,NavalStations controls,bool enemy,Material wood,Material trim,Material iron){
 hull=owner;hull.Cargo=this;directRounds=new int[controls.GunCount];deckCrates=new NavalCargoCrate[controls.GunCount];player=observer;stations=controls;hostile=enemy;
 int n=0;for(int kind=0;kind<3;kind++)for(int i=0;i<NavalCargoLayout.Boxes(hull.Definition,kind);i++,n++){
 var p=NavalCargoLayout.Position(hull.Definition,n);float x=p.X,y=p.Y,z=p.Z;
 var crate=new NavalCargoCrate(hull,new Vector3(x,y,z),kind,-1,wood,trim,iron,NavalCargoLayout.Units(hull.Definition,kind,i));Crates.Add(crate);
 if(kind==1)gameObject.AddComponent<NavalPlankSupply>().InitializeCargo(hull,crate);
 if(kind==2)gameObject.AddComponent<NavalCannonSupply>().InitializeCargo(hull,crate,player,stations,enemy);
 }
 for(int gun=0;gun<stations.GunCount;gun++){
  Vector3 post=stations.Post(gun+1);float side=NavalShipDefinition.GunSide(gun);
  if(hull.Definition.Destroyer){}
  else if(hull.Definition.SteamPowered){post.x+=side*1.65f/hull.WorldScale;post.z+=(post.z>1f?-3f:3f)/hull.WorldScale;}
  else {post.x+=side*.95f/hull.WorldScale;post.z+=1.5f/hull.WorldScale;}
 deckCrates[gun]=new NavalCargoCrate(hull,post,0,gun,wood,trim,iron);Crates.Add(deckCrates[gun]);}
 NavalSupplyRoutes.Warm(hull.Definition);
 int elements=0;foreach(var c in Crates)elements+=c.CollisionSources;
 Debug.Log("[NavalPrototype] Cargo compact collision: "+hull.Definition.Name+" crates="+Crates.Count+" source cells="+elements+" intact shapes="+(Crates.Count*2));
 }
 public string AmmoDiagnostics {get{
  int hold=0;foreach(var c in Crates)if(c.Kind==0&&c.Gun<0)hold+=c.Count;
  var text=new System.Text.StringBuilder();text.Append("hold=").Append(hold).Append(" deck=");
  for(int gun=0;gun<deckCrates.Length;gun++)text.Append(gun).Append(":").Append(AmmoAtGun(gun)).Append(",");
  text.Append(" runners=").Append(runners.Count).Append(" reserved=");foreach(int gun in reservedGuns)text.Append(gun).Append(",");return text.ToString();
 }}
 public string CollisionDiagnostics {get{
  int sources=0,shapes=0,compact=0;foreach(var c in Crates){sources+=c.CollisionSources;shapes+=c.CollisionShapes;if(c.CompactCollision)compact++;}
  return "source_cells="+sources+" active_shapes="+shapes+" compact_crates="+compact+"/"+Crates.Count;
 }}
 public int ActiveCompactCollisionShapes {get{int n=0;foreach(var c in Crates)n+=c.CompactCollisionShapes;return n;}}
 public int Total(int kind){int count=0;foreach(var c in Crates)if(c.Kind==kind)count+=c.Count;if(kind==0){foreach(int n in directRounds)count+=n;count+=carried;foreach(var worker in hull.RepairWorkers)if(worker!=null)count+=worker.CarriedAmmo;}return count;}
 public NavalCargoCrate Deck(int gun){return deckCrates!=null&&gun>=0&&gun<deckCrates.Length?deckCrates[gun]:null;}
 public bool Fire(int gun){var c=Deck(gun);if(c==null)return false;c.Refresh();if(c.Stock.Destroyed){if(directRounds[gun]<=0)return false;directRounds[gun]--;return true;}return c.Take(1)==1;}
 public void LoseMagazineAmmo(int stores)
 {
  int hold=0;foreach(var c in Crates)if(c.Kind==0&&c.Gun<0)hold+=c.Count;
  int loss=Mathf.CeilToInt(hold/(float)Mathf.Max(1,stores));
  foreach(var c in Crates)if(c.Kind==0&&c.Gun<0&&loss>0)loss-=c.Take(loss);
  Debug.Log("[NavalPrototype] Magazine stock lost: ship="+hull.Root.name+" remaining="+Total(0));
 }
 public void LoseReadyAmmo(int gun)
 {
  var crate=Deck(gun);if(crate==null)return;crate.Refresh();
  if(crate.Stock.Destroyed)directRounds[gun]=0;else crate.Take(crate.Count);
  Debug.Log("[NavalPrototype] Ready ammunition lost: ship="+hull.Root.name+" gun="+(gun+1)+" remaining="+Total(0));
 }
 public NavalCargoCrate Supply(){foreach(var c in Crates)if(c.Kind==0&&c.Gun<0&&c.Count>0)return c;return null;}
 public NavalCargoCrate EmptyGun(){NavalCargoCrate best=null;int least=int.MaxValue;foreach(var c in deckCrates){if(c==null)continue;int rounds=AmmoAtGun(c.Gun);if(NavalAmmoDispatch.BetterCandidate(rounds,c.Gun,least,best!=null?best.Gun:-1,stations.IsFunctional(c.Gun+1),reservedGuns.Contains(c.Gun))){least=rounds;best=c;}}return best;}
 public bool UsePlayerBoard(){if(PlayerBoards<=0)return false;PlayerBoards--;return true;}
 public void ResetCargo(){carried=0;System.Array.Clear(directRounds,0,directRounds.Length);PlayerBoards=0;foreach(var c in Crates)c.Reset();}
 private NavalCargoCrate near;
 private void Update(){if(Time.time<scan)return;scan=Time.time+.15f;foreach(var c in Crates)c.Refresh();near=null;
 if(hostile||player==null||!player.IsAlive()||!player.IsPlayerInputEnabled()||stations.Operating)return;
 float best=2f*2f;foreach(var c in Crates)if(c.Kind!=2){float d=(player.transform.position-hull.Root.TransformPoint(c.Pickup)-Vector3.up*.8f).sqrMagnitude;if(d<best){best=d;near=c;}}
 }
 private void LateUpdate(){if(near==null||!Input.GetKeyDown(KeyCode.J))return;
 if(near.Kind==1){if(PlayerBoards==0)PlayerBoards=near.Take(4);return;}
 if(near.Gun<0&&carried==0)carried=near.Take(5);else if(near.Gun>=0&&carried>0)carried-=Deliver(near.Gun,carried);}
 private void OnGUI(){if(near!=null)GUI.Box(new Rect(Screen.width*.5f-230,Screen.height-155,460,30),"J — "+(near.Kind==1?"взять доски":near.Gun<0?(hull.Definition.RifledWeapons?"взять снаряды":"взять ядра"):(hull.Definition.RifledWeapons?"зарядить орудие":"пополнить пушку"))+" | Ящик: "+near.Count+" | Несёшь: "+(near.Kind==1?PlayerBoards:carried));}
}
