public static class NavalShellFuse
{
    public const float Seconds=2f;
    public static bool Arms(bool mainCaliber,NavalPenetrationMaterial material,float residual,bool priorMetal)
    {
        if(residual<=.05f||material==NavalPenetrationMaterial.Wood)return false;
        return !mainCaliber||material==NavalPenetrationMaterial.Armor||priorMetal;
    }
    // Adjacent cells of one plate must not count as two obstacles.
    public static bool SeparateMetal(float planeDistance,float travel,bool sameHullShell)
    {return planeDistance>.25f&&(!sameHullShell||travel>NavalArmorMath.EntryPlateSeamDistance);}
}
