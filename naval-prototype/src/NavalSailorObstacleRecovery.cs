using UnityEngine;
// Bounded local avoidance shared by repair, firefighting, pumping and return routes.
public partial class NavalRepairSailor
{
    private float avoidanceAt,hopTime,hopPrevious,recoveryDashUntil,recoveryWindow;
    private bool deckGapEscape;
    private float deckGapRetryAt;private int deckGapProbe;
    private int recoveryAttempts;
    private readonly Vector3[] escapeNodes=new Vector3[289];
    private readonly int[] escapeParents=new int[289],escapeQueue=new int[289];
    private readonly byte[] escapeState=new byte[289],escapeSupport=new byte[289];
    private readonly byte[] escapeLinks=new byte[289*4];
    private readonly System.Collections.Generic.List<Vector3> escapePath=new System.Collections.Generic.List<Vector3>(32);
    private static RaycastHit[] navigationHits=new RaycastHit[32];
    private float NavigationRadius {get{var body=Controller();return body!=null?Mathf.Max(.15f,body.radius*Mathf.Max(Mathf.Abs(body.transform.lossyScale.x),Mathf.Abs(body.transform.lossyScale.z))):.38f;}}
    private bool NavigationStairs(Vector3 p)
    {return NavalShipRoutes.InStairwell(new NavalWalkPoint(p.x,p.y,p.z),hull.Definition);}
    private bool SubmergedNavigationPoint(Vector3 p){
        var world=NavalTestWorld.Active;if(world==null||!movingInWater)return false;
        Vector3 point=boat.TransformPoint(p);return world.WaterLevelAt(point)>point.y+2.6f;
    }
    private bool Supported(Vector3 p)
    {
        if(!InsideWalkingDeck(p))return false;
        if(SubmergedNavigationPoint(p))return true;
        if(NavigationStairs(p))return false;
        // A ray can still hit the intact half of a plank beneath the capsule.
        // Reject the candidate when any part of the body footprint overlaps a
        // destroyed deck cell, otherwise the escape grid plans into the same
        // hole that triggered recovery.
        if(hull.DeckGapAt(p,NavigationRadius))return false;
        return NavalPhysicsQueries.HasLocalSupport(hull,p);
    }
    private bool NavigationClear(Vector3 a,Vector3 b)
    {
        Vector3 bottom,top;float radius;NavigationCapsule(a,out bottom,out top,out radius);
        Vector3 delta=boat.TransformVector(b-a);
        // Live controller feet sit above the ideal deck plane. Sweeping down
        // from that pose classified the supporting deck as an impassable wall.
        if(Mathf.Abs(b.y-a.y)*hull.WorldScale<.65f&&!NavigationStairs(a)&&!NavigationStairs(b)&&
           NavalPhysicsQueries.HasLocalSupport(hull,a)&&NavalPhysicsQueries.HasLocalSupport(hull,b))
            delta=Vector3.ProjectOnPlane(delta,boat.up);
        if(delta.sqrMagnitude<.001f)return true;int count;
        while(true)
        {
            count=Physics.CapsuleCastNonAlloc(bottom,top,radius,delta.normalized,
                navigationHits,delta.magnitude,PhysicsManager.GetEnvironmentLayerMask(),QueryTriggerInteraction.Ignore);
            if(count<navigationHits.Length)break;System.Array.Resize(ref navigationHits,navigationHits.Length*2);
        }
        bool clear=true;bool? endpointClear=null;
        for(int i=0;i<count;i++){
            var hit=navigationHits[i];var shape=hit.collider;
            if(shape.transform.IsChildOf(robot.transform)||FriendlyCrewShape(shape))continue;
            // A capsule touching an inflated machinery box must be able to retreat.
            // Only a convex primitive at the start may be left, toward a verified clear endpoint.
            if(hit.distance<=.001f&&(shape is BoxCollider||shape is CapsuleCollider||shape is SphereCollider)){
                Vector3 centre=(bottom+top)*.5f,away=centre-shape.ClosestPoint(centre);
                bool insideStart=away.sqrMagnitude<.000001f;
                // ClosestPoint returns the point itself when already inside a
                // convex collider. A zero escape normal must not imprison the
                // sailor: a straight segment from inside to outside exits once.
                if(insideStart||Vector3.Dot(delta,away)>.0001f){
                    if(!endpointClear.HasValue)endpointClear=ClearBody(b);
                    if(endpointClear.Value)continue;
                }
            }
            clear=false;
        }
        System.Array.Clear(navigationHits,0,count);return clear;
    }
    private bool NavigationWalkClear(Vector3 a,Vector3 b)
    {
        int steps=Mathf.Max(1,Mathf.CeilToInt(boat.TransformVector(b-a).magnitude/.6f));
        bool leavingGap=hull.DeckGapAt(a,NavigationRadius);
        for(int i=1;i<=steps;i++){
            Vector3 point=Vector3.Lerp(a,b,(float)i/steps);bool gap=hull.DeckGapAt(point,NavigationRadius);
            if(!InsideWalkingDeck(point))return false;
            if(!SubmergedNavigationPoint(point)&&(!NavalPhysicsQueries.HasLocalSupport(hull,point)||!leavingGap&&gap))return false;
            if(!gap)leavingGap=false;
        }
        return NavigationClear(a,b);
    }
    private bool RecoverDeckGap(Vector3 target)
    {
        // A single nearest safe point often remained on the same damaged
        // patch. The sailor then stepped back towards the original waypoint,
        // detected the hole again and oscillated until the watchdog expired.
        // First build one bounded path which leaves the hole and rejoins the
        // requested route segment from the far side.
        System.Array.Clear(escapeSupport,0,escapeSupport.Length);
        System.Array.Clear(escapeLinks,0,escapeLinks.Length);
        for(int skip=0;skip<Mathf.Min(route.Count,8);skip++){
            Vector3 rejoin=route[skip];
            if(Mathf.Abs(rejoin.y-feet.y)*hull.WorldScale>=.3f)break;
            if(hull.DeckGapAt(rejoin,NavigationRadius))continue;
            if(GridEscape(rejoin,skip)){
                deckGapEscape=true;
                Debug.Log("[NavalPrototype] Crew damage detour: ship="+boat.name+" feet="+feet+" rejoin="+rejoin+" pending="+escapePending+" duty="+Status);
                return true;
            }
        }
        // If the damaged patch exceeds the local grid, at least move the
        // capsule completely onto verified support. A later obstruction pass
        // can continue from a valid start node.
        // Spread distant fallback probes across retries instead of 96 casts
        // per sailor on every frame of an inaccessible damaged patch.
        for(int probe=0;probe<12;probe++)
        {
            int sample=deckGapProbe++%96,ring=sample/16+1,n=sample%16;
            float angle=n*Mathf.PI/8f;
            Vector3 point=feet+new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle))*(ring*.2f);
            if(hull.DeckGapAt(point,NavigationRadius)||!Supported(point)||!ClearBody(point)||!NavigationClear(feet,point))continue;
            route.Insert(0,point);deckGapEscape=true;stuck=0;moveSpeed=0;
            Debug.Log("[NavalPrototype] Crew deck-gap escape: ship="+boat.name+" feet="+feet+" safe="+point+" duty="+Status);
            return true;
        }
        return false;
    }
    private readonly System.Collections.Generic.List<Vector3> escapeWaypoints=new System.Collections.Generic.List<Vector3>(32);
    private bool escapePending;
    private int escapeHead,escapeTail,escapeSkip,escapeRevision;
    private Vector3 escapeGoal,escapeOrigin;
    private bool GridEscape(Vector3 target,int skipWaypoints=0)
    {
        System.Array.Clear(escapeState,0,escapeState.Length);
        float spacing=.8f/hull.WorldScale;Vector3 gridOrigin=feet;
        // Use the real deck plane for detours, not the current controller skin offset.
        for(int level=0;level<hull.Definition.MaintenanceDeckCount;level++){
            float floor=hull.Definition.MaintenanceFloor(level);
            if(Mathf.Abs(feet.y-floor)*hull.WorldScale<.3f){gridOrigin.y=floor;break;}
        }
        if(Mathf.Abs(feet.y-hull.Definition.WeatherDeck)*hull.WorldScale<.3f)gridOrigin.y=hull.Definition.WeatherDeck;
        for(int z=0;z<17;z++)for(int x=0;x<17;x++)
            escapeNodes[z*17+x]=gridOrigin+new Vector3((x-8)*spacing,0,(z-8)*spacing);
        escapeNodes[144]=feet;
        escapeHead=0;escapeTail=1;escapeQueue[0]=144;escapeState[144]=2;escapeParents[144]=-1;
        escapeGoal=target;escapeOrigin=feet;escapeSkip=skipWaypoints;escapeRevision=hull.Revision;escapePending=true;
        return ResumeGridEscape();
    }
    private bool ResumeGridEscape()
    {
        if(hull.Revision!=escapeRevision||boat.TransformVector(feet-escapeOrigin).sqrMagnitude>.5f*.5f||route.Count<=escapeSkip||
            (route[escapeSkip]-escapeGoal).sqrMagnitude>.001f){escapePending=false;return false;}
        if(!NavalRecoverySlice.Take(Time.frameCount,GetInstanceID()))return true;
        long slice=System.Diagnostics.Stopwatch.GetTimestamp();Vector3 target=escapeGoal;
        int found=-1,processed=0;
        while(escapeHead<escapeTail)
        {
            if(processed++>=4||NavalRecoverySlice.Expired(slice))return true;
            int current=escapeQueue[escapeHead++];Vector3 point=escapeNodes[current];
            float remaining=boat.TransformVector(target-point).magnitude;
            // Rejoin only through a clear segment, never merely because a node is closer.
            bool approachStair=NavigationStairs(target)&&remaining<1.6f&&!hull.DeckGapAt(target,NavigationRadius);
            if(current!=144&&remaining<8f&&(SubmergedNavigationPoint(target)||!hull.DeckGapAt(target,NavigationRadius))&&
                (approachStair?NavigationClear(point,target):NavigationWalkClear(point,target)))
            {found=current;break;}
            for(int dz=-1;dz<=1;dz++)for(int dx=-1;dx<=1;dx++)
            {
                if(Mathf.Abs(dx)+Mathf.Abs(dz)!=1)continue;
                int x=current%17+dx,z=current/17+dz;if(x<0||x>16||z<0||z>16)continue;
                int next=z*17+x;if(escapeState[next]==2||escapeState[next]==3)continue;
                Vector3 candidate=escapeNodes[next];
                if(escapeSupport[next]==0)escapeSupport[next]=(byte)((SubmergedNavigationPoint(candidate)||!hull.DeckGapAt(candidate,NavigationRadius))&&Supported(candidate)&&ClearBody(candidate)?1:3);
                escapeState[next]=escapeSupport[next];
                if(escapeState[next]!=1)continue;
                int link=current*4+(dx<0?0:dx>0?1:dz<0?2:3);
                if(escapeLinks[link]==0)escapeLinks[link]=(byte)(NavigationWalkClear(point,candidate)?1:2);
                if(escapeLinks[link]!=1)continue;
                escapeState[next]=2;escapeParents[next]=current;escapeQueue[escapeTail++]=next;
            }
        }
        escapePending=false;if(found<0)return false;
        escapePath.Clear();for(int node=found;node!=144;node=escapeParents[node])escapePath.Add(escapeNodes[node]);
        // Preserve the already tested adjacent links. Recasting every possible
        // simplification chord produced quadratic physics bursts in v274 logs.
        escapeWaypoints.Clear();for(int i=escapePath.Count-1;i>=0;i--)escapeWaypoints.Add(escapePath[i]);
        if(escapeSkip>0)route.RemoveRange(0,escapeSkip);
        route.InsertRange(0,escapeWaypoints);stuck=0;moveSpeed=0;
        // Short collision-limited recovery burst; ordinary walking speed is unchanged.
        recoveryDashUntil=Time.time+.22f;return true;
    }
    private bool RecoverObstruction(Vector3 target)
    {
        if(Time.time<avoidanceAt||hopTime>0||NavigationStairs(feet))return false;
        if(Time.time>recoveryWindow){recoveryAttempts=0;recoveryWindow=Time.time+12f;}
        if(recoveryAttempts>=3)return false;
        recoveryAttempts++;avoidanceAt=Time.time+2f;
        if(!Supported(feet))return EscapeRaisedObstacle();
        // Cache only this bounded local search. A changed damage revision or
        // displaced start cancels a pending search before its next slice.
        System.Array.Clear(escapeSupport,0,escapeSupport.Length);
        System.Array.Clear(escapeLinks,0,escapeLinks.Length);
        // Search includes nodes behind the sailor, so a cannon/rail pocket can be exited first.
        if(Mathf.Abs(target.y-feet.y)*hull.WorldScale<.65f&&GridEscape(target))return true;
        // A transit waypoint can itself be inside a rotated gun/cleat. Rejoin a
        // later same-floor point only through fully supported, capsule-clear links.
        for(int skip=1;skip<Mathf.Min(route.Count,4)&&!NavigationStairs(target);skip++)
        {
            if(Mathf.Abs(route[skip].y-feet.y)*hull.WorldScale>=.65f||NavigationStairs(route[skip]))break;
            if(GridEscape(route[skip],skip))return true;
        }
        Vector3 forward=target-feet;forward.y=0;if(forward.sqrMagnitude<.001f)return false;forward.Normalize();
        Vector3 end=feet+forward*(1.1f/hull.WorldScale),up=Vector3.up*(.65f/hull.WorldScale);
        if(Supported(end)&&ClearBody(end)&&NavigationClear(feet,feet+up)&&NavigationClear(feet+up,end+up)&&NavigationClear(end+up,end))
        {hopTime=.65f;hopPrevious=0;stuck=0;return true;}
        // A dash cannot pass through a solid gun. Only use it along an already clear, supported escape.
        for(int n=0;n<8;n++)
        {
            float angle=n*Mathf.PI*.25f;Vector3 retreat=feet+new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle))*(.9f/hull.WorldScale);
            if(!ClearBody(retreat)||!NavigationWalkClear(feet,retreat))continue;
            route.Insert(0,retreat);recoveryDashUntil=Time.time+.22f;stuck=0;return true;
        }
        return false;
    }
    private bool EscapeRaisedObstacle()
    {
        // A sailor standing on a carriage/rail is not at a supported deck point. Walk down to a clear deck landing.
        float deck=feet.y>=hull.Definition.WeatherDeck-.2f?hull.Definition.WeatherDeck:
            hull.Definition.DeckCount>2&&feet.y>=.625f?.825f:hull.Floor;
        if(Mathf.Abs(feet.y-deck)*hull.WorldScale>2f)return false;
        for(int radius=1;radius<=3;radius++)for(int n=0;n<8;n++)
        {
            float angle=n*Mathf.PI*.25f;
            Vector3 point=feet+new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle))*(radius*.7f/hull.WorldScale);point.y=deck;
            if(!Supported(point)||!ClearBody(point))continue;
            if(!LedgePathClear(feet,point)){
                // First walk clear of the lid at the current height, then descend.
                Vector3 edge=new Vector3(point.x,feet.y,point.z);
                if(!ClearBody(edge)||!NavigationClear(feet,edge)||!NavigationClear(edge,point))continue;
                route.Insert(0,point);route.Insert(0,edge);stuck=0;moveSpeed=0;return true;
            }
            route.Insert(0,point);stuck=0;moveSpeed=0;return true;
        }
        return false;
    }
    private float HopStep(float dt)
    {
        if(hopTime<=0)return 0;
        hopTime=Mathf.Max(0,hopTime-dt);float lift=Mathf.Sin((1f-hopTime/.65f)*Mathf.PI)*.65f;
        if(hopTime==0)lift=0;float delta=lift-hopPrevious;hopPrevious=lift;return delta;
    }
}
