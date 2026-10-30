public static class NavalAmmoPriority {
 public static bool CanSupply(bool leaks,bool anotherRepairer,bool critical,bool trim,bool fire)
 {return !critical&&!trim&&!fire&&(!leaks||anotherRepairer);}
 // A spare hand may resupply while the ship is damaged once every active
 // survival duty already has an operator. This avoids starving all guns for
 // the entire duration of a fire without pulling the actual firefighters,
 // pump operator or repairer away from their work.
 public static bool CanSupplyWhenCovered(bool repairsCovered,bool pumpCovered,bool fireCovered)
 {return repairsCovered&&pumpCovered&&fireCovered;}
}
