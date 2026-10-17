using System;
using System.Collections.Generic;

// Fixed vertex slots, variable visible triangles. Hidden/detached cells use material -1.
public sealed class NavalBatchTopology
{
    private readonly int[] offsets,materials;
    private readonly int[][] source;
    private readonly List<List<int>> groups=new List<List<int>>();
    private readonly List<int> active=new List<int>();
    public int VertexCount {get;private set;}
    public List<int> ActiveMaterials {get{return active;}}
    public NavalBatchTopology(int[] vertexCounts,int[][] triangles)
    {
        if(vertexCounts.Length!=triangles.Length)throw new ArgumentException("Cell count mismatch");
        offsets=new int[vertexCounts.Length];materials=new int[vertexCounts.Length];source=triangles;
        for(int i=0;i<vertexCounts.Length;i++)
        {
            if(vertexCounts[i]<0||triangles[i].Length%3!=0)throw new ArgumentException("Invalid cell mesh");
            offsets[i]=VertexCount;VertexCount+=vertexCounts[i];materials[i]=-1;
            foreach(int index in triangles[i])if(index<0||index>=vertexCounts[i])throw new ArgumentException("Triangle outside cell");
        }
    }
    public int Offset(int cell){return offsets[cell];}
    public void SetCell(int cell,int material)
    {if(material< -1)throw new ArgumentOutOfRangeException("material");materials[cell]=material;}
    public List<int> Triangles(int material){return groups[material];}
    public void Rebuild()
    {
        active.Clear();foreach(List<int> group in groups)group.Clear();
        for(int i=0;i<materials.Length;i++)
        {
            int material=materials[i];if(material<0)continue;
            while(groups.Count<=material)groups.Add(new List<int>());
            List<int> group=groups[material];
            foreach(int index in source[i])group.Add(offsets[i]+index);
        }
        for(int i=0;i<groups.Count;i++)if(groups[i].Count>0)active.Add(i);
    }
}
