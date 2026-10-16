using System;
public static class NavalGunneryMath
{
    public const float ReloadSeconds=5f;
    private static readonly float[] weights={4f,3f,.5f,1.5f};
    // Waterline, guns, mast, wheel. Avoid repeating the last type when possible.
    public static int Choose(float roll,int previous,bool[] available)
    {
        float total=0f;int count=0;
        for(int i=0;i<4;i++)if(available[i])count++;
        for(int i=0;i<4;i++)if(available[i]&&(count==1||i!=previous))total+=weights[i];
        if(total==0f)return -1;
        float pick=Math.Max(0f,Math.Min(.999999f,roll))*total;
        for(int i=0;i<4;i++)if(available[i]&&(count==1||i!=previous))
        {pick-=weights[i];if(pick<0f)return i;}
        return -1;
    }
    public static float Spread(float distance){return Math.Min(6f,.4f+Math.Max(0f,distance)*.045f);}
    public const float LevelRange=32f*32f/9.81f;
    public static float ApproachWeight(float distance)
    {
        if(distance>LevelRange*1.25f)return 1f;
        if(distance>55f)return .15f;
        return Math.Max(-1f,Math.Min(.15f,(distance-45f)/20f));
    }
    public static float FlightTime(float horizontalDistance,float angle)
    {return horizontalDistance/(32f*(float)Math.Cos(angle*Math.PI/180f));}
}
