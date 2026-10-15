using System;
class FloodGridChecks
{
    static void Check(bool result,string name) { if(!result)throw new Exception(name); }
    static void Near(float a,float b,float tolerance,string name){Check(Math.Abs(a-b)<tolerance,name+": "+a+" vs "+b);}
    static void Main()
    {
        NavalFloodGrid grid=new NavalFloodGrid(12,24,5.75f,11.75f,2f);
        float[] floor=new float[grid.Water.Length];
        int source=grid.Index(-2.5f,-4f); grid.Add(source,8f);
        grid.Spread(0.02f,floor,1f);
        Check(grid.Water[source]<8f&&grid.Water[source+1]>0f,"Local inflow spreads into adjacent cell");
        Check(grid.Water[grid.Index(2.5f,4f)]==0f,"Far end does not fill immediately");
        Near(grid.Total,8f,0.0001f,"First-step mass conservation");
        for(int step=0;step<2000;step++)grid.Spread(0.05f,floor,1f);
        Near(grid.Total,8f,0.002f,"Long-run mass conservation");
        foreach(float value in grid.Water)Check(value>=0f,"No negative cells");
        grid.Pump(3f); Near(grid.Total,5f,0.002f,"Pump removes specified amount");
        grid.Pump(100f); Near(grid.Total,0f,0.00001f,"Pump cannot create negative volume");
        NavalFloodGrid pair=new NavalFloodGrid(2,1,2f,1f,1f);
        pair.Add(0,1.5f);pair.Add(1,0.5f);
        pair.Spread(1f,new float[]{0f,1f},1f);
        Near(pair.Water[0],1.5f,0.00001f,"Equal world-space heads stay still on tilted floor");
        pair=new NavalFloodGrid(2,1,2f,1f,1f); pair.Add(1,1f);
        pair.Spread(0.02f,new float[]{0f,0.5f},1f);
        Check(pair.Water[0]>0f,"Gravity drives water toward lower floor");
        pair=new NavalFloodGrid(2,1,2f,1f,1f); pair.Add(0,0.2f);
        pair.Spread(1f,new float[]{0f,1f},1f);
        Check(pair.Water[1]==0f,"Shallow water cannot flow uphill over a dry step");
        Check(grid.Index(-100f,-100f)==0&&grid.Index(100f,100f)==grid.Water.Length-1,"Hull openings map to boundary cells");
        NavalFloodGrid capped=new NavalFloodGrid(4,4,4f,4f,2f,4f);
        float accepted=capped.Add(0,100000f);
        Near(capped.Depth(0),4f,0.00001f,"Extreme inflow stops at hull rim");
        Near(accepted,capped.MaxCellVolume,0.00001f,"Only accepted inflow is counted");
        Check(capped.Add(0,100f)==0f,"Full cell does not admit more water");
        float[] incline=new float[16]; for(int i=0;i<16;i++)incline[i]=i*0.1f;
        for(int step=0;step<1000;step++)
        {
            capped.Add(step%16,100f); capped.Spread(0.05f,incline,0.85f);
            for(int i=0;i<16;i++)Check(capped.Depth(i)<=4.00001f&&capped.Water[i]>=0f,"Tilted transfer stays inside hull capacity");
        }
        float full=capped.Total; capped.Spread(5f,incline,0.85f);
        Near(capped.Total,full,0.002f,"Capacity-limited transfers conserve mass");
        capped.Pump(5f); Near(capped.Total,full-5f,0.002f,"Pump works after saturation");
        Console.WriteLine("PASS: local spread, finite propagation, mass conservation, pump, nonnegative volumes and gravity.");
    }
}
