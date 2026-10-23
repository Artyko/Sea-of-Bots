using System;
class ShipSpeedChecks
{
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 static void Main()
 {
  Check(NavalShipDefinition.Sloop.MaxSailingSpeed>NavalShipDefinition.Brigantine.MaxSailingSpeed&&NavalShipDefinition.Brigantine.MaxSailingSpeed>NavalShipDefinition.Galleon.MaxSailingSpeed,"Each production ship has distinct healthy sail speed");
  foreach(var ship in NavalShipDefinition.Choices)
  {
   float full=0,damaged=0;for(int i=0;i<2400;i++){full=NavalRigMath.StepSpeed(full,1,1,.05f,ship.MaxSailingSpeed);damaged=NavalRigMath.StepSpeed(damaged,1,.5f,.05f,ship.MaxSailingSpeed);}
   Check(Math.Abs(full-ship.MaxSailingSpeed)<.001,"Healthy ship converges to its own profile speed");
   Check(Math.Abs(damaged-ship.MaxSailingSpeed*.5f)<.001,"Damaged sails reduce profile speed");
   Check(NavalRigMath.StepSpeed(full,0,1,.05f,ship.MaxSailingSpeed)>full*.9f,"Inertia retained when releasing throttle");
  }
  Check(NavalShipSpeedMath.Measure(0,0,.05f)==0,"Blocked ship or pure vertical bobbing displays zero horizontal speed");
  Check(Math.Abs(NavalShipSpeedMath.Measure(.3f,.4f,.1f)-5)<.001,"Includes actual forward and lateral drift");
  Check(Math.Abs(NavalShipSpeedMath.Knots(1)-1.9438445f)<.0001,"Correct knots conversion");
  Check(NavalShipSpeedMath.Measure(1,1,0)==0,"Paused simulation cannot produce invalid speed");
  Console.WriteLine("PASS: three profile speeds, sail damage, inertia, actual horizontal displacement, zero blocked/bobbing speed and knots.");
 }
}
