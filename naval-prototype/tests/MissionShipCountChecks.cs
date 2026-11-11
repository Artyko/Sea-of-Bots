using System;
class MissionShipCountChecks{static void Main(){if(!NavalMissionShipMath.Counts(false,false)||NavalMissionShipMath.Counts(false,true)||NavalMissionShipMath.Counts(true,false)||NavalMissionShipMath.Counts(true,true))throw new Exception("Sinking ships cannot remain in mission count");Console.WriteLine("PASS: mission counters exclude sinking and sunk ships.");}}
