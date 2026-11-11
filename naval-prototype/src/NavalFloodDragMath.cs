using System;
// First-order displacement resistance: P ~ displacement^(2/3) * speed^3.
public static class NavalFloodDragMath
{
 public static float SpeedRetention(double dryDisplacement,double waterVolume,float attachedFraction){double dry=Math.Max(1,dryDisplacement*Math.Max(.01f,attachedFraction));return (float)Math.Pow(1+Math.Max(0,waterVolume)/dry,-2.0/9.0);}
}
