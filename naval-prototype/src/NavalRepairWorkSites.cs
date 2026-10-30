using System;
// Shared immutable tables: callers keep the original deck/end/corridor/shift loop order.
public static class NavalRepairWorkSites
{
    private static readonly float[] shifts={0f,-.35f,.35f,-.7f,.7f,-1.2f,1.2f};
    private static readonly float[] corridors={2.35f,2.1f,1.65f,.95f};
    // The first steam work lane runs over the low wing tanks. It brings the
    // sailor close enough to reach the complete lower armour belt; routing
    // already climbs the tank ramps and Surface() places the feet on its lid.
    private static readonly float[] steamCorridors={1.55f,1.18f,1.05f,.90f};
    public static int ShiftCount {get{return shifts.Length;}}
    public static int CorridorCount {get{return corridors.Length;}}
    public static float Shift(int index){return shifts[index];}
    public static float Corridor(int index){return corridors[index];}
    public static float Corridor(NavalShipDefinition ship,int index)
    {return ship.SteamPowered?steamCorridors[index]:corridors[index];}
    public static float Floor(float preferred,float bottom,int deck){return deck==0?preferred:preferred>.3f?bottom:.825f;}
    public static bool EndWall(float z){return Math.Abs(z)>BoatDimensions.HalfLength-.3f;}
    public static float EndZ(NavalShipDefinition ship,float z)
    {return (z<0?-1f:1f)*(ship.SteamPowered?5.05f:5.5f);}
}
