using UnityEngine;
using System.Collections.Generic;

public enum NavalEra
{
    Sail,
    Steam
}

public enum NavalPropulsionKind
{
    Sail,
    Steam
}

public interface INavalPropulsion
{
    NavalPropulsionKind Kind {get;}
    float PowerFactor {get;}
    float Command {get;}
    float Direction {get;}
    bool StationReady {get;}
    float UpdatePropulsion(float dt,float command,bool operatorPresent,Vector3 shipVelocity);
}

// Marker parsing is kept free of Unity types so malformed editor maps can be
// covered by the local test harness.
public static class NavalEraMarker
{
    public const string SailWater="NAVAL_OCEAN_SURFACE";
    public const string SteamWater="NAVAL_STEAM_OCEAN_SURFACE";

    public static bool IsWater(string name)
    {return name==SailWater||name==SteamWater;}

    public static bool TryResolve(IEnumerable<string> names,out NavalEra era,out string error)
    {
        int sail=0,steam=0;
        if(names!=null)foreach(string name in names)
        {
            if(name==SailWater)sail++;
            else if(name==SteamWater)steam++;
        }
        if(sail==1&&steam==0){era=NavalEra.Sail;error=null;return true;}
        if(steam==1&&sail==0){era=NavalEra.Steam;error=null;return true;}
        era=NavalEra.Sail;
        error=sail+steam==0
            ?"Load Naval Ocean or Steam Ocean: no naval water marker was found."
            :"The editor map must contain exactly one naval water marker, for either the sail or steam era.";
        return false;
    }
}
