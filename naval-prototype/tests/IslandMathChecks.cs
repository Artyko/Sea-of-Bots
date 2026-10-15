using System;
class IslandMathChecks
{
    static void Check(bool value,string name){if(!value)throw new Exception(name);}
    static void Main()
    {
        for(int seed=7;seed<12;seed++)
        {
            Check(NavalIslandMath.Height(0f,0f,30f,seed)>12f,"Island centre above sea");
            Check(NavalIslandMath.Height(100f,100f,30f,seed)==-35f,"Distant ocean has deep seabed");
            Check(NavalIslandMath.Height(12f,5f,30f,seed)==NavalIslandMath.Height(12f,5f,30f,seed),"Stable island profile");
            for(int x=-80;x<=80;x++)for(int z=-80;z<=80;z++)
            {float h=NavalIslandMath.Height(x,z,30f,seed);Check(!float.IsNaN(h)&&h>=-35f&&h<24f,"Terrain stays within depth and height bounds");}
        }
        Console.WriteLine("PASS: island peaks, open deep ocean, stable profiles and terrain height bounds.");
    }
}
