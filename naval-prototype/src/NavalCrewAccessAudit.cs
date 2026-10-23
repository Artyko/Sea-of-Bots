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
   var ship=hull.Definition;Add("helm",new Vector3(0,ship.HelmDeck,NavalShipDefinition.HelmZ(ship)));Add("tiller",stations.EmergencyPost);Add("sails",stations.Post(stations.SailStation));
   for(int gun=1;gun<=stations.GunCount;gun++)Add("gun "+gun,stations.Post(gun));
   if(hull.Bilge!=null)for(int i=0;i<hull.Bilge.PumpCount;i++)Add("pump "+i,hull.Bilge.PumpAt(i).Post+new Vector3(0,0,.65f/hull.WorldScale));
   if(hull.Cargo!=null)foreach(var crate in hull.Cargo.Crates)Add("cargo kind="+crate.Kind+" gun="+crate.Gun,crate.Pickup);
   for(int i=0;i<ship.CrewCount;i++)Add("standby "+i,new Vector3(NavalShipDefinition.StandbyX(ship,i),hull.Floor,NavalShipDefinition.StandbyZ(i)));
   for(int level=0;level<ship.MaintenanceDeckCount;level++)foreach(float x in new float[]{-2.35f,2.35f})foreach(float z in new float[]{-4.8f,-1.4f,1.4f,4.8f})Add("maintenance deck="+level,new Vector3(x,NavalBallastLayout.Surface(x,z,ship.MaintenanceFloor(level),ship.Scale),z));
   foreach(float side in new float[]{-1,1})Add("sea ladder",new Vector3(side*2.2f,ship.WeatherDeck,ship.LadderZ));
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
