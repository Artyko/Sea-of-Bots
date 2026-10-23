using System;
class MaintenancePriorityChecks {
 static void Check(bool value,string why){if(!value)throw new Exception(why);}
 static void Main(){
 foreach(var ship in NavalShipDefinition.Choices){
  // Regression: two healthy guns, opposite broken battery, active ordinary fire.
  var combat=new NavalCannonBattlePolicy();
  Check(!NavalMaintenancePriority.AllowCannonRepair(combat.AllowRepairs,true,false,false,false,false,false),ship.Name+": even a small fire vetoes both opposite-battery repairs");
  Check(!NavalMaintenancePriority.CanClaimCannon(true,true),"Second sailor cannot start a different gun refurbishment");
  Check(NavalMaintenancePriority.CanClaimCannon(true,false),"One spare sailor can refurbish in safety");
  combat.ObserveFire(.05f,true);
  Check(!NavalMaintenancePriority.AllowCannonRepair(combat.AllowRepairs,false,false,false,false,false,false),"Firing opportunity keeps healthy guns staffed");
  Check(NavalMaintenancePriority.DeferEquipment(true,false,true,false),"Cannon task yields to assigned firefighting");
  Check(NavalMaintenancePriority.DeferEquipment(false,false,true,false),"Nonurgent helm restoration cannot steal fire assignment");
  Check(!NavalMaintenancePriority.DeferEquipment(false,true,true,true),"Broken pump remains a survival repair candidate");
  Check(NavalMaintenancePriority.DeferEquipment(false,false,false,true),"Functional pump demand beats nonessential equipment restoration");
  Check(NavalMaintenancePriority.DeferOrdinaryFire(true,false),"Uncovered leak receives first ordinary response");
  Check(!NavalMaintenancePriority.DeferOrdinaryFire(true,true),"A separate assigned firefighter can work while leak repairer is underway");
 }
 for(int mask=0;mask<64;mask++){
 bool allowed=NavalMaintenancePriority.AllowCannonRepair(true,(mask&1)!=0,(mask&2)!=0,(mask&4)!=0,(mask&8)!=0,(mask&16)!=0,(mask&32)!=0);
 Check(allowed==(mask==0),"Every combination of survival demand blocks gun restoration");
 }
 Check(NavalMaintenancePriority.RepairTier(false,false,false)>NavalMaintenancePriority.RepairTier(true,true,false),"Flowing hull damage beats equipment score");
 Check(NavalMaintenancePriority.RepairTier(true,true,false)>NavalMaintenancePriority.RepairTier(true,false,false),"Pump repairs beat helm");
 Check(NavalMaintenancePriority.RepairTier(true,false,false)>NavalMaintenancePriority.RepairTier(true,false,true),"Helm repairs beat cannon refurbishment");
 Console.WriteLine("PASS: active-fire/opposite-battery regression on all profiles, single repairer, 64 survival combinations, firing priority, fire/leak coverage and repair tiers.");
 }
}
