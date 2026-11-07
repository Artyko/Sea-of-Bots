using System;
// Translation follows elapsed game time; buoyancy and AI retain their bounded integration step.
public static class NavalMotionClock
{
    public static float Elapsed(float frameSeconds){return Math.Max(0f,Math.Min(frameSeconds,.2f));}
}
