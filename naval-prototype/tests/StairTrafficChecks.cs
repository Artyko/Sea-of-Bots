using System;
class StairTrafficChecks {
 static void Check(bool value,string why){if(!value)throw new Exception(why);}
 static void Main(){int routes=0;
 foreach(var ship in NavalShipDefinition.Choices){
 float lane=NavalStairTraffic.Lane(ship.Scale,ship.StairWidth);
 Check(lane>0,"Each flight has a directional lane");
 Check(NavalStairTraffic.FriendlyPass(true,true,true,1),"Overlapping full-size friendly capsules can pass on narrow stairs");
 Check((ship.StairWidth*.5f-lane)*ship.Scale>=.8f,"Lane fits inside smallest actual stair width");
 for(int flight=0;flight<ship.DeckCount-1;flight++){
 float offset=flight==0?0:ship.UpperDeckOffset;
 var lower=new NavalWalkPoint(1.4f,ship.HoldFloor+offset,-4.5f);
 var upper=new NavalWalkPoint(-1.4f,.825f+offset,2);
 foreach(bool up in new bool[]{true,false}){
 var start=up?lower:upper;var goal=up?upper:lower;var path=NavalShipRoutes.Plan(start,goal,ship);int treads=0;
 foreach(var point in path)if(point.Z> -2.8f&&point.Z<.2f&&Math.Abs(point.Y-offset-NavalWalkRoutes.SurfaceY(point.Z,ship.HoldFloor))<.06f){
 Check(up?point.X>0:point.X<0,"All stair tread points stay on travel-right lane");treads++;}
 Check(treads>8,"Route traverses real stair geometry");var last=path[path.Count-1];Check(last.X==goal.X&&last.Y==goal.Y&&last.Z==goal.Z,"Traffic lanes preserve task endpoint");routes++;
 }
 }
 Check(!NavalStairTraffic.InPassage(new NavalWalkPoint(0,ship.HoldFloor,.1f),ship),"Dry floor under treads is not stair traffic");
 Check(NavalStairTraffic.FriendlyPass(true,true,false,1),"Friendly meeting at landing cannot deadlock");
 Check(!NavalStairTraffic.FriendlyPass(false,true,true,1),"Enemies retain collisions");
 Check(!NavalStairTraffic.FriendlyPass(true,false,false,1),"Open-deck collisions are restored");
 Check(!NavalStairTraffic.FriendlyPass(true,true,true,3),"Distant friend does not need passing override");
 }
 Console.WriteLine("PASS: "+routes+" opposite-direction stair routes, full-size lane clearance, both galleon flights, exact destinations, understair classification and friendly-only narrow-passage fallback.");
 }
}
