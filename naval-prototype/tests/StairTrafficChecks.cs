using System;
class StairTrafficChecks {
 static void Check(bool value,string why){if(!value)throw new Exception(why);}
 static void Main(){int routes=0;
 var profiles=new System.Collections.Generic.List<NavalShipDefinition>(NavalShipDefinition.Choices);profiles.AddRange(NavalShipDefinition.SteamChoices);
 foreach(var ship in profiles){
 float lane=NavalStairTraffic.Lane(ship.Scale,ship.StairWidth);
 Check(lane>0,"Each flight has a directional lane");
 Check(NavalStairTraffic.FriendlyPass(true,true,true,1),"Overlapping full-size friendly capsules can pass on narrow stairs");
 Check((ship.StairWidth*.5f-lane)*ship.Scale>=.8f,"Lane fits inside smallest actual stair width");
 for(int flight=0;flight<ship.DeckCount-1;flight++){
 float offset=flight==0?0:ship.UpperDeckOffset,flightFloor=flight==0?ship.HoldFloor:ship.UpperStairFloor;
 var lower=new NavalWalkPoint(1.4f,flight==0?ship.HoldFloor:.825f,-4.5f);
 var upper=new NavalWalkPoint(-1.4f,flight==0?.825f:ship.WeatherDeck,2);
 foreach(bool up in new bool[]{true,false}){
 var start=up?lower:upper;var goal=up?upper:lower;var path=NavalShipRoutes.Plan(start,goal,ship);int treads=0;
 foreach(var point in path)if(point.Z> -2.8f&&point.Z<.2f&&Math.Abs(point.Y-offset-NavalWalkRoutes.SurfaceY(point.Z,flightFloor))<.06f){
 Check(up?point.X>0:point.X<0,"All stair tread points stay on travel-right lane");treads++;}
 Check(treads>8,"Route traverses real stair geometry for "+ship.Name+" flight="+flight+" up="+up+" treads="+treads);var last=path[path.Count-1];Check(last.X==goal.X&&last.Y==goal.Y&&last.Z==goal.Z,"Traffic lanes preserve task endpoint for "+ship.Name+" flight="+flight+" up="+up+" last="+last.X+","+last.Y+","+last.Z+" goal="+goal.X+","+goal.Y+","+goal.Z);routes++;
 }
 }
 Check(!NavalStairTraffic.InPassage(new NavalWalkPoint(0,ship.HoldFloor,.1f),ship),"Dry floor under treads is not stair traffic");
 if(ship.DeckCount>2){
  var upperLanding=new NavalWalkPoint(-.14f,ship.WeatherDeck+.025f,.80f);
  var middleLanding=new NavalWalkPoint(-.14f,.83f,.80f);
  Check(NavalStairTraffic.PassageLevel(upperLanding,ship)==1&&NavalStairTraffic.PassageLevel(middleLanding,ship)==0,"Each logged landing belongs to its own flight");
  Check(!NavalStairTraffic.SamePassage(upperLanding,middleLanding,ship),"Separate flights cannot suppress a vertical-route rebuild");
  if(ship.SteamPowered){
   var loggedTop=new NavalWalkPoint(-.13f,ship.WeatherDeck+.025f,.51f);
   var pump=new NavalWalkPoint(0,.825f,-4.9f);
   var pumpRoute=NavalSupplyRoutes.Plan(loggedTop,pump,ship);bool upperFlight=false;
   foreach(var point in pumpRoute)if(NavalStairTraffic.PassageLevel(point,ship)==1){upperFlight=true;Check(Math.Abs(point.X)<.5f,"Cross-deck route keeps logged upper landing inside the real flight instead of battery lane");}
   Check(upperFlight,"Logged pump route enters the upper stair flight");
   var loggedMiddle=new NavalWalkPoint(-.14f,.85f,.51f);
   var bilge=new NavalWalkPoint(.72f,ship.HoldFloor,1.71f);
   var bilgeRoute=NavalSupplyRoutes.Plan(loggedMiddle,bilge,ship);bool lowerFlight=false;
   foreach(var point in bilgeRoute)if(NavalStairTraffic.PassageLevel(point,ship)==0)lowerFlight=true;
   Check(lowerFlight,"Logged firefighting-water route enters the lower stair flight");
   var bowFire=new NavalWalkPoint(-1.35f,ship.WeatherDeck,3.70f);
   var bowRoute=NavalSupplyRoutes.Plan(bilge,bowFire,ship);
   for(int i=0;i+1<bowRoute.Count;i++){var point=bowRoute[i];if(point.Y>ship.WeatherDeck-.12f&&point.Z>=3.05f&&point.Z<=4.65f)Check(Math.Abs(point.X)>=1.3f,"Bow fire route stays outside the main mount shield: i="+i+"/"+bowRoute.Count+" p="+point.X+","+point.Y+","+point.Z+" next="+bowRoute[i+1].X+","+bowRoute[i+1].Y+","+bowRoute[i+1].Z);}
   var aftWeather=new NavalWalkPoint(-.21f,ship.WeatherDeck,-4.64f);
   var middleStore=new NavalWalkPoint(-.60f,.825f,1.35f);
   var storeRoute=NavalSupplyRoutes.Plan(aftWeather,middleStore,ship);bool bypass=false;var prior=aftWeather;
   foreach(var point in storeRoute){if(!NavalStairTraffic.SamePassage(prior,point,ship)&&prior.Y>ship.WeatherDeck-.12f&&point.Y>ship.WeatherDeck-.12f&&NavalSupplyRoutes.Hits(prior,point,-1.18f,1.18f,-3.88f,.78f))throw new Exception("Logged plank route crosses open upper stair: "+prior.X+","+prior.Y+","+prior.Z+" -> "+point.X+","+point.Y+","+point.Z);if(point.Y>ship.WeatherDeck-.12f&&Math.Abs(point.X)>1.2f&&point.Z<.81f)bypass=true;prior=point;}
   Check(bypass,"Aft weather-deck worker receives a side bypass before descending");
   var gunExit=NavalShipRoutes.Plan(new NavalWalkPoint(-ship.GunPostX,ship.WeatherDeck,NavalShipDefinition.GunZ(ship,0)),new NavalWalkPoint(-1.4f,ship.WeatherDeck,1.05f),ship);
   Check(gunExit.Count>1&&gunExit[0].X<=-NavalShipRoutes.SteamBatteryLane+.001f&&Math.Abs(gunExit[0].Z-NavalShipDefinition.GunZ(ship,0))<.01f,"Aft gunner first exits laterally before crossing the upper stair opening");
   var underStart=new NavalWalkPoint(.60f,.85f,.12f);var underGoal=new NavalWalkPoint(.58f,.825f,2.47f);
   var underRoute=NavalShipRoutes.Plan(underStart,underGoal,ship);Check(underRoute.Count>1&&Math.Abs(underRoute[0].X)>=ship.StairWidth*.5f+.24f,
    "Middle-deck route immediately leaves the low underside of the upper stair");
  }
 }
 Check(NavalStairTraffic.FriendlyPass(true,true,false,1),"Friendly meeting at landing cannot deadlock");
 Check(!NavalStairTraffic.FriendlyPass(false,true,true,1),"Enemies retain collisions");
 Check(!NavalStairTraffic.FriendlyPass(true,false,false,1),"Open-deck collisions are restored");
 Check(!NavalStairTraffic.FriendlyPass(true,true,true,3),"Distant friend does not need passing override");
 Check(NavalWalkRoutes.Reached(.18f,.05f,0,4f),"A steam sailor offset 0.72m across a tread advances instead of stalling");
 Check(!NavalWalkRoutes.Reached(.18f,.05f,.08f,4f),"Tread arrival remains strict along the direction of travel");
 }
 Console.WriteLine("PASS: "+routes+" opposite-direction stair routes, full-size lane clearance, both galleon flights, exact destinations, understair classification and friendly-only narrow-passage fallback.");
 }
}
