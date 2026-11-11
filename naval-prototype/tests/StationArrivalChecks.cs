using System;
class StationArrivalChecks
{
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 static void Main()
 {
  Check(NavalStationArrival.CanAnchor(0)&&NavalStationArrival.CanAnchor(.15f),"Small station alignment allowed after walking arrival");
  Check(!NavalStationArrival.CanAnchor(.17f)&&!NavalStationArrival.CanAnchor(4)&&!NavalStationArrival.CanAnchor(100),"Remote or stair positions cannot snap back to unchanged station");
  Check(NavalStationArrival.CanRideMovingPost(.99f)&&NavalStationArrival.CanRideMovingPost(1f),"Seated crew follows bounded turntable motion");
  Check(!NavalStationArrival.CanRideMovingPost(1.01f)&&!NavalStationArrival.CanRideMovingPost(100f),"Moving post cannot retrieve displaced crew remotely");
  Check(NavalStationArrival.KeepMovingPost(true,.25f),"A gunner already at the previous rotating seat stays aboard while the mount trains");
  Check(!NavalStationArrival.KeepMovingPost(false,.25f)&&!NavalStationArrival.KeepMovingPost(true,1.01f),"Rear approach is retained for a gunner who never reached the seat or was genuinely displaced");
  Check(NavalStationArrival.AtMainBatterySeat(.02f,.10f,.02f,4f),"Raised main-battery footing is accepted without a fragile support ray");
  Check(!NavalStationArrival.AtMainBatterySeat(0f,.25f,0f,4f)&&!NavalStationArrival.AtMainBatterySeat(.2f,0f,0f,4f),"Main-battery seat cannot accept a sailor above or beside the turntable");
  Check(NavalStationArrival.OnRaisedSupport(.5f/12f,.15f/12f,0,12f),"Supported operator may finish within capsule skin clearance");
  Check(!NavalStationArrival.OnRaisedSupport(1f/12f,0,0,12f),"One metre away cannot count as station arrival");
  foreach(var ship in NavalShipDefinition.Choices)
  {
   float stairsToStation=6*ship.Scale;Check(!NavalStationArrival.CanAnchor(stairsToStation*stairsToStation),"No long teleport on any ship size");
   Check(1.35f<ship.GunMountX,"Inner fire approach is behind cannon mounting");
  }
  Console.WriteLine("PASS: station arrival guard rejects distant snapback and inner fire approach remains behind guns.");
 }
}
