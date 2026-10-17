using System;
using System.Collections.Generic;
class RigBatchChecks
{
    static void Check(bool value,string name){if(!value)throw new Exception(name);}
    static readonly int[] Cube={0,1,2,0,2,3,4,5,6,4,6,7,8,9,10,8,10,11,12,13,14,12,14,15,16,17,18,16,18,19,20,21,22,20,22,23};
    static void Main()
    {
        const int count=579;int[] sizes=new int[count],state=new int[count];int[][] triangles=new int[count][];
        for(int i=0;i<count;i++){sizes[i]=24;triangles[i]=Cube;}
        NavalBatchTopology topology=new NavalBatchTopology(sizes,triangles);Random random=new Random(8723);
        Check(topology.VertexCount==count*24,"all cells retain their own vertex slots");
        for(int frame=0;frame<400;frame++)
        {
            int expectedMaterials=0;
            for(int i=0;i<count;i++)
            {
                // -1 covers missing, detached and removed cells; other ids cover wood/cloth/scorch.
                state[i]=frame==0?-1:frame==1?0:random.Next(-1,4);
                topology.SetCell(i,state[i]);Check(topology.Offset(i)==24*i,"stable vertex offset");
            }
            topology.Rebuild();
            for(int material=0;material<4;material++)
            {
                List<int> expected=new List<int>();
                for(int i=0;i<count;i++)if(state[i]==material)foreach(int index in Cube)expected.Add(i*24+index);
                if(expected.Count==0){Check(!topology.ActiveMaterials.Contains(material),"empty material excluded");continue;}
                expectedMaterials++;Check(topology.ActiveMaterials.Contains(material),"material included");
                List<int> actual=topology.Triangles(material);Check(actual.Count==expected.Count,"triangle count after holes/detachment");
                for(int i=0;i<actual.Count;i++)Check(actual[i]==expected[i]&&actual[i]<topology.VertexCount,"same triangles, winding, material and vertex slot");
                topology.Rebuild();Check(object.ReferenceEquals(actual,topology.Triangles(material)),"triangle buffers reused");
            }
            Check(topology.ActiveMaterials.Count==expectedMaterials,"submesh material mapping");
        }
        for(int i=0;i<count;i++)topology.SetCell(i,0);topology.Rebuild();
        Check(topology.ActiveMaterials.Count==1&&topology.Triangles(0).Count==count*36,"complete restoration after total destruction");
        bool rejected=false;try{new NavalBatchTopology(new int[]{1},new int[][]{new int[]{0,1,0}});}catch(ArgumentException){rejected=true;}
        Check(rejected,"invalid source geometry rejected");
        Console.WriteLine("PASS: 400 rig render states x 579 cells, hidden/debris exclusion, material changes, winding, buffer reuse and full restoration.");
    }
}
