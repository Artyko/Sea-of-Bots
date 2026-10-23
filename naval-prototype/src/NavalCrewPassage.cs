using System.Collections.Generic;
using UnityEngine;
// Narrow-passage fail-safe for friendly sailors only. Hull, player and enemy collisions stay real.
[DefaultExecutionOrder(-50)]
public sealed class NavalCrewPassage : MonoBehaviour
{
 private static readonly Dictionary<CharacterController,NavalCrewPassage> owners=new Dictionary<CharacterController,NavalCrewPassage>();
 private NavalHull hull;
 private Character[] crew;
 private NavalRepairSailor[] workers;
 private CharacterController[] bodies;
 private bool[,] ignored;
 private float reviewAt;
 public void Initialize(NavalHull ship,Character[] sailors,NavalRepairSailor[] repair)
 {hull=ship;crew=sailors;workers=repair;bodies=new CharacterController[crew.Length];ignored=new bool[crew.Length,crew.Length];}
 public static void Prepare(CharacterController body)
 {NavalCrewPassage owner;if(body!=null&&owners.TryGetValue(body,out owner)&&owner!=null)owner.PrepareBody(body);}
 private bool InPassage(int index)
 {
  if(crew[index]==null||workers[index]==null)return false;
  Vector3 foot=crew[index].transform.position-Vector3.up*workers[index].FootOffset;
  if(InShip(hull,foot))return true;
  var world=NavalTestWorld.Active;
  NavalHull other=world!=null?world.PlayerHull:null;
  if(other!=null&&other!=hull&&InShip(other,foot))return true;
  if(world!=null&&world.EnemyBoat!=null){var ram=world.EnemyBoat.GetComponent<NavalRamDamage>();if(ram!=null&&ram.Hull!=hull&&InShip(ram.Hull,foot))return true;}
  return false;
 }
 private static bool InShip(NavalHull ship,Vector3 foot)
 {
  if(ship==null||ship.Root==null)return false;Vector3 local=ship.Root.InverseTransformPoint(foot);
  return NavalStairTraffic.InPassage(new NavalWalkPoint(local.x,local.y,local.z),ship.Definition);
 }
 private void Pair(int a,int b)
 {
  var first=bodies[a];var second=bodies[b];if(first==null||second==null)return;
  bool valid=crew[a]!=null&&crew[b]!=null&&crew[a].IsAlive()&&crew[b].IsAlive();
  bool pass=false;
  if(valid){float distance=Vector3.Distance(crew[a].transform.position,crew[b].transform.position);
   if(distance<1.8f&&crew[a].IsPlayerTeam==crew[b].IsPlayerTeam&&
      (workers[a]!=null&&workers[a].Busy||workers[b]!=null&&workers[b].Busy))pass=true;
   if(distance<2.5f)pass=pass||NavalStairTraffic.FriendlyPass(crew[a].IsPlayerTeam==crew[b].IsPlayerTeam,InPassage(a),InPassage(b),distance);}
  // HoldPosition toggles controllers; reapply a live pass before each Move as well as on review.
  if(pass||ignored[a,b])Physics.IgnoreCollision(first,second,pass);
  ignored[a,b]=pass;ignored[b,a]=pass;
 }
 private void PrepareBody(CharacterController body)
 {for(int i=0;i<bodies.Length;i++)if(bodies[i]==body){for(int j=0;j<bodies.Length;j++)if(i!=j)Pair(i,j);return;}}
 private void LateUpdate()
 {
  if(crew==null)return;
  for(int i=0;i<bodies.Length;i++)if(bodies[i]==null&&workers[i]!=null){if(!object.ReferenceEquals(bodies[i],null))owners.Remove(bodies[i]);bodies[i]=workers[i].BodyController;if(bodies[i]!=null)owners[bodies[i]]=this;}
  if(Time.unscaledTime<reviewAt)return;reviewAt=Time.unscaledTime+.1f;
  for(int i=0;i<bodies.Length;i++)for(int j=i+1;j<bodies.Length;j++)Pair(i,j);
 }
 private void OnDestroy()
 {
  if(bodies==null)return;
  for(int i=0;i<bodies.Length;i++)if(!object.ReferenceEquals(bodies[i],null)){owners.Remove(bodies[i]);if(bodies[i]!=null)for(int j=i+1;j<bodies.Length;j++)if(ignored[i,j]&&bodies[j]!=null)Physics.IgnoreCollision(bodies[i],bodies[j],false);}
 }
}
