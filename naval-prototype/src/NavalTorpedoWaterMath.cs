using System;
public static class NavalTorpedoWaterMath
{
 public static bool Submerged(float actualSurface,float centre){return !float.IsNaN(actualSurface)&&actualSurface-centre>=.23f;}
}
