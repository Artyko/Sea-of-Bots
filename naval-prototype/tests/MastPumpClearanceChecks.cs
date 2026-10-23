using System;
class MastPumpClearanceChecks
{
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 static void Main()
 {
  foreach(var ship in NavalShipDefinition.Choices)
  {
   Check(Math.Abs((ship.WeatherDeck-ship.MastFootY)*ship.Scale-.25f)<.001,"Mast is stepped only 25cm into weather deck");
   Check(Math.Abs(ship.MastFootWidth-.65f)<.001,"Original broad deck footing width restored");
   Check(ship.HoldFloor+2.6f/ship.Scale<ship.MastFootY,"Full-size sailor clears mast below weather deck");
   if(ship.DeckCount>2)Check(.825f+2.6f/ship.Scale<ship.MastFootY,"Middle deck also clear of hanging mast");
   foreach(float side in new[]{-1.65f,1.65f})foreach(float z in new[]{-4.5f,1.2f,4.8f})foreach(float approach in new[]{0f,-.55f,.55f})
   {
    var goal=new NavalWalkPoint(approach,ship.HoldFloor,-4.9f);
    var route=NavalShipRoutes.Plan(new NavalWalkPoint(side,ship.WeatherDeck,z),goal,ship);
    Check(route.Count>0,"Shared pump route exists");var end=route[route.Count-1];
    Check(Math.Abs(end.X-goal.X)+Math.Abs(end.Y-goal.Y)+Math.Abs(end.Z-goal.Z)<.001,"Pump route ends at selected approach");
    foreach(var point in route)Check(point.Y>=ship.HoldFloor-.01f&&point.Y<=ship.WeatherDeck+.01f,"Pump route stays between supported deck heights");
   }
  }
  Console.WriteLine("PASS: deck-stepped mast with broad footing, full-size lower/middle-deck clearance and 54 pump approaches on all ship profiles (geometry model, not live PhysX).");
 }
}
