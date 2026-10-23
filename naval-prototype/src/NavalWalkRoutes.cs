using System.Collections.Generic;
public struct NavalWalkPoint
{
    public float X,Y,Z;
    public NavalWalkPoint(float x,float y,float z){X=x;Y=y;Z=z;}
}
public static class NavalWalkRoutes
{
    public const int StepCount=48;
    public static bool InStairwell(float x,float y,float z,float halfWidth=1.1f,float holdFloor=BoatDimensions.HoldFloor)
    {
        if(System.Math.Abs(x)>=halfWidth||z<=-3.7f||z>=.85f||y<=holdFloor+.15f||y>=.825f)return false;
        // A robot below the treads must leave underneath and use the bottom
        // landing, rather than trying to rise vertically through the staircase.
        return System.Math.Abs(y-SurfaceY(z,holdFloor))<=.18f;
    }
    public static float SurfaceY(float z,float holdFloor)
    {
        float fraction=System.Math.Max(0,System.Math.Min(1,(z+3f)/3.5f));
        int step=System.Math.Max(1,System.Math.Min(StepCount,1+(int)(fraction*StepCount)));
        return holdFloor+(.825f-holdFloor)*step/StepCount;
    }
    public static bool Reached(float dx,float dy,float dz,float scale)
    {return (dx*dx+dz*dz)*scale*scale<.20f*.20f&&dy*scale>-.32f&&dy*scale<.65f;}
    private static NavalWalkPoint Step(int index,float holdFloor)
    {
        float y=holdFloor+(0.825f-holdFloor)*(index+1)/StepCount;
        return new NavalWalkPoint(0f,y,-3f+(index+0.5f)*3.5f/StepCount);
    }
    public static List<NavalWalkPoint> Down(float holdFloor=BoatDimensions.HoldFloor)
    {
        List<NavalWalkPoint> points=new List<NavalWalkPoint>();
        points.Add(new NavalWalkPoint(0f,0.825f,0.8f));points.Add(new NavalWalkPoint(0f,0.825f,0.43f));
        for(int i=StepCount-2;i>=0;i-=2)points.Add(Step(i,holdFloor));
        points.Add(new NavalWalkPoint(0f,holdFloor,-3.65f));return points;
    }
    public static List<NavalWalkPoint> UpFrom(float y,float z,float holdFloor=BoatDimensions.HoldFloor)
    {
        List<NavalWalkPoint> points=new List<NavalWalkPoint>();
        for(int i=0;i<StepCount;i+=2)
        {NavalWalkPoint p=Step(i,holdFloor);if(p.Y>=y+0.03f&&p.Z>=z-0.1f)points.Add(p);}
        points.Add(new NavalWalkPoint(0f,0.825f,0.43f));points.Add(new NavalWalkPoint(0f,0.825f,0.8f));return points;
    }
}
