using System;
public static class NavalSmokePlumeMath
{
    public const float Lifetime=12f,RiseSpeed=2.4f,Expansion=4f;
    public const int OpacityLevels=8;
    public const float ColumnInterval=.25f/1.5f,ColumnSize=1f,SourceCarry=.2f;
    public const int MaxColumns=8,MaxColumnPuffsPerTick=8;
    public static int Puffs(int burning){return burning<=0?0:burning<4?1:burning<10?2:3;}
    public static float FireSize(int burning){return ColumnSize+(float)Math.Min(.5,Math.Sqrt(Math.Max(0,burning-1))*.09);}
    public static float RelativeDrift(float speed,float age)
    {age=Math.Max(0,age);return SourceCarry*speed*(1-(float)Math.Exp(-1.6*age))/1.6f-speed*age;}
    private static float Smooth(float t){t=Math.Max(0,Math.Min(1,t));return t*t*(3-2*t);}
    public static float BirthScale(float age){return .55f+.45f*Smooth(age/.3f);}
    public static float Alpha(float age,float lifetime)
    {return .65f*Smooth(age/.25f)*(1-Smooth(age/Math.Max(.001f,lifetime)));}
    public static float Drag(float dt){return 1f-(float)Math.Exp(-1.6*Math.Max(0,dt));}
    public static int Level(float life,float total)
    {return Math.Max(0,Math.Min(OpacityLevels-1,(int)((1-Math.Max(0,life)/Math.Max(.001f,total))*OpacityLevels)));}
}
