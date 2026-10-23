using System;
public static class NavalFireIgnition
{
    public const float CannonChance=.02f;
    public static bool FromCannon(float roll){return roll<CannonChance;}
}
public static class NavalPowderLayout
{
    public const float CellSize=.25f,LayerHeight=.225f,Height=1.8f,Diameter=1.25f,HitRadius=1.1f;
    public const int Layers=8;
    public static int Count(NavalShipDefinition ship){return ship.DeckCount>2?4:Math.Max(1,Math.Min(3,ship.GunCount/2));}
    public static float X(NavalShipDefinition ship,int index)
    {
        if(ship.GunCount<4)return -.8f;
        float side=index%2==0?-1f:1f;
        // Keep the forward galleon barrel inboard of the boarding ladder landing.
        return side*(ship.DeckCount>2&&index>=2?1.85f-.2f/ship.Scale:3f-(Diameter*.5f+1.15f)/ship.Scale);
    }
    public static float Z(NavalShipDefinition ship,int index)
    {
        if(ship.GunCount<4)return 1.8f;
        int first=ship.GunCount>4&&index>=2?2:0;
        return (NavalShipDefinition.GunZ(ship,first)+NavalShipDefinition.GunZ(ship,first+2))*.5f;
    }
    public static float Floor(NavalShipDefinition ship){return ship.WeatherDeck;}
    public static bool Cell(int x,int y,int z)
    {
        float radius=y==0||y==Layers-1?.375f:y==1||y==Layers-2?.5f:.5625f;
        return (x*x+z*z)*CellSize*CellSize<=radius*radius+.00001f;
    }
}
public static class NavalFireResponseMath
{
    public static bool MayJoin(int responders,bool critical){return responders<(critical?2:1);}
    public static bool YieldHelm(bool urgent,bool responderAssigned,int freeHands)
    {return urgent&&!responderAssigned&&freeHands==1;}
    public static bool InterruptPump(bool urgent,bool criticalFlood)
    {return urgent&&!criticalFlood;}
    public static bool NeedsResponder(bool active,bool assigned,bool maintenanceReserved,bool urgent)
    {return active&&!assigned&&(urgent||!maintenanceReserved);}
}
public sealed class NavalFireState
{
    public readonly float Lifetime;
    public float Burned {get;private set;}
    public float WetUntil {get;private set;}
    public bool Burning {get;private set;}
    public NavalFireState(bool cloth){Lifetime=cloth?10f:26f;}
    public bool Ignite(float now)
    {if(Burning||Burned>=Lifetime||now<WetUntil)return false;Burning=true;return true;}
    public bool Step(float dt,bool underwater,float now)
    {
        if(underwater){Douse(now);return false;}
        if(!Burning)return false;
        Burned=Math.Min(Lifetime,Burned+Math.Max(0,dt));
        if(Burned<Lifetime)return false;Burning=false;return true;
    }
    public void Douse(float now){Burning=false;WetUntil=now+18f;}
}
public sealed class NavalPowderState
{
    public const float ImpactChance=.4f,InstantChance=.1f,FuseSeconds=12f;
    public bool Spent {get;private set;}
    public bool Wet {get;private set;}
    public float Fuse {get;private set;}
    private float heat;
    public bool Lit {get{return !Spent&&!Wet&&Fuse>0;}}
    public bool Ignite(float roll)
    {if(Spent||Wet||Lit||roll>=ImpactChance)return false;Fuse=FuseSeconds;return true;}
    // 10% immediate detonation, 40% fuse, 50% no ignition. Blast is dispatched outside hull damage.
    public bool Impact(float roll)
    {
        if(Spent||Wet)return false;
        if(roll<InstantChance){Spent=true;Fuse=0;return true;}
        if(roll<InstantChance+ImpactChance&&!Lit)Fuse=FuseSeconds;
        return false;
    }
    public bool Step(float dt,bool nearFire,bool underwater)
    {
        dt=Math.Max(0,dt);if(underwater)Douse();
        if(Spent||Wet)return false;
        if(!Lit){heat=Math.Max(0,heat+(nearFire?dt:-dt));if(heat>=2f)Fuse=FuseSeconds;return false;}
        Fuse=Math.Max(0,Fuse-dt);if(Fuse>0)return false;Spent=true;return true;
    }
    public void Douse(){Wet=true;Fuse=0;heat=0;}
    public void Reset(){Spent=Wet=false;Fuse=heat=0;}
}
