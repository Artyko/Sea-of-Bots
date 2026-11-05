using System;
public static class NavalFireIgnition
{
    public const float CannonChance=.01f;
    public static bool FromCannon(float roll){return roll<CannonChance;}
    // An armour-stopped shell cannot light timber behind the plate. A
    // penetrating heavy shell is more dangerous, but no longer guarantees a
    // fire on every first main-battery salvo.
    public static float ImpactChance(float power,bool penetrated)
    {return !penetrated||power<.9f?0f:Math.Min(.08f,CannonChance+Math.Max(0f,power-1.5f)*.015f);}
    public static bool FromImpact(float power,bool penetrated,float roll)
    {return roll<ImpactChance(power,penetrated);}
}
public static class NavalPowderLayout
{
    public const float CellSize=.25f,LayerHeight=.225f,Height=1.8f,Diameter=1.25f,HitRadius=1.1f;
    public const int Layers=8;
    public static int Count(NavalShipDefinition ship){return ship.SteamPowered?0:ship.DeckCount>2?4:Math.Max(1,Math.Min(3,ship.GunCount/2));}
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
public static class NavalMagazineLayout
{
    public const float Width=2.4f,Height=2.0f,Depth=3.0f,HitRadius=1.35f;
    public static int Count(NavalShipDefinition ship){return ship.SteamPowered&&!ship.Destroyer?2:0;}
    // These are built-in rooms behind the starboard longitudinal bulkhead,
    // not loose steel chests placed among the shell racks.
    public static float X(int index){return 1.65f;}
    public static float Z(int index){return index%2==0?-1.35f:1.35f;}
    public static float Z(NavalShipDefinition ship,int index){return ship.CapitalShip?(index%2==0?-2.8f:2.8f):Z(index);}
    public static float Floor(NavalShipDefinition ship){return ship.MaintenanceFloor(0);}
}
public static class NavalReadyAmmoLayout
{
    public const float Width=.90f,Height=1.15f,Depth=.70f,HitRadius=.80f;
    public static int Count(NavalShipDefinition ship){return ship.SteamPowered?ship.GunCount:0;}
    public static float X(NavalShipDefinition ship,int gun)
    {return ship.Destroyer?ship.GunPosition(gun).x-1.25f/ship.Scale:NavalShipDefinition.GunSide(gun)*(ship.GunMountX+.05f);}
    public static float Z(NavalShipDefinition ship,int gun)
    {
        if(ship.Destroyer)return ship.GunPosition(gun).z-1.5f/ship.Scale;
        float gunZ=NavalShipDefinition.GunZ(ship,gun);
        float offset=gunZ>1f?-.58f:.58f;float z=gunZ+offset;
        return Math.Abs(z-ship.LadderZ)<.8f?gunZ-offset:z;
    }
    public static float Floor(NavalShipDefinition ship){return ship.WeatherDeck;}
}
public static class NavalMagazineSupport
{
    // A locker remains fixed while at least roughly one third of its footprint
    // is carried by the deck. An empty support sample means the layout could not
    // be evaluated and must fail safe instead of deleting the locker at spawn.
    public static bool Stable(int intact,int total)
    {return total<=0||intact*100>=total*35;}
}
public static class NavalFireResponseMath
{
    public static bool ReservesMaintenance(bool maintenanceDuty,bool handlingPropulsion)
    {return maintenanceDuty;}
    public static bool MayJoin(int responders,bool critical){return responders<(critical?2:1);}
    public static bool YieldHelm(bool urgent,bool responderAssigned,int freeHands)
    {return urgent&&!responderAssigned&&freeHands==1;}
    public static bool InterruptPump(bool urgent,bool criticalFlood)
    {return urgent&&!criticalFlood;}
    public static bool NeedsResponder(bool active,bool assigned,bool maintenanceReserved,bool urgent)
    {return active&&!assigned&&(urgent||!maintenanceReserved);}
}
public static class NavalFireProtection
{
    // The main-calibre hoist and magazine are a sealed steel citadel.  An
    // ordinary deck fire outside the trunk cannot cook its propellant through
    // the armour.  Direct projectile hits and blast Impact() calls remain able
    // to ignite or detonate it.
    public static bool AllowsNearbyHeat(bool mainMagazine){return !mainMagazine;}
}
public static class NavalAmmunitionFireAssignment
{
    public static bool OwnsReadyUse(int assignedStation,int readyGun)
    {return readyGun>=0&&assignedStation==readyGun+1;}
    public static bool ShouldHandoff(bool ownerAvailable,bool alreadyOwned)
    {return ownerAvailable&&!alreadyOwned;}
    public static bool KeepOwner(bool samePriorityTarget,bool claimedByOwner,bool active)
    {return samePriorityTarget&&claimedByOwner&&active;}
}
public static class NavalFireWorkDeck
{
    // A deck itself must be attacked from its upper face. Trying every deck
    // let a ray occasionally slip past a seam and made a sailor below the
    // weather deck claim a burning plank that the bucket could not reach.
    public static float ForTarget(float targetY,NavalShipDefinition ship)
    {
        if(ship.DeckCount<3)return targetY>=(ship.HoldFloor+ship.WeatherDeck)*.5f?ship.WeatherDeck:ship.HoldFloor;
        if(targetY>=(.825f+ship.WeatherDeck)*.5f)return ship.WeatherDeck;
        return targetY>=(ship.HoldFloor+.825f)*.5f?.825f:ship.HoldFloor;
    }
}
public sealed class NavalFireState
{
    public readonly float Lifetime;
    public float Burned {get;private set;}
    public float WetUntil {get;private set;}
    public float Intensity {get;private set;}
    public bool Burning {get;private set;}
    public NavalFireState(bool cloth){Lifetime=cloth?10f:26f;}
    public bool Ignite(float now)
    {return Ignite(now,.30f);}
    public bool Ignite(float now,float strength)
    {
        if(Burned>=Lifetime||now<WetUntil)return false;
        float next=Math.Max(.18f,Math.Min(1f,strength));
        if(Burning){Intensity=Math.Max(Intensity,next);return false;}
        Burning=true;Intensity=next;return true;
    }
    public bool Step(float dt,bool underwater,float now)
    {
        if(underwater){Douse(now);return false;}
        if(!Burning)return false;
        dt=Math.Max(0,dt);Intensity=Math.Min(1f,Intensity+dt*(Lifetime<=10f?.085f:.045f));
        Burned=Math.Min(Lifetime,Burned+dt*(.62f+Intensity*.70f));
        if(Burned<Lifetime)return false;Burning=false;return true;
    }
    // A bucket immediately kills a small flame, while an established fire
    // needs a second pass. Full immersion and deck wash still call Douse().
    public bool Cool(float now,float water)
    {
        if(!Burning)return false;
        Intensity=Math.Max(0f,Intensity-Math.Max(0f,water)*.34f);
        if(Intensity>.10f)return true;
        Douse(now);return false;
    }
    public float SpreadChance {get{return Burning?Math.Min(.08f,.012f+Intensity*.07f):0f;}}
    public void Douse(float now){Burning=false;Intensity=0f;WetUntil=now+18f;}
}
public interface INavalExplosiveStore
{
    bool Spent {get;}
    bool Wet {get;}
    float Fuse {get;}
    bool Lit {get;}
    bool Ignite(float roll);
    bool Impact(float roll);
    bool Step(float dt,bool nearFire,bool underwater);
    void Douse();
    void Reset();
}
public static class NavalExplosiveImpact
{
    // A projectile physically striking the ready-use locker or a penetrated
    // magazine always leaves it burning unless it detonates immediately. This
    // is distinct from a nearby blast, which retains the probabilistic rules.
    public static bool DirectHit(INavalExplosiveStore store,float roll)
    {
        if(store==null||store.Spent||store.Wet)return false;
        if(store is NavalTorpedoWarheadState)return store.Impact(roll);
        float instant=store is NavalPowderState?NavalPowderState.InstantChance:NavalMagazineState.InstantChance;
        if(roll<instant)return store.Impact(0f);
        store.Ignite(0f);return false;
    }
}
public sealed class NavalPowderState:INavalExplosiveStore
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
// Early ironclads still carried propellant, but in enclosed magazines and
// charge cases rather than loose weather-deck barrels.  The closed locker is
// harder to ignite and cooks off more slowly; a penetrating hit can still
// start a delayed magazine fire or, rarely, detonate it immediately.
public sealed class NavalMagazineState:INavalExplosiveStore
{
    public const float ImpactChance=.24f,InstantChance=.04f,FuseSeconds=18f,HeatSeconds=4f;
    public bool Spent {get;private set;}
    public bool Wet {get;private set;}
    public float Fuse {get;private set;}
    private float heat;
    public bool Lit {get{return !Spent&&!Wet&&Fuse>0;}}
    public bool Ignite(float roll)
    {if(Spent||Wet||Lit||roll>=ImpactChance)return false;Fuse=FuseSeconds;return true;}
    public bool Impact(float roll)
    {
        if(Spent||Wet)return false;
        if(roll<InstantChance){Spent=true;Fuse=0;return true;}
        if(roll<InstantChance+ImpactChance&&!Lit)Fuse=FuseSeconds;
        return false;
    }
    public bool Step(float dt,bool nearFire,bool underwater)
    {
        dt=Math.Max(0,dt);if(underwater)Douse();if(Spent||Wet)return false;
        if(!Lit){heat=Math.Max(0,heat+(nearFire?dt:-dt*.5f));if(heat>=HeatSeconds)Fuse=FuseSeconds;return false;}
        Fuse=Math.Max(0,Fuse-dt);if(Fuse>0)return false;Spent=true;return true;
    }
    public void Douse(){Wet=true;Fuse=0;heat=0;}
    public void Reset(){Spent=Wet=false;Fuse=heat=0;}
}
