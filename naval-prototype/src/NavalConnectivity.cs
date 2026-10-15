using System.Collections.Generic;

public static class NavalConnectivity
{
    public static List<List<int>> Components(List<int>[] edges, bool[] absent)
    {
        bool[] visited=new bool[edges.Length];
        List<List<int>> result=new List<List<int>>();
        List<int> queue=new List<int>();
        for(int seed=0;seed<edges.Length;seed++)
        {
            if(absent[seed]||visited[seed])continue;
            List<int> component=new List<int>();
            queue.Clear();int head=0;
            visited[seed]=true; queue.Add(seed);
            while(head<queue.Count)
            {
                int a=queue[head++]; component.Add(a);
                foreach(int b in edges[a]) if(!visited[b]&&!absent[b])
                {visited[b]=true;queue.Add(b);}
            }
            result.Add(component);
        }
        return result;
    }
}
