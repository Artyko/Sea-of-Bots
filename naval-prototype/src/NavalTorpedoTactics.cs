using UnityEngine;
public sealed class NavalTorpedoTactics {
 public string Phase="inactive";
 private float withdrawUntil;private int side=1;private bool attackSideChosen;private float seenLaunch;private float finishSalvoAt;
 public bool HoldingLaunchCourse {get{return Phase=="launch window";}}
 public bool Course(Transform own,NavalHull h,NavalStations stations,Transform target,Vector3 velocity,bool retreat,ref float throttle,out Vector3 course){Phase="inactive";course=own.forward;var battery=stations.Torpedoes;if(battery==null||!battery.HasCombatPotential||retreat||h.CombatCritical||stations.RescueStop)return false;Vector3 delta=target.position-own.position;delta.y=0;float distance=delta.magnitude;
  if(battery.LastLaunch>seenLaunch){
   seenLaunch=battery.LastLaunch;
   if(finishSalvoAt<=0)finishSalvoAt=Time.time+(battery.SalvoStyle==NavalTorpedoSalvoMath.SequentialFan?5f:3f);
   if(battery.SalvoStyle==NavalTorpedoSalvoMath.LongSingle)finishSalvoAt=Time.time;
  }
  if(finishSalvoAt>0&&(Time.time>=finishSalvoAt||battery.SalvoStyle==NavalTorpedoSalvoMath.SequentialFan&&battery.AttackShots>=4)){
   finishSalvoAt=0;withdrawUntil=Time.time+22;attackSideChosen=false;battery.LaunchWindowUntil=0;
  }
  if(Time.time<withdrawUntil){Phase="withdraw after launch";throttle=1;course=Quaternion.Euler(0,side*35,0)*(-delta);return true;}
  if(!attackSideChosen&&Time.time>=withdrawUntil){attackSideChosen=true;battery.StartAttack();side=Vector3.Cross(own.forward,delta).y<0?-1:1;battery.SalvoStyle=NavalTorpedoSalvoMath.AttackStyle(Random.value,battery.Rounds,distance);battery.PreferredLaunchDistance=NavalTorpedoSalvoMath.LaunchDistance(battery.SalvoStyle,Random.value);Debug.Log("[NavalPrototype] Torpedo attack plan: ship="+own.name+" style="+battery.SalvoStyle+" distance="+battery.PreferredLaunchDistance.ToString("F0")+" side="+side);}
  float lead;if(!NavalTorpedoMath.Lead(delta.x,delta.z,velocity.x,velocity.z,out lead))lead=distance/NavalTorpedoMath.Speed;
  Vector3 intercept=delta+velocity*lead;intercept.y=0;
  if(distance>battery.PreferredLaunchDistance+180){Phase="approach";throttle=1;course=Quaternion.Euler(0,side*22,0)*intercept;return true;}
  if(distance<150){Phase="too close";throttle=1;course=-delta;withdrawUntil=Time.time+12;return true;}
  Phase="launch window";throttle=battery.SalvoStyle==NavalTorpedoSalvoMath.LongSingle?.72f:.85f;course=Quaternion.Euler(0,side*85,0)*intercept;float yaw=Mathf.Abs(Mathf.DeltaAngle(own.eulerAngles.y,Mathf.Atan2(intercept.x,intercept.z)*Mathf.Rad2Deg));if(distance<=battery.PreferredLaunchDistance+40&&yaw>=55&&yaw<=125)battery.LaunchWindowUntil=Time.time+.5f;return true;
 }
}
// Only visible wakes enter memory, with a reaction delay. Shore avoidance retains final authority.
public sealed class NavalTorpedoAvoidance {
 public string Phase="no visible threat";
 private readonly RaycastHit[] visibility=new RaycastHit[64];
 private bool Visible(Vector3 observer,Vector3 trace,Transform own){Vector3 delta=trace-observer;int count=Physics.RaycastNonAlloc(observer,delta.normalized,visibility,delta.magnitude,PhysicsManager.GetEnvironmentLayerMask(),QueryTriggerInteraction.Ignore);if(count==visibility.Length)return false;for(int i=0;i<count;i++)if(visibility[i].collider!=null&&!visibility[i].collider.transform.IsChildOf(own))return false;return true;}
 private NavalTorpedo observed;private float noticedAt,reviewAt,holdUntil;private Vector3 held;private float heldThrottle=1;
 public bool Course(Transform own,NavalHull hull,float speed,ref float throttle,out Vector3 course){course=own.forward;if(Time.time<reviewAt){if(Time.time<holdUntil){course=held;throttle=heldThrottle;return true;}return false;}reviewAt=Time.time+.25f;
  NavalTorpedo threat=null;float soon=float.PositiveInfinity;
  foreach(var torpedo in NavalTorpedo.Active){if(torpedo==null||torpedo.Source==own)continue;Vector3 offset=torpedo.transform.position-own.position;offset.y=0;if(offset.sqrMagnitude>torpedo.DetectionRange*torpedo.DetectionRange||Vector3.Angle(own.forward,offset)>150)continue;
   Vector3 observer=own.position+Vector3.up*6,trace=new Vector3(torpedo.transform.position.x,torpedo.transform.position.y+NavalTorpedoMath.Depth+.1f,torpedo.transform.position.z);
   if(!Visible(observer,trace,own))continue;
   Vector3 relative=torpedo.Velocity-own.forward*speed;float t;if(NavalTorpedoMath.Threat(offset.x,offset.z,relative.x,relative.z,hull.Definition.HullBeam*.5f+8,out t)&&t<soon){threat=torpedo;soon=t;}
  }
  if(threat==null){if(Time.time<holdUntil){course=held;throttle=heldThrottle;return true;}Phase="no visible threat";observed=null;return false;}if(observed!=threat){Phase="reaction delay";observed=threat;noticedAt=Time.time;return false;}if(Time.time-noticedAt<1.2f)return false;
  Vector3 incoming=-threat.Velocity;incoming.y=0;float best=-1;Vector3 bestCourse=own.forward;float bestThrottle=1;
  foreach(float turn in new[]{0f,90f,-90f,180f})foreach(float power in new[]{1f,.35f}){
   Vector3 candidate=Quaternion.Euler(0,turn,0)*incoming.normalized;
   if(NavalWorldMap.Active!=null&&NavalWorldMap.Active.ShoreClearance(own.position,candidate,own,hull.Definition.HullLength+30)<hull.Definition.HullLength*.5f+12)continue;
   float angle=own.eulerAngles.y,wanted=Mathf.Atan2(candidate.x,candidate.z)*Mathf.Rad2Deg,min=float.PositiveInfinity;Vector3 pos=own.position;
   float rate=Mathf.Max(1f,Mathf.Abs(NavalRigMath.RudderRate(speed,1,hull.Definition.SteeringRateScale)));
   for(float t=.5f;t<=Mathf.Min(25,soon+4);t+=.5f){angle=Mathf.MoveTowardsAngle(angle,wanted,rate*.5f);pos+=Quaternion.Euler(0,angle,0)*Vector3.forward*(speed*power*.5f);Vector3 offset=threat.transform.position+threat.Velocity*t-pos;offset.y=0;min=Mathf.Min(min,offset.sqrMagnitude);}
   float score=Mathf.Sqrt(min)-Mathf.Abs(Mathf.DeltaAngle(own.eulerAngles.y,wanted))*.03f;if(score>best){best=score;bestCourse=candidate;bestThrottle=power;}
  }
  Phase="evasive maneuver";held=bestCourse;heldThrottle=bestThrottle;holdUntil=Time.time+2;throttle=bestThrottle;course=held;throttle=heldThrottle;return true;
 }
}
