// Shared arbitration for both crews. Refurbishment never competes with survival work.
public static class NavalMaintenancePriority
{
 public static bool AllowCannonRepair(bool quiet,bool fire,bool leaks,bool pumpDemand,bool critical,bool trim,bool pumpBroken)
 {return quiet&&!fire&&!leaks&&!pumpDemand&&!critical&&!trim&&!pumpBroken;}
 public static bool CanClaimCannon(bool allowed,bool otherRepairer)
 {return allowed&&!otherRepairer;}
 public static bool DeferEquipment(bool cannon,bool pump,bool fire,bool pumpDemand)
 {return !pump&&(fire||pumpDemand);}
 public static int RepairTier(bool equipment,bool pump,bool cannon)
 {return !equipment?3:pump?2:cannon?0:1;}
 public static bool DeferOrdinaryFire(bool leaks,bool leakCovered)
 {return leaks&&!leakCovered;}
}
