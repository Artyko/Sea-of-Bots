using System;
class ProjectileWaterChecks
{
    static void Check(bool ok,string name){if(!ok)throw new Exception(name);}
    static float Travel(float dt)
    {
        float speed=32f*NavalProjectileWaterMath.EntrySpeedScale,travel=0f;
        for(int i=0;i<10000&&speed>=0.6f;i++)
        {speed*=NavalProjectileWaterMath.Damping(dt);travel+=speed*dt;}
        return travel;
    }
    static void Main()
    {
        Check(NavalProjectileWaterMath.DamagePower==0.5f,"Half underwater impact strength");
        float radius=NavalProjectileWaterMath.DamageRadiusScale;
        Check(Math.Abs(radius*radius-0.5f)<0.00001f,"Half hole area in thin shell");
        Check(NavalProjectileWaterMath.Damping(0f)==1f,"Pause preserves speed");
        Check(NavalProjectileWaterMath.Damping(0.1f)<1f,"Drag dissipates velocity");
        float travel=Travel(0.008f);
        Check(travel>1f&&travel<2f,"Typical shot travels one to two metres underwater");
        Check(Math.Abs(travel-Travel(0.004f))<0.04f,"Substep convergence");
        Check(NavalProjectileWaterMath.MaxTravel==2f,"Two metre path limit");
        Console.WriteLine("PASS: underwater attenuation, drag and typical travel "+travel.ToString("F2")+" m.");
    }
}
