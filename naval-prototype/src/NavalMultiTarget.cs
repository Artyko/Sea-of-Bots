using UnityEngine;
public partial class NavalStations{
 public bool CanEngageAnyShip(int station,Transform preferred){
  if(CanBearOnShip(station,preferred))return true;
  var world=NavalTestWorld.Active;if(world==null||!world.ConvoyMission||station<1||station>GunCount)return false;
  Vector3 velocity=Vector3.zero;return SelectWeaponTarget(station-1,-1,null,ref velocity)!=null;
 }
 private Transform SelectWeaponTarget(int gun,int heavySlot,Transform fallback,ref Vector3 velocity){
  var world=NavalTestWorld.Active;if(world==null||!world.ConvoyMission)return fallback;
  float range=heavySlot>=0?NavalCapitalLayout.Range:hull.Definition.GunRange,best=float.PositiveInfinity;Transform chosen=null;
  foreach(var candidate in world.ConvoyHulls()){
   if(candidate==null||candidate==hull||candidate.Sunk||candidate.SinkingDanger)continue;
   var controls=ForShip(candidate.Root);if(controls==null||controls.HostileToPlayer==HostileToPlayer)continue;
   Vector3 delta=candidate.Root.position-boat.position;float distance=delta.sqrMagnitude;if(distance>range*range)continue;
   bool bears=heavySlot>=0?NavalCapitalLayout.Bears(heavySlot,Mathf.Atan2(boat.InverseTransformDirection(delta).x,boat.InverseTransformDirection(delta).z)*Mathf.Rad2Deg):CanBearOnShip(gun+1,candidate.Root);
   if(bears&&distance<best){chosen=candidate.Root;best=distance;}
  }
  if(chosen!=null)velocity=world.FleetVelocity(chosen);return chosen;
 }
}
