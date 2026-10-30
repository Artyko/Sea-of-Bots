using System;

// Deterministic steam state used by both runtime code and local tests.
public sealed class NavalSteamState
{
    public const float MaximumCoal=100f;
    public const float FullBurn=0.06f;
    public const float IdleBurn=0.015f;
    public const float PressureRise=0.12f;
    public const float PressureFall=0.08f;
    public const float UnattendedPressureLimit=0.55f;
    public const float ReverseFactor=0.65f;
    public const float FloodPressureFall=0.16f;
    public float Coal {get;private set;}
    public float Pressure {get;private set;}
    public float LastBurnRate {get;private set;}
    public float Combustion {get;private set;}
    public float RuptureStress {get;private set;}
    public bool Ruptured {get;private set;}

    public NavalSteamState(){Reset();}
    public void Reset(){Coal=MaximumCoal;Pressure=UnattendedPressureLimit;LastBurnRate=RuptureStress=0f;Combustion=1f;Ruptured=false;}
    private static float Clamp01(float value){return value<0?0:value>1?1:value;}
    private static float Move(float value,float target,float delta)
    {return value<target?Math.Min(target,value+delta):Math.Max(target,value-delta);}
    public float RequestedPressure(float command)
    {return Coal<=0?0f:0.25f+0.75f*Clamp01(Math.Abs(command));}
    public float PressureLimit(bool engineer,float funnelIntegrity)
    {
        float draft=.20f+.80f*Clamp01(funnelIntegrity);
        return Math.Min(draft,engineer?1f:UnattendedPressureLimit);
    }
    public void Step(float dt,float command,bool engineer,float boilerIntegrity,float funnelIntegrity)
    {StepFlooded(dt,command,engineer,boilerIntegrity,funnelIntegrity,0f);}
    public bool StepFlooded(float dt,float command,bool engineer,float boilerIntegrity,float funnelIntegrity,float flooding)
    {
        dt=Math.Max(0f,dt);float demand=Clamp01(Math.Abs(command));flooding=Clamp01(flooding);
        Combustion=Ruptured?0f:1f-flooding;
        LastBurnRate=Coal>0?(IdleBurn+(FullBurn-IdleBurn)*demand)*Combustion:0f;
        Coal=Math.Max(0f,Coal-LastBurnRate*dt);
        float boiler=Clamp01(boilerIntegrity);
        float target=!Ruptured&&Coal>0&&boiler>0?Math.Min(RequestedPressure(command),PressureLimit(engineer,funnelIntegrity))*Combustion:0f;
        float rate=target>Pressure?PressureRise*boiler*Combustion:PressureFall+(FloodPressureFall-PressureFall)*flooding;
        Pressure=Clamp01(Move(Pressure,target,rate*dt));
        if(Ruptured){Pressure=0f;RuptureStress=0f;return false;}
        float stressRate=NavalBoilerRuptureMath.StressRate(Pressure,boiler,flooding);
        RuptureStress=Math.Max(0f,RuptureStress+(stressRate>0f?stressRate*dt:-NavalBoilerRuptureMath.CoolingRate*dt));
        if(RuptureStress<NavalBoilerRuptureMath.Threshold)return false;
        Ruptured=true;Pressure=LastBurnRate=0f;Combustion=0f;RuptureStress=0f;return true;
    }
    public float OutputThrottle(float command)
    {float value=Math.Max(-1f,Math.Min(1f,command));return value<0?value*ReverseFactor:value;}
    public float Power(float engineIntegrity)
    {return Pressure*Clamp01(engineIntegrity);}
    public float ConsumePressure(float amount)
    {float used=Math.Min(Pressure,Math.Max(0f,amount));Pressure-=used;return used;}
    public bool NeedsEngineer(float command,float funnelIntegrity)
    {
        if(Ruptured||Combustion<=.05f)return false;
        float target=Math.Min(RequestedPressure(command),PressureLimit(true,funnelIntegrity));
        return Coal>0&&Math.Abs(command)>.05f&&target>UnattendedPressureLimit+.001f;
    }
    public void RepairRupture(){Ruptured=false;RuptureStress=0f;}
}

public static class NavalSteamMainMath
{
    public const float MaximumPumpMultiplier=2.4f;
    public const float PumpPressureDraw=.18f;
    public const float LeakPressureDraw=.24f;
    public static float ConsumerIntegrity(float integrity)
    {return .15f+.85f*Math.Max(0f,Math.Min(1f,integrity));}
    public static float PumpMultiplier(float pressure,float integrity)
    {
        float available=Math.Max(0f,Math.Min(1f,pressure))*Math.Max(0f,Math.Min(1f,integrity));
        return 1f+(MaximumPumpMultiplier-1f)*available;
    }
    public static float PumpDraw(float dt,float multiplier)
    {return Math.Max(0f,dt)*PumpPressureDraw*Math.Max(0f,(multiplier-1f)/(MaximumPumpMultiplier-1f));}
    public static float LeakDraw(float dt,float pressure,float missingFraction)
    {return Math.Max(0f,dt)*LeakPressureDraw*Math.Max(0f,Math.Min(1f,pressure))*Math.Max(0f,Math.Min(1f,missingFraction));}
    public static float AuxiliaryReloadFactor(float integrity)
    {return .35f+.65f*Math.Max(0f,Math.Min(1f,integrity));}
}

public static class NavalBoilerFloodMath
{
    // World metres above the hold floor. Water reaches the grate first and
    // progressively blankets the firebox; stored steam remains in the drum.
    public const float GrateHeight=.15f,FireboxCrown=1.00f;
    public static float Flooding(float waterDepth)
    {return Math.Max(0f,Math.Min(1f,(waterDepth-GrateHeight)/(FireboxCrown-GrateHeight)));}
}

public static class NavalBoilerRuptureMath
{
    // Flood water cannot burst an intact boiler. A shell-weakened pressure
    // vessel can fail when a hot, highly pressurised drum is rapidly quenched.
    public const float MinimumPressure=.60f,MaximumSafeDamage=.60f,MinimumFlooding=.55f;
    public const float Threshold=.30f,CoolingRate=.12f;
    public static float StressRate(float pressure,float integrity,float flooding)
    {
        if(pressure<=MinimumPressure||integrity>=MaximumSafeDamage||flooding<=MinimumFlooding)return 0f;
        float hot=(pressure-MinimumPressure)/(1f-MinimumPressure);
        float weak=(MaximumSafeDamage-integrity)/MaximumSafeDamage;
        float quench=(flooding-MinimumFlooding)/(1f-MinimumFlooding);
        return Math.Max(0f,Math.Min(1f,hot))*Math.Max(0f,Math.Min(1f,weak))*Math.Max(0f,Math.Min(1f,quench));
    }
}

public static class NavalSteamSmokeMath
{
    public static float Density(float burnRate,float pressure)
    {
        float span=NavalSteamState.FullBurn-NavalSteamState.IdleBurn;
        float load=span>0?(burnRate-NavalSteamState.IdleBurn)/span:0f;
        load=Math.Max(0f,Math.Min(1f,load));pressure=Math.Max(0f,Math.Min(1f,pressure));
        return (.18f+.82f*load)*(.25f+.75f*pressure);
    }
    public static bool Emits(float burnRate,float flooding,float funnelIntegrity)
    {return burnRate>0f&&flooding<1f&&funnelIntegrity>0f;}
    public static bool IsExternalOutlet(bool missing,bool detached,float localY,float weatherDeck)
    {return !missing&&!detached&&localY>weatherDeck+.04f;}
}
