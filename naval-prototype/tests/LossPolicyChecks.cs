using System;
class LossPolicyChecks{
 static void Main(){
  if(NavalLossPolicy.Irrecoverable(.5f,0,1,100,2)||NavalLossPolicy.Irrecoverable(.93f,0,0,100,2)||NavalLossPolicy.Irrecoverable(.93f,0,1,1,2))throw new Exception("Recoverable ship retired");
  if(!NavalLossPolicy.Irrecoverable(.93f,0,.5f,10,2)||!NavalLossPolicy.Irrecoverable(.7f,80,0,0,2))throw new Exception("Terminal state missed");
  float seconds=0;for(int i=0;i<100;i++)seconds=NavalLossPolicy.Step(seconds,.05f,true);if(seconds>=8)throw new Exception("Retired too early");
  seconds=NavalLossPolicy.Step(seconds,2,false);if(seconds!=0)throw new Exception("Recovery did not clear timer");
  Console.WriteLine("PASS: recoverable flooding retained, sustained terminal flooding/capsize recognised, recovery hysteresis.");
 }
}
