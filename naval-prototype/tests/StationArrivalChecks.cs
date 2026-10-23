using System;
class StationArrivalChecks
{
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 static void Main()
 {
  Check(NavalStationArrival.CanAnchor(0)&&NavalStationArrival.CanAnchor(.15f),"Small station alignment allowed after walking arrival");
  Check(!NavalStationArrival.CanAnchor(.17f)&&!NavalStationArrival.CanAnchor(4)&&!NavalStationArrival.CanAnchor(100),"Remote or stair positions cannot snap back to unchanged station");
  foreach(var ship in NavalShipDefinition.Choices)
  {
   float stairsToStation=6*ship.Scale;Check(!NavalStationArrival.CanAnchor(stairsToStation*stairsToStation),"No long teleport on any ship size");
   Check(1.35f<ship.GunMountX,"Inner fire approach is behind cannon mounting");
  }
  Console.WriteLine("PASS: station arrival guard rejects distant snapback and inner fire approach remains behind guns.");
 }
}
