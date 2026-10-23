using System;
class AssaultDeckChecks
{
 static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
 static void Main(){int cases=0;foreach(var ship in NavalShipDefinition.Choices){
 float[] floors=ship.DeckCount>2?new float[]{ship.HoldFloor,.825f,ship.WeatherDeck}:new float[]{ship.HoldFloor,ship.WeatherDeck};
 foreach(float from in floors)foreach(float to in floors){
 var start=new NavalWalkPoint(1.4f,from,4);var goal=new NavalWalkPoint(-1.35f,to,-1);
 var route=NavalShipRoutes.Plan(start,goal,ship);Check(route.Count>0,"Enemy-deck pursuit route exists");var last=route[route.Count-1];
 Check(Math.Abs(last.X-goal.X)+Math.Abs(last.Y-goal.Y)+Math.Abs(last.Z-goal.Z)<.001,"Pursuit reaches correct opponent floor");
 if(from!=to){bool stairs=false;foreach(var p in route)if(NavalStairTraffic.InPassage(p,ship))stairs=true;Check(stairs,"Different-deck pursuit uses ship stairs");}
 Check(NavalAssaultMath.Floor(to,ship)==to,"All playable floors recognized");cases++;
 }
 Check(!NavalAssaultMath.SameDeck(ship.HoldFloor,ship.WeatherDeck,ship.Scale),"Upper and lower enemies are not fought through deck");
 }
 Check(NavalAssaultMath.Melee(2,0,true),"Nearby enemy activates native melee");
 Check(!NavalAssaultMath.Melee(2,2,true)&&!NavalAssaultMath.Melee(2,0,false),"Height or solid obstruction requires route");
 Check(!NavalAssaultMath.Melee(3,0,true)&&NavalAssaultMath.Melee(3,0,true,true),"Hysteresis prevents attack/walk switching at reach boundary");
 Console.WriteLine("PASS: "+cases+" upper/middle/hold routes on all ship types, floor-aware target pursuit, obstruction checks and melee hysteresis.");
 }
}
