using System;
public static class NavalFireWaterSupply
{
    // A negative cell denotes the sea. Bilge collection must remove exactly the carried volume.
    public static float Take(NavalFloodGrid grid,int cell,float capacity)
    {
        capacity=Math.Max(0,capacity);if(cell<0)return capacity;
        if(cell>=grid.Water.Length)return 0;
        return -grid.Add(cell,-Math.Min(capacity,grid.Water[cell]));
    }
}
