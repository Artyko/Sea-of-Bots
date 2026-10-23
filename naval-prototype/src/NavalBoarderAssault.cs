using UnityEngine;
public partial class NavalBoarder
{
 private float assaultRouteAt,assaultStalled;
 private Vector3 assaultGoal;
 private Character routedOpponent;
 private bool assaultMelee;
 private int assaultDetour;
 private bool TryAssaultFeet(Character target,NavalHull ship,out Vector3 feet)
 {
  var repair=target.GetComponent<NavalRepairSailor>();
  CharacterController body=target.GetComponentInChildren<CharacterController>();
  Vector3 foot=repair!=null?target.transform.position-Vector3.up*repair.FootOffset:body!=null?new Vector3(target.transform.position.x,body.bounds.min.y,target.transform.position.z):target.transform.position-Vector3.up*worker.FootOffset;
  RaycastHit hit;
  if(Physics.Raycast(foot+Vector3.up*.5f,Vector3.down,out hit,1.5f,PhysicsManager.GetEnvironmentLayerMask(),QueryTriggerInteraction.Ignore)&&hit.collider.transform.IsChildOf(ship.Root))
  {feet=ship.Root.InverseTransformPoint(hit.point);return true;}
  feet=Vector3.zero;return false;
 }
 private float AssaultTravelCost(Character target)
 {
  Vector3 start=current.Root.InverseTransformPoint(robot.transform.position-Vector3.up*worker.FootOffset),goal;
  if(!TryAssaultFeet(target,current,out goal))return float.PositiveInfinity;
  float cost=0;var previous=new NavalWalkPoint(start.x,start.y,start.z);
  foreach(var p in NavalSupplyRoutes.Plan(previous,new NavalWalkPoint(goal.x,goal.y,goal.z),current.Definition))
  {cost+=Vector3.Distance(new Vector3(previous.X,previous.Y,previous.Z),new Vector3(p.X,p.Y,p.Z))*current.WorldScale;previous=p;}
  return cost;
 }
 private void TickAssault(float dt,AIController ai)
 {
  CharacterController combatBody=worker.BodyController;
  if(combatBody!=null)combatBody.stepOffset=Mathf.Min(combatBody.stepOffset,.24f);
  Vector3 feet=current.Root.InverseTransformPoint(robot.transform.position-Vector3.up*worker.FootOffset),targetFeet;
  if(!TryAssaultFeet(opponent,current,out targetFeet)){opponent=null;return;}
  Vector3 delta=current.Root.TransformVector(targetFeet-feet);
  Vector3 hand=current.Root.TransformPoint(feet)+Vector3.up*1.1f;
  Vector3 sight=current.Root.TransformPoint(targetFeet)+Vector3.up*1.1f-hand;
  int hits=Physics.RaycastNonAlloc(hand,sight.normalized,jumpHits,sight.magnitude,PhysicsManager.GetEnvironmentLayerMask(),QueryTriggerInteraction.Ignore);
  bool clear=hits<jumpHits.Length;
  for(int i=0;i<hits;i++)if(jumpHits[i].collider!=null&&!jumpHits[i].collider.transform.IsChildOf(robot.transform)&&!jumpHits[i].collider.transform.IsChildOf(opponent.transform))clear=false;
  bool same=NavalAssaultMath.SameDeck(NavalAssaultMath.Floor(feet.y,current.Definition),NavalAssaultMath.Floor(targetFeet.y,current.Definition),current.WorldScale);
  if(same&&NavalAssaultMath.Melee(delta.magnitude,Vector3.Dot(delta,Vector3.up),clear,assaultMelee))
  {
   route.Clear();routedOpponent=null;assaultStalled=0;
   if(!assaultMelee){worker.BoardingWalk(false);robot.ActivateAI();assaultMelee=true;Report("melee engaged at floor "+targetFeet.y.ToString("F2"));}
   if(ai!=null){ai.SetTargetOverrides(false,false);if(ai.GetTargetOpponentCharacter()!=opponent&&SetOpponent!=null)SetOpponent.Invoke(ai,new object[]{opponent});}
   return;
  }
  assaultMelee=false;robot.DisableAI();ZeroVelocity();worker.BoardingWalk(true);
  if(routedOpponent!=opponent||route.Count==0||Time.time>=assaultRouteAt&&(targetFeet-assaultGoal).sqrMagnitude*current.WorldScale*current.WorldScale>2.25f)
  {
   route.Clear();routedOpponent=opponent;assaultGoal=targetFeet;assaultRouteAt=Time.time+1;Report("pursuing enemy at floor "+targetFeet.y.ToString("F2"));
   Vector3 goal=targetFeet;
   if(assaultDetour>0){float offset=(assaultDetour%2==0?1:-1)*.8f/current.WorldScale;goal.x=Mathf.Clamp(goal.x+offset,-2.4f,2.4f);}
   foreach(var p in NavalSupplyRoutes.Plan(new NavalWalkPoint(feet.x,feet.y,feet.z),new NavalWalkPoint(goal.x,goal.y,goal.z),current.Definition))route.Add(new Vector3(p.X,p.Y,p.Z));
  }
  if(route.Count==0)return;
  Vector3 before=robot.transform.position;MoveOnBoardingRoute(dt);
  float moved=Vector3.Distance(before,robot.transform.position);
  assaultStalled=moved<dt*.12f?assaultStalled+dt:0;
  if(assaultStalled>2.5f)
  {
   assaultStalled=0;assaultDetour=(assaultDetour+1)%4;routedOpponent=null;assaultRouteAt=0;
   // Back out of carriage pockets before retrying; a different final goal alone cannot free a trapped body.
   for(int n=0;n<8;n++)
   {
    float angle=(n+assaultDetour)*Mathf.PI*.25f;
    Vector3 retreat=feet+new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle))*(.9f/current.WorldScale);
    Vector3 from=current.Root.TransformPoint(feet),to=current.Root.TransformPoint(retreat);RaycastHit floorHit;
    if(!Physics.Raycast(to+Vector3.up*.3f,Vector3.down,out floorHit,.65f,PhysicsManager.GetEnvironmentLayerMask(),QueryTriggerInteraction.Ignore)||!floorHit.collider.transform.IsChildOf(current.Root)||Mathf.Abs(floorHit.point.y-from.y)>.3f||!SweepClear(from,to))continue;
    route.Insert(0,retreat);routedOpponent=opponent;assaultGoal=targetFeet;assaultRouteAt=Time.time+2;break;
   }
  }
 }
}
