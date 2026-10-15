using System;
public static class NavalLeakPriority
{
    public static float Score(float flow,float lastWet,float wetFlow,float now)
    {
        if(flow>0.00001f)return 1f+flow;
        return now-lastWet<=45f?0.1f+Math.Min(0.8f,wetFlow*0.25f):0f;
    }
}
