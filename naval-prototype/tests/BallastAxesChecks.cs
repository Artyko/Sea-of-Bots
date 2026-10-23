using System;
class BallastAxesChecks
{
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 static float Moment(NavalBallastTank[] tanks,bool longitudinal){float sum=0;foreach(var t in tanks)sum+=t.Water*(longitudinal?t.Z:t.X);return sum;}
 static void Main()
 {
  foreach(var ship in NavalShipDefinition.Choices)foreach(bool longitudinal in new[]{false,true})foreach(bool negative in new[]{false,true})
  {
   var grid=new NavalFloodGrid(12,24,5.75f,11.75f,ship.Scale,ship.RimY-ship.HoldFloor);
   var tanks=new[]{new NavalBallastTank(100,-2.45f,-3),new NavalBallastTank(100,-2.45f,3),new NavalBallastTank(100,2.45f,-3),new NavalBallastTank(100,2.45f,3)};
   var source=tanks[longitudinal?(negative?0:1):(negative?0:2)];source.Receive(20);
   float before=Moment(tanks,longitudinal),cross=Moment(tanks,!longitudinal);
   float moved=longitudinal?NavalBallastTransfer.MoveLongitudinal(grid,tanks,negative,5):NavalBallastTransfer.Move(grid,tanks,negative,5);
   float total=grid.Total;foreach(var t in tanks)total+=t.Total;
   Check(Math.Abs(total-20)<.001&&Math.Abs(moved-5)<.001,"Closed tank transfer conserves water and rate");
   Check(Math.Abs(Moment(tanks,longitudinal))<Math.Abs(before),"Both directions reduce selected mass moment");
   Check(Math.Abs(Moment(tanks,!longitudinal)-cross)<.001,"Transfer preserves the other axis moment");
   var full=new NavalBallastTank(1,negative?2.45f:-2.45f,negative?3:-3);full.Receive(1);
   Check((longitudinal?NavalBallastTransfer.MoveLongitudinal(grid,new[]{source,full},negative,5):NavalBallastTransfer.Move(grid,new[]{source,full},negative,5))==0,"No transfer into a full receiver");
   grid.Add(grid.Index(negative?-2:2,negative?-3:3),5);float initial=grid.Total;source.Drain(100);
   moved=longitudinal?NavalBallastTransfer.MoveLongitudinal(grid,tanks,negative,2):NavalBallastTransfer.Move(grid,tanks,negative,2);
   Check(Math.Abs(moved-2)<.001&&Math.Abs(grid.Total-(initial-2))<.001,"Open bilge can supply either correction axis");
  }
  bool ineffective=false;var plan=new NavalBallastTrim();for(int i=0;i<400;i++){plan.Update(.1f,10,true);plan.Observe(.1f,.2f,true);ineffective|=plan.Ineffective;}
  Check(ineffective,"Unimproved trim triggers ineffective-transfer fallback");
  Console.WriteLine("PASS: both axes and directions on all ship profiles; closed/bilge sources, conservation, cross-axis preservation, full receivers and ineffective-transfer detection.");
 }
}
