using System;
using System.Collections.Generic;
// Deck transitions share the same stair geometry. Two-deck profiles retain the original route exactly.
public static class NavalShipRoutes
{
    public static bool InStairwell(NavalWalkPoint p,NavalShipDefinition ship)
    {
        if(NavalWalkRoutes.InStairwell(p.X,p.Y,p.Z,ship.StairWidth*.5f+.175f,ship.HoldFloor))return true;
        return ship.DeckCount>2&&NavalWalkRoutes.InStairwell(p.X,p.Y-ship.UpperDeckOffset,p.Z,ship.StairWidth*.5f+.175f,ship.HoldFloor);
    }
    private static void Shift(List<NavalWalkPoint> route,List<NavalWalkPoint> points,float offset)
    {foreach(var p in points)route.Add(new NavalWalkPoint(p.X,p.Y+offset,p.Z));}
    private static void UpperPlan(List<NavalWalkPoint> route,NavalWalkPoint start,NavalWalkPoint goal,NavalShipDefinition ship)
    {
        float offset=ship.UpperDeckOffset;start.Y-=offset;goal.Y-=offset;
        Shift(route,NavalBallastRoutes.Plan(start,goal,false,ship.StairWidth,ship.HoldFloor,ship.Scale),offset);
    }
    public static List<NavalWalkPoint> Plan(NavalWalkPoint start,NavalWalkPoint goal,NavalShipDefinition ship)
    {return NavalStairTraffic.Apply(start,PlanCore(start,goal,ship),ship);}
    private static List<NavalWalkPoint> PlanCore(NavalWalkPoint start,NavalWalkPoint goal,NavalShipDefinition ship)
    {
        if(ship.DeckCount<3)return NavalBallastRoutes.Plan(start,goal,ship.Partitions,ship.StairWidth,ship.HoldFloor,ship.Scale);
        float offset=ship.UpperDeckOffset;var route=new List<NavalWalkPoint>();
        bool highGoal=goal.Y>.825f+ship.UpperDeckOffset*.5f;
        if(NavalWalkRoutes.InStairwell(start.X,start.Y-offset,start.Z,ship.StairWidth*.5f+.175f,ship.HoldFloor))
        {
            route.Add(new NavalWalkPoint(0,start.Y,start.Z));
            if(highGoal)Shift(route,NavalWalkRoutes.UpFrom(start.Y-offset,start.Z,ship.HoldFloor),offset);
            else foreach(var p in NavalWalkRoutes.Down(ship.HoldFloor))if(p.Y+offset<=start.Y+.03f&&p.Z<=start.Z+.1f)route.Add(new NavalWalkPoint(p.X,p.Y+offset,p.Z));
            start=route[route.Count-1];
        }
        bool highStart=start.Y>.825f+ship.UpperDeckOffset*.5f;
        if(highStart&&highGoal){UpperPlan(route,start,goal,ship);return route;}
        if(highStart)
        {
            UpperPlan(route,start,new NavalWalkPoint(0,ship.WeatherDeck,.8f),ship);
            Shift(route,NavalWalkRoutes.Down(ship.HoldFloor),offset);start=route[route.Count-1];
        }
        bool middleGoal=!highGoal&&goal.Y>.3f;
        if(middleGoal){
            bool lowerStair=NavalWalkRoutes.InStairwell(start.X,start.Y,start.Z,ship.StairWidth*.5f+.175f,ship.HoldFloor);
            if(start.Y<.3f||lowerStair){
                route.AddRange(NavalBallastRoutes.Plan(start,new NavalWalkPoint(0,.825f,.8f),ship.Partitions,ship.StairWidth,ship.HoldFloor,ship.Scale));
                start=route[route.Count-1];
            }
            UpperPlan(route,start,goal,ship);return route;
        }
        float middleTank=NavalBallastLayout.Surface(start.X,start.Z,.825f,ship.Scale);
        if(middleTank>.826f&&System.Math.Abs(start.Y-middleTank)<.2f){
            UpperPlan(route,start,new NavalWalkPoint(start.X<0?-1.65f:1.65f,.825f,start.Z),ship);
            start=route[route.Count-1];
        }
        NavalWalkPoint landing=highGoal?new NavalWalkPoint(0,.825f,-3.65f):goal;
        route.AddRange(NavalBallastRoutes.Plan(start,landing,ship.Partitions,ship.StairWidth,ship.HoldFloor,ship.Scale));
        if(highGoal)
        {
            Shift(route,NavalWalkRoutes.UpFrom(ship.HoldFloor,-3.65f,ship.HoldFloor),offset);
            UpperPlan(route,route[route.Count-1],goal,ship);
        }
        return route;
    }
}
