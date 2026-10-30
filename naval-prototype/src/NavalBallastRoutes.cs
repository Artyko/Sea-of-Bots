using System;
using System.Collections.Generic;
// Permanent end ramps keep maintenance access usable around the larger reservoirs.
public static class NavalBallastRoutes
{
    private static float Surface(NavalWalkPoint p,float floor,float scale,NavalBallastLayout.Dimensions d)
    {
        if(Math.Abs(p.X)<d.X-d.Width*.5f||Math.Abs(p.X)>d.X+d.Width*.5f||Math.Abs(p.Z)<d.InnerZ||Math.Abs(p.Z)>d.Z+d.Length*.5f)return floor;
        int step=Math.Max(1,Math.Min(d.Steps,1+(int)((Math.Abs(p.Z)-d.InnerZ)/(d.RampRun/scale/d.Steps))));
        return floor+d.Height*step/(d.Steps*scale);
    }
    private static bool OnTank(NavalWalkPoint p,float floor,float scale,NavalBallastLayout.Dimensions d)
    {float y=Surface(p,floor,scale,d);return y>floor+.001f&&p.Y>floor+.04f&&Math.Abs(p.Y-y)<.3f;}
    public static List<NavalWalkPoint> Plan(NavalWalkPoint start,NavalWalkPoint goal,bool partitions,float stairs,float floor,float scale)
    {return Plan(start,goal,partitions,stairs,floor,scale,new NavalBallastLayout.Dimensions(NavalBallastLayout.X,NavalBallastLayout.Z,NavalBallastLayout.Width,NavalBallastLayout.Length,NavalBallastLayout.Height,NavalBallastLayout.RampRun,NavalBallastLayout.Steps),1.65f,1.4f);}
    public static List<NavalWalkPoint> Plan(NavalWalkPoint start,NavalWalkPoint goal,bool partitions,float stairs,float floor,NavalShipDefinition ship)
    {return Plan(start,goal,partitions,stairs,floor,ship.Scale,NavalBallastLayout.For(ship),ship.SteamPowered?1.20f:1.65f,ship.SteamPowered?1.20f:1.4f);}
    private static List<NavalWalkPoint> Plan(NavalWalkPoint start,NavalWalkPoint goal,bool partitions,float stairs,float floor,float scale,NavalBallastLayout.Dimensions d,float lowerCorridor,float upperCorridor)
    {
        List<NavalWalkPoint> route=new List<NavalWalkPoint>();
        bool from=OnTank(start,floor,scale,d),to=OnTank(goal,floor,scale,d);
        if(from&&to&&start.X*goal.X>0&&start.Z*goal.Z>0){route.Add(goal);return route;}
        if(from)
        {
            float sign=start.Z<0?-1f:1f;
            float run=d.RampRun/scale/d.Steps;
            float top=d.InnerZ+d.RampRun/scale;
            if(Math.Abs(start.Z)>top)route.Add(new NavalWalkPoint(start.X,start.Y,sign*top));
            for(int i=d.Steps-1;i>=0;i--)
            {
                float z=d.InnerZ+(i+.5f)*run;
                if(z>Math.Abs(start.Z)+.01f)continue;
                route.Add(new NavalWalkPoint(start.X,floor+d.Height*(i+1)/(d.Steps*scale),sign*z));
            }
            start=new NavalWalkPoint(start.X,floor,sign*(d.InnerZ-.2f));route.Add(start);
        }
        NavalWalkPoint landing=goal;
        if(to)landing=new NavalWalkPoint(goal.X,floor,(goal.Z<0?-1f:1f)*(d.InnerZ-.2f));
        route.AddRange(NavalRepairRoutes.Plan(start,landing,partitions,stairs,floor,lowerCorridor,upperCorridor));
        if(to)
        {
            float sign=goal.Z<0?-1f:1f,run=d.RampRun/scale/d.Steps;
            for(int i=0;i<d.Steps;i++)
            {
                float z=d.InnerZ+(i+.5f)*run;if(z>Math.Abs(goal.Z)+.01f)break;
                route.Add(new NavalWalkPoint(goal.X,floor+d.Height*(i+1)/(d.Steps*scale),sign*z));
            }
            route.Add(goal);
        }
        return route;
    }
}
