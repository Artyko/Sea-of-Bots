using System;
using System.Collections.Generic;

public static class NavalConnectivity
{
    public static List<List<int>> Components(List<int>[] edges,bool[] absent)
    {return new NavalConnectivityWorkspace(edges.Length).Evaluate(edges,absent);}
}

public sealed class NavalConnectivityWorkspace
{
    private readonly bool[] visited;
    private readonly int[] queue;
    private readonly List<List<int>> pool=new List<List<int>>();
    private readonly List<List<int>> result=new List<List<int>>();
    public NavalConnectivityWorkspace(int count){visited=new bool[count];queue=new int[count];}
    public List<List<int>> Evaluate(List<int>[] edges,bool[] absent)
    {
        if(edges.Length!=visited.Length||absent.Length!=visited.Length)throw new ArgumentException("Graph size changed");
        Array.Clear(visited,0,visited.Length);result.Clear();
        for(int seed=0;seed<edges.Length;seed++)
        {
            if(absent[seed]||visited[seed])continue;
            if(result.Count==pool.Count)pool.Add(new List<int>());
            List<int> component=pool[result.Count];component.Clear();result.Add(component);
            int head=0,tail=0;visited[seed]=true;queue[tail++]=seed;
            while(head<tail)
            {
                int a=queue[head++];component.Add(a);
                foreach(int b in edges[a])if(!visited[b]&&!absent[b]){visited[b]=true;queue[tail++]=b;}
            }
        }
        return result;
    }
}
