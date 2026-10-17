using System;

// Shared, testable arbitration for both teams and every ship profile.
public static class NavalCrewPriorities
{
    public static bool ReserveHelm(int availableCrew,bool critical,bool maintenanceNeeded)
    {return availableCrew>0&&!(availableCrew==1&&critical&&maintenanceNeeded);}
    public static bool ShouldWithdraw(bool retreatDanger,bool gunsBroken,bool maintenanceNeeded)
    {return retreatDanger||gunsBroken&&maintenanceNeeded;}
    public static bool CanBoard(bool gunsBroken,bool leaks,bool pumpNeeded,bool retreating)
    {return gunsBroken&&!leaks&&!pumpNeeded&&!retreating;}
    public static float DefenseCost(int role,int assignedGuns,bool maintenance,bool repairing,bool critical,float distance)
    {
        float cost=role==0?60f:role>0?(assignedGuns>1?10f:20f):0f;
        if(maintenance)cost=Math.Max(cost,critical&&repairing?80f:40f);
        return cost+Math.Min(5f,Math.Max(0f,distance)*.05f);
    }
}
