using UnityEngine;
// One tactical owner per tick. Shore/collision recovery remains the final authority.
public sealed class NavalHelmDecision
{
 private readonly NavalCombatPosition combat=new NavalCombatPosition();
 private readonly NavalSternEvasion stern=new NavalSternEvasion();
 private readonly NavalFirePassMath firePass=new NavalFirePassMath();
 private int mode=-1,loggedPass;
 private float observedShot,nextTelemetry;
 private float heading;
 private bool initialized,mainAssistKnown,mainAssistLogged;
 public void Reset(){mode=-1;loggedPass=0;observedShot=nextTelemetry=0;initialized=mainAssistKnown=mainAssistLogged=false;combat.Reset();stern.Reset();firePass.Reset();}
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
   int port=guns.PortCombatWorking,starboard=guns.StarboardCombatWorking;
   int preferred=hull.Definition.SteamPowered?NavalSteamDamageControlTactics.HealthierSide(0,port,starboard,hull.LeftWater,hull.RightWater):0;
   float passMargin=hull.Definition.SteamPowered?72f:32f;
   float passDuration=hull.Definition.SteamPowered?18f:45f,quiet=hull.Definition.SteamPowered?8f:12f,separation=hull.Definition.SteamPowered?14f:20f;
   // Sailing ships retain the deliberate close pass. Steam ships use their
   // closed-loop ballistic stand-off instead; the old pass was the reason two
   // ironclads kept closing from 85 m until physical contact.
   if(!hull.Definition.SteamPowered&&(firePass.Phase>0||toward.magnitude<=clearance+passMargin)&&firePass.Step(dt,own.eulerAngles.y,bearing,toward.magnitude,clearance,port,starboard,guns.FiringSideAtShip(enemy),shot,guns.CanReachShipRange(enemy),preferred,passDuration,quiet,separation))
   {
    if(loggedPass==0){stern.Reset();combat.Reset();}
   float passHeading=firePass.Heading;
   if(firePass.Phase<3&&guns.MainBatteryHelmPriority)
   {
     passHeading=guns.MainBatteryHelmCourse(own.eulerAngles.y,bearing,firePass.Side);
   }
    desired=Quaternion.Euler(0,passHeading,0)*Vector3.forward;throttle=firePass.Throttle;
    if(loggedPass!=firePass.Phase){loggedPass=firePass.Phase;Debug.Log("[NavalPrototype] Fire pass: "+own.name+" phase="+loggedPass+"/"+firePass.PhaseName+" side="+firePass.Side+" distance="+toward.magnitude.ToString("F1")+" last crew shot="+guns.LastCrewShotAt.ToString("F1"));}
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
  bool mainAssist=mode==0&&guns.MainBatteryHelmPriority;
  if(!mainAssistKnown||mainAssist!=mainAssistLogged)
  {
   mainAssistKnown=true;mainAssistLogged=mainAssist;
   Debug.Log("[NavalPrototype] Main battery helm assist: ship="+own.name+" active="+mainAssist+" targetYaw="+NavalBroadsideMath.Delta(own.eulerAngles.y,Mathf.Atan2(toward.x,toward.z)*Mathf.Rad2Deg).ToString("F1")+" desiredHeading="+wanted.ToString("F1"));
  }
  if(hull.Definition.SteamPowered&&Time.time>=nextTelemetry)
  {
   nextTelemetry=Time.time+15f;
   Debug.Log("[NavalPrototype] Helm state: ship="+own.name+" heading="+own.eulerAngles.y.ToString("F1")+" wanted="+wanted.ToString("F1")+
    " error="+Mathf.DeltaAngle(own.eulerAngles.y,wanted).ToString("F1")+" targetYaw="+NavalBroadsideMath.Delta(own.eulerAngles.y,Mathf.Atan2(toward.x,toward.z)*Mathf.Rad2Deg).ToString("F1")+
    " distance="+toward.magnitude.ToString("F1")+" throttle="+throttle.ToString("F2")+" mainAssist="+mainAssist);
  }
  heading=NavalHelmIntentMath.Heading(heading,wanted,dt,mode==1?8:16);
  return Quaternion.Euler(0,heading,0)*Vector3.forward;
 }
}
