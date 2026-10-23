using UnityEngine;
public partial class NavalCrewController
{
    private float sailAssignmentAt,sailWarningAt;
    private void CoordinateSails(int protectedHelm=-1,bool final=false)
    {
        if(stations.SailOperator!=null||!stations.NeedsSailOperator)return;
        if(Time.time<sailAssignmentAt)return;sailAssignmentAt=Time.time+.25f;
        if(!final&&!stations.RescueStop&&!stations.ReverseManeuver&&(hull.HasRepairDemand||hull.CombatCritical||hull.UrgentTrim||hull.Fire!=null&&hull.Fire.HasFire))return;
        int chosen=-1;float best=float.PositiveInfinity;
        for(int pass=0;pass<2&&chosen<0;pass++)for(int i=0;i<workers.Length;i++)
        {
            var worker=workers[i];
            bool protectedGunner=!final&&!stations.RescueStop&&!stations.ReverseManeuver&&assigned[i]>0&&assigned[i]<=stations.GunCount&&cachedTarget!=null&&stations.CanBearOnShip(assigned[i],cachedTarget);
            if(protectedGunner)continue;
            if(i==protectedHelm||!stations.RescueStop&&assigned[i]==0||worker==null||!worker.CanHandleSails||
                boarders[i]!=null&&boarders[i].Busy||!final&&(worker.FireAssignment||worker.PumpAssignment)&&
                (!stations.ReverseManeuver||worker.FireAssignment&&hull.Fire!=null&&hull.Fire.Urgent))continue;
            if(pass==0&&assigned[i]>=0)continue;
            float cost=worker.SupportTravelCost(stations.SailPost);if(cost<best){best=cost;chosen=i;}
        }
        if(chosen>=0){assigned[chosen]=-1;workers[chosen].StartSails(stations);return;}
        if(!stations.ReverseManeuver)return;
        // A disabled gun refurbishment/queued job must not strand the whole ship at shore.
        for(int i=0;i<workers.Length;i++){
            var worker=workers[i];
            if(i==protectedHelm||assigned[i]==0||worker==null||!worker.CanBorrowForRecoverySails||boarders[i]!=null&&boarders[i].Busy)continue;
            float cost=worker.SupportTravelCost(stations.SailPost);if(cost<best){best=cost;chosen=i;}
        }
        if(chosen>=0){assigned[chosen]=-1;workers[chosen].StartRecoverySails(stations);}
        else if(Time.time>=sailWarningAt){sailWarningAt=Time.time+5f;Debug.Log("[NavalPrototype] Recovery waiting for eligible sail hand: "+hull.Root.name+" leaks="+hull.HasRepairDemand+" critical="+hull.CombatCritical); }
    }
}
