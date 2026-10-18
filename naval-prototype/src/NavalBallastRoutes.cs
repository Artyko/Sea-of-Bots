using System;
using System.Collections.Generic;
// Permanent end ramps keep maintenance access usable around the larger reservoirs.
public static class NavalBallastRoutes
{
    private static bool OnTank(NavalWalkPoint p,float floor,float scale)
    {float y=NavalBallastLayout.Surface(p.X,p.Z,floor,scale);return y>floor+.001f&&p.Y>floor+.04f&&Math.Abs(p.Y-y)<.3f;}
    public static List<NavalWalkPoint> Plan(NavalWalkPoint start,NavalWalkPoint goal,bool partitions,float stairs,float floor,float scale)
    {
        List<NavalWalkPoint> route=new List<NavalWalkPoint>();
        bool from=OnTank(start,floor,scale),to=OnTank(goal,floor,scale);
        if(from&&to&&start.X*goal.X>0&&start.Z*goal.Z>0){route.Add(goal);return route;}
        if(from)
        {
            float sign=start.Z<0?-1f:1f;
            float run=NavalBallastLayout.RampRun/scale/ NavalBallastLayout.Steps;
            float top=NavalBallastLayout.InnerZ+NavalBallastLayout.RampRun/scale;
            if(Math.Abs(start.Z)>top)route.Add(new NavalWalkPoint(start.X,start.Y,sign*top));
            for(int i=NavalBallastLayout.Steps-1;i>=0;i--)
            {
                float z=NavalBallastLayout.InnerZ+(i+.5f)*run;
                if(z>Math.Abs(start.Z)+.01f)continue;
                route.Add(new NavalWalkPoint(start.X,floor+NavalBallastLayout.Height*(i+1)/(NavalBallastLayout.Steps*scale),sign*z));
            }
            start=new NavalWalkPoint(start.X,floor,sign*(NavalBallastLayout.InnerZ-.2f));route.Add(start);
        }
        NavalWalkPoint landing=goal;
        if(to)landing=new NavalWalkPoint(goal.X,floor,(goal.Z<0?-1f:1f)*(NavalBallastLayout.InnerZ-.2f));
        route.AddRange(NavalRepairRoutes.Plan(start,landing,partitions,stairs,floor));
        if(to)
        {
            float sign=goal.Z<0?-1f:1f,run=NavalBallastLayout.RampRun/scale/NavalBallastLayout.Steps;
            for(int i=0;i<NavalBallastLayout.Steps;i++)
            {
                float z=NavalBallastLayout.InnerZ+(i+.5f)*run;if(z>Math.Abs(goal.Z)+.01f)break;
                route.Add(new NavalWalkPoint(goal.X,floor+NavalBallastLayout.Height*(i+1)/(NavalBallastLayout.Steps*scale),sign*z));
            }
            route.Add(goal);
        }
        return route;
    }
}
