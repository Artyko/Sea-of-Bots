using System;
class CrewDeckSafetyChecks
{
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 static void Main()
 {
  var profiles=new System.Collections.Generic.List<NavalShipDefinition>(NavalShipDefinition.Choices);profiles.AddRange(NavalShipDefinition.SteamChoices);
  foreach(var ship in profiles)
  {
   Check(NavalCrewDeckSafety.Inside(ship.Destroyer?ship.GunOperator(0).x:1.35f,ship.Destroyer?ship.GunOperator(0).z:2.4f,ship.HullHalfWidth(ship.Destroyer?ship.GunOperator(0).z:2.4f),ship.Scale,.38f),"Sail work post remains reachable inside safety margin");
   Check(!NavalCrewDeckSafety.Inside(3.2f,0,ship.HullHalfWidth(0),ship.Scale,.38f),"Ordinary walking cannot exit through side rail");
   Check(!NavalCrewDeckSafety.Inside(0,6.1f,ship.HullHalfWidth(6.1f),ship.Scale,.38f),"Ordinary walking cannot exit bow/stern");
   Check(NavalCrewDeckSafety.Near(0,6.01f,ship.HullHalfWidth(6.01f),ship.Scale,.38f),"Small moving-deck contact error remains attached");
   Check(!NavalCrewDeckSafety.Near(0,7f,ship.HullHalfWidth(7f),ship.Scale,.38f),"A real overboard fall is outside carry grace");
   for(int i=0;i<ship.CrewCount;i++){
    float x=NavalShipDefinition.StandbyX(ship,i),z=NavalShipDefinition.StandbyZ(ship,i);
    Check(NavalCrewDeckSafety.Inside(x,z,ship.HullHalfWidth(z),ship.Scale,.38f),"All standby posts remain inside actual hull");
   }
  }
  Check(NavalCrewDeckSafety.StepHeight(.8f)==.24f&&NavalCrewDeckSafety.StepHeight(.1f)==.1f,"Step-over cap never increases native step height");
  Check(NavalCrewDeckSafety.StepHeight(.8f)>NavalBallastLayout.Height/NavalBallastLayout.Steps,"Tank/stair steps remain climbable");
  Check(NavalDeckGapMath.Overlaps(0,.825f,0,.8f,4f,0,.70f,0,.25f,.25f,.25f),"A sailor footprint detects a missing deck cell before stepping into it");
  Check(!NavalDeckGapMath.Overlaps(.6f,.825f,0,.8f,4f,0,.70f,0,.25f,.25f,.25f),"A parallel lane clear of the hole remains usable for recovery");
  Check(!NavalDeckGapMath.Overlaps(0,.825f,0,.8f,4f,0,.70f,0,.25f,.8f,.25f),"Vertical wall damage is not mistaken for a deck gap");
  Check(NavalDeckGapMath.BlocksStep(false,true),"Safe footing cannot enter a missing deck cell");
  Check(!NavalDeckGapMath.BlocksStep(true,true),"A capsule already on a hole margin may retreat through the margin");
  Check(!NavalDeckGapMath.BlocksStep(true,false),"A capsule may leave a damaged deck gap for safe support");
  Console.WriteLine("PASS: actual tapered hull bounds, full-body edge margin, sail/standby access, safe step height and tank ramp clearance.");
 }
}
