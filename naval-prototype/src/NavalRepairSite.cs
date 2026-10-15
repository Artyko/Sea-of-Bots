using System;
public static class NavalRepairSite
{
    // Deck thickness occupies .575..825. An opening below it is reached from the hold.
    public static bool Upper(float holeY){return holeY>=0.575f;}
    public static float Floor(float holeY){return Upper(holeY)?0.825f:BoatDimensions.HoldFloor;}
    public static float WorkX(float holeX,float holeY)
    {float corridor=Upper(holeY)?1.4f:1.65f;return holeX<0f?-corridor:corridor;}
    // At a deck seam the centre is embedded in the floor: work on its exposed top edge.
    public static float AimY(float holeY){return Upper(holeY)?Math.Max(0.95f,holeY):holeY;}
    public static float AimY(float holeY,float standY)
    {return standY>0.3f?AimY(holeY):holeY;}
    public static float WorkZ(float holeZ){return Math.Max(-5.1f,Math.Min(5.1f,holeZ));}
    public static bool WithinReach(float dx,float dy,float dz)
    {return dx*dx+dy*dy+dz*dz<=4.8f*4.8f;}
}
