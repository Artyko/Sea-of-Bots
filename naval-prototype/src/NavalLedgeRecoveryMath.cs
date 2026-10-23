using System;
public static class NavalLedgeRecoveryMath
{
    public static bool Needed(float feetY,float scale){return (feetY-.825f)*scale>.2f;}
    public static float InwardX(float x){return Math.Max(-2.1f,Math.Min(2.1f,x));}
    public static float InwardZ(float z){return Math.Max(-5f,Math.Min(5f,z));}
    public static bool WalkedOff(float dx,float dy,float dz,float scale)
    {return (dx*dx+dz*dz)*scale*scale<.2f*.2f&&dy*scale<=.2f;}
    public static bool Arrived(float dx,float dy,float dz,float scale)
    {return (dx*dx+dz*dz)*scale*scale<.12f*.12f&&Math.Abs(dy)*scale<.18f;}
}
