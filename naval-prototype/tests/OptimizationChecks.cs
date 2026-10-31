using System;
using System.Collections.Generic;
class OptimizationChecks
{
    static void Check(bool value,string name){if(!value)throw new Exception(name);}
    static List<List<int>> Reference(List<int>[] edges,bool[] absent)
    {
        bool[] seen=new bool[edges.Length];List<List<int>> result=new List<List<int>>();
        for(int i=0;i<edges.Length;i++)
        {
            if(absent[i]||seen[i])continue;
            List<int> group=new List<int>();group.Add(i);seen[i]=true;
            for(int head=0;head<group.Count;head++)foreach(int b in edges[group[head]])
                if(!seen[b]&&!absent[b]){seen[b]=true;group.Add(b);}
            result.Add(group);
        }
        return result;
    }
    static void Main()
    {
        Check(NavalSpatialBatching.ChunkSize(NavalShipDefinition.Sloop)==2f,"Sail rendering keeps established chunk size");
        foreach(var steam in NavalShipDefinition.SteamChoices)Check(NavalSpatialBatching.ChunkSize(steam)==4f,"Dense steam hull uses low-draw-call chunks");
        Check(NavalSpatialBatching.ColliderChunkSize(NavalShipDefinition.Sloop)==2f,"Sail collider batching stays unchanged");
        foreach(var steam in NavalShipDefinition.SteamChoices)Check(NavalSpatialBatching.ColliderChunkSize(steam)==4f,"Steam physics keeps exact chunks small enough for bounded impact rebuilds");
        Check(NavalSpatialBatching.ColliderChunk(-3f,8f,true)==0&&NavalSpatialBatching.ColliderChunk(3f,8f,true)==0,"Centered steam chunk does not split the beam at zero");
        Random random=new Random(8621);
        for(int test=0;test<40;test++)
        {
            int count=random.Next(1,180);List<int>[] edges=new List<int>[count];bool[] absent=new bool[count];
            for(int i=0;i<count;i++)edges[i]=new List<int>();
            for(int i=0;i<count*4;i++){int a=random.Next(count),b=random.Next(count);edges[a].Add(b);edges[b].Add(a);}
            NavalConnectivityWorkspace workspace=new NavalConnectivityWorkspace(count);
            for(int step=0;step<120;step++)
            {
                for(int i=0;i<count;i++)absent[i]=random.NextDouble()<.6;
                List<List<int>> expected=Reference(edges,absent),actual=workspace.Evaluate(edges,absent);
                Check(expected.Count==actual.Count,"component count");
                for(int i=0;i<expected.Count;i++)
                {
                    Check(expected[i].Count==actual[i].Count,"component size");
                    for(int j=0;j<expected[i].Count;j++)Check(expected[i][j]==actual[i][j],"component ordering");
                }
            }
        }
        // Structural support is evaluated only after a damage-state change.
        // When it does run, the connectivity workspace and component lists are
        // reused: a ship-sized graph can be checked repeatedly without Gen0 GC.
        const int structureCells=2400;List<int>[] structure=new List<int>[structureCells];bool[] structureAbsent=new bool[structureCells],foundation=new bool[structureCells];
        for(int i=0;i<structureCells;i++){structure[i]=new List<int>(2);if(i>0)structure[i].Add(i-1);if(i+1<structureCells)structure[i].Add(i+1);}
        foundation[0]=true;var structureWorkspace=new NavalConnectivityWorkspace(structureCells);
        for(int warm=0;warm<4;warm++)structureWorkspace.Evaluate(structure,structureAbsent);
        GC.Collect();int collections=GC.CollectionCount(0);
        for(int pass=0;pass<500;pass++)
        {
            structureAbsent[1200]=pass%2==0;var groups=structureWorkspace.Evaluate(structure,structureAbsent);
            Check(NavalStructuralSupport.HasFoundation(groups[0],foundation),"Foundation component stays supported in stress test");
        }
        Check(GC.CollectionCount(0)==collections,"Reused structural support checks cause no Gen0 collections");
        NavalPointIndex index=new NavalPointIndex();const int points=8000;
        float[,] positions=new float[points,3];
        for(int i=0;i<points;i++)
        {
            for(int axis=0;axis<3;axis++)positions[i,axis]=(float)(random.NextDouble()*60-30);
            index.Add(i,positions[i,0],positions[i,1],positions[i,2]);
        }
        List<int> candidates=new List<int>();
        for(int test=0;test<500;test++)
        {
            float x=(float)(random.NextDouble()*70-35),y=(float)(random.NextDouble()*70-35),z=(float)(random.NextDouble()*70-35);
            float radius=test%20==0?100f:(float)random.NextDouble()*3f;
            index.Query(x,y,z,radius,candidates);HashSet<int> found=new HashSet<int>(candidates);
            Check(found.Count==candidates.Count,"no duplicate indexed candidates");
            for(int i=0;i<points;i++)
                if(Math.Abs(positions[i,0]-x)<=radius&&Math.Abs(positions[i,1]-y)<=radius&&Math.Abs(positions[i,2]-z)<=radius)
                    Check(found.Contains(i),"conservative bounds never miss a voxel");
        }
        index.Clear();index.Query(-1f,0f,0f,.1f,candidates);Check(candidates.Count==0,"rebuild clears stale cells");
        index.Add(42,-1f,0f,0f);index.Query(-1f,0f,0f,0f,candidates);Check(candidates.Count==1&&candidates[0]==42,"negative bin boundary");
        Console.WriteLine("PASS: 4800 reused connectivity snapshots, 500 ship-sized support checks without Gen0 GC, and 500 spatial queries preserve all candidates.");
    }
}
