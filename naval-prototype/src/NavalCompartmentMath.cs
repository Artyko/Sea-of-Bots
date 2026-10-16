using System;
public static class NavalCompartmentMath
{
    public const float WallX=1.4375f,DoorZ=2.625f;
    public static int Region(float x,float z)
    {return Math.Abs(x)<WallX?0:(x<0f?1:3)+(z>=0f?1:0);}
    public static float Move(NavalFloodGrid grid,int from,int to,float requested)
    {
        if(from==to)return 0f;
        float amount=Math.Min(Math.Max(0f,requested),Math.Min(grid.Water[from],grid.MaxCellVolume-grid.Water[to]));
        float removed=-grid.Add(from,-amount);grid.Add(to,removed);return removed;
    }
    public static bool NeedsTransfer(float left,float right,float heel)
    {return Math.Abs(heel)>8f&&Math.Abs(left-right)>4f;}
}
