using UnityEngine;
public sealed class NavalTorpedoTactics {
 public string Phase="inactive";
 private float withdrawUntil;private int side=1;private bool attackSideChosen;private float seenLaunch;private float finishSalvoAt;private bool closingForLaunch=true;
 public bool HoldingLaunchCourse {get{return Phase=="launch window";}}
 public bool Course(Transform own,NavalHull h,NavalStations stations,Transform target,Vector3 velocity,bool retreat,ref float throttle,out Vector3 course){Phase="inactive";course=own.forward;var battery=stations.Torpedoes;if(battery==null||!battery.HasCombatPotential||retreat||h.CombatCritical||stations.RescueStop)return false;Vector3 delta=target.position-own.position;delta.y=0;float distance=delta.magnitude;var world=NavalTestWorld.Active;bool convoy=world!=null&&world.ConvoyMission;
  if(battery.LastLaunch>seenLaunch){
   seenLaunch=battery.LastLaunch;
   if(finishSalvoAt<=0)finishSalvoAt=Time.time+(battery.SalvoStyle==NavalTorpedoSalvoMath.SequentialFan?5f:3f);
   if(battery.SalvoStyle==NavalTorpedoSalvoMath.LongSingle)finishSalvoAt=Time.time;
  }
  if(finishSalvoAt>0&&(Time.time>=finishSalvoAt||battery.SalvoStyle==NavalTorpedoSalvoMath.SequentialFan&&battery.AttackShots>=4)){
   finishSalvoAt=0;withdrawUntil=Time.time+(convoy?12f:22f);closingForLaunch=true;attackSideChosen=false;battery.LaunchWindowUntil=0;
  }
  if(Time.time<withdrawUntil){Phase="withdraw after launch";throttle=1;course=Quaternion.Euler(0,side*35,0)*(-delta);return true;}
  if(!attackSideChosen&&Time.time>=withdrawUntil){attackSideChosen=true;battery.StartAttack();side=Vector3.Cross(own.forward,delta).y<0?-1:1;var mission=NavalTestWorld.Active;int formation=mission!=null&&mission.ConvoyMission?mission.InterceptorLane(own):0;if(Mathf.Abs(formation)==1)side=formation;battery.SalvoStyle=NavalTorpedoSalvoMath.AttackStyle(Random.value,battery.Rounds,distance);battery.PreferredLaunchDistance=NavalTorpedoSalvoMath.LaunchDistance(battery.SalvoStyle,Random.value);
   Debug.Log("[NavalPrototype] Torpedo attack plan: ship="+own.name+" style="+battery.SalvoStyle+" distance="+battery.PreferredLaunchDistance.ToString("F0")+" side="+side);}
  int lane=world!=null&&world.ConvoyMission?world.InterceptorLane(own):0;
  float lead;if(!NavalTorpedoMath.Lead(delta.x,delta.z,velocity.x,velocity.z,out lead))lead=distance/NavalTorpedoMath.Speed;
  Vector3 intercept=delta+velocity*lead;intercept.y=0;
  // Separate approach points along and across the convoy instead of converging on one hull centre.
  if(convoy&&distance>450f){
   float flank=lane==-1?-280f:lane==1?280f:420f;
   intercept+=(target.right*flank+target.forward*(lane==2?-260f:lane==-1?180f:-120f))*Mathf.Clamp01((distance-450f)/550f);
  }
  // On convoy passes, keep closing until the ship is inside the actual launch
  // envelope. Turning broadside too early lets the moving convoy pull away.
  if(convoy){
   float launchDistance=battery.PreferredLaunchDistance;
   if(distance>launchDistance+40f)closingForLaunch=true;
   else if(distance<=launchDistance)closingForLaunch=false;
   if(closingForLaunch){Phase="reapproach";battery.LaunchWindowUntil=0;throttle=1;course=Quaternion.Euler(0,side*12f,0)*intercept;return true;}
  }
  if(distance>battery.PreferredLaunchDistance+180){Phase="approach";throttle=1;course=Quaternion.Euler(0,side*(Mathf.Abs(lane)==1?33:22),0)*intercept;return true;}
  if(distance<150){Phase="too close";closingForLaunch=true;throttle=1;course=-delta;withdrawUntil=Time.time+12;return true;}
  Phase="launch window";throttle=battery.SalvoStyle==NavalTorpedoSalvoMath.LongSingle?.72f:.85f;course=Quaternion.Euler(0,side*85,0)*intercept;float yaw=Mathf.Abs(Mathf.DeltaAngle(own.eulerAngles.y,Mathf.Atan2(intercept.x,intercept.z)*Mathf.Rad2Deg));if(distance<=battery.PreferredLaunchDistance+40&&yaw>=55&&yaw<=125)battery.LaunchWindowUntil=Time.time+.5f;return true;
 }
}
// Only visible wakes enter memory, with a reaction delay. Shore avoidance retains final authority.
public sealed class NavalTorpedoAvoidance {
 public string Phase="no visible threat";
 private static readonly float[] EvasionTurns={0f,90f,-90f,180f};
 private static readonly float[] EvasionThrottle={1f,.35f};
 private readonly RaycastHit[] visibility=new RaycastHit[64];
 private bool Visible(Vector3 observer,Vector3 trace,Transform own){Vector3 delta=trace-observer;int count=Physics.RaycastNonAlloc(observer,delta.normalized,visibility,delta.magnitude,PhysicsManager.GetEnvironmentLayerMask(),QueryTriggerInteraction.Ignore);if(count==visibility.Length)return false;for(int i=0;i<count;i++)if(visibility[i].collider!=null&&!visibility[i].collider.transform.IsChildOf(own))return false;return true;}
 private bool Spotted(NavalTorpedo torpedo,Transform own,Vector3 trace)
 {
  float range=torpedo.DetectionRange;
  Vector3 offset=trace-own.position;offset.y=0;
  if(offset.sqrMagnitude<=range*range&&Visible(own.position+Vector3.up*6,trace,own))return true;
  var world=NavalTestWorld.Active;
  if(world==null||!world.ConvoyMission)return false;
  var controls=NavalStations.ForShip(own);
  if(controls==null)return false;
  for(int i=0;i<NavalPhysicsQueries.HullSlotCount;i++)
  {
   var ally=NavalPhysicsQueries.HullAtSlot(i);
   if(ally==null||ally.Sunk||ally.Root==own||ally.Root==torpedo.Source)continue;
   var allyControls=NavalStations.ForShip(ally.Root);
   if(allyControls==null||allyControls.HostileToPlayer!=controls.HostileToPlayer)continue;
   offset=trace-ally.Root.position;offset.y=0;
   if(offset.sqrMagnitude<=range*range&&Visible(ally.Root.position+Vector3.up*6,trace,ally.Root))return true;
  }
  return false;
 }
 private NavalTorpedo observed;private float noticedAt,reviewAt,holdUntil;private Vector3 held;private float heldThrottle=1;
 public bool Course(Transform own,NavalHull hull,float speed,ref float throttle,out Vector3 course){course=own.forward;if(Time.time<reviewAt){if(Time.time<holdUntil){course=held;throttle=heldThrottle;return true;}return false;}reviewAt=Time.time+.25f;
  NavalTorpedo threat=null;float soon=float.PositiveInfinity;
  foreach(var torpedo in NavalTorpedo.Active){if(torpedo==null||torpedo.Source==own)continue;
   var source=NavalStations.ForShip(torpedo.Source);var defender=NavalStations.ForShip(own);
   if(source!=null&&defender!=null&&source.HostileToPlayer==defender.HostileToPlayer)continue;Vector3 offset=torpedo.transform.position-own.position;offset.y=0;if(offset.sqrMagnitude>850f*850f)continue;
   Vector3 trace=new Vector3(torpedo.transform.position.x,torpedo.transform.position.y+NavalTorpedoMath.Depth+.1f,torpedo.transform.position.z);
   Vector3 relative=torpedo.Velocity-own.forward*speed;float t;if(NavalTorpedoMath.Threat(offset.x,offset.z,relative.x,relative.z,hull.Definition.HullBeam*.5f+8,out t)&&t<soon&&Spotted(torpedo,own,trace)){threat=torpedo;soon=t;}
  }
  if(threat==null){if(Time.time<holdUntil){course=held;throttle=heldThrottle;return true;}Phase="no visible threat";observed=null;return false;}if(observed!=threat){Phase="reaction delay";observed=threat;noticedAt=Time.time;return false;}if(Time.time-noticedAt<(NavalTestWorld.Active!=null&&NavalTestWorld.Active.ConvoyMission?.6f:1.2f))return false;
  Vector3 incoming=-threat.Velocity;incoming.y=0;float best=-1;Vector3 bestCourse=own.forward;float bestThrottle=1;
  foreach(float turn in EvasionTurns)foreach(float power in EvasionThrottle){
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
