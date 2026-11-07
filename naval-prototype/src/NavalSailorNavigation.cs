using UnityEngine;

public partial class NavalRepairSailor
{
    private readonly NavalRouteFailureMemory routeFailures=new NavalRouteFailureMemory();
    private readonly NavalNavigationRecovery navigationRecovery=new NavalNavigationRecovery();
    private readonly NavalMovementWatchdog movementWatchdog=new NavalMovementWatchdog();
    private float stairRecoveryAt,deckReplanAt,deckRouteTrustedUntil;private int stairExitWaypoints,lastStairExitSide;private float blockLogAt;
    private enum RouteResult {Moving,Arrived,Blocked,Fell}
    // All maintenance duties use one movement, arrival and obstruction policy.
    // The caller owns the job-specific response to an obstruction.
    private RouteResult FollowRoute(float dt,float timeout,bool finishAtRepairReach=false)
    {
        using(NavalPerformance.Measure(NavalPerfArea.CrewRoute)){
            float q=hull.WorldScale;
            if(route.Count>0){var target=route[0];if(routeFailures.Blocked(feet.x*q,feet.y*q,feet.z*q,target.x*q,target.y*q,target.z*q,hull.Revision,Time.time)&&!(finishAtRepairReach&&leak!=null&&CanReachLeak()))return RouteResult.Blocked;}
            var result=FollowRouteCore(dt,timeout,finishAtRepairReach);
            if(result==RouteResult.Blocked&&route.Count>0){var target=route[0];routeFailures.Mark(feet.x*q,feet.y*q,feet.z*q,target.x*q,target.y*q,target.z*q,hull.Revision,Time.time);}
            return result;
        }
    }

