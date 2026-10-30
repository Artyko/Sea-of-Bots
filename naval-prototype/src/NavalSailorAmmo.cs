using UnityEngine;
public partial class NavalRepairSailor {
 public bool DeliveringAmmo {get{return ammoTarget!=null;}}
 private NavalCargoCrate ammoTarget,ammoSource;private int carriedAmmo;private float ammoRetry,ammoHandling;
 public int CarriedAmmo {get{return robot!=null&&robot.IsAlive()?carriedAmmo:0;}}
 private void CancelAmmo(){if(hull!=null&&hull.Cargo!=null)hull.Cargo.ReleaseRunner(this,ammoTarget);ammoTarget=ammoSource=null;ammoHandling=0;}
 private int ammoApproach;
 private bool PlanAmmoPoint(NavalCargoCrate crate){
  for(int i=ammoApproach;i<5;i++){
   Vector3 point=crate.Pickup+new Vector3(0,0,(i==0?0:i%2==1?1:-1)*((i+1)/2)*.35f/hull.WorldScale);
   if(crate.Gun<0)point=CargoApproach(crate.Pickup,i==0?0:i%2==1?1:2);
   if(!Supported(point)||!ClearBody(point))continue;
   ammoApproach=i;route.Clear();stuck=0;PlanTo(point);return true;
  }return false;
 }
 private bool AmmoSurvivalAllows(){
  bool repairer=false,firefighter=false;
  foreach(var worker in hull.RepairWorkers){
   if(worker==null||worker==this||worker.robot==null||!worker.robot.IsAlive()||worker.Stranded||worker.Rescuing)continue;
   if(worker.RepairingLeak)repairer=true;if(worker.FightingFire)firefighter=true;
  }
  bool repairsCovered=!hull.HasRepairDemand||repairer;
  bool pumpCovered=hull.Bilge==null||!hull.Bilge.WantsOperator||hull.Bilge.CrewOperating||hull.Bilge.PlayerUsing;
  bool fireCovered=hull.Fire==null||!hull.Fire.HasFire||firefighter;
  return NavalAmmoPriority.CanSupplyWhenCovered(repairsCovered,pumpCovered,fireCovered);
 }
 private bool TickAmmo(float dt,bool allowed){
 var cargo=hull.Cargo;if(cargo==null)return false;
 // Once a heavy shell has left the protected magazine, finish the short trip
 // to the gun. Dropping the reservation here made a fetched load disappear
 // whenever flooding briefly crossed the critical threshold.
 if(ammoTarget!=null&&carriedAmmo==0&&!AmmoSurvivalAllows()){
  CancelAmmo();BeginReturn();return false;
 }
 if(ammoTarget==null){
 if(!allowed||Busy&&!ReturningWithoutWork||Time.time<ammoRetry||!cargo.CanAssignRunner||!AmmoSurvivalAllows())return false;
 var target=cargo.EmptyGun();NavalCargoCrate source=null;float nearest=float.PositiveInfinity;
 int targetRounds=target!=null?cargo.AmmoAtGun(target.Gun):0;
 if(target!=null)foreach(var candidate in cargo.Crates){
  // Never rob another gun's ready-use locker.  Every delivery starts in the
  // protected hold, so two simultaneous runners cannot make the battery
  // oscillate ammunition between port and starboard positions.
  if(candidate.Kind!=0||candidate.Gun>=0||candidate.Count<=0)continue;
  float cost=SupportTravelCost(candidate.Pickup);if(cost<nearest){nearest=cost;source=candidate;}
 }if(target==null||source==null&&carriedAmmo==0)return false;
 Debug.Log("[NavalPrototype] Ammo run assigned: sailor="+robot.GetInstanceID()+" ship="+boat.name+" gun="+target.Gun+" rounds="+targetRounds);
 // Release the previous duty before registering this runner. Release() also
 // clears an old ammo job; doing it after AssignRunner removed the runner from
 // the HashSet while leaving the gun reserved, so telemetry showed runners=0
 // and several sailors could be dispatched into the same route.
 Release();route.Clear();returning=changingPost=false;
 if(!cargo.AssignRunner(this,target))return false;
 ammoTarget=target;ammoSource=source;ammoHandling=0;ammoApproach=0;if(!PlanAmmoPoint(carriedAmmo>0?target:source)){CancelAmmo();ammoRetry=Time.time+5f;BeginReturn();return true;}
 }
 robot.DisableAI();
 if(carriedAmmo==0&&(ammoSource==null||ammoSource.Count<=0)){CancelAmmo();BeginReturn();return true;}
 if(route.Count>0){var result=FollowRoute(dt,4f);if(result==RouteResult.Blocked){ammoApproach++;if(!PlanAmmoPoint(carriedAmmo>0?ammoTarget:ammoSource)){ammoRetry=Time.time+5f;CancelAmmo();BeginReturn();}}
 if(result==RouteResult.Fell){stranded=true;carriedAmmo=0;CancelAmmo();route.Clear();}return true;}
 HoldPosition();ammoHandling+=dt;if(ammoHandling<1f)return true;ammoHandling=0;
 if(carriedAmmo==0){carriedAmmo=ammoSource.Take(Mathf.Min(Mathf.Min(5,20-cargo.AmmoAtGun(ammoTarget.Gun)),ammoSource.Count));if(carriedAmmo==0){CancelAmmo();BeginReturn();return true;}
 Debug.Log("[NavalPrototype] Ammo fetched: sailor="+robot.GetInstanceID()+" ship="+boat.name+" rounds="+carriedAmmo);
 if(cannonPartsVisual==null){cannonPartsVisual=gameObject.AddComponent<NavalCannonPartsVisual>();cannonPartsVisual.Initialize(model,footOffset);}cannonPartsVisual.Pose(1);
 ammoApproach=0;if(!PlanAmmoPoint(ammoTarget)){ammoRetry=Time.time+5f;CancelAmmo();BeginReturn();}
 }else {int delivered=cargo.Deliver(ammoTarget.Gun,carriedAmmo);Debug.Log("[NavalPrototype] Ammo delivered: sailor="+robot.GetInstanceID()+" ship="+boat.name+" gun="+ammoTarget.Gun+" rounds="+delivered);carriedAmmo-=delivered;if(cannonPartsVisual!=null)cannonPartsVisual.Pose(0);CancelAmmo();BeginReturn();}
 return true;
 }
}
