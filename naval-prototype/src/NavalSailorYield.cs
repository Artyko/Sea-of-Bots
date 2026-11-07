using UnityEngine;
public partial class NavalRepairSailor {
 private float yieldUntil,yieldRetry;private Vector3 yieldPoint;
 public bool Yielding {get{return Time.time<yieldUntil;}}
 public bool WaitingForPassage {get{return Navigating&&stuck>.25f;}}
 public bool RequestPassageYield(NavalRepairSailor approaching){
  if(approaching==null||approaching==this||Yielding||Time.time<yieldRetry||Busy&&!ReturningWithoutWork&&!changingPost&&!CanYieldWorkingPassage(approaching)||CapitalReserved||stranded||Rescuing||Defending||Assaulting||robot==null||!robot.IsAlive())return false;
  if(approaching.hull!=hull||approaching.robot==null||approaching.robot.IsPlayerTeam!=robot.IsPlayerTeam)return false;
  if(boat.TransformVector(feet-approaching.feet).sqrMagnitude>3f*3f||Mathf.Abs(feet.y-approaching.feet.y)*hull.WorldScale>.8f)return false;
  var controls=NavalStations.ForShip(boat);if(controls!=null&&(home-controls.Post(0)).sqrMagnitude<.01f)return false;
  yieldRetry=Time.time+.75f;
  Vector3 away=feet-approaching.feet;away.y=0;if(away.sqrMagnitude<.001f)away=Vector3.right;away.Normalize();
  for(int side=0;side<8;side++){
   float angle=side==0?90:side==1?-90:side==2?45:side==3?-45:side==4?135:side==5?-135:side==6?180:0;
   Vector3 candidate=feet+Quaternion.Euler(0,angle,0)*away*(1.9f/hull.WorldScale);
   if(approaching.route.Count>0){
    Vector3 line=approaching.route[0]-approaching.feet;line.y=0;float length=line.magnitude;
    if(length>.001f){Vector3 direction=line/length;Vector3 nearest=approaching.feet+direction*Mathf.Clamp(Vector3.Dot(candidate-approaching.feet,direction),0,length);
     if(boat.TransformVector(candidate-nearest).magnitude<NavigationRadius+approaching.NavigationRadius+.1f)continue;}
   }
   if(!InsideWalkingDeck(candidate)||!Supported(candidate)||!ClearBody(candidate)||!NavigationWalkClear(feet,candidate))continue;
   yieldPoint=candidate;yieldUntil=Time.time+1.8f;yieldRetry=Time.time+5f;
   Debug.Log("[NavalPrototype] Crew yielding passage: ship="+boat.name+" sailor="+robot.GetInstanceID()+" for="+approaching.robot.GetInstanceID()+" point="+candidate);return true;
  }
  return false;
 }
 private bool CanYieldWorkingPassage(NavalRepairSailor approaching){
  // Keep the current job and route while one of two approaching workers steps aside.
  if(approaching==null||!Navigating||approaching.robot==null||robot==null)return false;
  if(PassagePriority!=approaching.PassagePriority)return PassagePriority>approaching.PassagePriority;
  return robot.GetInstanceID()>approaching.robot.GetInstanceID();
 }
 private bool TickPassageYield(float dt){
  if(!Yielding)return false;

  robot.DisableAI();Move(yieldPoint,dt,true);
  Walking(boat.TransformVector(yieldPoint-feet).sqrMagnitude>.04f,dt);return true;
 }
}
