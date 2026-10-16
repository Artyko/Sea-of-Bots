using System.Collections.Generic;
public struct NavalWalkPoint
{
    public float X,Y,Z;
    public NavalWalkPoint(float x,float y,float z){X=x;Y=y;Z=z;}
}
public static class NavalWalkRoutes
{
    public const int StepCount=48;
    public static bool InStairwell(float x,float y,float z,float halfWidth=1.1f)
    {return System.Math.Abs(x)<halfWidth&&z>-3.7f&&z<0.85f&&y>BoatDimensions.HoldFloor+0.15f&&y<0.78f;}
    private static NavalWalkPoint Step(int index)
    {
        float y=BoatDimensions.HoldFloor+(0.825f-BoatDimensions.HoldFloor)*(index+1)/StepCount;
        return new NavalWalkPoint(0f,y,-3f+(index+0.5f)*3.5f/StepCount);
    }
    public static List<NavalWalkPoint> Down()
    {
        List<NavalWalkPoint> points=new List<NavalWalkPoint>();
        points.Add(new NavalWalkPoint(0f,0.825f,0.8f));points.Add(new NavalWalkPoint(0f,0.825f,0.43f));
        for(int i=StepCount-2;i>=0;i-=2)points.Add(Step(i));
        points.Add(new NavalWalkPoint(0f,BoatDimensions.HoldFloor,-3.65f));return points;
    }
    public static List<NavalWalkPoint> UpFrom(float y,float z)
    {
        List<NavalWalkPoint> points=new List<NavalWalkPoint>();
        for(int i=0;i<StepCount;i+=2)
        {NavalWalkPoint p=Step(i);if(p.Y>=y+0.03f&&p.Z>=z-0.1f)points.Add(p);}
        points.Add(new NavalWalkPoint(0f,0.825f,0.43f));points.Add(new NavalWalkPoint(0f,0.825f,0.8f));return points;
    }
}
