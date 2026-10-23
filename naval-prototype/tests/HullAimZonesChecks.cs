using System;
class HullAimZonesChecks
{
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 static void Main()
 {
  foreach(var ship in NavalShipDefinition.Choices)
  {
   Check(NavalHullAimZones.Facing(0,-20,0,-5.8f,ship.HullHalfWidth(-5.8f)),"Stern waterline is targetable from astern");
   Check(!NavalHullAimZones.Facing(0,-20,0,5.8f,ship.HullHalfWidth(5.8f)),"Do not aim through hull at far bow");
   Check(NavalHullAimZones.Facing(0,20,0,5.8f,ship.HullHalfWidth(5.8f)),"Bow waterline targetable head on");
   Check(NavalHullAimZones.Facing(-20,0,-ship.HullHalfWidth(0),0,ship.HullHalfWidth(0)),"Port targeting retained");
   Check(NavalHullAimZones.Facing(20,0,ship.HullHalfWidth(0),0,ship.HullHalfWidth(0)),"Starboard targeting retained");
   Check(!NavalHullAimZones.Facing(20,0,-ship.HullHalfWidth(0),0,ship.HullHalfWidth(0)),"Far broadside is not selected through hull");
  }
  Console.WriteLine("PASS: stern, bow, both broadsides and hidden far surfaces across ship profiles.");
 }
}
