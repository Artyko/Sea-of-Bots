using System;
public static class NavalHullAimZones
{
    public static bool Facing(float viewX,float viewZ,float x,float z,float halfWidth)
    {
        // End-on views target the exposed transom/bow; beam views target the facing side.
        return Math.Abs(viewZ)>Math.Abs(viewX)?z*(viewZ<0?-1:1)>5.25f:
            x*(viewX<0?-1:1)>=halfWidth-.35f;
    }
}
