using System;
public static class NavalHoldEscapeRules
{
    public static bool InHold(float x,float y,float z,float holdFloor=BoatDimensions.HoldFloor,float weatherDeck=.825f)
    {
        return Math.Abs(x)<BoatDimensions.HalfWidth-0.1f&&Math.Abs(z)<BoatDimensions.HalfLength-0.1f&&
            y>=holdFloor-0.3f&&y<(weatherDeck==.825f?.6f:weatherDeck-.225f);
    }
}
