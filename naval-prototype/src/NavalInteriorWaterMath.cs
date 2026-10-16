using System;
public static class NavalInteriorWaterMath
{
    // Intersect the local flood surface along world up, rather than along
    // the tilted ship's up axis. Returns a world Y for the querying point.
    public static float WorldLevel(float worldY,float localY,float surfaceY,float worldUpLocalY)
    {
        if(worldUpLocalY<=0.0001f)return float.NegativeInfinity;
        return worldY+(surfaceY-localY)/worldUpLocalY;
    }
}
