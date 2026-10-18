using System;
class SpareMaintenanceChecks
{
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 static void Main()
 {
  Check(NavalMaintenanceMath.CanAcquireMaintenance(false,true,false,true),"Broken-post spare may help an existing repairer without critical flooding");
  Check(!NavalMaintenanceMath.CanAcquireMaintenance(false,false,false,true),"Working gunner retains calm-water one-worker rule");
  Check(NavalMaintenanceMath.CanAcquireMaintenance(false,true,false,false),"Unassigned hand takes first useful job");
  Check(NavalMaintenanceMath.NeedPump(true,true,true,false,true),"Idle hand pumps while another patches");
  Check(!NavalMaintenanceMath.NeedPump(true,true,true,false,false),"No additional healthy gunner called for routine pumping");
  Check(!NavalMaintenanceMath.NeedPump(true,false,true,false,true),"First available worker patches reachable hole before pumping");
  Check(!NavalMaintenanceMath.CanAcquireMaintenance(true,true,true,false),"Final boarding attack still overrides maintenance");
  Check(NavalMaintenanceMath.CanAcquireMaintenance(false,false,true,true),"Emergency still allows additional damage control");
  Check(NavalMaintenanceMath.FreeWorkersNeeded(true,true,true,false,false,false,false,false,false)==0,"Spare exception does not increase normal gunner recalls");
  Console.WriteLine("PASS: broken-post spare assists repairs/pumping, healthy gunner protected, patch priority and last-attack override.");
 }
}
