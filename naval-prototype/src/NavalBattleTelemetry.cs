using UnityEngine;
// One sampled CPU frame in sixty; log only every fifteen simulation seconds, no extra HUD.
[DefaultExecutionOrder(-1000)]
public sealed class NavalBattleTelemetry : MonoBehaviour
{
 private float elapsed;
 private NavalNativeFrameProfile nativeProfile;
 private NavalLoopFrameProfile loopProfile;
 private void OnDestroy(){if(nativeProfile!=null)nativeProfile.Dispose();if(loopProfile!=null)loopProfile.Dispose();}
 private int frames,fixedSteps;
 private readonly System.Collections.Generic.List<float> frameTimes=new System.Collections.Generic.List<float>(1024);
 private void FixedUpdate(){fixedSteps++;}
 private void Update()
 {
  var world=NavalTestWorld.Active;bool active=world!=null&&world.SimulationActive&&Time.timeScale>0;
  if(active&&world.ConvoyMission){if(nativeProfile==null)nativeProfile=new NavalNativeFrameProfile();nativeProfile.Capture();if(loopProfile==null)loopProfile=new NavalLoopFrameProfile();loopProfile.CaptureFrame();}
  if(active){elapsed+=Time.unscaledDeltaTime;frames++;frameTimes.Add(Time.unscaledDeltaTime*1000f);
   if(elapsed>=15f){
    frameTimes.Sort();
    float p95=frameTimes[Mathf.CeilToInt(frameTimes.Count*.95f)-1];
    var own=world.PlayerHull;var other=world.EnemyBoat!=null?world.EnemyBoat.GetComponent<NavalRamDamage>():null;
  Debug.Log("[NavalPrototype] Performance v467: era="+NavalSessionSettings.Era+" mission="+(world.AirRaidMission?"airraid":world.ConvoyMission?"convoy7":"duel")+" ships="+(own!=null?own.Definition.Name:"none")+"/"+(other!=null?other.Hull.Definition.Name:"none")+
     " fps="+(frames/elapsed).ToString("F1")+" fixed_step_ms="+(Time.fixedDeltaTime*1000f).ToString("F2")+" fixed_steps_per_frame="+((float)fixedSteps/Mathf.Max(1,frames)).ToString("F2")+" frame_ms="+(elapsed*1000f/Mathf.Max(1,frames)).ToString("F2")+" p95_ms="+p95.ToString("F2")+" cpu_ms avg/frame / max single call / max sampled frame (inclusive): "+NavalPerformance.AutoReport());
    if(!world.Spectating)Debug.Log("[NavalPrototype] Player support: foot="+world.PlayerFootPoint+" radius="+world.PlayerSupportRadius.ToString("F2")+" fallback_hits_interval="+world.ConsumePlayerSupportFallbacks()+" frame_cache_hits_interval="+world.ConsumePlayerSupportCacheHits());
    if(loopProfile!=null)Debug.Log("[NavalPrototype] Unity loop average ms/frame: "+loopProfile.Report());
    if(nativeProfile!=null)Debug.Log("[NavalPrototype] Native frame average ms: "+nativeProfile.Report());
    Debug.Log("[NavalPrototype] Support cache: "+NavalPhysicsQueries.ConsumeDiagnostics());
    if(world.ConvoyMission){foreach(var member in world.ConvoyHulls())LogShip(member);}
    else {LogShip(own);LogShip(other!=null?other.Hull:null);}
    elapsed=0;frames=0;fixedSteps=0;frameTimes.Clear();
   }
  }
  NavalPerformance.AutoFrame(active);
 }
 private static void LogShip(NavalHull hull)
 {
  if(hull==null)return;var controls=NavalStations.ForShip(hull.Root);if(controls==null)return;
  if(hull.Cargo!=null)Debug.Log("[NavalPrototype] Ammo stock: ship="+hull.Root.name+" "+hull.Cargo.AmmoDiagnostics);
  if(hull.Cargo!=null)Debug.Log("[NavalPrototype] Cargo physics: ship="+hull.Root.name+" "+hull.Cargo.CollisionDiagnostics);
  if(hull.PhysicsBody!=null)Debug.Log("[NavalPrototype] Compound shapes: ship="+hull.Root.name+" "+hull.PhysicsBody.ShapeDiagnostics);
  var steam=hull.Root.GetComponent<NavalSteamPlant>();if(steam!=null)Debug.Log("[NavalPrototype] Steam plant: ship="+hull.Root.name+" "+steam.Diagnostics);
  if(hull.Fire!=null)Debug.Log("[NavalPrototype] Fire visuals: ship="+hull.Root.name+" "+hull.Fire.VisualDiagnostics);
  if(hull.Definition.Destroyer)Debug.Log("[NavalPrototype] Open gun attack: ship="+hull.Root.name+" "+controls.GunAttackDiagnostics);
  if(controls.AirDefense!=null)Debug.Log("[NavalPrototype] AA state: ship="+hull.Root.name+controls.AirDefense.Diagnostics);
  if(controls.Torpedoes!=null)Debug.Log("[NavalPrototype] Torpedo battery: ship="+hull.Root.name+" "+controls.Torpedoes.Diagnostics);
  if(controls.HasMainBattery)Debug.Log("[NavalPrototype] Main battery state: ship="+hull.Root.name+" "+controls.MainBatteryDiagnostics);
  Debug.Log("[NavalPrototype] Combat state: ship="+hull.Root.name+" guns="+controls.WorkingGuns+"/"+controls.GunCount+" heel_inhibited="+controls.HeelInhibitedGuns+
   " ammo="+(hull.Cargo!=null?hull.Cargo.Total(0):0)+" boards="+(hull.Cargo!=null?hull.Cargo.Total(1):0)+" kits="+controls.AvailableCannonKits+" canRestore="+controls.CanRestoreGun+" mustBoard="+controls.MustBoard+
   " approach="+controls.LastAttackApproach+" rescue="+controls.RescueStop+" sunk="+hull.Sunk);
  Debug.Log("[NavalPrototype] Downflood: ship="+hull.Root.name+" water="+hull.Volume.ToString("F1")+
   " accepted_m3s="+hull.OpeningInflow.ToString("F3")+" pressure_m3s="+hull.OpeningPressure.ToString("F3")+
   " rim_head_m="+hull.SubmergedRimHead.ToString("F3")+" roll="+NavalHullStability.Roll(hull.Root).ToString("F1")+
   " pitch_deg="+(Mathf.Atan2(-hull.Root.forward.y,hull.Root.up.y)*Mathf.Rad2Deg).ToString("F1"));
 }
 private void OnDisable(){NavalPerformance.AutoFrame(false);NavalPerformance.AutoReport();}
}
