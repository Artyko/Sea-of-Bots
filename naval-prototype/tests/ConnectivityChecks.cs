using System;
using System.Collections.Generic;
class ConnectivityChecks
{
    static void Check(bool value,string name){if(!value)throw new Exception(name);}
    static List<int>[] Graph()
    {
        // Deck 0-1-2-3, support post 4, upper beam 5-6; the post is the
        // only connection to the deck, while beam 6 also has a side bracket 3.
        List<int>[] edges=new List<int>[7];for(int i=0;i<7;i++)edges[i]=new List<int>();
        int[,] links={{0,1},{1,2},{2,3},{1,4},{4,5},{5,6},{6,3}};
        for(int i=0;i<links.GetLength(0);i++){int a=links[i,0],b=links[i,1];edges[a].Add(b);edges[b].Add(a);}
        return edges;
    }
    static void Main()
    {
        List<int>[] graph=Graph();bool[] absent=new bool[7];
        Check(NavalConnectivity.Components(graph,absent).Count==1,"Intact structure remains one piece");
        absent[4]=true;
        Check(NavalConnectivity.Components(graph,absent).Count==1,"Surviving side bracket keeps beam attached");
        absent[3]=true;
        List<List<int>> pieces=NavalConnectivity.Components(graph,absent);
        Check(pieces.Count==2&&pieces[0].Count==3&&pieces[1].Count==2,"Complete severing detaches entire beam");
        absent[5]=true;
        pieces=NavalConnectivity.Components(graph,absent);
        Check(pieces.Count==2&&pieces[1].Count==1,"Isolated single voxel also detaches");
        absent=new bool[7];
        Check(NavalConnectivity.Components(graph,absent).Count==1,"Repair restores original connections");
        for(int i=0;i<7;i++)absent[i]=true;
        Check(NavalConnectivity.Components(graph,absent).Count==0,"Fully destroyed structure has no components");
        Console.WriteLine("PASS: supported pieces, alternate brackets, severed groups, isolated voxels and repair.");
    }
}
