using System;

public enum NavalPenetrationMaterial {Wood,Iron,Machinery,Armor}

public static class NavalArmorMath
{
    public const float BreakWear=1.9f;
    public const float RicochetCosine=.34f,SpallThreshold=.70f;
    public const float PointBlankRange=15f,CloseRange=55f,NormalRange=110f,LongRange=180f,ExtremeRange=260f;
    public static float BlastRadius(float radius,float multiplier)
    {return Math.Max(0f,radius)*Math.Max(0f,Math.Min(1f,multiplier));}
    public static float AddWear(float current,float power)
    {return Math.Max(0f,current)+Math.Max(0f,power);}
    public static bool Broken(float wear){return wear>=BreakWear;}
    public static bool Penetrates(float current,float power){return Broken(AddWear(current,power));}
    public static float ExplosionArmorWear(float power,float distance,float radius)
    {
        if(power<=0f||radius<=0f||distance>=radius)return 0f;
        // Pressure falls with the square of normalized distance. A ready-use
        // locker can tear its adjacent plate, magazines damage a wider patch,
        // and plates near the outer wave only accumulate dents.
        float pressure=1f-Math.Max(0f,distance)/radius;
        return Math.Max(0f,power)*pressure*pressure*.90f;
    }
    public static float ObliquityFactor(float impactCosine)
    {return Math.Max(.28f,Math.Min(1f,Math.Abs(impactCosine)));}
    public static float AngledPower(float penetrationPower,float impactCosine)
    {return Math.Max(0f,penetrationPower)*ObliquityFactor(impactCosine);}
    public static bool Ricochets(bool armored,float impactCosine,float angledPower)
    {return armored&&Math.Abs(impactCosine)<RicochetCosine&&angledPower<BreakWear*1.35f;}
    public static float RicochetWear(float angledPower){return Math.Max(.08f,angledPower*.30f);}
    public static float RemainingResistance(float wear,NavalPenetrationMaterial material)
    {return material==NavalPenetrationMaterial.Armor?Math.Max(0f,BreakWear-Math.Max(0f,wear)):Resistance(material);}
    public static float ResidualAfterWear(float angledPower,float wear,NavalPenetrationMaterial material)
    {return Math.Max(0f,Math.Max(0f,angledPower)-RemainingResistance(wear,material));}
    public static float SpallPower(float angledPower,float impactCosine,bool ricochet)
    {
        float shock=Math.Max(0f,angledPower)*(ricochet?.45f:.72f)*(.65f+.35f*ObliquityFactor(impactCosine));
        return shock>=SpallThreshold?Math.Min(2.2f,shock):0f;
    }
    public static float SpallRadius(float spallPower)
    {return spallPower<=0f?0f:Math.Min(1.8f,.55f+spallPower*.45f);}
    // Armour penetration follows retained shell energy rather than applying the
    // same value at every distance.  A well-aligned rifled shell can punch one
    // intact plate at useful combat range; oblique and distant hits still need
    // repeated strikes, so armour remains valuable without creating a stalemate.
    public static float PenetrationPower(float basePower,float travelled)
    {
        basePower=Math.Max(0f,basePower);travelled=Math.Max(0f,travelled);
        float multiplier;
        if(travelled<=PointBlankRange)multiplier=1.85f;
        else if(travelled<=CloseRange)multiplier=Lerp(1.85f,1.72f,(travelled-PointBlankRange)/(CloseRange-PointBlankRange));
        else if(travelled<=NormalRange)multiplier=Lerp(1.72f,1.60f,(travelled-CloseRange)/(NormalRange-CloseRange));
        else if(travelled<=LongRange)multiplier=Lerp(1.60f,1.32f,(travelled-NormalRange)/(LongRange-NormalRange));
        else if(travelled<=ExtremeRange)multiplier=Lerp(1.32f,.68f,(travelled-LongRange)/(ExtremeRange-LongRange));
        else multiplier=.68f;
        return basePower*multiplier;
    }
    public static NavalPenetrationMaterial Material(bool armored,bool metallic,bool equipment)
    {return armored?NavalPenetrationMaterial.Armor:equipment?NavalPenetrationMaterial.Machinery:metallic?NavalPenetrationMaterial.Iron:NavalPenetrationMaterial.Wood;}
    public static float Resistance(NavalPenetrationMaterial material)
    {
        switch(material){case NavalPenetrationMaterial.Armor:return BreakWear;case NavalPenetrationMaterial.Machinery:return 1.35f;case NavalPenetrationMaterial.Iron:return 1.05f;default:return .45f;}
    }
    public static float ResidualPower(float basePower,float travelled,NavalPenetrationMaterial material)
    {return Math.Max(0f,PenetrationPower(basePower,travelled)-Resistance(material));}
    public static bool Perforates(float basePower,float travelled,NavalPenetrationMaterial material)
    {return ResidualPower(basePower,travelled,material)>.05f;}
    public static float InternalFuseDepth(float residualPower)
    {return Math.Max(1.1f,Math.Min(3.0f,1.15f+Math.Max(0f,residualPower)*.25f));}
    public static float InternalFuseDepth(float residualPower,NavalPenetrationMaterial material)
    {
        float materialDelay=material==NavalPenetrationMaterial.Armor?1f:material==NavalPenetrationMaterial.Iron?.78f:material==NavalPenetrationMaterial.Machinery?.62f:.45f;
        return Math.Max(.55f,Math.Min(3.2f,InternalFuseDepth(residualPower)*materialDelay));
    }
    public static bool ArmsInternalFuse(NavalPenetrationMaterial material,float residualPower)
    {return residualPower>.05f;}
    public static float ExitVelocityFactor(float impactPower,float residualPower,NavalPenetrationMaterial material)
    {
        float retained=impactPower<=.001f?0f:Math.Max(0f,Math.Min(1f,residualPower/impactPower));
        float materialLoss=material==NavalPenetrationMaterial.Armor?.82f:material==NavalPenetrationMaterial.Machinery?.75f:material==NavalPenetrationMaterial.Iron?.78f:.90f;
        return Math.Max(.38f,Math.Min(.92f,(.42f+retained*.50f)*materialLoss));
    }
    public static float MechanismArmorPower(float armorPower,bool mainBattery)
    {return Math.Max(0f,armorPower)*(mainBattery?1.35f:1f);}
    public static float InternalBlastPower(float basePower,float residualPower)
    {
        basePower=Math.Max(0f,basePower);residualPower=Math.Max(0f,residualPower);
        // A shell which has really crossed a plate must not turn into a tiny
        // exterior splash. Light AP produces a compact splinter burst; the
        // main battery retains enough explosive effect to wreck a compartment.
        return basePower>2f?Math.Min(basePower,2.15f+residualPower*.72f):
            Math.Min(Math.Max(1.05f,basePower*1.35f),1.05f+residualPower*.72f);
    }
    public static float InternalBlastRadiusMultiplier(float basePower,float residualPower)
    {return basePower>2f?Math.Min(1.75f,1.35f+Math.Max(0f,residualPower)*.075f):Math.Min(1.32f,1.10f+Math.Max(0f,residualPower)*.12f);}
    public static float InternalFireChance(float basePower,float residualPower)
    {
        // Internal damage remains substantial, but ignition is exceptional:
        // v250 turned 8 of 22 bursts into fires and quickly saturated both
        // ships. Main-calibre bursting charge is still more dangerous.
        return basePower>2f?Math.Min(.32f,.14f+Math.Max(0f,residualPower)*.03f):
            Math.Min(.14f,.05f+Math.Max(0f,residualPower)*.035f);
    }
    public static int InternalFireLimit(float basePower,float residualPower)
    {return basePower>2f?(residualPower>3f?3:2):(residualPower>1f?2:1);}
    // A sweep is measured from the beginning of its sub-step. On the step in
    // which the shell enters the hull, only the distance after the entry
    // surface counts as internal travel.
    public static float InternalEncounterDistance(float travelledInside,float hitDistance,float entryOffset)
    {return Math.Max(0f,travelledInside)+Math.Max(0f,hitDistance-Math.Max(0f,entryOffset));}
    private static float Lerp(float a,float b,float t){return a+(b-a)*Math.Max(0f,Math.Min(1f,t));}
}
public static class NavalImpactMaterial
{
    public static bool IsMetal(bool armored,bool metallic,bool repairableEquipment)
    {return armored||metallic||repairableEquipment;}
}
