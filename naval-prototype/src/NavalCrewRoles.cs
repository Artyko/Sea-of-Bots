public static class NavalCrewRoles
{
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
