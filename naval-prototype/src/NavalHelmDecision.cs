using UnityEngine;
// One tactical owner per tick. Shore/collision recovery remains the final authority.
public sealed class NavalHelmDecision
{
 private readonly NavalCombatPosition combat=new NavalCombatPosition();
 private readonly NavalSternEvasion stern=new NavalSternEvasion();
 private readonly NavalFirePassMath firePass=new NavalFirePassMath();
 private int mode=-1,loggedPass;
 private float observedShot;
 private float heading;
 private bool initialized;
 public void Reset(){mode=-1;loggedPass=0;observedShot=0;initialized=false;combat.Reset();stern.Reset();firePass.Reset();}
 public Vector3 Course(Transform own,NavalHull hull,NavalStations guns,Transform enemy,Vector3 enemyVelocity,float speed,float dt,bool retreat,bool targetRetreat,NavalRamDamage ram,ref float throttle)
 {
  int next=NavalHelmIntentMath.Mode(guns.RescueStop,guns.MustBoard,retreat||guns.CrewRepairRetreat||guns.AllGunsBroken&&guns.CanRestoreGun);
  if(next!=mode){initialized=false;loggedPass=0;observedShot=guns.LastCrewShotAt;mode=next;combat.Reset();stern.Reset();firePass.Reset();}
  Vector3 toward=enemy.position-own.position;toward.y=0;
  if(toward.sqrMagnitude<.001f)toward=own.forward;
  throttle=toward.magnitude<24?.35f:.8f;
  Vector3 desired;
  if(mode==3){throttle=0;desired=own.forward;}
  else if(mode==2){throttle=1;desired=toward;}
  else if(mode==1){
   throttle=1;desired=stern.Course(own,hull,guns,enemy,ram,true,dt,-toward,ref throttle);
  }else{
   NavalStations other=NavalStations.ForShip(enemy);
   float clearance=NavalPursuitMath.StandOff(hull.WorldScale,other!=null?other.ShipScale:hull.WorldScale);
   float bearing=Mathf.Atan2(toward.x,toward.z)*Mathf.Rad2Deg;
   bool shot=guns.LastCrewShotAt>observedShot;observedShot=guns.LastCrewShotAt;
   if((firePass.Phase>0||toward.magnitude<=clearance+32)&&firePass.Step(dt,own.eulerAngles.y,bearing,toward.magnitude,clearance,guns.PortWorking,guns.StarboardWorking,guns.FiringSideAtShip(enemy),shot,guns.CanReachShipRange(enemy)))
   {
    if(loggedPass==0){stern.Reset();combat.Reset();}
    desired=Quaternion.Euler(0,firePass.Heading,0)*Vector3.forward;throttle=firePass.Throttle;
    if(loggedPass!=firePass.Phase){loggedPass=firePass.Phase;Debug.Log("[NavalPrototype] Fire pass: "+own.name+" phase="+loggedPass+" side="+firePass.Side+" distance="+toward.magnitude.ToString("F1")+" last crew shot="+guns.LastCrewShotAt.ToString("F1"));}
   }
   else
   {
    if(loggedPass!=0){stern.Reset();combat.Reset();}loggedPass=0;
    desired=stern.Course(own,hull,guns,enemy,ram,false,dt,Vector3.zero,ref throttle);
    if(desired.sqrMagnitude<.001f)desired=combat.Course(own,hull,guns,enemy,enemyVelocity,speed,ref throttle,targetRetreat);
   }
  }
  if(!initialized){initialized=true;heading=own.eulerAngles.y;}
  float wanted=Mathf.Atan2(desired.x,desired.z)*Mathf.Rad2Deg;
  heading=NavalHelmIntentMath.Heading(heading,wanted,dt,mode==1?8:16);
  return Quaternion.Euler(0,heading,0)*Vector3.forward;
 }
}
