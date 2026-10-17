using System;
using System.Collections.Generic;

// Wood carries wood. Cloth can hang from supported wood but cannot prop up a broken spar.
public sealed class NavalRigSupportGraph
{
    private readonly List<int>[] edges;
    private readonly float[,] bounds;
    private readonly bool[] fabric,mast;
    private readonly int[] queue;
    private readonly bool[] visited;
    private readonly List<List<int>> groups=new List<List<int>>();
    public readonly bool[] Supported;
    private readonly float deck;
    public NavalRigSupportGraph(float[,] cells,bool[] cloth,bool[] vertical,int[] fabricIndex,int columns,float deckHeight)
    {
        bounds=cells;fabric=cloth;mast=vertical;deck=deckHeight;int count=cloth.Length;
        edges=new List<int>[count];queue=new int[count];visited=new bool[count];Supported=new bool[count];
        for(int i=0;i<count;i++)edges[i]=new List<int>();
        for(int a=0;a<count;a++)for(int b=a+1;b<count;b++)
        {
            bool touches;
            if(fabric[a]&&fabric[b])
                touches=Math.Abs(fabricIndex[a]%columns-fabricIndex[b]%columns)+Math.Abs(fabricIndex[a]/columns-fabricIndex[b]/columns)==1;
            else
            {
                touches=true;
                for(int axis=0;axis<3;axis++)
                {
                    // Cloth ties are evaluated in the resting configuration, not the current gust.
                    float tolerance=fabric[a]||fabric[b]?(axis==2?.6f:.001f):.001f;
                    if(Math.Abs(bounds[a,axis]-bounds[b,axis])>bounds[a,axis+3]+bounds[b,axis+3]+tolerance){touches=false;break;}
                }
            }
            if(touches){edges[a].Add(b);edges[b].Add(a);}
        }
    }
    public void Evaluate(bool[] absent,bool foundation)
    {
        Array.Clear(Supported,0,Supported.Length);int head=0,tail=0;
        if(foundation)for(int i=0;i<Supported.Length;i++)
            if(mast[i]&&!absent[i]&&bounds[i,1]-bounds[i,4]<=deck){Supported[i]=true;queue[tail++]=i;}
        while(head<tail)
        {
            int a=queue[head++];foreach(int b in edges[a])
                if(!fabric[b]&&!absent[b]&&!Supported[b]){Supported[b]=true;queue[tail++]=b;}
        }
        float upper=float.MaxValue,lower=float.MinValue;
        for(int i=0;i<Supported.Length;i++)if(mast[i]&&!absent[i]&&!Supported[i])upper=Math.Min(upper,bounds[i,1]);
        if(upper<float.MaxValue)for(int i=0;i<Supported.Length;i++)
            if(mast[i]&&!absent[i]&&Supported[i]&&bounds[i,1]<upper)lower=Math.Max(lower,bounds[i,1]);
        float cut=upper==float.MaxValue?float.MaxValue:lower==float.MinValue?float.MinValue:(upper+lower)*.5f;
        head=tail=0;
        for(int i=0;i<Supported.Length;i++)if(fabric[i]&&!absent[i]&&bounds[i,1]<cut)
            foreach(int b in edges[i])if(!fabric[b]&&Supported[b]){Supported[i]=true;queue[tail++]=i;break;}
        while(head<tail)
        {
            int a=queue[head++];foreach(int b in edges[a])
                if(fabric[b]&&!absent[b]&&!Supported[b]&&bounds[b,1]<cut){Supported[b]=true;queue[tail++]=b;}
        }
    }
    public List<List<int>> FallingComponents(bool[] absent)
    {
        // Connectivity is recomputed only after damage; reusing work arrays avoids per-cell garbage.
        Array.Clear(visited,0,visited.Length);foreach(List<int> group in groups)group.Clear();int used=0;
        for(int seed=0;seed<Supported.Length;seed++)
        {
            if(absent[seed]||Supported[seed]||visited[seed])continue;
            if(used==groups.Count)groups.Add(new List<int>());List<int> component=groups[used++];
            int head=0,tail=0;queue[tail++]=seed;visited[seed]=true;
            while(head<tail)
            {
                int a=queue[head++];component.Add(a);
                foreach(int b in edges[a])if(!absent[b]&&!Supported[b]&&!visited[b]){visited[b]=true;queue[tail++]=b;}
            }
        }
        return groups; // Unused pooled groups are empty.
    }
}
