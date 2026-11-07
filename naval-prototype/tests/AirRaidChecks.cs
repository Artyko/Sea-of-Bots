using System;
class AirRaidChecks
{
    static void Main()
    {
        if(NavalAirRaidMath.SupportDelay(0)!=60f||NavalAirRaidMath.SupportDelay(1)!=240f||NavalAirRaidMath.SupportDelay(.5f)!=150f)throw new Exception("Hidden random support interval");
        for(int wave=1;wave<=3;wave++)if((NavalAirRaidMath.Approach(wave,true)-NavalAirRaidMath.Approach(1,true)).sqrMagnitude>.0001f)throw new Exception("Support bearing must remain fixed");
        if(NavalAirRaidMath.SupportDelay(1)>NavalAirRaidMath.SupportDeadline||NavalAirRaidMath.SupportDeadline!=600f)throw new Exception("Ten-minute deadline");
        int aircraft=0;
        for(int wave=0;wave<NavalAirRaidMath.Waves;wave++)aircraft+=NavalAirRaidMath.AircraftInWave(wave);
        if(aircraft!=17||aircraft*NavalAirRaidMath.BombsPerAircraft!=68||
            NavalAirRaidMath.AircraftInWave(3)!=0)throw new Exception("Unexpected wave composition");
        for(int height=12;height<96;height++)
        for(int speed=-35;speed<=-3;speed++)
        {
            float t=NavalAirRaidMath.FallTime(height,speed);
            if(t<=0f||Math.Abs(height+speed*t-.5f*9.81f*t*t)>.001f)
                throw new Exception("Bomb release does not reach the aiming plane");
        }
        Console.WriteLine("PASS: three waves, seventeen aircraft, 68 bombs and 2772 release solutions.");
    }
}
