using System;
// Shared immutable tables: callers keep the original deck/end/corridor/shift loop order.
public static class NavalRepairWorkSites
{
    private static readonly float[] shifts={0f,-.35f,.35f,-.7f,.7f,-1.2f,1.2f};
    private static readonly float[] corridors={2.35f,2.1f,1.65f,.95f};
    public static int ShiftCount {get{return shifts.Length;}}
    public static int CorridorCount {get{return corridors.Length;}}
    public static float Shift(int index){return shifts[index];}
    public static float Corridor(int index){return corridors[index];}
    public static float Floor(float preferred,float bottom,int deck){return deck==0?preferred:preferred>.3f?bottom:.825f;}
    public static bool EndWall(float z){return Math.Abs(z)>BoatDimensions.HalfLength-.3f;}
}
