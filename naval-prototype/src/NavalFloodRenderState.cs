using System;

// Render-only snapshots. The conserved hydraulic grid remains authoritative.
public sealed class NavalFloodRenderState
{
    public readonly float[] Depths;
    public readonly int[] ChangedCells;
    public int ChangedCount {get;private set;}
    public int WetCount {get;private set;}
    public NavalFloodRenderState(int count){Depths=new float[count];ChangedCells=new int[count];}
    public bool Refresh(NavalFloodGrid grid)
    {
        ChangedCount=0;
        for(int i=0;i<Depths.Length;i++)
        {
            float depth=grid.Depth(i);if(depth<=.0005f)depth=0f;
            float previous=Depths[i];
            bool wet=depth>0f,wasWet=previous>0f;
            if(wet==wasWet&&Math.Abs(depth-previous)<=.0001f)continue;
            if(wet!=wasWet)WetCount+=wet?1:-1;
            Depths[i]=depth;ChangedCells[ChangedCount++]=i;
        }
        return ChangedCount>0;
    }
}
