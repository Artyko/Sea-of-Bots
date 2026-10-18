using System;
using System.Diagnostics;
class StabilityChecks
{
 static void Check(bool ok,string name){if(!ok)throw new Exception(name);}
 static NavalMassMoments Load(NavalShipDefinition ship,float amount,float moved)
 {
  var m=new NavalMassMoments();
  m.Add(amount-moved,-2.45*ship.Scale,ship.HoldFloor*ship.Scale+.4);
  m.Add(moved,2.45*ship.Scale,ship.HoldFloor*ship.Scale+.6);return m;
 }
 static float Settle(NavalStabilityMath model,NavalMassMoments mass)
 {for(int i=0;i<2000;i++)model.Step(.05f,mass,0);return model.Angle;}
 static void Main()
 {
  var section=new NavalHydrostaticSection();section.Solve(4,-2,2,0,8);
  Check(Math.Abs(section.Area-8)<.0001&&Math.Abs(section.Waterline)<.0001,"Displacement area solved from waterline");
  Check(Math.Abs(section.CenterX)<.0001&&Math.Abs(section.CenterY+1)<.0001,"Buoyancy centroid matches submerged rectangle");
  section.Solve(4,-2,2,45,8);Check(Math.Abs(section.Area-8)<.0002,"Clipping preserves displacement at large heel");
  foreach(var ship in new[]{NavalShipDefinition.Sloop,NavalShipDefinition.Brigantine})
  {
   var dry=new NavalStabilityMath(ship.Scale,ship.HoldFloor,ship.RimY);var empty=new NavalMassMoments();
   Check(dry.Torque(empty,5,0)<0&&dry.Torque(empty,-5,0)>0,"Dry hull has restoring torque on both sides");
   Check(Math.Abs(dry.Torque(empty,5,0)+dry.Torque(empty,-5,0))<.02,"Restoring geometry is symmetric");
   dry.Observe(20);float immediate=dry.Step(.05f,empty,0);Check(Math.Abs(immediate-20)<1,"No instantaneous artificial leveling");
   Check(Math.Abs(Settle(dry,empty))<.1,"Damped dry hull returns through physical righting torque");
   float water=ship==NavalShipDefinition.Sloop?80:180;
   var loaded=new NavalStabilityMath(ship.Scale,ship.HoldFloor,ship.RimY);float before=Settle(loaded,Load(ship,water,0));
   float moved=2*NavalBallastLayout.Capacity(ship.Scale);var afterMass=Load(ship,water,moved);
   Check(Math.Abs(afterMass.Mass-water)<.001,"Transfer changes moments, not mass");
   float after=Settle(loaded,afterMass);
   Check(before>5&&Math.Abs(after)<before*.55f,"Ballast still reduces listed equilibrium with a more stable dry hull");
   var mirrored=new NavalStabilityMath(ship.Scale,ship.HoldFloor,ship.RimY);var mass=Load(ship,water,0);mass.MX=-mass.MX;
   Check(Math.Abs(Settle(mirrored,mass)+before)<.02,"Port/starboard symmetry");
   var tank=new NavalBallastTank(NavalBallastLayout.Capacity(ship.Scale),2.45f,3);
   var grid=new NavalFloodGrid(12,24,5.75f,11.75f,ship.Scale,3);for(int i=0;i<grid.Water.Length;i++)grid.Add(i,.2f);
   tank.Fill(grid,0,.1f);var tankMass=new NavalMassMoments();loaded.AddTank(ref tankMass,tank,ship.Scale,ship.HoldFloor);
   Check(Math.Abs(tankMass.Mass-tank.Water)<.00001&&tankMass.MX/tankMass.Mass>2*ship.Scale,"Tank water uses its real outer lever arm");
   Check(tank.FillFraction>0&&tank.FillFraction<1,"Per-tank fill fraction reflects volume");tank.Breach(grid);Check(tank.FillFraction==0,"Broken tank UI becomes empty");
   Console.WriteLine(ship.Name+": equilibrium "+before.ToString("F1")+" -> "+after.ToString("F1")+" degrees, transferred "+moved.ToString("F1")+" m3 at unchanged mass.");
  }
  var large=new NavalStabilityMath(2,NavalShipDefinition.Sloop.HoldFloor,NavalShipDefinition.Sloop.RimY);
  var severe=new NavalMassMoments();severe.Add(large.DryMass*.8,-large.Beam*.4,large.Top*.8);
  Check(Math.Abs(Settle(large,severe))>90,"Severe high asymmetric flooding can still capsize without an angle clamp");
  var a=new NavalStabilityMath(2,-2.225f,1.775f);var b=new NavalStabilityMath(2,-2.225f,1.775f);a.Observe(15);b.Observe(15);
  for(int i=0;i<500;i++)a.Step(.01f,new NavalMassMoments(),0);for(int i=0;i<100;i++)b.Step(.05f,new NavalMassMoments(),0);
  Check(Math.Abs(a.Angle-b.Angle)<.3,"Bounded substeps give consistent frame rates");
  Stopwatch watch=Stopwatch.StartNew();for(int i=0;i<2000;i++)a.Step(.016f,new NavalMassMoments(),1);watch.Stop();
  Console.WriteLine("PASS: clipped hydrostatics, dry restoration, inertia/damping, conserved ballast transfer, strong list recovery, UI fractions and timestep convergence. 2000 core steps ms="+watch.Elapsed.TotalMilliseconds.ToString("F1")+" (not Unity FPS).");
 }
}
