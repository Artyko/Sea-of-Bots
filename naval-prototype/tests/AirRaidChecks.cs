using System;
class AirRaidChecks
{
    static void Main()
    {
        int aircraft=0;
        for(int wave=0;wave<NavalAirRaidMath.Waves;wave++)aircraft+=NavalAirRaidMath.AircraftInWave(wave);
        if(aircraft!=8||aircraft*NavalAirRaidMath.BombsPerAircraft!=32||
            NavalAirRaidMath.AircraftInWave(3)!=0)throw new Exception("Unexpected wave composition");
        for(int height=12;height<96;height++)
        for(int speed=-35;speed<=-3;speed++)
        {
            float t=NavalAirRaidMath.FallTime(height,speed);
            if(t<=0f||Math.Abs(height+speed*t-.5f*9.81f*t*t)>.001f)
                throw new Exception("Bomb release does not reach the aiming plane");
        }
        Console.WriteLine("PASS: three waves, eight aircraft, 32 bombs and 2772 release solutions.");
    }
}
