using System;
public static class NavalPowderBlastMath
{
    public const float HullRadius=6f,RobotRadius=5.5f,Power=3f;
    public static bool HitsRobot(float squaredDistance){return squaredDistance<=RobotRadius*RobotRadius;}
}
