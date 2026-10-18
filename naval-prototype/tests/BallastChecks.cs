using System;
class BallastChecks
{
 static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
 static bool Near(float a,float b){return Math.Abs(a-b)<.001f;}
 static void Main()
 {
  NavalFloodGrid grid=new NavalFloodGrid(12,24,5.75f,11.75f,2,3);
  int source=grid.Index(-2,3);grid.Add(source,2);
  NavalBallastTank tank=new NavalBallastTank(1,2.45f,3);
  Check(Near(tank.Fill(grid,source,4),1),"Physical capacity limits filling");
  Check(Near(grid.Total+tank.Total,2)&&Near(grid.Total,1),"Transfer conserves water and isolates it from the bilge");
  float[] floors=new float[grid.Water.Length];for(int i=0;i<floors.Length;i++)floors[i]=grid.X(i)*.5f;
  grid.Spread(1,floors,.8f);Check(Near(tank.Water,1),"Hull tilt and open-bilge spreading cannot drain a sealed tank");
  tank.Breach(grid);Check(tank.Broken&&Near(tank.Water,0)&&Near(grid.Total+tank.Total,2),"Breach releases stored water without changing ship mass");
  for(int i=0;i<grid.Water.Length;i++)Check(grid.Water[i]>0,"Burst immediately spreads into the whole open hold");
  tank.Breach(grid);Check(Near(grid.Total+tank.Total,2),"Repeated damage cannot duplicate spilled water");
  Check(tank.Fill(grid,source,1)==0,"Broken reservoir cannot be filled");
  tank.Reseal();float amount=tank.Fill(grid,source,.001f);Check(amount>0,"Fully repaired reservoir can be used again");
  Check(Near(tank.Drain(100),amount)&&tank.Water==0,"Overboard pumping can empty closed ballast");
  NavalFloodGrid full=new NavalFloodGrid(2,2,2,2,1,1);full.Add(0,1);
  NavalBallastTank reserve=new NavalBallastTank(1,1,0);reserve.Fill(full,0,1);
  for(int i=0;i<4;i++)full.Add(i,1);reserve.Breach(full);
  Check(Near(reserve.SpillPending,1),"Full hold retains overflow accounting rather than deleting water");
  full.Add(2,-1);reserve.Spill(full);Check(reserve.SpillPending<.001&&Near(full.Total,4),"Deferred overflow enters newly available room");
  NavalBallastTrim failed=new NavalBallastTrim();for(int i=0;i<200;i++){failed.Update(.1f,16);failed.Observe(.1f,.16f,true);}
  Check(failed.Ineffective&&failed.Cooldown>0,"Ineffective trim stops after settling and averaging");
  for(int i=0;i<130;i++){failed.Update(.1f,16);failed.Observe(.1f,0,false);}Check(failed.Cooldown==0,"Overboard action eventually permits a fresh trial");
  NavalBallastTrim effective=new NavalBallastTrim();for(int i=0;i<200;i++){float heel=16-i*.03f;effective.Update(.1f,heel);effective.Observe(.1f,.16f,true);}
  Check(!effective.Ineffective&&effective.Cooldown==0,"Improving trim continues");
  foreach(NavalShipDefinition ship in new[]{NavalShipDefinition.Sloop,NavalShipDefinition.Brigantine})
  {
   Check(!ship.Partitions&&ship.Compartments,"Both ships retain pump equipment but remove old passage barriers");
   Check(NavalBallastLayout.X-NavalBallastLayout.Width*.5f>1.65f+.38f/ship.Scale,"Tank bodies clear ordinary walking corridors");
   Check(NavalBallastLayout.RampRun>NavalBallastLayout.Height,"End ramp has horizontal room within tank footprint");
   Check(NavalBallastLayout.Height/NavalBallastLayout.Steps<=.21f,"Permanent ramp rises stay small enough for walking");
   Check(NavalBallastLayout.Z-NavalBallastLayout.Length*.5f>.7f,"Keep staircase and center crossing clear");
   Check(NavalBallastLayout.X+NavalBallastLayout.Width*.5f<BoatDimensions.HalfWidth-.1f,"Reservoir fits inside hull");
  }
  Console.WriteLine("PASS: sealed volume, capacity, burst to entire hold, no duplication, full-grid conservation, repair, trim feedback and open layouts on both ships.");
 }
}
