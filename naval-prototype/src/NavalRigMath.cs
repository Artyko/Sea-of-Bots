using System;

public static class NavalRigMath
{
    // Bounds in the original ship coordinates. Surviving cells must have a
    // continuous path to a cell embedded in the deck, not merely exist above it.
    public static bool[] Supported(float[,] bounds, bool[] missing, float deckHeight)
    {
        int count = missing.Length;
        bool[] reached = new bool[count];
        int[] queue = new int[count];
        int head = 0, tail = 0;
        for (int i = 0; i < count; i++)
            if (!missing[i] && bounds[i, 1] - bounds[i, 4] <= deckHeight)
            { reached[i] = true; queue[tail++] = i; }
        while (head < tail)
        {
            int a = queue[head++];
            for (int b = 0; b < count; b++)
            {
                if (missing[b] || reached[b]) continue;
                bool touching = true;
                for (int axis = 0; axis < 3; axis++)
                    if (Math.Abs(bounds[a, axis] - bounds[b, axis]) > bounds[a, axis + 3] + bounds[b, axis + 3] + 0.001f)
                    { touching = false; break; }
                if (touching) { reached[b] = true; queue[tail++] = b; }
            }
        }
        return reached;
    }
    public static float SailPower(int remaining, int total, bool attached)
    { return attached && total > 0 ? Math.Max(0f, Math.Min(1f, (float)remaining / total)) : 0f; }
    public static float RudderRate(float speed, float rudder)
    { return Math.Abs(speed) < 0.05f ? 0f : rudder * 3f * speed; }
    public static float StepRudder(float current,float target,float dt)
    {
        target=Math.Max(-1f,Math.Min(1f,target));
        return target+(current-target)*(float)Math.Exp(-2f*Math.Max(0f,dt));
    }
    public static float StepSpeed(float speed, float throttle, float sailPower, float dt,float maxSpeed=4f)
    {
        float target = throttle * maxSpeed * sailPower;
        // Releasing the helm takes longer to stop than active reverse thrust.
        bool coasting = Math.Abs(target) < 0.001f;
        float response = coasting ? 0.14f : (speed * target < 0f ? 0.42f : 0.30f);
        float next = target + (speed - target) * (float)Math.Exp(-response * Math.Max(0f, dt));
        return coasting && Math.Abs(next) < 0.02f ? 0f : next;
    }
    public static float BreakHeight(float[,] bounds, bool[] missing, bool[] supported)
    {
        float upper = float.MaxValue, lower = float.MinValue;
        for (int i=0;i<missing.Length;i++)
            if (!missing[i] && !supported[i]) upper = Math.Min(upper,bounds[i,1]);
        if (upper == float.MaxValue) return upper;
        for (int i=0;i<missing.Length;i++)
            if (!missing[i] && supported[i] && bounds[i,1] < upper) lower = Math.Max(lower,bounds[i,1]);
        return lower == float.MinValue ? float.MinValue : (upper+lower)*0.5f;
    }
}
