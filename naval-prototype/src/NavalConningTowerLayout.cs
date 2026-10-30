using System;
using System.Collections.Generic;

public static class NavalConningTowerLayout
{
    // The whole tower is aft of the open upper stairwell (aft edge -3.88).
    // Its doorway and exterior approach therefore remain on supported deck.
    public const float CenterZ=-4.88f,HalfWidth=1.30f,HalfDepth=.72f,PlatformRise=.12f;
    // Two independent forward doors flank the wheel.  The logged v242 route
    // used x=0 and therefore walked directly into the wheel at z=-4.30.
    public const float DoorInnerX=.16f,DoorOuterX=1.00f,EntryX=.70f,BypassX=1.58f,ApproachZ=-3.94f;
    // Port and starboard openings provide two additional physical exits.
    public const float SideDoorZ=-4.98f,SideDoorHalfDepth=.30f;
    // The lintel starts 3.04 world metres above the raised floor on the
    // ironclad: enough for the game's 2.5 m crew controller to enter without
    // clipping.  A deep upper belt still visibly carries the roof.
    public const float LowerWallTop=.68f,SlitTop=.88f,RoofBottom=1.30f,RoofThickness=.12f;
    public const int StairCount=4;
    public const float StairRise=PlatformRise/StairCount,StairDepth=.10f,StairSpacing=.06f,FirstStairOffset=.22f;
    public static float FrontZ {get{return CenterZ+HalfDepth;}}
    public static float RearZ {get{return CenterZ-HalfDepth;}}
    public static float PlatformY(NavalShipDefinition ship){return ship.WeatherDeck+PlatformRise;}
    public static float StairZ(int step){return FrontZ+FirstStairOffset-step*StairSpacing;}
    public static float StairTop(int step,NavalShipDefinition ship){return ship.WeatherDeck+StairRise*(step+1);}
    public static NavalWalkPoint HelmPost(NavalShipDefinition ship){return new NavalWalkPoint(0,PlatformY(ship),CenterZ);}
    public static bool Inside(NavalWalkPoint point,NavalShipDefinition ship)
    {return point.Y>=ship.WeatherDeck-.05f&&Math.Abs(point.X)<HalfWidth&&point.Z>RearZ&&point.Z<FrontZ;}
    public static bool DoorLane(NavalWalkPoint point,NavalShipDefinition ship)
    {
        float x=Math.Abs(point.X);
        return point.Y>=ship.WeatherDeck-.05f&&x>=DoorInnerX&&x<=DoorOuterX&&point.Z>=CenterZ-.05f&&point.Z<=ApproachZ+.05f;
    }
    private static void Add(List<NavalWalkPoint> route,NavalWalkPoint point)
    {
        if(route.Count>0){var last=route[route.Count-1];if(Math.Abs(last.X-point.X)+Math.Abs(last.Y-point.Y)+Math.Abs(last.Z-point.Z)<.01f)return;}
        route.Add(point);
    }
    private static void AddEntry(List<NavalWalkPoint> route,NavalShipDefinition ship,bool inward,float side)
    {
        float deck=ship.WeatherDeck,platform=PlatformY(ship),x=side*EntryX;
        if(inward)
        {
            Add(route,new NavalWalkPoint(x,deck,ApproachZ));
            for(int step=0;step<StairCount;step++)Add(route,new NavalWalkPoint(x,StairTop(step,ship),StairZ(step)));
            Add(route,new NavalWalkPoint(x,platform,FrontZ-.16f));
            // Finish behind the wheel before crossing toward either command
            // post; this prevents the last diagonal segment clipping a spoke.
            Add(route,new NavalWalkPoint(x,platform,CenterZ-.02f));
        }
        else
        {
            Add(route,new NavalWalkPoint(x,platform,CenterZ-.02f));
            Add(route,new NavalWalkPoint(x,platform,FrontZ-.16f));
            for(int step=StairCount-1;step>=0;step--)Add(route,new NavalWalkPoint(x,StairTop(step,ship),StairZ(step)));
            Add(route,new NavalWalkPoint(x,deck,ApproachZ));
        }
    }
    public static List<NavalWalkPoint> Route(NavalWalkPoint start,List<NavalWalkPoint> planned,NavalShipDefinition ship)
    {
        if(!ship.SteamPowered||planned.Count==0)return planned;
        NavalWalkPoint goal=planned[planned.Count-1];bool from=Inside(start,ship),to=Inside(goal,ship);
        if(!from&&!to)return planned;
        var result=new List<NavalWalkPoint>(planned.Count+8);
        float side=Math.Abs(start.X)>.05f?Math.Sign(start.X):1f;
        if(from)
        {
            AddEntry(result,ship,false,side);Add(result,new NavalWalkPoint(side*BypassX,ship.WeatherDeck,ApproachZ));
        }
        foreach(var point in planned)
        {
            if(Inside(point,ship)||DoorLane(point,ship))continue;
            Add(result,point);
        }
        if(to)
        {
            if(Math.Abs(goal.X)>.05f)side=Math.Sign(goal.X);
            else if(result.Count>0&&Math.Abs(result[result.Count-1].X)>.05f)side=Math.Sign(result[result.Count-1].X);
            Add(result,new NavalWalkPoint(side*BypassX,ship.WeatherDeck,ApproachZ));
            AddEntry(result,ship,true,side);
            Add(result,goal);
        }
        return result;
    }
}
