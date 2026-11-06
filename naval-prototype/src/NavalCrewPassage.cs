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
 private bool[,] ignored;private Collider[][] shapes;
 private float reviewAt;private float[] shapeRefreshAt;
 public void Initialize(NavalHull ship,Character[] sailors,NavalRepairSailor[] repair)
 {hull=ship;crew=sailors;workers=repair;bodies=new CharacterController[crew.Length];ignored=new bool[crew.Length,crew.Length];shapes=new Collider[crew.Length][];shapeRefreshAt=new float[crew.Length];}
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
 private void Pair(int a,int b,bool reviewTraffic)
 {
  using(NavalPerformance.Measure(NavalPerfArea.CrewPassage))ReviewPair(a,b,reviewTraffic);
 }

 private void ReviewPair(int a,int b,bool reviewTraffic)
 {
  var first=bodies[a];var second=bodies[b];if(first==null||second==null)return;
  bool valid=crew[a]!=null&&crew[b]!=null&&crew[a].IsAlive()&&crew[b].IsAlive();
  bool pass=false;
  if(valid){
   float separation=(crew[a].transform.position-crew[b].transform.position).sqrMagnitude;
   if(separation>=20.25f&&!ignored[a,b])return;
   float distance=Mathf.Sqrt(separation);
   bool sameTeam=crew[a].IsPlayerTeam==crew[b].IsPlayerTeam;
   bool navigating=workers[a]!=null&&workers[a].Navigating||workers[b]!=null&&workers[b].Navigating;
   // In the destroyer's lower side aisles two full capsules cannot pass.
   // Keep the pass active until both bodies are clear, even when a task pauses.
   bool narrowPass=false;
   if(sameTeam&&hull.Definition.Destroyer&&distance<(ignored[a,b]?4.5f:3.5f)){
    Vector3 pa=hull.Root.InverseTransformPoint(crew[a].transform.position);
    Vector3 pb=hull.Root.InverseTransformPoint(crew[b].transform.position);
    narrowPass=pa.y<hull.Definition.WeatherDeck-.1f&&pb.y<hull.Definition.WeatherDeck-.1f
     &&Mathf.Abs(pa.y-pb.y)*hull.WorldScale<1f&&pa.x*pb.x>0
     &&Mathf.Min(Mathf.Abs(pa.x),Mathf.Abs(pb.x))*hull.WorldScale>2.5f;
   }
   if(narrowPass)pass=true;
   if(reviewTraffic&&!narrowPass&&sameTeam&&distance<3f&&workers[a]!=null&&workers[b]!=null){
    if(workers[a].WaitingForPassage&&!workers[b].Navigating)workers[b].RequestPassageYield(workers[a]);
    else if(workers[b].WaitingForPassage&&!workers[a].Navigating)workers[a].RequestPassageYield(workers[b]);
   }
   if(reviewTraffic&&!narrowPass&&sameTeam&&distance<4f&&workers[a]!=null&&workers[b]!=null&&workers[a].Navigating&&workers[b].Navigating&&Vector3.Dot(workers[a].TravelDirection,workers[b].TravelDirection)<-.2f){
    Vector3 pa=hull.Root.InverseTransformPoint(crew[a].transform.position-Vector3.up*workers[a].FootOffset),pb=hull.Root.InverseTransformPoint(crew[b].transform.position-Vector3.up*workers[b].FootOffset);
    if(NavalStairTraffic.SamePassage(new NavalWalkPoint(pa.x,pa.y,pa.z),new NavalWalkPoint(pb.x,pb.y,pb.z),hull.Definition)){
     int rankA=workers[a].PassagePriority,rankB=workers[b].PassagePriority;bool firstWins=rankA<rankB||rankA==rankB&&crew[a].GetInstanceID()<crew[b].GetInstanceID();workers[firstWins?b:a].WaitForCrewTraffic();
    }
   }
   if(distance<2.8f&&sameTeam&&(navigating||workers[a]!=null&&workers[a].Busy||workers[b]!=null&&workers[b].Busy))pass=true;
   if(distance<2.5f)pass=pass||NavalStairTraffic.FriendlyPass(crew[a].IsPlayerTeam==crew[b].IsPlayerTeam,InPassage(a),InPassage(b),distance);}
  // HoldPosition toggles controllers; reapply a live pass before each Move as well as on review.
  if(pass||ignored[a,b])ApplyPass(a,b,pass);
  ignored[a,b]=pass;ignored[b,a]=pass;
 }
 private void ApplyPass(int a,int b,bool pass){
  if(crew[a]!=null&&(shapes[a]==null||Time.time>=shapeRefreshAt[a])){shapes[a]=crew[a].GetComponentsInChildren<Collider>(true);shapeRefreshAt[a]=Time.time+1f;}
  if(crew[b]!=null&&(shapes[b]==null||Time.time>=shapeRefreshAt[b])){shapes[b]=crew[b].GetComponentsInChildren<Collider>(true);shapeRefreshAt[b]=Time.time+1f;}
  var first=bodies[a];var second=bodies[b];
  SetPass(first,second,pass);
  if(first!=null&&shapes[b]!=null)foreach(var c in shapes[b])if(c!=second)SetPass(first,c,pass);
  if(second!=null&&shapes[a]!=null)foreach(var c in shapes[a])if(c!=first)SetPass(second,c,pass);
 }
 private static void SetPass(Collider first,Collider second,bool pass)
 {
  // Re-enabling a controller can clear the native flag. Check it rather than
  // trusting the crew's desired state or rewriting a flag that already matches.
  if(first!=null&&second!=null&&first!=second&&Physics.GetIgnoreCollision(first,second)!=pass)
   Physics.IgnoreCollision(first,second,pass);
 }
 private void PrepareBody(CharacterController body)
 {
  if(!body.enabled)return;
  for(int i=0;i<bodies.Length;i++)if(bodies[i]==body)
  {
   Vector3 position=crew[i].transform.position;
   for(int j=0;j<bodies.Length;j++)
    if(i!=j&&(ignored[i,j]||crew[j]!=null&&(crew[j].transform.position-position).sqrMagnitude<20.25f))
     Pair(i,j,false);
   return;
  }
 }
 private void LateUpdate()
    {
        using(NavalPerformance.Measure(NavalPerfArea.PassageLate)) LateUpdateMeasured();
    }
    private void LateUpdateMeasured()
 {
  if(crew==null)return;
  for(int i=0;i<bodies.Length;i++)if(bodies[i]==null&&workers[i]!=null){if(!object.ReferenceEquals(bodies[i],null))owners.Remove(bodies[i]);bodies[i]=workers[i].BodyController;if(bodies[i]!=null)owners[bodies[i]]=this;}
  if(Time.unscaledTime<reviewAt)return;reviewAt=Time.unscaledTime+.1f;
  for(int i=0;i<bodies.Length;i++)for(int j=i+1;j<bodies.Length;j++)Pair(i,j,true);
 }
 public void RemoveSailor(int index)
 {
  var body=bodies[index];
  if(!object.ReferenceEquals(body,null))owners.Remove(body);
  if(body!=null)for(int i=0;i<bodies.Length;i++)
   if(i!=index&&ignored[index,i]&&bodies[i]!=null)ApplyPass(index,i,false);
  for(int i=0;i<bodies.Length;i++)ignored[index,i]=ignored[i,index]=false;
  bodies[index]=null;shapes[index]=null;shapeRefreshAt[index]=0f;
 }
 public void ResizeRoster(Character[] sailors,NavalRepairSailor[] repair)
 {
  int old=bodies.Length;
  crew=sailors;workers=repair;
  System.Array.Resize(ref bodies,sailors.Length);
  System.Array.Resize(ref shapes,sailors.Length);
  System.Array.Resize(ref shapeRefreshAt,sailors.Length);
  var expanded=new bool[sailors.Length,sailors.Length];
  for(int a=0;a<old;a++)for(int b=0;b<old;b++)expanded[a,b]=ignored[a,b];
  ignored=expanded;
 }
 private void OnDestroy()
 {
  if(bodies==null)return;
  for(int i=0;i<bodies.Length;i++)if(!object.ReferenceEquals(bodies[i],null)){owners.Remove(bodies[i]);if(bodies[i]!=null)for(int j=i+1;j<bodies.Length;j++)if(ignored[i,j]&&bodies[j]!=null)ApplyPass(i,j,false);}
 }
}
