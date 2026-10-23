using UnityEngine;
public partial class NavalRepairSailor
{
 private NavalCannonSupply cannonStock;
 private int cannonPartsStage,cannonPartsReplans;
 private float cannonPickupTime;
 private readonly NavalCannonKit cannonKit=new NavalCannonKit();
 private bool cannonKitHeld {get{return cannonKit.Held;}}
 private NavalCannonPartsVisual cannonPartsVisual;
 public bool CarryingCannonParts(NavalRepairableMechanism mechanism){return cannonKitHeld&&leak!=null&&leak.RepairMechanism==mechanism;}
 private string CannonPartsStatus {get{return cannonPartsStage==1?"Going to spare parts":cannonPartsStage==2?"Taking spare parts":"Carrying cannon parts";}}
 private void PartsPose(int mode)
 {
  if(cannonPartsVisual==null&&mode!=0){cannonPartsVisual=gameObject.AddComponent<NavalCannonPartsVisual>();cannonPartsVisual.Initialize(model,footOffset);}
  if(cannonPartsVisual!=null)cannonPartsVisual.Pose(mode);
  if(hammer!=null)hammer.gameObject.SetActive(mode==0||mode==3);
 }
 private void CancelCannonParts(){cannonPartsStage=0;cannonKit.Drop();cannonPickupTime=0;PartsPose(0);}
 private bool PlanPartsPickup()
 {
  NavalCannonSupply supply=null;float cost=float.PositiveInfinity;Vector3 destination=Vector3.zero;
  foreach(var candidate in hull.CannonSupplies)if(candidate.Available&&CargoRetry(candidate)){
   float travel=SupportTravelCost(candidate.PickupPost);if(travel>=cost)continue;
   for(int i=0;i<3;i++){
    Vector3 point=CargoApproach(candidate.PickupPost,i);
    if(!Supported(point)||!ClearBody(point))continue;
    cost=travel;supply=candidate;destination=point;break;
   }
  }
  if(supply==null)return false;cannonStock=supply;route.Clear();stuck=0;PlanTo(destination);return true;
 }
 private bool TickCannonParts(float dt)
 {
  if(leak==null||leak.RepairMechanism==null||!leak.RepairMechanism.IsCannon)return false;
  var mechanism=leak.RepairMechanism;
  if(mechanism.PartsDelivered)
  {mechanism.ReportRepair(work,route.Count>0?"К пушке":"Ремонт");if(route.Count==0)PartsPose(3);return false;}
  if(cannonPartsStage==0)
  {
   cannonPartsStage=1;cannonPartsReplans=0;cannonPickupTime=0;
   if(!PlanPartsPickup()){ContinueRepair();return true;}
  }
  mechanism.ReportRepair(0,cannonPartsStage<3?"За запчастями":"Доставка");robot.DisableAI();
  if(!cannonKitHeld&&(cannonStock==null||!cannonStock.Available)){ContinueRepair();return true;}
  if(route.Count>0)
  {
   var result=FollowRoute(dt,4f);
   if(result==RouteResult.Fell){stranded=true;ContinueRepair();return true;}
   if(result==RouteResult.Blocked)
   {
    if(!cannonKitHeld&&cannonStock!=null)blockedCargo[cannonStock]=Time.time+10f;
    if(++cannonPartsReplans<=2)
    {if(cannonKitHeld){route.Clear();stuck=0;PlanTo(repairDestination);}else if(PlanPartsPickup())return true;else ContinueRepair();}
    else ContinueRepair();
   }
   if(cannonKitHeld)PartsPose(1);return true;
  }
  HoldPosition();Walking(false,dt);
  if(!cannonKitHeld)
  {
   cannonPartsStage=2;PartsPose(2);cannonPickupTime+=dt;
   if(cannonPickupTime<1.5f)return true;
   if(!cannonKit.Take(cannonStock!=null&&cannonStock.TakeKit())){ContinueRepair();return true;}cannonPartsStage=3;PartsPose(1);route.Clear();stuck=0;PlanTo(repairDestination);return true;
  }
  mechanism.PartsDelivered=cannonKit.Deliver();cannonPartsStage=0;progress.Reset();PartsPose(3);return true;
 }
}
