using System;
class BreathClockChecks
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static void Main()
    {
        NavalBreathClock clock=new NavalBreathClock();
        Check(!clock.Step(-0.2f,60f)&&clock.Air==30f,"Head above water never loses air");
        Check(!clock.Step(0.2f,29f)&&clock.Air==1f,"Survives 29 seconds");
        Check(clock.Step(0.2f,1f)&&clock.Air==0f,"Drowns at 30 seconds");
        clock=new NavalBreathClock();clock.Step(0.2f,20f);
        clock.Step(0f,1f);Check(clock.Air==9f,"Surface jitter does not reset reserve");
        clock.Step(-0.2f,0.001f);Check(clock.Air==30f,"Air restores immediately in the first surfaced frame");
        clock.Step(-0.2f,100f);Check(clock.Air==30f,"Recovery capped at capacity");
        clock.Step(0.2f,0f);Check(clock.Air==30f,"Pause consumes no air");
        clock.Step(0.2f,1f);Check(clock.Air==29f,"Next dive starts with full reserve");
        NavalBreathClock a=new NavalBreathClock(),b=new NavalBreathClock();
        a.Step(0.2f,10f);for(int i=0;i<100;i++)b.Step(0.2f,0.1f);
        Check(Math.Abs(a.Air-b.Air)<0.001f,"Air drain independent of frame rate");
        Console.WriteLine("PASS: 30 second drowning, surface recovery, jitter and pause.");
    }
}
