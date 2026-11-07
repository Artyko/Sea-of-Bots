using System;
using System.Collections.Generic;
using UnityEngine;
class StairRepairAccessChecks
{
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    static void Main(){
        foreach(var ship in new[]{NavalShipDefinition.Dreadnought,NavalShipDefinition.Brigantine})
        for(int flight=0;flight<ship.DeckCount-1;flight++)foreach(int step in new[]{5,24,42}){
            var goal=NavalStairRepairSites.Candidate(ship,flight,step,1);
            Check(NavalStairRepairSites.Flight(goal,ship)==flight,"Repair point lies on its physical tread");
            float offset=flight==0?0:ship.UpperDeckOffset,floor=flight==0?ship.HoldFloor:ship.UpperStairFloor;
            foreach(bool above in new[]{false,true}){
                var start=new NavalWalkPoint(0,(above?.825f:floor)+offset,above?NavalWalkRoutes.TopLandingZ:NavalWalkRoutes.BottomLandingZ);
                var route=NavalSupplyRoutes.Plan(start,goal,ship);
                Check(route.Count>1,"Explicit tread route to repair from either landing");
                var end=route[route.Count-1];Check(Math.Abs(end.Y-goal.Y)<.001f&&Math.Abs(end.Z-goal.Z)<.001f,"Repair height retained");
                bool found=false;foreach(var p in route)if(NavalStairRepairSites.Flight(p,ship)==flight&&Math.Abs(p.Z-goal.Z)>.05f)found=true;
                Check(found,"Route approaches along steps instead of dropping from a deck");
            }
        }
        var dread=NavalShipDefinition.Dreadnought;
        var from=new NavalWalkPoint(-.42f,dread.WeatherDeck,4.90f);
        var goalDeck=new NavalWalkPoint(1.50f,dread.WeatherDeck,2.63f);
        var detour=NavalCapitalLayout.Route(from,new List<NavalWalkPoint>{goalDeck},dread);
        Check(detour.Count>1,"Blast-displaced bow start leaves inflated turret bounds before passing bridge");
        foreach(var p in detour)Check(!(Math.Abs(p.X)<NavalRaisedBridge.HalfWidth(dread)+.8f/dread.Scale&&Math.Abs(p.Z-NavalCapitalLayout.BridgeZ)<.50f+.8f/dread.Scale),"Bridge detour clears actual widened walls");
        Console.WriteLine("PASS: repair access from both ends of both flights, correct tread elevations and displaced bow/bridge detour.");
    }
}
