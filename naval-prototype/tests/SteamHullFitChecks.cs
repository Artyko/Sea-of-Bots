using System;
class SteamHullFitChecks
{
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 static void Fits(NavalShipDefinition ship,float x,float halfWidth,float z,string item)
 {Check(Math.Abs(x)+halfWidth<=ship.HullHalfWidth(z)+.001f,item+" crosses the armour shell at z="+z);}
 static void Main()
 {
  var ship=NavalShipDefinition.Ironclad;
  float pipe=NavalPumpLayout.PipeHalfSpan(ship,-5.45f);
  Check(pipe<=ship.HullHalfWidth(-5.45f)-.13f,"Stern pump pipes stay behind both sides");
  foreach(int gun in new[]{0,2,4})
  {
   float z=NavalShipDefinition.GunZ(ship,gun);
   Check(NavalIroncladVisualLayout.GunFoundationInside(ship,z),"Gun foundation does not project through the side");
   Fits(ship,ship.GunMountX,.53f*ship.GunFixtureScale,z,"Casemate shield and pedestal");
  }
  Fits(ship,Math.Abs(NavalSteamLayout.BoilerX),.34f,NavalSteamLayout.BoilerZ,"Boiler fittings");
  Fits(ship,Math.Abs(NavalSteamLayout.EngineX),.37f,NavalSteamLayout.EngineZ,"Engine fittings");
  Fits(ship,Math.Abs(NavalSteamLayout.FunnelX),.26f,NavalSteamLayout.FunnelZ,"Funnel uptake");
  var ballast=NavalBallastLayout.For(ship);
  foreach(float z in new[]{ballast.InnerZ,ballast.Z+ballast.Length*.5f})Fits(ship,ballast.X,ballast.Width*.5f,z,"Wing ballast tank");
  for(int i=0;i<NavalCargoLayout.Boxes(ship,0)+NavalCargoLayout.Boxes(ship,1)+NavalCargoLayout.Boxes(ship,2);i++)
  {var p=NavalCargoLayout.Position(ship,i);Fits(ship,Math.Abs(p.X),.55f/ship.Scale,p.Z,"Cargo locker");}
  Fits(ship,0,NavalMainMagazineLayout.Width*.5f/ship.Scale,NavalMainMagazineLayout.Z,"Main magazine trunk");

  var start=new NavalWalkPoint(1.80f,ship.WeatherDeck,-.45f);
  var goal=new NavalWalkPoint(NavalShipRoutes.SteamBatteryLane,ship.WeatherDeck,.80f);
  var route=NavalShipRoutes.Plan(start,goal,ship);
  Check(route.Count>=2,"Logged middle-casemate route receives a square corner exit");
  Check(Math.Abs(route[0].X-NavalShipRoutes.SteamBatteryLane)<.001f&&Math.Abs(route[0].Z-start.Z)<.001f,
      "Logged sailor first moves laterally clear of the pedestal instead of scraping it diagonally");
  Console.WriteLine("PASS: steam machinery, stores, ballast, pump piping and casemate fittings stay inside the tapered hull; logged battery corner is routed squarely.");
 }
}
