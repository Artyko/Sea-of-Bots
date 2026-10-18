using System;
class CrewPrioritiesChecks
{
 static void Check(bool value,string message){if(!value)throw new Exception(message);}
 static void Main(){
  Check(NavalCrewPriorities.ReserveHelm(4,true,true),"Enough crew keeps helm during flood emergency");
  Check(NavalCrewPriorities.ReserveHelm(1,false,true),"Last survivor keeps steering outside critical danger");
  Check(!NavalCrewPriorities.ReserveHelm(1,true,true),"Last survivor must fight critical flooding instead of holding helm");
  Check(!NavalCrewPriorities.ReserveHelm(0,true,true),"No absent helmsman");
  Check(NavalMaintenanceMath.FreeWorkersNeeded(true,false,true,false,false,false,false,false,true)==2,"First two hands patch and pump");
  Check(NavalMaintenanceMath.FreeWorkersNeeded(false,false,false,false,true,false)==1,"Broken pump still calls an emergency bucket worker");
  Check(NavalMaintenanceMath.FreeWorkersNeeded(true,true,true,true,true,false)==1,"Critical water gets a bucket after repairer and pump are assigned");
  Check(NavalMaintenanceMath.FreeWorkersNeeded(true,true,true,true,true,true)==0,"Existing bucket does not strip an extra gunner");
  Check(NavalMaintenanceMath.FreeWorkersNeeded(false,false,false,false,false,false)==0,"No routine bucket jobs below critical threshold");
  Check(!NavalMaintenanceMath.NeedPump(true,false,true),"Sole repairer patches reachable inflow before pumping");
  Check(NavalMaintenanceMath.NeedPump(true,true,true,true),"Second worker pumps while first patches");
  Check(NavalMaintenanceMath.NeedPump(true,false,false),"Unreachable leak cannot prevent pumping");
  float idle=NavalCrewPriorities.DefenseCost(-1,2,false,false,false,50f);
  float extraGun=NavalCrewPriorities.DefenseCost(1,2,false,false,false,1f);
  float lastGun=NavalCrewPriorities.DefenseCost(1,1,false,false,false,1f);
  float pump=NavalCrewPriorities.DefenseCost(-1,1,true,false,false,1f);
  float helm=NavalCrewPriorities.DefenseCost(0,1,false,false,false,1f);
  float criticalRepair=NavalCrewPriorities.DefenseCost(-1,1,true,true,true,1f);
  Check(idle<extraGun&&extraGun<lastGun&&lastGun<pump&&pump<helm,"Defender prefers idle then extra gun, protecting pumping and helm");
  Check(helm<criticalRepair,"Last reachable lifesaving patch is protected even over helm");
  Check(NavalCrewPriorities.CanBoard(true,false,false,false),"Dry disarmed ship may board");
  Check(NavalCrewPriorities.CanBoard(true,true,false,false),"Last attack overrides leaks");
  Check(NavalCrewPriorities.CanBoard(true,false,true,false),"Last attack overrides pumping");
  Check(NavalCrewPriorities.CanBoard(true,false,false,true),"Last attack overrides repair retreat");
  Check(!NavalCrewPriorities.CanBoard(false,false,false,false),"Working guns retain normal combat");
  Check(!NavalCrewPriorities.ShouldWithdraw(false,true,true),"Disarmed ship commits to last attack despite maintenance");
  Check(!NavalCrewPriorities.ShouldWithdraw(false,true,false),"Dry disarmed ship may approach for boarding");
  Check(!NavalCrewPriorities.ShouldWithdraw(false,false,true),"Ordinary repair with working guns preserves early-retreat threshold");
  Check(NavalCrewPriorities.ShouldWithdraw(true,false,false),"Shared flood danger always overrides attack");
  Check(NavalMaintenanceMath.FreeWorkersNeeded(true,false,false,false,false,false,true,false,true)==2,"Leak and broken pump reserve separate repairers");
  Check(NavalMaintenanceMath.FreeWorkersNeeded(true,true,false,false,true,false,true,true)==1,"Already assigned leak/pump repair leaves only a bucket vacancy");
  for(int mask=0;mask<8;mask++)
  {
   bool leaks=(mask&1)!=0,pumpNeeded=(mask&2)!=0,danger=(mask&4)!=0;
   Check(NavalCrewPriorities.CanBoard(true,leaks,pumpNeeded,danger),"Disarmed crew boards at every flood severity");
   Check(!NavalCrewPriorities.ShouldWithdraw(danger,true,leaks||pumpNeeded),"Final attack never withdraws for repairs");
   Check(!NavalCrewPriorities.CanBoard(false,leaks,pumpNeeded,danger),"One remaining working cannon preserves normal combat");
  }
  Console.WriteLine("PASS: patch/pump/bucket staffing, scarce crew, last survivor, defender priorities and last-attack overrides.");
 }
}
