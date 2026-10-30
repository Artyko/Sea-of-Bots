using System;
public static class NavalFireWaterSupply
{
    // Intakes sit in the gaps between the three broadside mounts. The former
    // points at z +/-3 were centred on guns, while the bow point is now inside
    // the main-battery footprint.
    public static readonly float[] IntakeZ={-4.35f,-1.60f,1.55f};
    public static float SeaX(NavalShipDefinition ship,float z)
    {return Math.Max(.35f,Math.Min(2.05f,ship.HullHalfWidth(z)-.35f));}
    // A negative cell denotes the sea. Bilge collection must remove exactly the carried volume.
    public static float Take(NavalFloodGrid grid,int cell,float capacity)
    {
        capacity=Math.Max(0,capacity);if(cell<0)return capacity;
        if(cell>=grid.Water.Length)return 0;
        return -grid.Add(cell,-Math.Min(capacity,grid.Water[cell]));
    }
}
