using System;
// SI metres and seconds: independent of ship size, shared by all compartment layouts.
public static class NavalFloodResponseMath
{
    public const float FitSeconds=3f,RemoveSeconds=1.5f,ReopenDelay=15f;
    public static bool Isolate(float depth,float corridorDepth,float inflow)
    {return depth>=.3f&&depth-corridorDepth>.15f&&(inflow>.02f||depth>=.5f);}
    public static bool SafeToRemove(float depth,float corridorDepth,float inflow)
    {return depth<.12f&&Math.Abs(depth-corridorDepth)<.12f&&inflow<=.001f;}
    public static bool Crosses(float x,float z,float targetX,float targetZ,float wallX,float openingZ)
    {
        if((x-wallX)*(targetX-wallX)>0f||Math.Abs(targetX-x)<.0001f)return false;
        float t=(wallX-x)/(targetX-x);
        return t>=0f&&t<=1f&&Math.Abs(z+(targetZ-z)*t-openingZ)<.85f;
    }
}
