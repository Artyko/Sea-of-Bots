using System;
// Fraction of sinking displacement and absolute heel. Shared by crew and helm tactics.
public sealed class NavalCombatEmergency
{
    public bool Critical {get;private set;}
    public bool RepairRetreat {get;private set;}
    public void Step(float fill,float heel)
    {
        heel=Math.Abs(heel);
        if(!Critical)Critical=fill>=.75f||fill>=.35f&&heel>=20f;
        else if(fill<=.6f&&heel<=14f)Critical=false;
        // Leave the firing line before the final gunner is needed below deck.
        if(!RepairRetreat)RepairRetreat=fill>=.5f||fill>=.3f&&heel>=16f;
        else if(fill<=.4f&&heel<=12f)RepairRetreat=false;
    }
    public static bool ReleaseGunner(int assignedGuns,bool critical)
    {return assignedGuns>1||assignedGuns==1&&critical;}
}
