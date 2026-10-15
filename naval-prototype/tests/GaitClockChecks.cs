using System;
class GaitClockChecks
{
    static void Check(bool b,string name){if(!b)throw new Exception(name);}
    static void Main()
    {
        NavalGaitClock clock=new NavalGaitClock();clock.Advance(0f,2f);Check(clock.Phase==0f,"Standing and ship transport do not step");
        clock.Advance(0.5f,2f);Check(Math.Abs(clock.Phase-0.25f)<0.00001f,"Step phase follows travelled distance");
        clock.Advance(1.5f,2f);Check(Math.Abs(clock.Phase)<0.00001f,"Full stride loops smoothly");
        for(int i=0;i<100;i++)clock.Advance(0.02f,2f);Check(Math.Abs(clock.Phase-1f)<0.00001f||Math.Abs(clock.Phase)<0.00001f,"Distance phase independent of frame subdivisions");
        float saved=clock.Phase;clock.Advance(-1f,2f);Check(clock.Phase==saved,"Invalid distance does not run animation backwards");
        Console.WriteLine("PASS: movement-driven gait, idle, loop and frame subdivision.");
    }
}
