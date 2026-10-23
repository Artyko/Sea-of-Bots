public static class NavalSailRecoveryPolicy
{
 public static bool MayBorrow(bool cannonRepair,bool waiting,bool returning,bool pump,bool critical,bool trim,bool bailing,bool carryingWater,bool patch,bool patchCovered,bool travellingToPatch)
 {
  if(carryingWater)return false;
  return cannonRepair||waiting||returning||pump&&!critical&&!trim||bailing&&!critical||patch&&patchCovered&&travellingToPatch;
 }
}
