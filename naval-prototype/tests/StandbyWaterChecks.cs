using System;
class StandbyWaterChecks{
 static void Main(){
  if(!NavalStandbyWaterPolicy.Unsafe(.7f)||NavalStandbyWaterPolicy.Unsafe(.3f))throw new Exception("Flooded station threshold");
  if(!NavalStandbyWaterPolicy.DryAgain(.1f)||NavalStandbyWaterPolicy.DryAgain(.4f))throw new Exception("Dry-return hysteresis");
  if(NavalStandbyWaterPolicy.AALimit(true,4)!=2||NavalStandbyWaterPolicy.AALimit(false,4)!=4||NavalStandbyWaterPolicy.AALimit(true,1)!=1)throw new Exception("Emergency retains limited AA coverage");
  Console.WriteLine("PASS: flooded standby rejection, dry-return hysteresis and nonzero emergency AA allocation.");
 }
}
