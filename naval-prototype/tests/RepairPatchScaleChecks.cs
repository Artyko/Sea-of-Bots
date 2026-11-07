using System;
class RepairPatchScaleChecks{
 static void Main(){foreach(var ship in NavalShipDefinition.SteamChoices){
  if(!NavalRepairPatchMath.Within(1f/ship.Scale,0,0,ship.Scale)||NavalRepairPatchMath.Within(2f/ship.Scale,0,0,ship.Scale))throw new Exception("Repair patch must use world metres");
 }Console.WriteLine("PASS: one-metre damage belongs to patch, two-metre damage does not, independently of ship scale.");}
}
