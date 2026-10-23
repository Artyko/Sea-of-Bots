using UnityEngine;
public partial class NavalCrewController
{
    private float supportReview;
    private bool[] supportEligible;
    private float[] supportCosts;
    private void CoordinateSupport()
    {
        bool changed=false;foreach(var worker in workers)if(worker!=null&&worker.ChoosingNextTask)changed=true;
        if(Time.time<supportReview&&!changed)return;supportReview=Time.time+.3f;
        if(supportEligible==null){supportEligible=new bool[workers.Length];supportCosts=new float[workers.Length];}
        foreach(var worker in workers)if(worker!=null){worker.PumpAssignment=false;worker.FireAssignment=false;}
        var bilge=hull.Bilge;
        // Mark incumbent before allocating other work. Never borrow an operator mid-route.
        bool pumpOwned=bilge!=null&&bilge.Operator!=null;
        if(pumpOwned)for(int n=0;n<bilge.PumpCount;n++){var operatorWorker=bilge.PumpAt(n).Operator;if(operatorWorker!=null)operatorWorker.PumpAssignment=true;}
        bool pumpUrgent=hull.CombatCritical||hull.UrgentTrim;
        if(pumpUrgent&&!pumpOwned)AssignPump();
        var fire=hull.Fire;
        if(fire==null||!fire.HasFire){if(!pumpOwned&&!pumpUrgent)AssignPump();AssignEmergencySecondPump(pumpUrgent);return;}
        int existing=0;foreach(var worker in workers)if(worker!=null&&worker.FightingFire){worker.FireAssignment=true;existing++;}
        int slots=(fire.CriticalFire?2:1)-existing;
        for(int slot=0;slot<slots;slot++)
        {
            for(int i=0;i<workers.Length;i++)
            {
                supportEligible[i]=workers[i]!=null&&assigned[i]<0&&(boarders[i]==null||!boarders[i].Busy)&&!workers[i].FireAssignment&&!workers[i].PumpAssignment&&workers[i].CanReceiveSupport(true);
                supportCosts[i]=float.PositiveInfinity;if(!supportEligible[i])continue;
                Vector3 nearestPoint=Vector3.zero;float nearest=float.PositiveInfinity;
                for(int n=0;n<fire.TargetCount;n++)
                {
                    var target=fire.TargetAt(n);if(!target.Active||!fire.CanClaim(target,workers[i]))continue;
                    float distance=(hull.Root.TransformPoint(target.Point)-crew[i].transform.position).sqrMagnitude;
                    if(distance<nearest){nearest=distance;nearestPoint=target.Point;}
                }
                if(!float.IsInfinity(nearest))supportCosts[i]=workers[i].SupportTravelCost(nearestPoint);
            }
            int chosen=NavalCrewSupportPolicy.Choose(-1,supportEligible,supportCosts);if(chosen<0)break;
            workers[chosen].FireAssignment=true;
        }
        if(!pumpOwned&&!pumpUrgent)AssignPump();
        AssignEmergencySecondPump(pumpUrgent);
    }
    private void AssignEmergencySecondPump(bool urgent)
    {
        var bilge=hull.Bilge;if(!urgent||bilge==null||bilge.PumpCount<2||bilge.OpenPumpSlots==0)return;
        int count=0;foreach(var w in workers)if(w!=null&&w.PumpAssignment)count++;
        if(count<2)AssignPump();
    }
    private void AssignPump()
    {
        var bilge=hull.Bilge;if(bilge==null||!bilge.Functional||!bilge.WantsOperator||bilge.OpenPumpSlots==0)return;
        for(int i=0;i<workers.Length;i++)
        {
            supportEligible[i]=workers[i]!=null&&assigned[i]<0&&(boarders[i]==null||!boarders[i].Busy)&&!workers[i].FireAssignment&&!workers[i].PumpAssignment&&workers[i].CanReceiveSupport(false);
            supportCosts[i]=supportEligible[i]?workers[i].SupportTravelCost(bilge.NearestPumpPost(hull.Root.InverseTransformPoint(crew[i].transform.position))):float.PositiveInfinity;
        }
        int chosen=NavalCrewSupportPolicy.Choose(-1,supportEligible,supportCosts);
        if(chosen>=0)workers[chosen].PumpAssignment=true;
    }
}
