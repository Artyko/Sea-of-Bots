using System;
class BallastTrimChecks
{
 static void Check(bool ok,string name){if(!ok)throw new Exception(name);}
 static void Tick(NavalBallastTrim trim,float heel,float amount,bool work){trim.Update(.1f,heel);trim.Observe(.1f,amount,work);}
 static float Coupled(NavalShipDefinition ship,bool trimEnabled)
 {
  var model=new NavalStabilityMath(ship.Scale,ship.HoldFloor,ship.RimY);model.Observe(4);
  var grid=new NavalFloodGrid(12,24,5.75f,11.75f,ship.Scale,ship.RimY-ship.HoldFloor);var floors=new float[288];
  float initial=ship==NavalShipDefinition.Sloop?80:180,remaining=initial;
  for(int i=0;i<288&&remaining>0;i++)if(grid.X(i)<-1.8f)remaining-=grid.Add(i,remaining);
  Check(remaining<.001f,"Coupled load fits");
  var tanks=new[]{new NavalBallastTank(NavalBallastLayout.Capacity(ship.Scale),-2.45f,-3),new NavalBallastTank(NavalBallastLayout.Capacity(ship.Scale),-2.45f,3),new NavalBallastTank(NavalBallastLayout.Capacity(ship.Scale),2.45f,-3),new NavalBallastTank(NavalBallastLayout.Capacity(ship.Scale),2.45f,3)};
  var trim=new NavalBallastTrim();float finalSum=0;
  for(int step=0;step<4000;step++)
  {
   double angle=model.Angle*Math.PI/180;
   for(int i=0;i<288;i++)floors[i]=(float)((grid.X(i)*Math.Sin(angle)+ship.HoldFloor*Math.Cos(angle))*ship.Scale);
   grid.Spread(.025f,floors,(float)Math.Cos(angle));
   float wave=(float)(3*Math.Sin(step*.025*.6));
   if(trimEnabled){trim.Update(.025f,model.Angle-wave);float moved=trim.WantsTransfer?NavalBallastTransfer.Move(grid,tanks,trim.FilteredHeel>0,.04f):0;trim.Observe(.025f,moved,moved>0);}
   var mass=new NavalMassMoments();for(int i=0;i<288;i++)mass.Add(grid.Water[i],grid.X(i)*ship.Scale,(ship.HoldFloor+grid.Depth(i)*.5)*ship.Scale);
   foreach(var tank in tanks)model.AddTank(ref mass,tank,ship.Scale,ship.HoldFloor);
   Check(Math.Abs(mass.Mass-initial)<.04f,"Coupled transfer conserves all mass");model.Step(.025f,mass,wave);
   if(step>=3600)finalSum+=Math.Abs(model.Angle);
  }
  return finalSum/400;
 }
 static void Main()
 {
  var delayed=new NavalBallastTrim();
  for(int i=0;i<360;i++){float heel=14-Math.Max(0,i-90)*.015f+(float)Math.Sin(i*.12)*1.1f;Tick(delayed,heel,.16f,true);Check(delayed.Cooldown==0,"Slow response and waves do not cancel useful transfer");}
  var hysteresis=new NavalBallastTrim();Tick(hysteresis,7,.16f,true);
  for(int i=0;i<180;i++)Tick(hysteresis,5.5f+(float)Math.Sin(i*.15)*1.5f,.16f,true);
  Check(hysteresis.WantsTransfer,"Continue trimming below entry threshold while still listed");
  for(int i=0;i<100;i++)Tick(hysteresis,2,0,false);Check(!hysteresis.WantsTransfer,"Stop near level without overfilling opposite side");
  var stalled=new NavalBallastTrim();for(int i=0;i<190;i++)Tick(stalled,16,.16f,true);
  Check(stalled.Ineffective&&stalled.Cooldown>0,"Genuinely ineffective work still switches to overboard");
  for(int i=0;i<80;i++)Tick(stalled,-10,0,false);Check(stalled.Cooldown==0,"Changed heel invalidates stale failure cooldown");
  foreach(var ship in new[]{NavalShipDefinition.Sloop,NavalShipDefinition.Brigantine})
  {
   float uncontrolled=Coupled(ship,false),controlled=Coupled(ship,true);
   Check(controlled<uncontrolled*.75f,"Closed-loop trim improves coupled free-water list without overboard pumping");
   Console.WriteLine(ship.Name+": mean final list without trim "+uncontrolled.ToString("F1")+" -> with trim "+controlled.ToString("F1")+" degrees at unchanged mass.");
   var grid=new NavalFloodGrid(12,24,5.75f,11.75f,ship.Scale,ship.RimY-ship.HoldFloor);
   var tanks=new[]{new NavalBallastTank(10,-2.45f,-3),new NavalBallastTank(10,-2.45f,3),new NavalBallastTank(10,2.45f,-3),new NavalBallastTank(10,2.45f,3)};
   // Former region 0 was entirely excluded from the old pump intake.
   for(int i=0;i<grid.Water.Length;i++)if(grid.X(i)<0&&grid.X(i)>-1.4f)grid.Add(i,.001f);
   float before=grid.Total,moved=NavalBallastTransfer.Move(grid,tanks,true,.16f);
   Check(moved>.01f&&Math.Abs(before-grid.Total-tanks[2].Water-tanks[3].Water)<.001f,"Shallow central cells are valid intakes and transfer conserves mass");
   grid.Pump(100);tanks[2].Drain(100);tanks[3].Drain(100);
   tanks[0].Receive(3);before=tanks[0].Total;
   moved=NavalBallastTransfer.Move(grid,tanks,true,1.6f);
   Check(Math.Abs(moved-1.6f)<.001f&&Math.Abs(tanks[0].Total+tanks[2].Total-before)<.001f,"Dry hold permits sealed port-to-starboard transfer without deleting water");
   tanks[2].Breach(grid);tanks[3].Breach(grid);before=tanks[0].Water;
   Check(NavalBallastTransfer.Move(grid,tanks,true,1)==0&&tanks[0].Water==before,"Broken targets cannot consume source water");
   tanks[0].Breach(grid);tanks[1].Breach(grid);
   Check(NavalBallastTransfer.Move(grid,tanks,false,1)==0,"No valid destination means no transfer");
  }
  Console.WriteLine("PASS: delayed response, wave filtering, entry/exit hysteresis, genuine stall, cooldown reset, central intakes, sealed-to-sealed transfer and damage conservation on both ship profiles.");
 }
}
