using System;
public static class NavalShipSpeedMath
{
    public static float Measure(float dx,float dz,float dt)
    {return dt>0?(float)Math.Sqrt(dx*dx+dz*dz)/dt:0;}
    public static float Knots(float metresPerSecond){return Math.Max(0,metresPerSecond)*1.9438445f;}
}
