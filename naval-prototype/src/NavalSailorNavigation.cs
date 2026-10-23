using UnityEngine;

public partial class NavalRepairSailor
{
    private readonly NavalMovementWatchdog movementWatchdog=new NavalMovementWatchdog();
    private float stairRecoveryAt,deckReplanAt;private int stairExitWaypoints,lastStairExitSide;private float blockLogAt;
    private enum RouteResult {Moving,Arrived,Blocked,Fell}
    // All maintenance duties use one movement, arrival and obstruction policy.
    // The caller owns the job-specific response to an obstruction.
    private RouteResult FollowRoute(float dt,float timeout,bool finishAtRepairReach=false)
    {
        if(route.Count==0){stairExitWaypoints=0;return RouteResult.Arrived;}
        Vector3 target=route[0];bool final=route.Count==1;
        // A stale route must not pull a sailor vertically through an intact deck.
        bool submergedRepair=final&&leak!=null&&NavalTestWorld.Active!=null&&NavalTestWorld.Active.WaterLevelAt(boat.TransformPoint(feet))>boat.TransformPoint(feet).y+2.6f;
        if(!submergedRepair&&stairExitWaypoints==0&&ledgeWaypoints==0&&Time.time>=deckReplanAt&&Mathf.Abs(target.y-feet.y)*hull.WorldScale>1f&&
           new Vector2(target.x-feet.x,target.z-feet.z).magnitude*hull.WorldScale<1.2f){
            deckReplanAt=Time.time+2f;Vector3 goal=route[route.Count-1];
            route.Clear();stuck=0;PlanTo(goal);
            if(route.Count==0)return RouteResult.Arrived;
            target=route[0];final=route.Count==1;
            Debug.Log("[NavalPrototype] Rebuilt stale deck route: ship="+boat.name+" feet="+feet+" next="+target+" duty="+Status);
        }
        float recoveryFloor;
        if(stairExitWaypoints==0&&ledgeWaypoints==0&&Time.time>=stairRecoveryAt&&NavalStairRecovery.Needed(
            new NavalWalkPoint(feet.x,feet.y,feet.z),new NavalWalkPoint(target.x,target.y,target.z),hull.Definition,out recoveryFloor)){
            stairRecoveryAt=Time.time+2f;Vector3 goal=route[route.Count-1];
            int preferred=lastStairExitSide!=0?-lastStairExitSide:feet.x<0?-1:1;
            for(int attempt=0;attempt<2;attempt++){
                var recovery=NavalStairRecovery.Exit(new NavalWalkPoint(feet.x,feet.y,feet.z),
                    new NavalWalkPoint(goal.x,goal.y,goal.z),hull.Definition,recoveryFloor,attempt==0?preferred:-preferred,NavigationRadius);
                var exit=recovery[0];Vector3 point=new Vector3(exit.X,exit.Y,exit.Z);
                // The controller executes every segment. Never jump to a tread above the head.
                if(!ClearBody(point)||!NavigationClear(feet,point))continue;
                route.Clear();Append(recovery);stairExitWaypoints=2;lastStairExitSide=attempt==0?preferred:-preferred;stuck=0;moveSpeed=0;
                Debug.Log("[NavalPrototype] Crew stair underflight recovery: ship="+boat.name+" feet="+feet+" exit="+point+" duty="+Status);
                target=route[0];final=false;break;
            }
        }
        float before=boat.TransformVector(target-feet).magnitude;
        Move(target,dt,final);
        bool motionExpired=movementWatchdog.Step(dt,feet.x*hull.WorldScale,feet.y*hull.WorldScale,feet.z*hull.WorldScale);
        bool recovering=ledgeWaypoints>0;
        bool closeRepair=!recovering&&finishAtRepairReach&&leak!=null&&final&&Mathf.Abs(feet.y-target.y)<.3f&&
            new Vector2(feet.x-target.x,feet.z-target.z).magnitude<.4f&&CanReachLeak();
        bool walkedOff=ledgeWaypoints==2&&NavalLedgeRecoveryMath.WalkedOff(feet.x-target.x,feet.y-target.y,feet.z-target.z,hull.WorldScale);
        bool stairPoint=NavalShipRoutes.InStairwell(new NavalWalkPoint(target.x,target.y,target.z),hull.Definition);
        bool reached=walkedOff||closeRepair||(stairExitWaypoints>0?NavalStairRecovery.Arrived(feet.x-target.x,feet.y-target.y,feet.z-target.z,hull.WorldScale):stairPoint&&!recovering?
            NavalWalkRoutes.Reached(feet.x-target.x,feet.y-target.y,feet.z-target.z,hull.WorldScale):recovering?NavalLedgeRecoveryMath.Arrived(feet.x-target.x,feet.y-target.y,feet.z-target.z,hull.WorldScale):final&&returning?AtStation(target):NavalRouteArrival.Reached(feet.x,feet.y,feet.z,target.x,target.y,target.z,final));
        if(reached&&final&&finishAtRepairReach&&leak!=null&&!CanReachLeak())reached=false;
        stuck=NavalRouteProgress.Step(stuck,before,boat.TransformVector(target-feet).magnitude,dt,reached,Time.time<passageWorkUntil);
        if(closeRepair)route.Clear();else if(reached){route.RemoveAt(0);if(recovering)ledgeWaypoints--;if(stairExitWaypoints>0){stairExitWaypoints--;if(stairExitWaypoints==0)lastStairExitSide=0;}}
        if(feet.y<hull.Floor-.7f)return RouteResult.Fell;
        if(motionExpired){movementWatchdog.Reset();Debug.Log("[NavalPrototype] Crew route stalled for 8 seconds at "+feet+" target="+target+" ship="+boat.name+" profile="+hull.Definition.Name+" duty="+Status);return RouteResult.Blocked;}
        if(stairExitWaypoints==0&&stuck>.45f&&RecoverObstruction(target))return RouteResult.Moving;
        if(stuck>timeout){LogRouteBlock(target);return RouteResult.Blocked;}
        return route.Count==0?RouteResult.Arrived:RouteResult.Moving;
    }
    private void LogRouteBlock(Vector3 target){
        if(Time.time<blockLogAt)return;blockLogAt=Time.time+4f;
        var body=Controller();float radius=NavigationRadius,height=body!=null?Mathf.Max(1.8f,body.bounds.size.y):2.6f;
        Vector3 foot=boat.TransformPoint(feet);string nearby="";int count=0;
        foreach(var c in Physics.OverlapCapsule(foot+Vector3.up*(radius+.03f),foot+Vector3.up*Mathf.Max(radius+.03f,height-radius),radius+.05f,PhysicsManager.GetEnvironmentLayerMask(),QueryTriggerInteraction.Ignore)){
            if(c.transform.IsChildOf(robot.transform))continue;
            nearby+=" "+c.name+"@"+boat.InverseTransformPoint(c.bounds.center);if(++count>=4)break;
        }
        Debug.Log("[NavalPrototype] Route blocked: sailor="+robot.GetInstanceID()+" ship="+boat.name+" duty="+Status+" feet="+feet+" target="+target+" radius="+radius.ToString("F2")+" height="+height.ToString("F2")+" stairExit="+stairExitWaypoints+" nearby="+nearby);
    }
    private void TryFindLeak()
    {
        if(Time.time<scanAt)return;
        scanAt=Time.time+.25f;
        // Damage elsewhere must not erase this sailor's inaccessible-site cooldowns.
        scanRevision=hull.Revision;FindLeak();
    }
}
