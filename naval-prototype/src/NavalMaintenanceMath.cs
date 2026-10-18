public static class NavalMaintenanceMath
{
    public const float PumpOffsetZ=-.45f;
    public const float PumpOutRate=1.2f, PumpTransferRate=1.6f; // cubic metres per second, all operators and ships
    public static int FreeWorkersNeeded(bool leaks,bool repairAssigned,bool pumpNeeded,bool pumpAssigned,bool critical=false,bool bailAssigned=false,bool pumpBroken=false,bool pumpRepairAssigned=false,bool parallel=false)
    {
        if(!parallel&&!critical)
        {
            if(repairAssigned||pumpAssigned||pumpRepairAssigned||bailAssigned)return 0;
            return leaks||pumpNeeded||pumpBroken?1:0;
        }
        return (leaks&&!repairAssigned?1:0)+(pumpNeeded&&!pumpAssigned?1:0)+(critical&&!bailAssigned&&(!pumpNeeded||pumpAssigned)?1:0)+(pumpBroken&&!pumpRepairAssigned?1:0);
    }
    public static bool CanAcquireMaintenance(bool finalAttack,bool spare,bool parallel,bool otherWorker)
    {return !finalAttack&&(spare||parallel||!otherWorker);}
    public static bool NeedPump(bool activeLeaks,bool otherRepair,bool repairAvailable,bool parallel=false,bool spare=false)
    {return (!activeLeaks||otherRepair||!repairAvailable)&&(!otherRepair||parallel||spare);}
}
