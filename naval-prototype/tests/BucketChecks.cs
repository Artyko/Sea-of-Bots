using System;
public class BucketChecks
{
    static void Equal(float a,float b){if(Math.Abs(a-b)>0.0001f)throw new Exception(a+" != "+b);}
    public static void Main()
    {
        NavalFloodGrid grid=new NavalFloodGrid(4,4,4f,4f,2f,4f);
        grid.Add(grid.Index(0f,0f),2f);
        NavalBucketLoad first=new NavalBucketLoad(),second=new NavalBucketLoad();
        Equal(first.Scoop(grid,0f,0f,.6f,1.5f),.6f);
        Equal(grid.Total+first.Amount,2f);
        second.Scoop(grid,0f,0f,.6f,1.5f);
        Equal(grid.Total+first.Amount+second.Amount,2f);
        first.Return(grid,0f,0f);Equal(first.Amount,0f);Equal(grid.Total+second.Amount,2f);
        float poured=second.Dump();Equal(poured,.6f);Equal(grid.Total,1.4f);
        second.Scoop(grid,-100f,-100f,.6f,.1f);Equal(second.Amount,0f);
        second.Scoop(grid,0f,0f,10f,10f);Equal(second.Amount,1.4f);Equal(grid.Total,0f);
        foreach(float value in grid.Water)if(value<0f)throw new Exception("Negative water");
        Console.WriteLine("Bucket conservation, capacity, two workers, interruption and dump checks passed.");
    }
}
