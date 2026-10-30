using System;
// Convex end taper shared by shell geometry, water clipping and active flood cells.
public static class NavalHullOutline
{
    public static float HalfWidth(float z,bool tapered,bool ironclad=false)
    {
        if(!tapered)return BoatDimensions.HalfWidth;
        if(ironclad)
        {
            // The steam corvette is appreciably finer than the sailing hulls:
            // a long entrance forward, a rounded run aft and no rectangular
            // 24 m beam carried all the way to either end.
            const float beam=2.35f;
            float start=z>=0?3.30f:4.70f,span=z>=0?2.70f:1.30f;
            float u=Math.Max(0,Math.Min(1,(Math.Abs(z)-start)/span));
            float smooth=u*u*(3-2*u);
            return beam-smooth*(z>=0?2.00f:.85f);
        }
        float t=Math.Max(0,Math.Min(1,(Math.Abs(z)-5.2f)/.8f));
        return BoatDimensions.HalfWidth-t*(z>=0?1.5f:.75f);
    }
    public static float EndPlane(float x,float z,int plane)
    {
        if(plane<2)return (plane==0?x:-x)+1.875f*(z-5.2f)-3;
        return (plane==2?x:-x)+.9375f*(-z-5.2f)-3;
    }
}
