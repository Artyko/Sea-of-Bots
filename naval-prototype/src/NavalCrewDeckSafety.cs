using System;
public static class NavalCrewDeckSafety
{
    public static bool Inside(float x,float z,float halfWidth,float scale,float radius)
    {
        float margin=Math.Max(.05f,radius)/Math.Max(.1f,scale);
        return Math.Abs(z)<=BoatDimensions.HalfLength-margin&&Math.Abs(x)<=halfWidth-margin;
    }
    public static float StepHeight(float native){return Math.Min(native,.24f);}
}
