using System;
public static class NavalCrewDeckSafety
{
    public static bool Inside(float x,float z,float halfWidth,float scale,float radius)
    {
        float margin=Math.Max(.05f,radius)/Math.Max(.1f,scale);
        return Math.Abs(z)<=BoatDimensions.HalfLength-margin&&Math.Abs(x)<=halfWidth-margin;
    }
    public static bool Near(float x,float z,float halfWidth,float scale,float radius,float slack=.45f)
    {
        float localSlack=Math.Max(0,slack)/Math.Max(.1f,scale);
        float margin=Math.Max(.05f,radius)/Math.Max(.1f,scale);
        return Math.Abs(z)<=BoatDimensions.HalfLength-margin+localSlack&&Math.Abs(x)<=halfWidth-margin+localSlack;
    }
    public static float StepHeight(float native){return Math.Min(native,.24f);}
}

// Fast damage-space test used before a CharacterController step. It keeps the
// normal route hot path free of raycasts and only asks the existing recovery
// planner for a detour when the sailor's footprint would overlap a missing
// horizontal hull cell.
public static class NavalDeckGapMath
{
    public static bool Overlaps(float px,float py,float pz,float radiusWorld,float scale,
        float cx,float cy,float cz,float sx,float sy,float sz)
    {
        if(sy>.35f||sx<.10f||sz<.10f)return false;
        float safeScale=Math.Max(.1f,scale),radius=Math.Max(.05f,radiusWorld)/safeScale;
        float top=cy+sy*.5f;
        if(Math.Abs(py-top)>Math.Max(.08f,.45f/safeScale))return false;
        float margin=radius*.72f;
        return Math.Abs(px-cx)<=sx*.5f+margin&&Math.Abs(pz-cz)<=sz*.5f+margin;
    }
    // Once a displaced capsule already overlaps a hole margin it must be
    // allowed to retreat toward a verified safe node.  Blocking both entry
    // and exit trapped it forever on the first damaged plank edge.
    public static bool BlocksStep(bool currentOverlap,bool proposedOverlap)
    {return proposedOverlap&&!currentOverlap;}
}