    private RouteResult FollowRouteCore(float dt,float timeout,bool finishAtRepairReach)
    {
        // Work from the intact edge as soon as the claimed patch is in real reach.
        // Requiring the final waypoint sent workers towards the missing floor itself.
        if(finishAtRepairReach&&leak!=null&&!returning&&CanReachLeak()){
            route.Clear();escapePending=false;stairExitWaypoints=0;
            navigationRecovery.Reset();movementWatchdog.Reset();stuck=0f;
            HoldPosition();return RouteResult.Arrived;
        }
        if(TickTrafficWait(dt))return RouteResult.Moving;
        if(route.Count==0){escapePending=false;stairExitWaypoints=0;return RouteResult.Arrived;}
        if(escapePending){
            ResumeGridEscape();
            if(escapePending){movementWatchdog.Reset();Walking(false,dt);HoldPosition();ApplyUnsupportedFall(dt);return RouteResult.Moving;}
        }
        Vector3 target=route[0];bool final=route.Count==1;
        // Finish vertical ladder travel before generic deck recovery can replace its endpoint.
        if(ClimbAftAALadder(target,dt)){
            navigationRecovery.Reset();movementWatchdog.Reset();stuck=0f;
            if(boat.TransformVector(target-feet).sqrMagnitude<.04f*.04f)route.RemoveAt(0);
            return route.Count==0?RouteResult.Arrived:RouteResult.Moving;
        }
        int recoveryPhase=navigationRecovery.Step(dt,feet.x*hull.WorldScale,feet.y*hull.WorldScale,feet.z*hull.WorldScale);
        if(recoveryPhase==3){
            navigationRecovery.Reset();LogRouteBlock(target);
            return RouteResult.Blocked;
        }
        if(recoveryPhase==2&&!NavigationStairs(feet)){
            Vector3 goal=route[route.Count-1];
            // Try rejoining a later segment, rather than the same obstructed corner.
            for(int skip=Mathf.Min(route.Count-1,6);skip>=0;skip--){
                if(Mathf.Abs(route[skip].y-feet.y)*hull.WorldScale>.3f)continue;
                System.Array.Clear(escapeSupport,0,escapeSupport.Length);System.Array.Clear(escapeLinks,0,escapeLinks.Length);
                if(GridEscape(route[skip],skip))return RouteResult.Moving;
            }
            route.Clear();PlanTo(goal);stuck=0;movementWatchdog.Reset();
            if(route.Count==0)return RouteResult.Blocked;
            target=route[0];final=route.Count==1;
        }
        if(recoveryPhase==1){avoidanceAt=0;RecoverObstruction(target);if(escapePending)return RouteResult.Moving;}

        // A stale route must not pull a sailor vertically through an intact deck.
        bool submergedRepair=final&&leak!=null&&NavalTestWorld.Active!=null&&NavalTestWorld.Active.WaterLevelAt(boat.TransformPoint(feet))>boat.TransformPoint(feet).y+2.6f;
        // Landings are part of a stair passage too. The old check only knew
        // about the sloping flight, so a sailor standing on a landing saw the
        // next point on the other deck and repeatedly rebuilt a valid route.
        // A point on the upper flight and a point on the lower flight are not
        // one continuous stair transition. Treating them as one suppressed the
        // stale-route rebuild and sent sailors vertically through the middle deck.
        bool stairTransition=NavalStairTraffic.SamePassage(new NavalWalkPoint(feet.x,feet.y,feet.z),
            new NavalWalkPoint(target.x,target.y,target.z),hull.Definition)||
            NavalRaisedBridge.Flight(new NavalWalkPoint(feet.x,feet.y,feet.z),hull.Definition)&&NavalRaisedBridge.Flight(new NavalWalkPoint(target.x,target.y,target.z),hull.Definition);
        if(!submergedRepair&&!stairTransition&&stairExitWaypoints==0&&ledgeWaypoints==0&&Time.time>=deckReplanAt&&Time.time>=deckRouteTrustedUntil&&Mathf.Abs(target.y-feet.y)*hull.WorldScale>1f&&
           new Vector2(target.x-feet.x,target.z-feet.z).magnitude*hull.WorldScale<1.2f){
            deckReplanAt=Time.time+2f;Vector3 oldTarget=target,goal=route[route.Count-1];
            route.Clear();stuck=0;PlanTo(goal);
            if(route.Count==0)return RouteResult.Arrived;
            // The freshly generated route contains explicit treads and has
            // passed the static clearance planner. Give it enough time to
            // cross a complete flight instead of reclassifying every landing
            // pose as another stale vertical shortcut two seconds later.
            deckRouteTrustedUntil=Time.time+12f;
            target=route[0];final=route.Count==1;
            Debug.Log("[NavalPrototype] Rebuilt stale deck route: ship="+boat.name+" feet="+feet+" stale="+oldTarget+" next="+target+" duty="+Status);
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
        // Door treads in the armoured conning tower need tighter arrival than
        // an open deck waypoint.  The normal 1.12 m intermediate tolerance
        // advanced sailors while their capsule was still inside the jamb,
        // after which the lateral bypass drove them into the front armour.
        bool preciseTower=NavalConningTowerLayout.DoorLane(new NavalWalkPoint(target.x,target.y,target.z),hull.Definition);
        bool reached=walkedOff||closeRepair||(NavalCapitalAADeck.Waypoint(target,hull.Definition)?NavalCapitalAADeck.Reached(feet,target,hull.WorldScale):stairExitWaypoints>0?NavalStairRecovery.Arrived(feet.x-target.x,feet.y-target.y,feet.z-target.z,hull.WorldScale):stairPoint&&!recovering?
            (NavalRaisedBridge.Flight(new NavalWalkPoint(target.x,target.y,target.z),hull.Definition)?NavalRaisedBridge.ReachedFlight(feet.x-target.x,feet.y-target.y,feet.z-target.z,hull.WorldScale):hull.Definition.Destroyer?NavalDestroyerLayout.ReachedRamp(feet.x-target.x,feet.y-target.y,feet.z-target.z,hull.WorldScale):NavalWalkRoutes.Reached(feet.x-target.x,feet.y-target.y,feet.z-target.z,hull.WorldScale)):recovering?NavalLedgeRecoveryMath.Arrived(feet.x-target.x,feet.y-target.y,feet.z-target.z,hull.WorldScale):final&&returning?AtStation(target):NavalRouteArrival.ReachedWorld(feet.x-target.x,feet.y-target.y,feet.z-target.z,hull.WorldScale,final||preciseTower));
        // A displaced capsule may straddle a tread pin. Rejoin only the next
        // supported segment of this flight; normal tread arrival stays strict.
        if(!reached&&!final&&!recovering&&stairExitWaypoints==0&&stairPoint&&stuck>.5f&&
           boat.TransformVector(target-feet).sqrMagnitude<NavigationRadius*NavigationRadius*1.1f&&Mathf.Abs(target.y-feet.y)*hull.WorldScale<.2f&&
           NavalStairTraffic.SamePassage(new NavalWalkPoint(feet.x,feet.y,feet.z),new NavalWalkPoint(route[1].x,route[1].y,route[1].z),hull.Definition)&&
           NavalPhysicsQueries.HasLocalSupport(hull,feet)&&NavalPhysicsQueries.HasLocalSupport(hull,route[1])&&NavigationClear(feet,route[1])){
            reached=true;motionExpired=false;movementWatchdog.Reset();
        }
        if(!reached&&!final&&!recovering&&stairExitWaypoints==0&&!stairPoint&&stuck>.35f&&
            boat.TransformVector(target-feet).sqrMagnitude<.65f*.65f&&Mathf.Abs(target.y-feet.y)*hull.WorldScale<.3f&&
            Mathf.Abs(route[1].y-feet.y)*hull.WorldScale<.3f&&ClearBody(route[1])&&NavigationWalkClear(feet,route[1]))
        {reached=true;motionExpired=false;movementWatchdog.Reset();}
        if(reached&&final&&finishAtRepairReach&&leak!=null&&!CanReachLeak())reached=false;
        stuck=NavalRouteProgress.Step(stuck,before,boat.TransformVector(target-feet).magnitude,dt,reached,Time.time<passageWorkUntil);
        if(reached){motionExpired=false;movementWatchdog.Reset();}
        if(closeRepair)route.Clear();else if(reached){route.RemoveAt(0);if(recovering)ledgeWaypoints--;if(stairExitWaypoints>0){stairExitWaypoints--;if(stairExitWaypoints==0)lastStairExitSide=0;}}
        if(feet.y<hull.Floor-.7f)return RouteResult.Fell;
        if(motionExpired){movementWatchdog.Reset();Debug.Log("[NavalPrototype] Crew route stalled for 8 seconds: sailor="+robot.GetInstanceID()+" at="+feet+" target="+target+" ship="+boat.name+" profile="+hull.Definition.Name+" duty="+Status);return RouteResult.Blocked;}
        // A waypoint below a rack lid is not reachable by pushing vertically down.
        // Route off the raised obstacle first, even when the horizontal error is zero.
        if(!stairPoint&&stairExitWaypoints==0&&stuck>.45f&&feet.y>target.y+.4f/hull.WorldScale&&Time.time>=avoidanceAt){
            avoidanceAt=Time.time+2f;if(EscapeRaisedObstacle())return RouteResult.Moving;
        }
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
        Debug.Log("[NavalPrototype] Route blocked: sailor="+robot.GetInstanceID()+" ship="+boat.name+" duty="+Status+" feet="+feet+" target="+target+" radius="+radius.ToString("F2")+" height="+height.ToString("F2")+" stairExit="+stairExitWaypoints+" contacts="+movementContacts+" capsuleCentre="+(body!=null?boat.InverseTransformPoint(body.bounds.center):feet)+" footOffset="+footOffset.ToString("F2")+" nearby="+nearby);
    }
    private void TryFindLeak()
    {
        if(Time.time<scanAt)return;
        scanAt=Time.time+.25f;
        // Damage elsewhere must not erase this sailor's inaccessible-site cooldowns.
        scanRevision=hull.Revision;FindLeak();
    }
}
