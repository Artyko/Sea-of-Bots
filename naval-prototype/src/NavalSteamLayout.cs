using UnityEngine;

// Static layout shared by geometry, crew routing and local validation. The
// The engine-order telegraph is integrated into the protected helm position,
// so one sailor controls both rudder and machinery without changing stations.
public static class NavalSteamLayout
{
    public const float OperatorX=0f;
    public const float HelmWheelForward=.42f;
    public static float WheelForward(NavalShipDefinition ship){return ship.Destroyer?1.4f/ship.Scale:ship.CapitalShip?1.68f/ship.Scale:HelmWheelForward;}
    public static float WheelHeight(NavalShipDefinition ship){return 1.36f/ship.Scale;}
    public const float ConsoleWorldOffsetX=-1.25f;
    public const float ConsoleWorldHeight=1.08f;
    public const float MinimumOperatorClearance=1.20f;
    public const float BoilerX=-1.45f,BoilerZ=1.35f;
    public static float BoilerZFor(NavalShipDefinition ship){return ship.CapitalShip?.1f:BoilerZ;}
    public static float MainHeight(NavalShipDefinition ship){return 3.6f/ship.Scale;}
    public const float EngineX=-1.45f,EngineZ=-1.35f;
    public static float ScrewY(NavalShipDefinition ship){return System.Math.Min(-.72f,ship.HoldFloor-.25f); }
    // The funnel and its uptake sit directly above the port boiler room.
    public const float FunnelX=-1.82f;
    public const float FunnelZ=BoilerZ;
    public static Vector3 OperatorPost(NavalShipDefinition ship)
    {return new Vector3(OperatorX,ship.HelmDeck,NavalShipDefinition.HelmZ(ship));}
    public static Vector3 ControlOffset(float scale)
    {return new Vector3(ConsoleWorldOffsetX/scale,ConsoleWorldHeight/scale,0f);}
    public static Vector3 PedestalOffset(float scale)
    {return new Vector3(ConsoleWorldOffsetX/scale,.46f/scale,0f);}
    public static bool BlocksPassage(NavalWalkPoint a,NavalWalkPoint b,NavalShipDefinition ship)
    {
        if(ship.Destroyer)return !NavalDestroyerNavigation.Clear(a,b,ship);
        if(!ship.SteamPowered||System.Math.Abs(a.Y-b.Y)>.08f)return false;
        const float margin=.23f;float boilerZ=BoilerZFor(ship);
        if(System.Math.Abs(a.Y-ship.HoldFloor)<.08f){
            if(NavalSupplyRoutes.Hits(a,b,BoilerX-.48f-margin,BoilerX+.62f+margin,boilerZ-.90f-margin,boilerZ+.90f+margin))return true;
            if(NavalSupplyRoutes.Hits(a,b,EngineX-.33f-margin,EngineX+.41f+margin,EngineZ-.60f-margin,EngineZ+.60f+margin))return true;
            for(int i=0;i<2;i++){
                float z=NavalMagazineLayout.Z(ship,i);
                if(NavalSupplyRoutes.Hits(a,b,NavalCompartmentMath.WallX-margin,NavalCompartmentMath.WallX+NavalMagazineLayout.Width/ship.Scale+margin,z-NavalMagazineLayout.Depth/(2*ship.Scale)-margin,z+NavalMagazineLayout.Depth/(2*ship.Scale)+margin))return true;
            }
        }
        if(!ship.CapitalShip&&System.Math.Abs(a.Y-.825f)<.08f&&NavalSupplyRoutes.Hits(a,b,BoilerX-.10f-margin,BoilerX+.10f+margin,BoilerZ-.10f-margin,BoilerZ+.10f+margin))return true;
        if(ship.CapitalShip&&System.Math.Abs(a.Y-.825f)<.08f)foreach(float z in new[]{BoilerZFor(ship)-.4f,BoilerZFor(ship)+.4f})
            if(NavalSupplyRoutes.Hits(a,b,BoilerX-.09f-margin,BoilerX+.09f+margin,z-.09f-margin,z+.09f+margin))return true;
        if(ship.CapitalShip&&System.Math.Abs(a.Y-.825f)<.08f)foreach(float z in new[]{.4f,-2f})
            if(NavalSupplyRoutes.Hits(a,b,-1.06f-margin,-.84f+margin,z-.11f-margin,z+.11f+margin))return true;
        return false;
    }
}
