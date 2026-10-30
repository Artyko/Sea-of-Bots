using System;
class FireWaterSupplyChecks
{
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 static void Main()
 {
  var profiles=new System.Collections.Generic.List<NavalShipDefinition>(NavalShipDefinition.Choices);profiles.AddRange(NavalShipDefinition.SteamChoices);
  foreach(var ship in profiles)
  {
   var grid=new NavalFloodGrid(12,24,5.75f,11.75f,ship.Scale,ship.RimY-ship.HoldFloor);
   int cell=grid.Index(-1.5f,2);grid.Add(cell,3);float before=grid.Total;
   float bucket=NavalFireWaterSupply.Take(grid,cell,1.8f);
   Check(Math.Abs(bucket-1.8f)<.001&&Math.Abs(grid.Total+bucket-before)<.001,"Bilge pickup conserves water mass on every ship");
   float partial=NavalFireWaterSupply.Take(grid,cell,1.8f);Check(Math.Abs(partial-1.2f)<.001,"Partially drained source supplies only actual remaining water");
   Check(NavalFireWaterSupply.Take(grid,cell,1.8f)==0,"Already pumped-dry source requests reselection instead of inventing water");
   float sea=NavalFireWaterSupply.Take(grid,-1,1.8f);Check(sea==1.8f&&grid.Total==0,"Sea collection fills bucket without adding bilge water");
   foreach(float z in NavalFireWaterSupply.IntakeZ){
    Check(NavalFireWaterSupply.SeaX(ship,z)<ship.HullHalfWidth(z),"Sea bucket post remains on the tapered deck "+ship.Name);
    if(ship.SteamPowered)for(int gun=0;gun<ship.GunCount;gun++)Check(Math.Abs(z-NavalShipDefinition.GunZ(ship,gun))*ship.Scale>3.5f,"Steam water intake stays between gun mounts");
   }
   grid.Add(cell,bucket+partial);Check(Math.Abs(grid.Total-before)<.001,"Water poured inside returns conserved volume");
  }
  Console.WriteLine("PASS: bilge/sea supplies, depleted sources, partial buckets and conserved carried water on all profiles.");
 }
}
