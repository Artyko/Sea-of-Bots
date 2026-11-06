using System;
// Fraction of sinking displacement and absolute heel. Shared by crew and helm tactics.
public sealed class NavalCombatEmergency
{
    public bool Critical {get;private set;}
    public bool RepairRetreat {get;private set;}
    public void Step(float fill,float heel,bool unstableLeak=false,bool earlyRetreat=false)
    {
        heel=Math.Abs(heel);
        if(!Critical)Critical=unstableLeak||fill>=.75f||fill>=.35f&&heel>=20f;
        else if(fill<=.6f&&heel<=14f)Critical=false;
        // Leave the firing line before the final gunner is needed below deck.
        float retreatFill=earlyRetreat?.20f:.35f;
        float recoverFill=earlyRetreat?.12f:.25f;
        if(!RepairRetreat)RepairRetreat=unstableLeak||fill>=retreatFill||fill>=retreatFill-.05f&&heel>=16f;
        else if(fill<=recoverFill&&heel<=12f)RepairRetreat=false;
    }
    public static bool ReleaseGunner(int assignedGuns,bool critical)
    {return assignedGuns>1||assignedGuns==1&&critical;}
}
