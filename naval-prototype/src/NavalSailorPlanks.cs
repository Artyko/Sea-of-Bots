using UnityEngine;
public partial class NavalRepairSailor
{
 private NavalPlankSupply plankStock;
 private readonly NavalPlankBundle planks=new NavalPlankBundle();
 private int plankStage,plankReplans;
 private float plankPickupTime,plankRetryAt;
 private NavalCannonPartsVisual plankVisual;
 private bool NeedsPlanks(NavalHullVoxel cell){return cell!=null&&!cell.RepairableEquipment&&(cell.HydraulicShell||cell.InternalBarrier);}
 private bool CanSupplyPlanks {get{return planks.Count>0||Time.time>=plankRetryAt&&hull.HasPlankStock;}}
 private string PlankStatus {get{return plankStage==1?"Going to planks":plankStage==2?"Taking planks":"Carrying repair planks";}}
 private void PlankPose(int mode)
 {
  if(plankVisual==null&&mode!=0){plankVisual=gameObject.AddComponent<NavalCannonPartsVisual>();plankVisual.Initialize(model,footOffset,true);}
  if(plankVisual!=null)plankVisual.Pose(mode);
  if(hammer!=null)hammer.gameObject.SetActive(mode==0||mode==3);
 }
 private void CancelPlankTrip(){plankStage=0;plankPickupTime=0;PlankPose(0);}
 private bool PlanPlankPickup()
 {
  plankStock=null;float cost=float.PositiveInfinity;Vector3 destination=Vector3.zero;
  foreach(var candidate in hull.PlankSupplies)if(candidate.Available&&CargoRetry(candidate)){
   float travel=SupportTravelCost(candidate.PickupPost);if(travel>=cost)continue;
   for(int i=0;i<3;i++){
    Vector3 point=CargoApproach(candidate.PickupPost,i);
    if(!Supported(point)||!ClearBody(point))continue;
    cost=travel;plankStock=candidate;destination=point;break;
   }
  }
  if(plankStock==null)return false;route.Clear();stuck=0;PlanTo(destination);return true;
 }
 private void FailedPlankTrip(){plankRetryAt=Time.time+8f;ContinueRepair();}
 private bool TickPlanks(float dt)
 {
  if(!NeedsPlanks(leak))return false;
  if(!leak.Missing&&!leak.Detached){ContinueRepair();return true;}
  if(plankStage==0&&planks.Count>0){PlankPose(route.Count>0?1:3);return false;}
  if(plankStage==0){plankStage=1;plankReplans=0;plankPickupTime=0;if(!PlanPlankPickup()){FailedPlankTrip();return true;}}
  robot.DisableAI();
  if(planks.Count==0&&(plankStock==null||!plankStock.Available)){FailedPlankTrip();return true;}
  if(route.Count>0){
   var result=FollowRoute(dt,4f);
   if(result==RouteResult.Fell){stranded=true;ContinueRepair();return true;}
   if(result==RouteResult.Blocked){
    if(planks.Count==0&&plankStock!=null)blockedCargo[plankStock]=Time.time+10f;
    if(++plankReplans>2){FailedPlankTrip();return true;}
    if(planks.Count>0){if(!ReplanRepair())FailedPlankTrip();}
    else if(!PlanPlankPickup())FailedPlankTrip();
   }
   if(plankStage!=0&&planks.Count>0)PlankPose(1);
   return true;
  }
  HoldPosition();Walking(false,dt);
  if(planks.Count==0){
   plankStage=2;PlankPose(2);plankPickupTime+=dt;if(plankPickupTime<1.2f)return true;
   if(!planks.Refill(plankStock!=null&&plankStock.TakeBundle())){FailedPlankTrip();return true;}
   plankStage=3;PlankPose(1);stuck=0;PlanTo(repairDestination);return true;
  }
  plankStage=0;progress.Reset();PlankPose(3);return true;
 }
}
