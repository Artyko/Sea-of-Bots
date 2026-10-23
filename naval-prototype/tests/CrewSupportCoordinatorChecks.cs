using System;
class CrewSupportCoordinatorChecks
{
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 static void Main()
 {
  var eligible=new[]{true,true,true,true};var costs=new[]{15f,3f,20f,6f};
  int owner=NavalCrewSupportPolicy.Choose(-1,eligible,costs);Check(owner==1,"Nearest eligible worker selected");
  for(int i=0;i<200;i++){costs[0]=.1f;costs[1]=30;Check(NavalCrewSupportPolicy.Choose(owner,eligible,costs)==1,"Another worker becoming nearer never steals an active reservation");}
  eligible[1]=false;Check(NavalCrewSupportPolicy.Choose(owner,eligible,costs)==0,"Owner death/unavailability allows explicit reassignment");
  var demand=new NavalPumpDemand();demand.Step(.1f,true,false);Check(demand.Active,"Water above entry threshold requests an operator");
  for(int i=0;i<100;i++){demand.Step(.1f,i%2==0,false);Check(demand.Active,"Threshold noise does not cancel the pump trip");}
  for(int i=0;i<19;i++){demand.Step(.1f,false,true);Check(demand.Active,"Keep assignment during settling period after drainage");}
  demand.Step(.2f,false,true);Check(!demand.Active,"Sustained near-empty state releases operator");
  demand.Step(.1f,true,false);demand.Reset();Check(!demand.Active,"Broken pump/sunk ship clears demand");
  Console.WriteLine("PASS: nearest dispatch, sticky active ownership, explicit reassignment, noisy demand, settled completion and equipment reset.");
 }
}
