using System;
public static class NavalIslandMath
{
    public static float Height(float x,float z,float radius,int seed)
    {
        double angle=Math.Atan2(z,x);
        double coast=radius*(1+0.10*Math.Sin(angle*3+seed)+0.06*Math.Cos(angle*5-seed));
        double t=Math.Sqrt(x*x+z*z)/coast;
        if(t>1.25)return -35f;
        double h=(1-t)*22-1.4+0.8*Math.Sin(x*0.18+seed)*Math.Cos(z*0.17-seed);
        return (float)Math.Floor(h/1.2)*1.2f;
    }
}
