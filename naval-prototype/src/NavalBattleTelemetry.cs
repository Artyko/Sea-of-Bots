using UnityEngine;
// One sampled CPU frame in sixty; log only every fifteen simulation seconds, no extra HUD.
[DefaultExecutionOrder(-1000)]
public sealed class NavalBattleTelemetry : MonoBehaviour
{
 private float elapsed;
 private int frames;
 private void Update()
 {
  var world=NavalTestWorld.Active;bool active=world!=null&&world.SimulationActive&&Time.timeScale>0;
  if(active){elapsed+=Time.unscaledDeltaTime;frames++;
   if(elapsed>=15f){
    var own=world.PlayerHull;var other=world.EnemyBoat!=null?world.EnemyBoat.GetComponent<NavalRamDamage>():null;
    Debug.Log("[NavalPrototype] Performance v227: ships="+(own!=null?own.Definition.Name:"none")+"/"+(other!=null?other.Hull.Definition.Name:"none")+
     " fps="+(frames/elapsed).ToString("F1")+" frame_ms="+(elapsed*1000f/Mathf.Max(1,frames)).ToString("F2")+" cpu_ms avg/max inclusive: "+NavalPerformance.AutoReport());
    if(!world.Spectating)Debug.Log("[NavalPrototype] Player support: foot="+world.PlayerFootPoint+" radius="+world.PlayerSupportRadius.ToString("F2")+" fallback_hits="+world.PlayerSupportFallbacks);
    LogShip(own);LogShip(other!=null?other.Hull:null);
    elapsed=0;frames=0;
   }
  }
  NavalPerformance.AutoFrame(active);
 }
 private static void LogShip(NavalHull hull)
 {
  if(hull==null)return;var controls=hull.Root.GetComponent<NavalStations>();if(controls==null)return;
  if(hull.Cargo!=null)Debug.Log("[NavalPrototype] Cargo physics: ship="+hull.Root.name+" "+hull.Cargo.CollisionDiagnostics);
  Debug.Log("[NavalPrototype] Combat state: ship="+hull.Root.name+" guns="+controls.WorkingGuns+"/"+controls.GunCount+
   " ammo="+(hull.Cargo!=null?hull.Cargo.Total(0):0)+" boards="+(hull.Cargo!=null?hull.Cargo.Total(1):0)+" kits="+controls.AvailableCannonKits+" canRestore="+controls.CanRestoreGun+" mustBoard="+controls.MustBoard+
   " approach="+controls.LastAttackApproach+" rescue="+controls.RescueStop+" sunk="+hull.Sunk);
  Debug.Log("[NavalPrototype] Downflood: ship="+hull.Root.name+" water="+hull.Volume.ToString("F1")+
   " accepted_m3s="+hull.OpeningInflow.ToString("F3")+" pressure_m3s="+hull.OpeningPressure.ToString("F3")+
   " rim_head_m="+hull.SubmergedRimHead.ToString("F3")+" roll="+NavalHullStability.Roll(hull.Root).ToString("F1")+
   " pitch_deg="+(Mathf.Atan2(-hull.Root.forward.y,hull.Root.up.y)*Mathf.Rad2Deg).ToString("F1"));
 }
 private void OnDisable(){NavalPerformance.AutoFrame(false);NavalPerformance.AutoReport();}
}
