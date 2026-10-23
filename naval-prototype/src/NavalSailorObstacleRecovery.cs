using UnityEngine;
// Bounded local avoidance shared by repair, firefighting, pumping and return routes.
public partial class NavalRepairSailor
{
    private float avoidanceAt,hopTime,hopPrevious,recoveryDashUntil,recoveryWindow;
    private int recoveryAttempts;
    private readonly Vector3[] escapeNodes=new Vector3[81];
    private readonly int[] escapeParents=new int[81],escapeQueue=new int[81];
    private readonly byte[] escapeState=new byte[81],escapeSupport=new byte[81];
    private readonly byte[] escapeLinks=new byte[81*81];
    private readonly System.Collections.Generic.List<Vector3> escapePath=new System.Collections.Generic.List<Vector3>(32);
    private static RaycastHit[] navigationHits=new RaycastHit[32];
    private bool NavigationStairs(Vector3 p)
    {return NavalShipRoutes.InStairwell(new NavalWalkPoint(p.x,p.y,p.z),hull.Definition);}
    private bool Supported(Vector3 p)
    {
        if(!InsideWalkingDeck(p)||NavigationStairs(p))return false;
        RaycastHit hit;Vector3 world=boat.TransformPoint(p);
        return Physics.Raycast(world+boat.up*.25f,-boat.up,out hit,.55f,PhysicsManager.GetEnvironmentLayerMask(),QueryTriggerInteraction.Ignore)&&
            hit.collider.transform.IsChildOf(boat)&&Mathf.Abs(boat.InverseTransformPoint(hit.point).y-p.y)*hull.WorldScale<.3f;
    }
    private bool NavigationClear(Vector3 a,Vector3 b)
    {
        var body=Controller();float height=body!=null?Mathf.Max(1.8f,body.bounds.size.y):2.6f;
        Vector3 start=boat.TransformPoint(a),delta=boat.TransformVector(b-a);if(delta.sqrMagnitude<.001f)return true;
        int count;
        while(true)
        {
            count=Physics.CapsuleCastNonAlloc(start+Vector3.up*.45f,start+Vector3.up*(height-.4f),.38f,delta.normalized,
                navigationHits,delta.magnitude,PhysicsManager.GetEnvironmentLayerMask(),QueryTriggerInteraction.Ignore);
            if(count<navigationHits.Length)break;System.Array.Resize(ref navigationHits,navigationHits.Length*2);
        }
        bool clear=true;for(int i=0;i<count;i++)if(!navigationHits[i].collider.transform.IsChildOf(robot.transform))clear=false;
        System.Array.Clear(navigationHits,0,count);return clear;
    }
    private bool NavigationWalkClear(Vector3 a,Vector3 b)
    {
        int steps=Mathf.Max(1,Mathf.CeilToInt(boat.TransformVector(b-a).magnitude/.6f));
        for(int i=1;i<=steps;i++)if(!Supported(Vector3.Lerp(a,b,(float)i/steps)))return false;
        return NavigationClear(a,b);
    }
    private readonly System.Collections.Generic.List<Vector3> escapeWaypoints=new System.Collections.Generic.List<Vector3>(32);
    private bool GridEscape(Vector3 target,int skipWaypoints=0)
    {
        System.Array.Clear(escapeState,0,escapeState.Length);
        float spacing=.8f/hull.WorldScale;
        for(int z=0;z<9;z++)for(int x=0;x<9;x++)
            escapeNodes[z*9+x]=feet+new Vector3((x-4)*spacing,0,(z-4)*spacing);
        int head=0,tail=1;escapeQueue[0]=40;escapeState[40]=2;escapeParents[40]=-1;
        int found=-1;
        while(head<tail)
        {
            int current=escapeQueue[head++];Vector3 point=escapeNodes[current];
            float remaining=boat.TransformVector(target-point).magnitude;
            // Rejoin only through a clear segment, never merely because a node is closer.
            bool approachStair=NavigationStairs(target)&&remaining<1.6f;
            if(current!=40&&
                (approachStair?NavigationClear(point,target):NavigationWalkClear(point,target)))
            {found=current;break;}
            for(int dz=-1;dz<=1;dz++)for(int dx=-1;dx<=1;dx++)
            {
                if(Mathf.Abs(dx)+Mathf.Abs(dz)!=1)continue;
                int x=current%9+dx,z=current/9+dz;if(x<0||x>8||z<0||z>8)continue;
                int next=z*9+x;if(escapeState[next]==2||escapeState[next]==3)continue;
                Vector3 candidate=escapeNodes[next];
                if(escapeSupport[next]==0)escapeSupport[next]=(byte)(Supported(candidate)&&ClearBody(candidate)?1:3);
                escapeState[next]=escapeSupport[next];
                if(escapeState[next]!=1)continue;
                int link=current*81+next;
                if(escapeLinks[link]==0)escapeLinks[link]=(byte)(NavigationWalkClear(point,candidate)?1:2);
                if(escapeLinks[link]!=1)continue;
                escapeState[next]=2;escapeParents[next]=current;escapeQueue[tail++]=next;
            }
        }
        if(found<0)return false;
        escapePath.Clear();for(int node=found;node!=40;node=escapeParents[node])escapePath.Add(escapeNodes[node]);
        // Simplify safe straight runs, retaining backing-out and corner waypoints.
        Vector3 start=feet;int index=escapePath.Count-1;
        escapeWaypoints.Clear();
        while(index>=0)
        {
            int furthest=index;for(int j=index-1;j>=0;j--)if(NavigationWalkClear(start,escapePath[j]))furthest=j;
            escapeWaypoints.Add(escapePath[furthest]);start=escapePath[furthest];index=furthest-1;
        }
        if(skipWaypoints>0)route.RemoveRange(0,skipWaypoints);
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
        // These caches live only for this synchronous recovery attempt: another
        // sailor, moving equipment or a new frame must get fresh physics queries.
        System.Array.Clear(escapeSupport,0,escapeSupport.Length);
        System.Array.Clear(escapeLinks,0,escapeLinks.Length);
        // Search includes nodes behind the sailor, so a cannon/rail pocket can be exited first.
        if(Mathf.Abs(target.y-feet.y)*hull.WorldScale<.3f&&GridEscape(target))return true;
        // A transit waypoint can itself be inside a rotated gun/cleat. Rejoin a
        // later same-floor point only through fully supported, capsule-clear links.
        for(int skip=1;skip<Mathf.Min(route.Count,4)&&!NavigationStairs(target);skip++)
        {
            if(Mathf.Abs(route[skip].y-feet.y)*hull.WorldScale>=.3f||NavigationStairs(route[skip]))break;
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
            if(!Supported(point)||!ClearBody(point)||!LedgePathClear(feet,point))continue;
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
