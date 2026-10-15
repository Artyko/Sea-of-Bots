public static class NavalCrewRoles
{
    public static bool HelmUseful(float sailPower,float speed)
    {return sailPower>0.0001f||System.Math.Abs(speed)>0.15f;}
    public static bool CanRepair(bool available,int worker,int combatWorker)
    {return available&&worker!=combatWorker;}
    public static int RepairCandidate(bool firstFree,bool secondFree,int combatWorker)
    {
        if(firstFree&&combatWorker!=0)return 0;
        if(secondFree&&combatWorker!=1)return 1;
        return -1;
    }
    public static int HelmCandidate(bool firstFree,bool secondFree,int combatWorker,bool playerAtHelm)
    {
        if(playerAtHelm)return -1;
        if(firstFree&&combatWorker!=0)return 0;
        if(secondFree&&combatWorker!=1)return 1;
        return -1;
    }
}
