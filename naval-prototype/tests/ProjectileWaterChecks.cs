using System;
class ProjectileWaterChecks
{
    static void Check(bool ok,string name){if(!ok)throw new Exception(name);}
    static float Travel(float dt,float speed=32f,float power=1f)
    {
        speed*=NavalProjectileWaterMath.EntrySpeedScale;float travel=0f,limit=NavalProjectileWaterMath.TravelLimit(power);
        for(int i=0;i<10000&&speed>=0.6f&&travel<limit;i++)
        {speed*=NavalProjectileWaterMath.Damping(dt,power);travel+=Math.Min(speed*dt,limit-travel);}
        return travel;
    }
    static void Main()
    {
        Check(!NavalProjectileWaterMath.EntersWater(float.PositiveInfinity,float.PositiveInfinity),"Dry hold below ocean stays air");
        Check(!NavalProjectileWaterMath.EntersWater(2f,.1f),"Shot above local flood surface stays air");
        Check(NavalProjectileWaterMath.EntersWater(.1f,-.1f),"Entering actual compartment water attenuates shot");
        Check(NavalProjectileWaterMath.EntersWater(float.PositiveInfinity,-.1f),"Leaving hull through hole into sea detects water");
        Check(NavalProjectileWaterMath.EntersWater(-.1f,-.2f),"Shot initially in water is detected");
        Check(NavalProjectileWaterMath.DamagePower==0.5f,"Half underwater impact strength");
        Check(NavalProjectileWaterMath.ImpactPowerScale(1f)==NavalProjectileWaterMath.DamagePower,"Sail-era round shot keeps original underwater damage");
        Check(NavalProjectileWaterMath.ImpactPowerScale(1.2f)==NavalProjectileWaterMath.RifledDamagePower&&NavalProjectileWaterMath.RifledDamagePower>.7f,"Rifled shell can eventually open the deliberately targeted submerged armour belt");
        float radius=NavalProjectileWaterMath.DamageRadiusScale;
        Check(Math.Abs(radius*radius-0.5f)<0.00001f,"Half hole area in thin shell");
        Check(NavalProjectileWaterMath.ImpactRadiusScale(1.2f)>radius&&NavalProjectileWaterMath.ImpactRadiusScale(1.2f)<1f,"Rifled underwater patch stays attenuated but is not vanishingly small");
        Check(NavalProjectileWaterMath.Damping(0f)==1f,"Pause preserves speed");
        Check(NavalProjectileWaterMath.Damping(0.1f)<1f,"Drag dissipates velocity");
        float travel=Travel(0.008f);
        Check(travel>2f&&travel<3.1f,"Round shot gains a longer but still compact underwater path");
        Check(Math.Abs(travel-Travel(0.004f))<0.04f,"Substep convergence");
        Check(NavalProjectileWaterMath.MaxTravel==3f,"Three metre round-shot path limit");
        Check(NavalProjectileWaterMath.TravelLimit(1f)==NavalProjectileWaterMath.MaxTravel,"Round shot retains its old underwater range");
        Check(NavalProjectileWaterMath.TravelLimit(1.2f)==NavalProjectileWaterMath.RifledTravel&&NavalProjectileWaterMath.RifledTravel>=6f,"Rifled shell can reach the first inner spaces behind the submerged belt");
        Check(NavalProjectileWaterMath.TravelLimit(5.2f)==NavalProjectileWaterMath.HeavyRifledTravel&&NavalProjectileWaterMath.HeavyRifledTravel==10f,"Main-calibre shell has a distinct deep underwater path");
        Check(Travel(.008f,55f,1.55f)>6f,"Casemate shell reaches its expanded range under drag");
        Check(Travel(.008f,105f,5.2f)>9.5f,"Main-calibre shell reaches its expanded range under drag");
        Check(NavalProjectileWaterMath.Damping(.1f,1.2f)>NavalProjectileWaterMath.Damping(.1f,1f),"Modern pointed shell loses speed more slowly underwater");
        Console.WriteLine("PASS: underwater attenuation, drag and typical travel "+travel.ToString("F2")+" m.");
    }
}
