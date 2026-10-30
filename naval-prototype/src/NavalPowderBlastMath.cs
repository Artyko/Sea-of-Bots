using System;
public static class NavalPowderBlastMath
{
    public const float HullRadius=6f,RobotRadius=5.5f,Power=3f;
    public static bool HitsRobot(float squaredDistance){return squaredDistance<=RobotRadius*RobotRadius;}
}

public static class NavalAmmunitionBlastMath
{
    public const float ReadyHullRadius=7.2f,ReadyRobotRadius=6.5f,ReadyPower=3.8f;
    public const float MagazineHullRadius=9.2f,MagazineRobotRadius=8f,MagazinePower=4.8f;
    public static float HullRadius(bool readyUse,bool magazine,bool mainMagazine)
    {return mainMagazine?NavalMainMagazineBlastMath.HullRadius:magazine?MagazineHullRadius:readyUse?ReadyHullRadius:NavalPowderBlastMath.HullRadius;}
    public static float RobotRadius(bool readyUse,bool magazine,bool mainMagazine)
    {return mainMagazine?NavalMainMagazineBlastMath.RobotRadius:magazine?MagazineRobotRadius:readyUse?ReadyRobotRadius:NavalPowderBlastMath.RobotRadius;}
    public static float Power(bool readyUse,bool magazine,bool mainMagazine)
    {return mainMagazine?NavalMainMagazineBlastMath.Power:magazine?MagazinePower:readyUse?ReadyPower:NavalPowderBlastMath.Power;}
    public static float VisualScale(bool readyUse,bool magazine,bool mainMagazine)
    {return mainMagazine?1.85f:magazine?1.45f:readyUse?1.22f:1f;}
    public static bool HitsRobot(float squaredDistance,bool readyUse,bool magazine,bool mainMagazine)
    {float radius=RobotRadius(readyUse,magazine,mainMagazine);return squaredDistance<=radius*radius;}
}
