using System;
class AmmoPriorityChecks {static void Main(){try{
 if(!NavalAmmoPriority.CanSupply(false,false,false,false,false))throw new Exception("Quiet ship can resupply");
 if(!NavalAmmoPriority.CanSupply(true,true,false,false,false))throw new Exception("Covered noncritical leak permits spare ammo runner");
 if(NavalAmmoPriority.CanSupply(true,false,false,false,false))throw new Exception("Uncovered leak keeps repair priority");
 if(NavalAmmoPriority.CanSupply(true,true,true,false,false)||NavalAmmoPriority.CanSupply(true,true,false,true,false)||NavalAmmoPriority.CanSupply(true,true,false,false,true))throw new Exception("Critical flooding, trim and fire interrupt supply");
 Console.WriteLine("PASS: ammo survival priority with covered/uncovered leaks, critical flooding, trim and fire.");
 }catch(Exception e){Console.WriteLine(e);Environment.ExitCode=1;}}}
