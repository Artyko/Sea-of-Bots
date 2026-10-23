using UnityEngine;
public partial class NavalRepairSailor
{
    private bool nextTaskPending;
    // Deferred by one tick: avoids recursive task cancellation/dispatch and station reassignment in between.
    private void DispatchNextTask()
    {
        nextTaskPending=false;
        feet=boat.InverseTransformPoint(robot.transform.position-Vector3.up*footOffset);
        bool allowed=MayAcquireMaintenance();
        if(allowed)
        {
            if(hull.Fire!=null&&(hull.Fire.Urgent||FireAssignment&&!NavalMaintenancePriority.DeferOrdinaryFire(hull.HasRepairDemand,OtherLeakRepairer())))
            {if(TickFire(0,true)||nextTaskPending||fireJob!=null)return;}
            scanRevision=hull.Revision;scanAt=Time.time+.25f;FindLeak();
            if(leak!=null)return;
            if(hull.Fire!=null&&hull.Fire.HasFire&&!hull.CombatCritical)
            {if(TickFire(0,true)||nextTaskPending||fireJob!=null)return;}
            if(TickPump(0,true)||nextTaskPending||pumpDuty||leak!=null)return;
            if(TickBailing(0,true)||nextTaskPending||bailState!=0||leak!=null)return;
            if(ReserveRepairWait()){waitingForRepair=true;return;}
        }
        // Combat recall and walking home happen only after checking available useful work.
        if(combatReturnPending&&NavalRepairDispatch.CanRecall(false,hull.HasRepairDemand,OtherLeakRepairer(),BucketWater))
        {ResumeCombatPost();return;}
        returning=true;changingPost=false;PlanTo(home);
    }
}
