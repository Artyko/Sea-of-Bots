using System.Collections.Generic;
using UnityEngine;
public sealed class NavalTorpedo:MonoBehaviour {
 public static readonly List<NavalTorpedo> Active=new List<NavalTorpedo>();
 public Vector3 Velocity {get;private set;}public float Travel {get;private set;}public Transform Source {get;private set;}
 private Character attacker;
 private Renderer torpedoRenderer;
 public float DetectionRange {get;private set;}
 private bool detected;
 private NavalWater sea;private Material water,bodyMaterial;private float seaLevel,wakeAt;private readonly RaycastHit[] hits=new RaycastHit[128];
 private struct HullPose
 {
  public Transform Root;
  public Matrix4x4 WorldToLocal;
 }
 private readonly HullPose[] hullPoses=new HullPose[NavalPhysicsQueries.HullSlotCount];
 public void Initialize(Vector3 heading,Transform owner,NavalWater ocean,Material foam,Character sourceActor=null){Source=owner;attacker=sourceActor;var mission=NavalTestWorld.Active;if(mission!=null)mission.RecordConvoyShot(owner,true,false);
 var observer=NavalTestWorld.Active;
 detected=observer==null||observer.PlayerHull==null||owner==observer.PlayerHull.Root||(NavalStations.ForShip(owner)!=null&&!NavalStations.ForShip(owner).HostileToPlayer);
 DetectionRange=Random.Range(220f,340f);
 torpedoRenderer=GetComponent<Renderer>();
 torpedoRenderer.enabled=detected;sea=ocean;water=new Material(foam);bodyMaterial=new Material(GetComponent<Renderer>().sharedMaterial);GetComponent<Renderer>().sharedMaterial=bodyMaterial;Velocity=heading.normalized*NavalTorpedoMath.Speed;seaLevel=sea.Height(transform.position.x,transform.position.z);Active.Add(this);for(int slot=0;slot<hullPoses.Length;slot++){
  NavalHull hull=NavalPhysicsQueries.HullAtSlot(slot);
  if(hull!=null&&hull.Root!=null)hullPoses[slot]=new HullPose{Root=hull.Root,WorldToLocal=hull.Root.worldToLocalMatrix};
 }}
 private bool Cast(Vector3 a,Vector3 b,Transform only,out RaycastHit selected){selected=new RaycastHit();Vector3 delta=b-a;float length=delta.magnitude;if(length<.001f)return false;int count=Physics.SphereCastNonAlloc(a,.23f,delta/length,hits,length,PhysicsManager.GetEnvironmentLayerMask(),QueryTriggerInteraction.Ignore);float nearest=float.PositiveInfinity;bool found=false;
  for(int i=0;i<count;i++){var hit=hits[i];if(hit.collider==null)continue;if(only!=null&&!hit.collider.transform.IsChildOf(only))continue;if(Travel<8&&Source!=null&&hit.collider.transform.IsChildOf(Source))continue;if(hit.distance<nearest){selected=hit;nearest=hit.distance;found=true;}}
  // Saturation is rare, but cannot silently discard the nearest real obstacle.
  if(count==hits.Length){foreach(var hit in Physics.SphereCastAll(a,.23f,delta/length,length,PhysicsManager.GetEnvironmentLayerMask(),QueryTriggerInteraction.Ignore)){if(only!=null&&!hit.collider.transform.IsChildOf(only)||Travel<8&&Source!=null&&hit.collider.transform.IsChildOf(Source))continue;if(hit.distance<nearest){selected=hit;nearest=hit.distance;found=true;}}}
  return found;
 }
 private bool MovingContact(int slot,Vector3 a,Vector3 b,out RaycastHit hit)
 {
  hit=new RaycastHit();
  NavalHull hull=NavalPhysicsQueries.HullAtSlot(slot);
  Transform ship=hull!=null?hull.Root:null;
  HullPose prior=hullPoses[slot];
  hullPoses[slot]=ship==null?default(HullPose):new HullPose{Root=ship,WorldToLocal=ship.worldToLocalMatrix};
  if(ship==null||hull.SimulationRetired||prior.Root!=ship||Travel<8f&&Source==ship)return false;
  Vector3 carried=ship.TransformPoint(prior.WorldToLocal.MultiplyPoint3x4(a));
  // Include the complete relative sweep, so a fast hull crossing the torpedo
  // between frames remains a candidate even when both endpoints are distant.
  Vector3 start=carried-ship.position,end=b-ship.position;
  float radius=hull.Definition.HullLength+hull.Definition.HullBeam+40f;
  if(!NavalTorpedoMath.SweepNearHull(start.x,start.z,end.x,end.z,radius))return false;
  return Cast(carried,b,ship,out hit);
 }
 private void Update(){var world=NavalTestWorld.Active;if(world==null||!world.SimulationActive)return;float dt=Time.deltaTime;Vector3 a=transform.position,b=a+Velocity*dt;
  // Slowly filtered sea level follows swell, not the short crest under every frame.
  seaLevel=Mathf.Lerp(seaLevel,sea.Height(b.x,b.z),1-Mathf.Exp(-dt*.35f));b.y=seaLevel-NavalTorpedoMath.Depth;
  RaycastHit hit,relative;
  bool contact=Cast(a,b,null,out hit);
  for(int slot=0;slot<hullPoses.Length;slot++)
   if(MovingContact(slot,a,b,out relative)&&(!contact||(relative.point-a).sqrMagnitude<(hit.point-a).sqrMagnitude))
   {hit=relative;contact=true;}
  float step=Vector3.Distance(a,b);Travel+=contact?Mathf.Min(step,Vector3.Distance(a,hit.point)):step;
  if(contact){if(NavalTorpedoMath.Armed(Travel))Explode(hit);else Debug.Log("[NavalPrototype] Torpedo unarmed contact: travel="+Travel);Destroy(gameObject);return;}
  transform.position=b;transform.rotation=Quaternion.LookRotation(Velocity)*Quaternion.Euler(90,0,0);if(!detected&&world.PlayerHull!=null&&world.PlayerHull.Root!=null){
   Vector3 offset=b-world.PlayerHull.Root.position;
   offset.y=0;
   if(offset.sqrMagnitude<=DetectionRange*DetectionRange){detected=true;torpedoRenderer.enabled=true;}
  }
  if(detected&&Time.time>=wakeAt){wakeAt=Time.time+.12f;NavalShotPuff.Create(transform.parent,new Vector3(b.x,seaLevel+.04f,b.z),Vector3.up*.15f,water,1.4f,.22f);}
  if(Travel>=NavalTorpedoMath.Range){Debug.Log("[NavalPrototype] Torpedo range exhausted: source="+(Source!=null?Source.name:"destroyed")+" travel="+Travel.ToString("F1"));Destroy(gameObject);}
 }
 private void Explode(RaycastHit hit){Debug.Log("[NavalPrototype] Torpedo armed contact: obstacle="+hit.collider.name+" position="+hit.point+" travel="+Travel.ToString("F1"));var cell=NavalVoxelHit.Resolve(hit.collider,hit.point);if(cell!=null&&cell.Owner!=null){var h=cell.Owner;var mission=NavalTestWorld.Active;if(mission!=null)mission.RecordConvoyShot(Source,true,true);int removed=h.Damage(h.Root.InverseTransformPoint(hit.point),NavalTorpedoMath.BlastRadius,true,cell,NavalTorpedoMath.BlastPower,NavalTorpedoMath.BlastArmorPower,true);var ram=h.Root.GetComponent<NavalRamDamage>();if(ram!=null)ram.ExplosionImpulse(Velocity.normalized*1.2f);
   Debug.Log("[NavalPrototype] Torpedo hit: ship="+h.Root.name+" blast_radius_m="+NavalTorpedoMath.BlastRadius.ToString("F2")+" removed_voxels="+removed+" travel_m="+Travel.ToString("F0"));}
  DamageCrew(hit.point);
  // Damage stays on the contacted vessel; an island cannot transmit the blast through itself.
  var batch=transform.parent.GetComponent<NavalSmokeBatch>();if(batch==null)batch=transform.parent.gameObject.AddComponent<NavalSmokeBatch>();
  Vector3 surface=new Vector3(hit.point.x,sea.Height(hit.point.x,hit.point.z)+.2f,hit.point.z);
  for(int i=0;i<96;i++)
  {
   bool core=i<36;
   Vector3 spread=Random.insideUnitSphere;
   spread.y=0;
   Vector3 spray=Vector3.up*Random.Range(core?25f:10f,core?37f:19f)+spread*(core?4f:15f);
   batch.EmitDebris(surface+spread*(core?1.4f:3.5f),spray,water,Random.Range(core?.9f:.45f,core?1.8f:1.15f));
  }
 }
 private void DamageCrew(Vector3 point){if(attacker==null)return;var seen=new HashSet<BaseBodyPart>();foreach(var shape in Physics.OverlapSphere(point,NavalTorpedoMath.CrewBlastRadius,~0,QueryTriggerInteraction.Ignore)){
  var part=shape.GetComponentInParent<BaseBodyPart>();var actor=part!=null?part.GetOwner():null;if(part==null||!seen.Add(part)||actor==null||!actor.IsAlive()||actor.IsImmuneToDamage()||!part.CanBeDamaged)continue;
  Vector3 end=shape.ClosestPoint(point),delta=end-point;RaycastHit wall;if(Physics.Raycast(point+delta.normalized*.2f,delta.normalized,out wall,Mathf.Max(0,delta.magnitude-.25f),PhysicsManager.GetEnvironmentLayerMask(),QueryTriggerInteraction.Ignore)){var c=NavalVoxelHit.Resolve(wall.collider,wall.point);if(c==null||!c.Missing)continue;}
  part.TryStabVolume(point,end+delta.normalized*.25f,GetInstanceID(),20,attacker,FireType.None,false,DamageSourceType.None);
 }}
 private void OnDestroy(){Active.Remove(this);if(water!=null)Destroy(water);if(bodyMaterial!=null)Destroy(bodyMaterial);}
}
