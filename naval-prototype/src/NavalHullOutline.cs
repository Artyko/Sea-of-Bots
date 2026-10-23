using System;
// Convex end taper shared by shell geometry, water clipping and active flood cells.
public static class NavalHullOutline
{
    public static float HalfWidth(float z,bool tapered)
    {
        if(!tapered)return BoatDimensions.HalfWidth;
        float t=Math.Max(0,Math.Min(1,(Math.Abs(z)-5.2f)/.8f));
        return BoatDimensions.HalfWidth-t*(z>=0?1.5f:.75f);
    }
    public static float EndPlane(float x,float z,int plane)
    {
        if(plane<2)return (plane==0?x:-x)+1.875f*(z-5.2f)-3;
        return (plane==2?x:-x)+.9375f*(-z-5.2f)-3;
    }
}
