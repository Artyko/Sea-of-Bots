public static class NavalMaintenanceMath
{
    public const float PumpOffsetZ=-.45f;
    public const float PumpOutRate=1.2f, PumpTransferRate=1.6f; // cubic metres per second, all operators and ships
    public static int FreeWorkersNeeded(bool leaks,bool repairAssigned,bool pumpNeeded,bool pumpAssigned)
    {return (leaks&&!repairAssigned?1:0)+(pumpNeeded&&!pumpAssigned?1:0);}
    public static bool NeedPump(bool activeLeaks,bool otherRepair,bool repairAvailable,float volume,float dangerVolume)
    {return !activeLeaks||otherRepair||!repairAvailable||volume>=dangerVolume;}
}
