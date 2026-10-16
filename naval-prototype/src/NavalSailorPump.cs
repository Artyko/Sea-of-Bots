using UnityEngine;
public partial class NavalRepairSailor
{
    private bool pumpDuty;
    private float pumpRetry,pumpReview;
    private bool pumpArrived;
    private void ReleasePump()
    {if(hull!=null&&hull.Bilge!=null&&hull.Bilge.Operator==this)hull.Bilge.Operator=null;pumpDuty=false;}
    public void CancelPumpDuty(){ReleasePump();route.Clear();BeginReturn();}
    private bool TickPump(float dt,bool allowed)
    {
        NavalBilgeSystem system=hull.Bilge;
        if(system==null)return false;
        if(pumpDuty&&(!system.WantsOperator||system.PlayerUsing))
        {ReleasePump();BeginReturn();return false;}
        bool otherRepair=false;
        foreach(NavalRepairSailor worker in hull.RepairWorkers)if(worker!=null&&worker!=this&&worker.HasJob)otherRepair=true;
        if(pumpDuty&&hull.HasActiveLeaks&&!otherRepair&&Time.time>=pumpReview)
        {
            pumpReview=Time.time+1f;HoldPosition();TryFindLeak();
            if(leak!=null){ReleasePump();return false;}
        }
        if(!pumpDuty)
        {
            if(!allowed||leak!=null||bailState!=0||system.Operator!=null||system.PlayerUsing||!system.WantsOperator||Time.time<pumpRetry)return false;
            bool repairAvailable=false;
            float dangerVolume=BoatDimensions.SinkVolume*hull.VolumeScale*.25f;
            if(hull.HasActiveLeaks&&!otherRepair&&hull.Volume<dangerVolume)
            {TryFindLeak();repairAvailable=leak!=null;if(repairAvailable)return false;}
            if(!NavalMaintenanceMath.NeedPump(hull.HasActiveLeaks,otherRepair,repairAvailable,hull.Volume,dangerVolume))return false;
            system.Operator=this;pumpDuty=true;pumpArrived=false;pumpReview=Time.time+1f;
            Debug.Log("[NavalPrototype] Crew assigned to bilge pump");returning=false;changingPost=false;route.Clear();stuck=0f;
            feet=boat.InverseTransformPoint(robot.transform.position-boat.up*footOffset);PlanTo(system.PumpPost);
        }
        robot.DisableAI();
        if(route.Count>0)
        {
            RouteResult movement=FollowRoute(dt,5f);
            if(movement==RouteResult.Blocked||movement==RouteResult.Fell){pumpRetry=Time.time+10f;ReleasePump();BeginReturn();}
        }
        else
        {
            if(!pumpArrived){pumpArrived=true;Debug.Log("[NavalPrototype] Crew reached bilge pump; pumping started");}
            Walking(false,dt);HoldPosition();
            Vector3 facing=-boat.forward;facing.y=0f;if(facing.sqrMagnitude>.01f)robot.transform.rotation=Quaternion.LookRotation(facing);
            if(hammer!=null)hammer.localRotation=hammerRest*Quaternion.Euler(Mathf.Sin(Time.time*8f)*35f,0f,0f);
            system.Pump(dt,true);
        }
        return true;
    }
}
