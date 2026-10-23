using UnityEngine;
// Stateful escape heading anchored at entry, before normal shore navigation.
public sealed class NavalSternEvasion
{
 private NavalSternEvasionMath plan;
 private bool active;
 private float baseHeading,counterHeading,bestError,progressAt,attemptAt,turnBudget;
 private int counterSide;
 public void Reset(){active=false;counterSide=0;if(plan!=null)plan.Reset();}
 public Vector3 Course(Transform own,NavalHull hull,NavalStations guns,Transform enemy,NavalRamDamage ram,bool retreat,float dt,Vector3 original,ref float throttle)
 {
  if(enemy==null||guns.AllGunsBroken||guns.RescueStop){Reset();return original;}
  if(plan==null)plan=new NavalSternEvasionMath(own.GetInstanceID());
  Vector3 toward=enemy.position-own.position;toward.y=0;
  Vector3 forward=own.forward;forward.y=0;forward.Normalize();Vector3 right=Vector3.Cross(Vector3.up,forward);
  float ahead=Vector3.Dot(toward,forward),side=Vector3.Dot(toward,right);
  var other=NavalStations.ForShip(enemy);float otherScale=other!=null?other.ShipScale:hull.WorldScale;
  float close=NavalPursuitMath.StandOff(hull.WorldScale,otherScale);
  bool stern=ahead< -Mathf.Max(2,toward.magnitude*.25f)&&toward.magnitude<close+55;
  bool contact=ram!=null&&ram.Opponent!=null&&ram.Opponent.transform==enemy&&Time.time-ram.LastContact<.5f;
  bool canFire=!retreat&&plan.CounterSide>0&&guns.FiringSideAtShip(enemy)==plan.CounterSide;
  bool progressing=false;
  if(!retreat&&counterSide>0)
  {
   float firingGoal=NavalBroadsideMath.Heading(Mathf.Atan2(toward.x,toward.z)*Mathf.Rad2Deg,counterSide);
   float error=Mathf.Abs(Mathf.DeltaAngle(own.eulerAngles.y,firingGoal));
   if(error<bestError-1){bestError=error;progressAt=Time.time;}
   progressing=error>8&&Time.time-progressAt<4&&Time.time-attemptAt<turnBudget;
  }
  bool engaged=plan.Step(dt,stern,contact,toward.magnitude<close+5,side,retreat,guns.PortWorking,guns.StarboardWorking,canFire||progressing);
  if(!engaged){active=false;counterSide=0;return original;}
  if(!active){active=true;Vector3 away=-toward.normalized;baseHeading=Mathf.Atan2(away.x,away.z)*Mathf.Rad2Deg;}
  throttle=1;
  float heading=baseHeading+plan.Offset;
  if(plan.CounterSide>0)
  {
   float bearing=Mathf.Atan2(toward.x,toward.z)*Mathf.Rad2Deg;
   float firingHeading=NavalBroadsideMath.Heading(bearing,plan.CounterSide);
   if(counterSide!=plan.CounterSide)
   {
    counterSide=plan.CounterSide;counterHeading=firingHeading;
    bestError=Mathf.Abs(Mathf.DeltaAngle(own.eulerAngles.y,firingHeading));
    progressAt=attemptAt=Time.time;
    turnBudget=NavalHelmIntentMath.TurnBudget(bestError,ram!=null?ram.Velocity.magnitude:0);
   }
   // Commit to a bounded firing pass. Track only small bearing changes instead
   // of continuously chasing the target round a complete circle.
   heading=counterHeading+Mathf.Clamp(Mathf.DeltaAngle(counterHeading,firingHeading),-20,20);
  }
  else counterSide=0;
  return Quaternion.Euler(0,heading,0)*Vector3.forward;
 }
}
