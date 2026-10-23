using System;
class GalleonRefitChecks
{
 static void Check(bool ok,string name){if(!ok)throw new Exception(name);}
 static NavalStabilityMath Model(bool refit)
 {var s=NavalShipDefinition.Galleon;return refit?s.CreateStability():new NavalStabilityMath(s.Scale,s.HoldFloor,s.RimY,.1f,0);}
 static float Trial(bool refit)
 {
  var ship=NavalShipDefinition.Galleon;var model=Model(refit);model.Observe(4);
  var grid=new NavalFloodGrid(12,24,5.75f,11.75f,ship.Scale,ship.RimY-ship.HoldFloor,refit);var floors=new float[288];float peak=0;
  for(int i=0;i<3200;i++)
  {
   // Controlled severe breach: 20m3/s for 20 seconds, repair then stops ingress.
   if(i<800)grid.Add(grid.Index(-2.65f,2),.5f);
   double angle=model.Angle*Math.PI/180;for(int j=0;j<288;j++)floors[j]=(float)((grid.X(j)*Math.Sin(angle)+ship.HoldFloor*Math.Cos(angle))*ship.Scale);
   grid.Spread(.025f,floors,(float)Math.Cos(angle));if(i>=720)grid.Pump(.025f*NavalMaintenanceMath.PumpOutRate);
   var mass=new NavalMassMoments();for(int j=0;j<288;j++)mass.Add(grid.Water[j],grid.X(j)*ship.Scale,(ship.HoldFloor+grid.Depth(j)*.5f)*ship.Scale);
   model.Step(.025f,mass,(float)(3*Math.Sin(i*.025*.6)));peak=Math.Max(peak,Math.Abs(model.Angle));
  }
  return peak;
 }
 static void Main()
 {
  var ship=NavalShipDefinition.Galleon;var old=Model(false);var current=Model(true);
  current.Torque(new NavalMassMoments(),0,0);
  Check(Math.Abs(current.Waterline-ship.DesignWaterline*ship.Scale)<.001,"Dry displacement matches raised galleon design waterline");
  Check(current.DryY<old.DryY-5&&current.DryY<current.Waterline,"Dry CG is genuinely lower and below sea, not a hidden angle clamp");
  var previous=new NavalStabilityMath(ship.Scale,ship.HoldFloor,ship.RimY,NavalStabilityMath.DefaultGMFraction,ship.DesignWaterline+1.2f/ship.Scale,ship.HoldFloor*ship.Scale+ship.DryCenterHeight);
  previous.Torque(new NavalMassMoments(),0,0);
  Check(Math.Abs(previous.Waterline-current.Waterline-1.2f)<.001,"Galleon floats 1.2 metres higher than previous design");
  Check(Math.Abs(current.DryMass-(current.Waterline-current.Bottom)*current.Beam*current.Length)<.1,"Raised draught uses matching displaced volume");
  Check(!ship.IsOpening(3,.825f,ship.LadderZ,.1f,.1f,.1f),"No underwater boarding door in middle deck");
  for(int gun=0;gun<6;gun++)Check(!ship.IsOpening(3,1.35f,NavalShipDefinition.GunZ(ship,gun),.1f,.1f,.1f),"No submerged legacy middle-deck gunports");
  float before=Trial(false),after=Trial(true);Check(after<before*.6f&&after<12,"Severe-breach trial preserves crew response time with lower CG and proper displacement");
  var combat=new NavalCombatEmergency();combat.Step(.04f,24,true);Check(combat.Critical&&combat.RepairRetreat,"Large active leak/list mobilizes last gunner before 35 percent flood");
  var parallel=new NavalMaintenanceEscalation();parallel.Step(.05f,.03f,14,false,true);Check(parallel.Parallel,"Early severe inflow/list allows repair and pump in parallel");
  var calm=new NavalCombatEmergency();calm.Step(.04f,24);Check(!calm.Critical,"Heel alone does not mobilize last gunner without a severe active leak");
  var random=new Random(114);for(int i=0;i<10000;i++)
  {
   float x=(float)(random.NextDouble()*8-4),z=(float)(random.NextDouble()*14-7);bool mask=Math.Abs(x)<=3&&Math.Abs(z)<=6;
   for(int p=0;p<4;p++)mask&=NavalHullOutline.EndPlane(x,z,p)<=0;
   Check(mask==(Math.Abs(z)<=6&&Math.Abs(x)<=ship.HullHalfWidth(z)),"Water clip matches tapered geometry");
  }
  var grid=new NavalFloodGrid(12,24,5.75f,11.75f,ship.Scale,ship.RimY-ship.HoldFloor,true);int invalid=0;
  for(int i=0;i<288;i++)if(!grid.IsActive(i)){invalid++;Check(grid.Add(i,100)==0,"Cut-away corners cannot hold water");}
  Check(invalid>0,"Outline actually excludes corner cells");
  var tank=new NavalBallastTank(64.7f,2.45f,3);tank.Receive(64.7f);tank.Breach(grid);Check(Math.Abs(grid.Total+tank.Total-64.7f)<.001,"Tank spill conserves mass within tapered hull");
  var floors=new float[288];for(int i=0;i<288;i++)floors[i]=grid.X(i)*.3f;grid.Spread(1,floors,.8f);
  for(int i=0;i<288;i++)if(!grid.IsActive(i))Check(grid.Water[i]==0,"Spreading cannot fill empty sea corners");
  Console.WriteLine("PASS: refit waterline "+current.Waterline.ToString("F1")+"m local-world datum; dry CG "+old.DryY.ToString("F1")+" -> "+current.DryY.ToString("F1")+"m; controlled severe-breach peak "+before.ToString("F1")+" -> "+after.ToString("F1")+" deg; top guns, sealed middle, early damage control, tapered clip and conserved masked flooding.");
 }
}
