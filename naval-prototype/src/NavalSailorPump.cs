using UnityEngine;
public partial class NavalRepairSailor
{
    private bool pumpDuty;
    private NavalPumpStation pumpStation;
    private float pumpRetry,pumpReview;
    private bool pumpArrived;
    private int pumpApproach;
    private bool PlanPumpApproach(NavalBilgeSystem system)
    {
        for(int n=0;n<3;n++)
        {
            int choice=pumpApproach++%3;Vector3 goal=pumpStation.Post;
            goal.z+=.4f/hull.WorldScale;
            goal.x=choice==0?0:choice==1?-.55f:.55f;
            if(!ClearBody(goal)||!Supported(goal))continue;
            feet=boat.InverseTransformPoint(robot.transform.position-Vector3.up*footOffset);
            route.Clear();stuck=0;PlanTo(goal);return true;
        }
        return false;
    }
    private void ReleasePump()
    {if(pumpStation!=null&&pumpStation.Operator==this)pumpStation.Operator=null;pumpStation=null;pumpDuty=false;}
    public void CancelPumpDuty(){ReleasePump();route.Clear();BeginReturn();}
    private bool TickPump(float dt,bool allowed)
    {
        NavalBilgeSystem system=hull.Bilge;
        if(system==null)return false;
        if(!system.Functional||pumpDuty&&(pumpStation==null||!pumpStation.Functional))
        {
            if(pumpDuty){ReleasePump();BeginReturn();return true;}
            if(allowed&&leak==null&&bailState==0)TryFindLeak();
            return false;
        }
        if(pumpDuty&&(!system.WantsOperator||system.PlayerUsingStation(pumpStation)))
        {ReleasePump();BeginReturn();return false;}
        bool otherRepair=false;
        foreach(NavalRepairSailor worker in hull.RepairWorkers)if(worker!=null&&worker!=this&&worker.RepairingLeak)otherRepair=true;
        if(pumpDuty&&pumpArrived&&hull.HasRepairDemand&&!system.UrgentTrim&&!otherRepair&&Time.time>=pumpReview)
        {
            pumpReview=Time.time+1f;HoldPosition();TryFindLeak();
            if(leak!=null){ReleasePump();return false;}
        }
        if(!pumpDuty)
        {
            if(!PumpAssignment||!allowed||leak!=null||bailState!=0||system.OpenPumpSlots==0||!system.WantsOperator||Time.time<pumpRetry)return false;
            bool repairAvailable=false;
            if(hull.HasRepairDemand&&!system.UrgentTrim&&!otherRepair)
            {TryFindLeak();repairAvailable=leak!=null;if(repairAvailable)return false;}
            if(!system.UrgentTrim&&!NavalMaintenanceMath.NeedPump(hull.HasRepairDemand,otherRepair,repairAvailable,hull.ParallelMaintenance,SpareMaintenance))return false;
            pumpStation=system.NearestPump(feet,true);if(pumpStation==null)return false;
            waitingForRepair=false;pumpStation.Operator=this;pumpDuty=true;pumpArrived=false;pumpReview=Time.time+1f;
            Debug.Log("[NavalPrototype] Crew assigned to bilge pump");returning=false;changingPost=false;route.Clear();stuck=0f;
            pumpApproach=0;
            if(!PlanPumpApproach(system)){pumpRetry=Time.time+2f;ReleasePump();BeginReturn();return false;}
        }
        robot.DisableAI();
        if(route.Count>0)
        {
            RouteResult movement=FollowRoute(dt,5f);
            if(movement==RouteResult.Fell){stranded=true;ReleasePump();route.Clear();return true;}
            if(movement==RouteResult.Blocked)
            {
                if(pumpApproach<3&&PlanPumpApproach(system))return true;
                pumpRetry=Time.time+3f;ReleasePump();BeginReturn();
            }
        }
        else
        {
            if(!pumpArrived){pumpArrived=true;Debug.Log("[NavalPrototype] Crew reached bilge pump; pumping started");}
            Walking(false,dt);HoldPosition();
            Vector3 facing=-boat.forward;facing.y=0f;if(facing.sqrMagnitude>.01f)robot.transform.rotation=Quaternion.LookRotation(facing);
            if(hammer!=null)hammer.localRotation=hammerRest*Quaternion.Euler(Mathf.Sin(Time.time*8f)*35f,0f,0f);
            system.Pump(dt,true,pumpStation);
        }
        return true;
    }
}
