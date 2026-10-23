using System;
class CrewDeckSafetyChecks
{
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 static void Main()
 {
  foreach(var ship in NavalShipDefinition.Choices)
  {
   Check(NavalCrewDeckSafety.Inside(1.35f,2.4f,ship.HullHalfWidth(2.4f),ship.Scale,.38f),"Sail work post remains reachable inside safety margin");
   Check(!NavalCrewDeckSafety.Inside(3.2f,0,ship.HullHalfWidth(0),ship.Scale,.38f),"Ordinary walking cannot exit through side rail");
   Check(!NavalCrewDeckSafety.Inside(0,6.1f,ship.HullHalfWidth(6.1f),ship.Scale,.38f),"Ordinary walking cannot exit bow/stern");
   for(int i=0;i<ship.CrewCount;i++)Check(NavalCrewDeckSafety.Inside(NavalShipDefinition.StandbyX(i),NavalShipDefinition.StandbyZ(i),ship.HullHalfWidth(NavalShipDefinition.StandbyZ(i)),ship.Scale,.38f),"All standby posts remain inside actual hull");
  }
  Check(NavalCrewDeckSafety.StepHeight(.8f)==.24f&&NavalCrewDeckSafety.StepHeight(.1f)==.1f,"Step-over cap never increases native step height");
  Check(NavalCrewDeckSafety.StepHeight(.8f)>NavalBallastLayout.Height/NavalBallastLayout.Steps,"Tank/stair steps remain climbable");
  Console.WriteLine("PASS: actual tapered hull bounds, full-body edge margin, sail/standby access, safe step height and tank ramp clearance.");
 }
}
