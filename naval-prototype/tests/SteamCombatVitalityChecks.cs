using System;

class SteamCombatVitalityChecks
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static void Near(float actual,float expected,float tolerance,string message)
    {if(Math.Abs(actual-expected)>tolerance)throw new Exception(message+": "+actual+" != "+expected);}
    static void Main()
    {
        float straight=NavalArmorMath.AngledPower(2f,1f),oblique=NavalArmorMath.AngledPower(2f,.25f);
        Check(straight>oblique,"Oblique armour impact retains less normal penetration energy");
        Check(NavalArmorMath.Ricochets(true,.2f,oblique)&&!NavalArmorMath.Ricochets(false,.2f,oblique),"Only oblique armour contact ricochets");
        Check(NavalArmorMath.SpallPower(straight,1f,false)>0f,"A strong stopped hit creates internal spall");
        Check(NavalArmorMath.SpallPower(.2f,1f,false)==0f,"A weak stopped hit cannot create arbitrary spall");
        Check(NavalArmorMath.ResidualAfterWear(1.2f,1.2f,NavalPenetrationMaterial.Armor)>
              NavalArmorMath.ResidualAfterWear(1.2f,0f,NavalPenetrationMaterial.Armor),"Local plate wear increases residual energy");

        float assisted=NavalSteamMainMath.PumpMultiplier(1f,1f),hand=NavalSteamMainMath.PumpMultiplier(0f,1f);
        Near(assisted,NavalSteamMainMath.MaximumPumpMultiplier,.0001f,"Healthy high-pressure pump receives full steam assistance");
        Near(hand,1f,.0001f,"No pressure falls back to hand-pump output");
        Check(NavalSteamMainMath.PumpMultiplier(.8f,.25f)<NavalSteamMainMath.PumpMultiplier(.8f,1f),"Pump-main damage reduces assistance");
        Check(NavalSteamMainMath.PumpDraw(1f,assisted)>0f&&NavalSteamMainMath.LeakDraw(1f,.8f,.5f)>0f,"Pumping and live leaks both consume pressure");
        Check(NavalSteamMainMath.AuxiliaryReloadFactor(0f)<NavalSteamMainMath.AuxiliaryReloadFactor(1f),"Auxiliary-main damage slows the ammunition hoist");
        var steam=new NavalSteamState();float before=steam.Pressure;steam.ConsumePressure(.1f);Near(steam.Pressure,before-.1f,.0001f,"Shared pressure reserve accepts pump/leak draw");

        Check(NavalCasemateHeelMath.LowSide(8f)==1&&NavalCasemateHeelMath.LowSide(-8f)==2,"Heel sign identifies the low battery");
        Check(NavalCasemateHeelMath.ReloadFactor(0,9f)<1f&&NavalCasemateHeelMath.ReloadFactor(1,9f)==1f,"Only the low-side battery reload is slowed");
        Check(!NavalCasemateHeelMath.CanOperate(0,14f)&&NavalCasemateHeelMath.CanOperate(1,14f),"Severe heel inhibits only the submerged-side casemate");
        Check(NavalSteamDamageControlTactics.HealthierSide(1,3,3,12f,2f)==2,"Flooded port side is turned away and healthy starboard battery is exposed");
        Check(NavalSteamDamageControlTactics.HealthierSide(2,3,0,12f,2f)==1,"Damage control never selects a destroyed battery");

        var pass=new NavalFirePassMath();
        Check(pass.Step(.05f,0f,0f,50f,40f,2,2,0,false,true,2)&&pass.Side==2,"Steam attack pass accepts the healthier battery preference");
        pass.Reset();bool separated=false;float heading=90f;
        for(int tick=0;tick<500;tick++)
        {
            bool shot=tick%100==0;pass.Step(.05f,heading,0f,70f,50f,2,2,2,shot,true,2,18f,8f,14f);
            heading=pass.Heading;if(pass.Phase==3){separated=true;break;}
        }
        Check(separated,"Steam broadside pass separates after its bounded firing window even while guns keep firing");
        var ready=new NavalMagazineState();
        for(int i=0;i<200;i++)Check(!ready.Step(.25f,false,false),"Enclosed ammunition cannot ignite without impact or nearby fire");
        Check(!ready.Lit&&!ready.Spent,"No spontaneous ammunition incident after a long quiet interval");
        var warning=new NavalMagazineState();Check(!warning.Impact(.10f)&&warning.Lit,"Ready ammunition impact can start a warning fuse");
        var instant=new NavalMagazineState();Check(instant.Impact(.01f)&&instant.Spent,"Rare direct impact can cause an immediate local explosion");
        var inert=new NavalMagazineState();Check(!inert.Impact(.90f)&&!inert.Lit&&!inert.Spent,"Most ammunition impacts cause no reaction");
        Check(NavalAmmunitionBlastMath.ReadyPower>NavalPowderBlastMath.Power&&NavalAmmunitionBlastMath.ReadyHullRadius>NavalPowderBlastMath.HullRadius,"Ready-use shell explosion is more serious than an old powder barrel");
        Check(NavalAmmunitionBlastMath.MagazinePower>NavalAmmunitionBlastMath.ReadyPower&&NavalAmmunitionBlastMath.MagazineHullRadius>NavalAmmunitionBlastMath.ReadyHullRadius,"Enclosed magazine is more destructive than one ready-use locker");
        Check(NavalMainMagazineBlastMath.Power>NavalAmmunitionBlastMath.MagazinePower&&NavalMainMagazineBlastMath.HullRadius>NavalAmmunitionBlastMath.MagazineHullRadius,"Main-calibre magazine remains the catastrophic top tier");
        Console.WriteLine("PASS: angled armour, bounded spall, shared steam pressure, pump fallback, heel-limited casemates, damage-control side choice and non-spontaneous ammunition incidents.");
    }
}
