using System;
class FireWaterSupplyChecks
{
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 static void Main()
 {
  foreach(var ship in NavalShipDefinition.Choices)
  {
   var grid=new NavalFloodGrid(12,24,5.75f,11.75f,ship.Scale,ship.RimY-ship.HoldFloor);
   int cell=grid.Index(-1.5f,2);grid.Add(cell,3);float before=grid.Total;
   float bucket=NavalFireWaterSupply.Take(grid,cell,1.8f);
   Check(Math.Abs(bucket-1.8f)<.001&&Math.Abs(grid.Total+bucket-before)<.001,"Bilge pickup conserves water mass on every ship");
   float partial=NavalFireWaterSupply.Take(grid,cell,1.8f);Check(Math.Abs(partial-1.2f)<.001,"Partially drained source supplies only actual remaining water");
   Check(NavalFireWaterSupply.Take(grid,cell,1.8f)==0,"Already pumped-dry source requests reselection instead of inventing water");
   float sea=NavalFireWaterSupply.Take(grid,-1,1.8f);Check(sea==1.8f&&grid.Total==0,"Sea collection fills bucket without adding bilge water");
   grid.Add(cell,bucket+partial);Check(Math.Abs(grid.Total-before)<.001,"Water poured inside returns conserved volume");
  }
  Console.WriteLine("PASS: bilge/sea supplies, depleted sources, partial buckets and conserved carried water on all profiles.");
 }
}
