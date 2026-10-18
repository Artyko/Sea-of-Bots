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
    public static float AirReserve(float exitSeconds)
    {return Math.Max(8f,Math.Min(22f,exitSeconds+4f));}
    public static bool MustEscape(bool headUnder,bool sinking,float air,float exitSeconds)
    {return sinking||headUnder&&air<=AirReserve(exitSeconds);}
    public static bool CanResume(bool headUnder,bool sinking,float air,float drySeconds,bool stableFloor)
    {return !headUnder&&!sinking&&air>=NavalBreathClock.Capacity-1f&&drySeconds>=.75f&&stableFloor;}
}
