using System;
public static class NavalTargetVisibility
{
    // Within 45 degrees of either beam; bow/stern approaches cannot
    // deliberately target deck mechanisms through the hull.
    public static bool Broadside(float localX,float localZ)
    {return Math.Abs(localX)>.01f&&Math.Abs(localX)>=Math.Abs(localZ);}
}
