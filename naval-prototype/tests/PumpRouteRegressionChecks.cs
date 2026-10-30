using System;
using System.Collections.Generic;

class PumpRouteRegressionChecks
{
    static void Check(bool value,string why){if(!value)throw new Exception(why);}
    static void Main()
    {
        NavalShipDefinition ship=NavalShipDefinition.Ironclad;
        CheckRepeated("middle-to-lower",
            new NavalWalkPoint(-.14f,.85f,.60f),
            new NavalWalkPoint(0f,ship.HoldFloor,-4.9f+.65f/ship.Scale),ship,true);
        CheckRepeated("weather-to-middle",
            new NavalWalkPoint(-.23f,ship.WeatherDeck+.025f,1.03f),
            new NavalWalkPoint(0f,.825f,-4.9f+.65f/ship.Scale),ship,false);
        Console.WriteLine("PASS: repeated logged ironclad pump routes retain explicit continuous stair exits.");
    }
    static void CheckRepeated(string name,NavalWalkPoint start,NavalWalkPoint goal,NavalShipDefinition ship,bool needsLowerLanding)
    {
        for(int pass=0;pass<40;pass++)CheckRoute(name+" pass="+pass,start,goal,ship,needsLowerLanding);
    }
    static void CheckRoute(string name,NavalWalkPoint start,NavalWalkPoint goal,NavalShipDefinition ship,bool needsLowerLanding)
    {
        List<NavalWalkPoint> route=NavalSupplyRoutes.Plan(start,goal,ship);
        Check(route.Count>0,name+" route exists");
        NavalWalkPoint previous=start;bool changedDeck=false,lowerLanding=!needsLowerLanding;
        foreach(NavalWalkPoint point in route)
        {
            float rise=Math.Abs(point.Y-previous.Y)*ship.Scale;
            Check(rise<.65f,name+" contains a direct vertical shortcut: "+Format(previous)+" -> "+Format(point));
            if(rise>.02f)changedDeck=true;
            if(Math.Abs(point.Y-ship.HoldFloor)<.01f&&Math.Abs(point.Z+3.65f)<.12f)lowerLanding=true;
            previous=point;
        }
        Check(changedDeck,name+" route includes a stair flight");
        Check(lowerLanding,name+" loses the lower stair exit and walks underneath the flight");
        Check(Math.Abs(previous.X-goal.X)<.001f&&Math.Abs(previous.Y-goal.Y)<.001f&&Math.Abs(previous.Z-goal.Z)<.001f,name+" preserves pump approach");
    }
    static string Format(NavalWalkPoint p){return "("+p.X.ToString("F2")+","+p.Y.ToString("F2")+","+p.Z.ToString("F2")+")";}
}
