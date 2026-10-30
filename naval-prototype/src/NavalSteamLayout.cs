using UnityEngine;

// Static layout shared by geometry, crew routing and local validation. The
// The engine-order telegraph is integrated into the protected helm position,
// so one sailor controls both rudder and machinery without changing stations.
public static class NavalSteamLayout
{
    public const float OperatorX=0f;
    public const float HelmWheelForward=.42f;
    public const float ConsoleWorldOffsetX=-1.25f;
    public const float ConsoleWorldHeight=1.08f;
    public const float MinimumOperatorClearance=1.20f;
    public const float BoilerX=-1.82f,BoilerZ=1.35f;
    public const float EngineX=-1.82f,EngineZ=-1.35f;
    // The funnel and its uptake sit directly above the port boiler room.
    public const float FunnelX=BoilerX;
    public const float FunnelZ=BoilerZ;
    public static Vector3 OperatorPost(NavalShipDefinition ship)
    {return new Vector3(OperatorX,NavalConningTowerLayout.PlatformY(ship),NavalConningTowerLayout.CenterZ);}
    public static Vector3 ControlOffset(float scale)
    {return new Vector3(ConsoleWorldOffsetX/scale,ConsoleWorldHeight/scale,0f);}
    public static Vector3 PedestalOffset(float scale)
    {return new Vector3(ConsoleWorldOffsetX/scale,.46f/scale,0f);}
}
