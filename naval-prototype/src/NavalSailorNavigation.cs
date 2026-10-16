using UnityEngine;

public partial class NavalRepairSailor
{
    private enum RouteResult {Moving,Arrived,Blocked,Fell}
    // All maintenance duties use one movement, arrival and obstruction policy.
    // The caller owns the job-specific response to an obstruction.
    private RouteResult FollowRoute(float dt,float timeout,bool finishAtRepairReach=false)
    {
        if(route.Count==0)return RouteResult.Arrived;
        Vector3 target=route[0];bool final=route.Count==1;
        float before=boat.TransformVector(target-feet).magnitude;
        Move(target,dt,final);
        bool recovering=ledgeWaypoints>0;
        bool closeRepair=!recovering&&finishAtRepairReach&&leak!=null&&final&&Mathf.Abs(feet.y-target.y)<.3f&&
            new Vector2(feet.x-target.x,feet.z-target.z).magnitude<.4f&&CanReachLeak();
        bool reached=closeRepair||(recovering?NavalLedgeRecoveryMath.Arrived(feet.x-target.x,feet.y-target.y,feet.z-target.z,hull.WorldScale):NavalRouteArrival.Reached(feet.x,feet.y,feet.z,target.x,target.y,target.z,final));
        stuck=NavalRouteProgress.Step(stuck,before,boat.TransformVector(target-feet).magnitude,dt,reached,Time.time<passageWorkUntil);
        if(closeRepair)route.Clear();else if(reached){route.RemoveAt(0);if(recovering)ledgeWaypoints--;}
        if(feet.y<hull.Floor-.7f)return RouteResult.Fell;
        if(stuck>timeout)return RouteResult.Blocked;
        return route.Count==0?RouteResult.Arrived:RouteResult.Moving;
    }
    private void TryFindLeak()
    {
        if(Time.time<scanAt&&scanRevision==hull.Revision)return;
        scanAt=Time.time+.25f;
        if(scanRevision!=hull.Revision)retryAfter.Clear();
        scanRevision=hull.Revision;FindLeak();
    }
}
