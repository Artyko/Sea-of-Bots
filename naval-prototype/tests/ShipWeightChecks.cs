using System;
class ShipWeightChecks
{
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 static float Settle(NavalStabilityMath model,NavalMassMoments load)
 {for(int i=0;i<2400;i++)model.Step(.025f,load,0);return Math.Abs(model.Angle);}
 static float Flood(NavalShipDefinition ship,float fraction,bool free)
 {
  var model=ship.CreateStability();model.Observe(4);
  var grid=new NavalFloodGrid(12,24,5.75f,11.75f,ship.Scale,ship.RimY-ship.HoldFloor,ship.DeckCount>2);
  float amount=(float)model.DryMass*fraction,remaining=amount;
  for(int i=0;i<288&&remaining>0;i++)if(grid.X(i)<-1.8f)remaining-=grid.Add(i,remaining);
  Check(remaining<.01,"Flood load fits");float peak=0;var floors=new float[288];
  for(int step=0;step<2400;step++)
  {
   double phi=model.Angle*Math.PI/180;
   for(int i=0;i<288;i++)floors[i]=(float)((grid.X(i)*Math.Sin(phi)+ship.HoldFloor*Math.Cos(phi))*ship.Scale);
   if(free)grid.Spread(.025f,floors,(float)Math.Cos(phi));
   var mass=new NavalMassMoments();for(int i=0;i<288;i++)mass.Add(grid.Water[i],grid.X(i)*ship.Scale,(ship.HoldFloor+grid.Depth(i)*.5f)*ship.Scale);
   model.Step(.025f,mass,(float)(3*Math.Sin(step*.025*.6)));peak=Math.Max(peak,Math.Abs(model.Angle));
  }
  Check(Math.Abs(grid.Total-amount)<.1,"Flood mass is conserved");return peak;
 }
 static void FreeSurface(NavalShipDefinition ship)
 {
  var model=ship.CreateStability();
  var grid=new NavalFloodGrid(12,24,5.75f,11.75f,ship.Scale,ship.RimY-ship.HoldFloor,ship.DeckCount>2);
  int cells=0;for(int i=0;i<288;i++)if(grid.IsActive(i))cells++;
  for(int i=0;i<288;i++)grid.Add(i,(float)model.DryMass*.05f/cells);
  var fixedLoad=new NavalMassMoments();var floors=new float[288];double phi=5*Math.PI/180;
  for(int i=0;i<288;i++)
  {
   fixedLoad.Add(grid.Water[i],grid.X(i)*ship.Scale,(ship.HoldFloor+grid.Depth(i)*.5)*ship.Scale);
   floors[i]=(float)((grid.X(i)*Math.Sin(phi)+ship.HoldFloor*Math.Cos(phi))*ship.Scale);
  }
  for(int step=0;step<2400;step++)grid.Spread(.025f,floors,(float)Math.Cos(phi));
  var freeLoad=new NavalMassMoments();for(int i=0;i<288;i++)freeLoad.Add(grid.Water[i],grid.X(i)*ship.Scale,(ship.HoldFloor+grid.Depth(i)*.5)*ship.Scale);
  Check(freeLoad.MX<fixedLoad.MX-1,"Free water actually runs to the low side");
  Check(model.Torque(freeLoad,5,0)>model.Torque(fixedLoad,5,0),"Free surface reduces restoring moment versus the same restrained load");
 }
 static void Main()
 {
  foreach(var ship in NavalShipDefinition.Choices)
  {
   var model=ship.CreateStability();double draft=ship.DesignWaterline*ship.Scale-model.Bottom;
   double kg=model.DryY-model.Bottom,gm=draft*.5+model.Beam*model.Beam/(12*draft)-kg;
   Check(kg>0&&kg<ship.HoldClearance,"Dry CG is within lower hold");
   Check(gm>0,"Positive initial stability");
   double measured=-model.Torque(new NavalMassMoments(),.1f,0)/(model.DryMass*9.81*Math.Sin(.1*Math.PI/180));
   Check(Math.Abs(measured-gm)<.003,"Righting torque agrees with KG/KB/BM independently of tuning GM");
   model.Observe(20);Check(Math.Abs(model.Step(.025f,new NavalMassMoments(),0)-20)<.5,"No instant leveling");
   Check(Settle(model,new NavalMassMoments())<.2,"Dry ship recovers from 20 degrees");
   float small=Flood(ship,.01f,true),medium=Flood(ship,.05f,true),large=Flood(ship,.15f,true);
   Check(small<12&&medium<25,"One/five percent free flood leaves time for crew action");
   FreeSurface(ship);
   // Same water mass, moved between actual side-tank lever arms; no artificial restoring force.
   double water=2*NavalBallastLayout.Capacity(ship.Scale);var low=new NavalMassMoments();low.Add(water,-2.45*ship.Scale,model.Bottom+.4);
   float before=Settle(ship.CreateStability(),low);var balanced=new NavalMassMoments();balanced.Add(water*.5,-2.45*ship.Scale,model.Bottom+.4);balanced.Add(water*.5,2.45*ship.Scale,model.Bottom+.4);
   float after=Settle(ship.CreateStability(),balanced);Check(before>.2&&after<before*.1,"Real mass transfer reduces list at unchanged mass");
   var high=new NavalMassMoments();high.Add(model.DryMass*.8,-model.Beam*.4,model.Top*.8);
   float severe=Settle(ship.CreateStability(),high);Check(severe>90,"Heavy high asymmetric load can still capsize");
   Console.WriteLine(ship.Name+": KG "+kg.ToString("F2")+"m above hold floor; G-sea "+(model.DryY-ship.DesignWaterline*ship.Scale).ToString("F2")+"m; GM "+gm.ToString("F2")+"m; free flood 1/5/15% peaks "+small.ToString("F1")+"/"+medium.ToString("F1")+"/"+large.ToString("F1")+"deg; tank list "+before.ToString("F1")+" -> "+after.ToString("F1")+"deg; severe "+severe.ToString("F1")+"deg.");
  }
  Console.WriteLine("PASS: all production ship profiles, physical dry CG, analytic small-angle stability, dry recovery, conserved free flooding, ballast transfer and unclamped capsize.");
 }
}
