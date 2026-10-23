using System;
class MovementWatchdogChecks
{
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 static void Main()
 {
  var monitor=new NavalMovementWatchdog();bool stalled=false;
  for(int i=0;i<170;i++)stalled|=monitor.Step(.05f,(float)Math.Sin(i)*.04f,0,(float)Math.Cos(i)*.04f);
  Check(stalled,"Small collision jitter cannot conceal eight-second immobilization");
  monitor.Reset();for(int i=0;i<500;i++)Check(!monitor.Step(.05f,i*.04f,0,0),"Real walking progress keeps task active");
  monitor.Reset();for(int i=0;i<150;i++)Check(!monitor.Step(.05f,0,0,0),"No premature timeout during short recovery");
  Check(monitor.Step(.6f,0,0,0),"Waypoint/replan changes do not reset displacement timer");
  foreach(var ship in NavalShipDefinition.Choices)for(int sailor=0;sailor<ship.CrewCount;sailor++)
  {
   float x=NavalShipDefinition.StandbyX(sailor),z=NavalShipDefinition.StandbyZ(sailor);
   for(int gun=0;gun<ship.GunCount;gun++)
   {
    float dx=Math.Max(0,Math.Abs(x-NavalShipDefinition.GunSide(gun)*ship.GunMountX)-.375f*ship.GunFixtureScale);
    float dz=Math.Max(0,Math.Abs(z-NavalShipDefinition.GunZ(ship,gun))-.45f*ship.GunFixtureScale);
    Check((dx*dx+dz*dz)*ship.Scale*ship.Scale>.5f*.5f,"Every standby post clears cannon carriage plus full-size body margin");
   }
   Check(z>.85f,"Standby posts clear the central stair opening");
  }
  Console.WriteLine("PASS: persistent collision-stall detection, genuine progress, recovery reset and cannon-clear standby posts for all crews.");
 }
}
