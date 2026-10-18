using System;
class FloodedStabilityChecks
{
 static void Check(bool ok,string name){if(!ok)throw new Exception(name);}
 static float Simulate(NavalShipDefinition ship,float water,float gm)
 {
  var model=new NavalStabilityMath(ship.Scale,ship.HoldFloor,ship.RimY,gm);model.Observe(4);
  var grid=new NavalFloodGrid(12,24,5.75f,11.75f,ship.Scale,ship.RimY-ship.HoldFloor);var floors=new float[288];
  // A fresh port breach introduces local water, which remains free to spread.
  float remaining=water;for(int i=0;i<288&&remaining>0;i++)if(grid.X(i)<-1.8f)remaining-=grid.Add(i,remaining);
  Check(remaining<.001f,"All test water fits in the grid");float peak=0;
  for(int step=0;step<2400;step++)
  {
   double angle=model.Angle*Math.PI/180;
   for(int i=0;i<288;i++)floors[i]=(float)((grid.X(i)*Math.Sin(angle)+ship.HoldFloor*Math.Cos(angle))*ship.Scale);
   grid.Spread(.025f,floors,(float)Math.Cos(angle));var mass=new NavalMassMoments();
   for(int i=0;i<288;i++)mass.Add(grid.Water[i],grid.X(i)*ship.Scale,(ship.HoldFloor+grid.Depth(i)*.5)*ship.Scale);
   model.Step(.025f,mass,(float)(3*Math.Sin(step*.025*.6)));peak=Math.Max(peak,Math.Abs(model.Angle));
  }
  Check(Math.Abs(grid.Total-water)<.03f,"Coupled flooding conserves water");return peak;
 }
 static void Main()
 {
  foreach(var ship in new[]{NavalShipDefinition.Sloop,NavalShipDefinition.Brigantine})
  {
   float small=Simulate(ship,10,NavalStabilityMath.DefaultGMFraction),moderate=Simulate(ship,40,NavalStabilityMath.DefaultGMFraction),old=Simulate(ship,40,.025f);
   Check(small<7,"Small local ingress does not destabilize the vessel in waves");
   Check(moderate<12&&moderate<old*.75f,"Moderate local flooding remains stable and improves on v104 calibration");
   Console.WriteLine(ship.Name+": 10m3 peak "+small.ToString("F1")+" deg; 40m3 peak "+moderate.ToString("F1")+" deg; previous calibration "+old.ToString("F1")+" deg.");
  }
  Console.WriteLine("PASS: coupled free-water spreading, waves, moderate-flood stability and conserved water on both ship profiles.");
 }
}
