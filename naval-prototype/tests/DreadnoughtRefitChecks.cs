using System;
class DreadnoughtRefitChecks
{
 static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
 static void Main(){
  var ship=NavalShipDefinition.Dreadnought;
  Check(Math.Abs(NavalSteamLayout.WheelHeight(ship)*ship.Scale-1.36f)<.001f,"Wheel at corvette operator height");
  Check(Math.Abs(NavalSteamLayout.MainHeight(ship)*ship.Scale-3.6f)<.001f,"Mains remain above the head and reachable for repair");
  Check(NavalSteamLayout.WheelForward(ship)+.05f<.42f,"Wheel pedestal clears bridge front wall");
  var from=new NavalWalkPoint(.73f,ship.HoldFloor+.068f,.96f);var goal=new NavalWalkPoint(.5f,ship.HoldFloor,1.55f);
  var route=NavalSupplyRoutes.Plan(from,goal,ship);Check(route.Count>0,"Logged compact-rack escape has route");
  Check(route[0].X<from.X,"Logged rack-edge start first backs toward inboard clearance");
  var raised=NavalCapitalLayout.TurretRise(3);Check(raised>0&&NavalCapitalLayout.TurretRise(4)==0,"Inner aft turret superfires over outer stern mount");
  Console.WriteLine("PASS: logged rack-edge escape, corvette-height helm and steam mains, raised stern mounting and front-wall clearance.");
 }
}
