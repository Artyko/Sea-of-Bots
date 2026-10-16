using System;
public static class NavalHoldEscapeRules
{
    public static bool InHold(float x,float y,float z)
    {
        return Math.Abs(x)<BoatDimensions.HalfWidth-0.1f&&Math.Abs(z)<BoatDimensions.HalfLength-0.1f&&
            y>=BoatDimensions.HoldFloor-0.3f&&y<0.6f;
    }
}
