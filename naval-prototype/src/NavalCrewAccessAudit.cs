using UnityEngine;
using System.Collections.Generic;
// One target per review, once after spawn. Reports real physics rather than assuming route clearance.
public sealed class NavalCrewAccessAudit:MonoBehaviour {
 private NavalHull hull;private NavalStations stations;private float at;private int next,blocked;private bool prepared;
 private readonly List<Vector3> points=new List<Vector3>();private readonly List<string> names=new List<string>();private Collider[] hits=new Collider[32];
 public void Initialize(NavalHull h,NavalStations s){hull=h;stations=s;at=Time.time+1.5f;}
 private void Add(string name,Vector3 point){names.Add(name);points.Add(point);}
 private void Update(){
  if(hull==null||Time.time<at)return;at=Time.time+.15f;
  CharacterController body=null;foreach(var worker in hull.RepairWorkers)if(worker!=null&&worker.BodyController!=null&&worker.BodyController.enabled){body=worker.BodyController;break;}
  if(body==null)return;
  if(!prepared){
   var ship=hull.Definition;Add("helm",new Vector3(0,ship.HelmDeck,NavalShipDefinition.HelmZ(ship)));Add("tiller",stations.EmergencyPost);Add(ship.SteamPowered?"engine telegraph":"sails",stations.Post(stations.SailStation));
   if(stations.Torpedoes!=null)for(int mount=0;mount<4;mount++)Add("torpedo operator "+mount,stations.Torpedoes.Post(mount));
   if(ship.Destroyer&&stations.SteamPlant!=null)for(int watch=0;watch<2;watch++)Add("oil engineer "+watch,stations.SteamPlant.OilEngineerPost(watch));
   if(ship.Destroyer)for(int valve=0;valve<3;valve++)Add("steam valve "+valve,new Vector3(5.5f/ship.Scale,ship.HoldFloor,(-23-valve*2)/ship.Scale));
   for(int gun=1;gun<=stations.GunCount;gun++)Add("gun "+gun,stations.Post(gun));
   if(stations.HasMainBattery)for(int slot=0;slot<hull.Definition.HeavyCrewCount;slot++)Add("main battery crew "+slot,stations.MainBatteryCrewPost(slot));
   if(hull.Bilge!=null)for(int i=0;i<hull.Bilge.PumpCount;i++)Add("pump "+i,hull.Bilge.PumpAt(i).Post+new Vector3(0,0,.65f/hull.WorldScale));
   if(hull.Cargo!=null)foreach(var crate in hull.Cargo.Crates)Add("cargo kind="+crate.Kind+" gun="+crate.Gun,crate.Pickup);
   // The last two steam-era sailors belong inside the rotating main mount and
   // have no ordinary standby post. Auditing the obsolete bow posts reported
   // false failures after the main battery replaced that area.
   int standbyCount=ship.SteamPowered?ship.CrewCount-ship.HeavyCrewCount:ship.CrewCount;
   for(int i=0;i<standbyCount;i++)Add("standby "+i,NavalShipDefinition.StandbyPost(ship,i));
   if(ship.Destroyer){foreach(float z in new[]{26f,18f,10f,2f,-6f,-14f,-25f})foreach(int side in new[]{-1,1})Add("machinery service",new Vector3(side*3.6f/ship.Scale,ship.HoldFloor,z/ship.Scale));foreach(int row in NavalDestroyerAccess.BulkheadRows)foreach(int side in new[]{-1,1}){float z=NavalDestroyerAccess.BulkheadZ(row);Add("door approach",new Vector3(side*5.2f/ship.Scale,ship.HoldFloor,(z-1.5f)/ship.Scale));Add("door exit",new Vector3(side*5.2f/ship.Scale,ship.HoldFloor,(z+1.5f)/ship.Scale));}}
   float[] auditX=ship.SteamPowered?new float[]{-.50f,.50f}:new float[]{-2.35f,2.35f};
   float[] auditZ=ship.SteamPowered?new float[]{-4f,1.4f,3.2f,4f}:new float[]{-4.8f,-1.4f,1.4f,4.8f};
   for(int level=0;!ship.Destroyer&&level<ship.MaintenanceDeckCount;level++)foreach(float x in auditX)foreach(float z in auditZ)Add("maintenance deck="+level,new Vector3(x,NavalBallastLayout.Surface(x,z,ship.MaintenanceFloor(level),ship),z));
   foreach(float side in new float[]{-1,1})Add("sea ladder",new Vector3(side*(ship.HullHalfWidth(ship.LadderZ)-.35f),ship.WeatherDeck,ship.LadderZ));
   prepared=true;
  }
  if(next>=points.Count){Debug.Log("[NavalPrototype] Access audit finished: ship="+hull.Root.name+" profile="+hull.Definition.Name+" checked="+points.Count+" blocked="+blocked);enabled=false;return;}
  float radius=body.radius*Mathf.Max(Mathf.Abs(body.transform.lossyScale.x),Mathf.Abs(body.transform.lossyScale.z)),height=body.bounds.size.y;
  Vector3 foot=hull.Root.TransformPoint(points[next]);int count;
  while(true){count=Physics.OverlapCapsuleNonAlloc(foot+Vector3.up*(radius+.03f),foot+Vector3.up*Mathf.Max(radius+.03f,height-radius),radius,hits,PhysicsManager.GetEnvironmentLayerMask(),QueryTriggerInteraction.Ignore);if(count<hits.Length)break;System.Array.Resize(ref hits,hits.Length*2);}
  string obstacle="";for(int i=0;i<count;i++)if(hits[i]!=null&&hits[i].GetComponentInParent<Character>()==null){obstacle=hits[i].name+"@"+hull.Root.InverseTransformPoint(hits[i].bounds.center);break;}
  bool support=NavalPhysicsQueries.HasSupport(foot+Vector3.up*(radius+.25f),Vector3.down,.5f,hull.Root,radius);System.Array.Clear(hits,0,count);
  if(obstacle!=""||!support){blocked++;Debug.Log("[NavalPrototype] Access audit blocked: ship="+hull.Root.name+" target="+names[next]+" local="+points[next]+" radius="+radius.ToString("F2")+" support="+support+" obstacle="+obstacle);}
  next++;
 }
}
