using System;
class AmmoPriorityChecks {static void Main(){try{
 if(!NavalAmmoPriority.CanSupply(false,false,false,false,false))throw new Exception("Quiet ship can resupply");
 if(!NavalAmmoPriority.CanSupply(true,true,false,false,false))throw new Exception("Covered noncritical leak permits spare ammo runner");
 if(NavalAmmoPriority.CanSupply(true,false,false,false,false))throw new Exception("Uncovered leak keeps repair priority");
 if(NavalAmmoPriority.CanSupply(true,true,true,false,false)||NavalAmmoPriority.CanSupply(true,true,false,true,false)||NavalAmmoPriority.CanSupply(true,true,false,false,true))throw new Exception("Critical flooding, trim and fire interrupt supply");
 if(!NavalAmmoPriority.CanSupplyWhenCovered(true,true,true))throw new Exception("Spare hand can supply once survival stations are covered");
 if(NavalAmmoPriority.CanSupplyWhenCovered(false,true,true)||NavalAmmoPriority.CanSupplyWhenCovered(true,false,true)||NavalAmmoPriority.CanSupplyWhenCovered(true,true,false))throw new Exception("Every active survival duty needs coverage before supply");
 Console.WriteLine("PASS: ammo survival priority with covered/uncovered repair, pump and fire duties.");
 }catch(Exception e){Console.WriteLine(e);Environment.ExitCode=1;}}}
