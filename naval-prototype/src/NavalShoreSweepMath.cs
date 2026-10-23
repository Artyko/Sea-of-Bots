using System;
public static class NavalShoreSweepMath
{
    // Limit motion of the furthest hull corner to 10 cm between tests.
    public static int Steps(float distance,float angle,float radius)
    {return Math.Max(1,(int)Math.Ceiling((Math.Abs(distance)+Math.Abs(angle)*Math.PI/180.0*Math.Max(0,radius))/.1));}
}
