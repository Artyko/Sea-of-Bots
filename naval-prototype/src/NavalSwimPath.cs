using System;
public struct NavalSwimPoint
{
    public float X,Z;
    public NavalSwimPoint(float x,float z){X=x;Z=z;}
}
public static class NavalSwimPath
{
    [ThreadStatic] private static NavalSwimWorkspace workspace;
    public static float Route(NavalSwimPoint start,NavalSwimPoint goal,out NavalSwimPoint next)
    {
        if(workspace==null)workspace=new NavalSwimWorkspace();
        return workspace.Route(start,goal,out next);
    }
    private static bool Clip(float p,float d,float min,float max,ref float first,ref float last)
    {
        if(Math.Abs(d)<0.00001f)return p>min&&p<max;
        float a=(min-p)/d,b=(max-p)/d;if(a>b){float swap=a;a=b;b=swap;}
        first=Math.Max(first,a);last=Math.Min(last,b);return first<last;
    }
    public static bool Clear(NavalSwimPoint a,NavalSwimPoint b)
    {
        float first=0f,last=1f;
        return !(Clip(a.X,b.X-a.X,-3.45f,3.45f,ref first,ref last)&&Clip(a.Z,b.Z-a.Z,-6.5f,6.5f,ref first,ref last));
    }
    public static float Distance(NavalSwimPoint a,NavalSwimPoint b)
    {float dx=a.X-b.X,dz=a.Z-b.Z;return (float)Math.Sqrt(dx*dx+dz*dz);}
}
public sealed class NavalSwimWorkspace
{
    private readonly NavalSwimPoint[] points=new NavalSwimPoint[6];
    private readonly float[] distance=new float[6];
    private readonly bool[] used=new bool[6];
    private readonly int[] previous=new int[6];
    public float Route(NavalSwimPoint start,NavalSwimPoint goal,out NavalSwimPoint next)
    {
        if(Math.Abs(start.X)<3.45f&&Math.Abs(start.Z)<6.5f)
        {
            NavalSwimPoint exit=start;
            if(3.45f-Math.Abs(start.X)<6.5f-Math.Abs(start.Z))exit.X=start.X<0f?-3.7f:3.7f;
            else exit.Z=start.Z<0f?-6.8f:6.8f;
            NavalSwimPoint unused;float remaining=Route(exit,goal,out unused);
            next=exit;return remaining+NavalSwimPath.Distance(start,exit);
        }
        if(NavalSwimPath.Clear(start,goal)){next=goal;return NavalSwimPath.Distance(start,goal);}
        points[0]=start;points[1]=new NavalSwimPoint(-3.7f,-6.8f);points[2]=new NavalSwimPoint(3.7f,-6.8f);
        points[3]=new NavalSwimPoint(3.7f,6.8f);points[4]=new NavalSwimPoint(-3.7f,6.8f);points[5]=goal;
        for(int i=0;i<6;i++){distance[i]=i==0?0f:float.MaxValue;used[i]=false;previous[i]=-1;}
        for(int step=0;step<6;step++)
        {
            int current=-1;for(int i=0;i<6;i++)if(!used[i]&&(current<0||distance[i]<distance[current]))current=i;
            if(current<0||distance[current]==float.MaxValue)break;used[current]=true;
            for(int j=0;j<6;j++)if(!used[j]&&NavalSwimPath.Clear(points[current],points[j]))
            {float value=distance[current]+NavalSwimPath.Distance(points[current],points[j]);if(value<distance[j]){distance[j]=value;previous[j]=current;}}
        }
        int node=5;if(previous[node]<0){next=goal;return float.MaxValue;}
        while(previous[node]>0)node=previous[node];next=points[node];return distance[5];
    }
}
