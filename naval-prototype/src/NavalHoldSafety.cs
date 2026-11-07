using System;
// Shared survival decision: deep water alone does not cancel damage control.
public static class NavalHoldSafety
{
    public static float ExitSeconds(float x,float y,float z,float floor,float scale,float speed,bool stairs)
    {
        float travel;
        if(stairs)travel=Math.Max(0f,.825f-y)+Math.Max(0f,.8f-z)+2.5f;
        else travel=Math.Abs(Math.Abs(x)-1.65f)+Math.Abs(z+3.65f)+1.65f+(float)Math.Sqrt(4.45f*4.45f+(.825f-floor)*(.825f-floor))+2.5f;
        return travel*Math.Max(.1f,scale)/Math.Max(1f,speed*.8f);
    }
    public static float DestroyerExitSeconds(float x, float z, float scale, float rise, float speed)
    {
        float longitudinal = Math.Abs(Math.Abs(z * scale) - 16.5f);
        float lateral = Math.Abs(Math.Abs(x * scale) - 5.6f) + 2f;
        float flight = (float)Math.Sqrt(64f + rise * rise);
        return (longitudinal + lateral + flight + 2f) / Math.Max(1f, speed * .8f);
    }

    public static float AirReserve(float exitSeconds)
    {return Math.Max(8f,Math.Min(22f,exitSeconds+4f));}
    public const float EscapeAirSeconds=22f;
    public static bool MustEscape(bool headUnder,bool sinking,float air,float exitSeconds)
    {return headUnder&&air<=EscapeAirSeconds;}
    public static bool CanFinishPatch(float air,float exitSeconds,float remaining)
    {
        // Spend only the contingency buffer once hands are already on the patch.
        return remaining>0f&&remaining<=4f&&air>Math.Max(8f,AirReserve(exitSeconds)-4f)+remaining;
    }
    public static bool CanResume(bool headUnder,bool sinking,float air,float drySeconds,bool stableFloor)
    {return !headUnder&&!sinking&&air>=NavalBreathClock.Capacity-1f&&drySeconds>=.75f&&stableFloor;}
}
