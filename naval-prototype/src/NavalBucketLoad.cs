using System;

// Volume remains aboard during carrying; only Dump removes it from the ship.
public class NavalBucketLoad
{
    public float Amount {get;private set;}
    public float Scoop(NavalFloodGrid grid,float x,float z,float capacity,float reach)
    {
        for(int i=0;i<grid.Water.Length&&Amount<capacity;i++)
        {
            float dx=grid.X(i)-x,dz=grid.Z(i)-z;
            if(dx*dx+dz*dz>reach*reach)continue;
            Amount-=grid.Add(i,-Math.Max(0f,capacity-Amount));
        }
        return Amount;
    }
    public void Return(NavalFloodGrid grid,float x,float z)
    {
        Amount-=grid.Add(grid.Index(x,z),Amount);
        for(int i=0;i<grid.Water.Length&&Amount>0f;i++)Amount-=grid.Add(i,Amount);
    }
    public float Dump(){float result=Amount;Amount=0f;return result;}
}
