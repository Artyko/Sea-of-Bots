using System;
using System.Collections.Generic;
// Repairing a stair needs actual tread heights, not a waypoint projected onto a deck.
public static class NavalStairRepairSites
{
    public static int Flight(NavalWalkPoint p,NavalShipDefinition ship){
        if(ship.Destroyer||Math.Abs(p.X)>ship.StairWidth*.5f||p.Z<=-3f||p.Z>=.5f)return -1;
        for(int f=0;f<ship.DeckCount-1;f++){
            float offset=f==0?0:ship.UpperDeckOffset,floor=f==0?ship.HoldFloor:ship.UpperStairFloor;
            if(Math.Abs(p.Y-offset-NavalWalkRoutes.SurfaceY(p.Z,floor))*ship.Scale<.25f)return f;
        }
        return -1;
    }
    public static NavalWalkPoint Candidate(NavalShipDefinition ship,int flight,int step,int lane){
        float offset=flight==0?0:ship.UpperDeckOffset,floor=flight==0?ship.HoldFloor:ship.UpperStairFloor;
        float z=-3f+(step+.5f)*3.5f/NavalWalkRoutes.StepCount;
        return new NavalWalkPoint(lane*NavalStairTraffic.Lane(ship.Scale,ship.StairWidth),offset+NavalWalkRoutes.SurfaceY(z,floor),z);
    }
    public static bool TryRoute(NavalWalkPoint start,NavalWalkPoint goal,NavalShipDefinition ship,out List<NavalWalkPoint> route){
        route=null;int flight=Flight(goal,ship);if(flight<0)return false;
        route=new List<NavalWalkPoint>();float z=start.Z;
        if(Flight(start,ship)!=flight){
            float offset=flight==0?0:ship.UpperDeckOffset,floor=flight==0?ship.HoldFloor:ship.UpperStairFloor;
            bool above=Math.Abs(start.Y-(.825f+offset))<Math.Abs(start.Y-(floor+offset));
            var entry=new NavalWalkPoint(0,(above?.825f:floor)+offset,above?NavalWalkRoutes.TopLandingZ:NavalWalkRoutes.BottomLandingZ);
            route.AddRange(NavalSupplyRoutes.Plan(start,entry,ship));z=entry.Z;
            if(above)route.Add(new NavalWalkPoint(goal.X,.825f+offset,NavalWalkRoutes.UpperTreadEntryZ));
        }
        bool up=goal.Z>=z;
        for(int n=0;n<NavalWalkRoutes.StepCount;n++){
            int step=up?n:NavalWalkRoutes.StepCount-1-n;var point=Candidate(ship,flight,step,0);point.X=goal.X;
            if(up?point.Z>z&&point.Z<goal.Z:point.Z<z&&point.Z>goal.Z)route.Add(point);
        }
        route.Add(goal);return true;
    }
}
