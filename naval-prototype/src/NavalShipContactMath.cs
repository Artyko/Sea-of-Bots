using System;
public static class NavalShipContactMath
{
    public const float HalfWidth=6.45f,HalfLength=12.75f;
    private struct Axes
    {
        public double RX,RZ,FX,FZ;
        public Axes(float yaw)
        {double angle=yaw*Math.PI/180.0;RX=Math.Cos(angle);FZ=RX;FX=Math.Sin(angle);RZ=-FX;}
    }
    public static float SafeFraction(float x,float z,float yaw,float nextX,float nextZ,float turn,float otherX,float otherZ,float otherYaw,float scaleA=1f,float scaleB=1f)
    {
        // A swept bounding circle contains every intermediate hull rotation.
        // Reject distant pairs before trigonometry, subdivision or binary searches.
        double dx=nextX-x,dz=nextZ-z,length=dx*dx+dz*dz;
        double closest=length>0?Math.Max(0,Math.Min(1,((otherX-x)*dx+(otherZ-z)*dz)/length)):0;
        double nearX=x+dx*closest-otherX,nearZ=z+dz*closest-otherZ;
        double coordinates=Math.Max(Math.Max(Math.Abs(x),Math.Abs(z)),Math.Max(Math.Max(Math.Abs(nextX),Math.Abs(nextZ)),Math.Max(Math.Abs(otherX),Math.Abs(otherZ))));
        double radius=Math.Sqrt(HalfWidth*HalfWidth+HalfLength*HalfLength)*(Math.Abs(scaleA)+Math.Abs(scaleB))+.0001+coordinates*.000001;
        if(nearX*nearX+nearZ*nearZ>radius*radius)return 1f;
        int steps=Math.Max(1,(int)Math.Ceiling(Math.Max(Math.Sqrt(dx*dx+dz*dz)/0.25,Math.Abs(turn)/0.5)));
        Axes fixedOther=new Axes(otherYaw),fixedOwn=turn==0f?new Axes(yaw):new Axes();
        float safe=0f;
        for(int i=1;i<=steps;i++)
        {
            float t=(float)i/steps;
            if(OverlapPose(x+(nextX-x)*t,z+(nextZ-z)*t,yaw+turn*t,otherX,otherZ,fixedOther,fixedOwn,turn==0f,scaleA,scaleB))
            {
                float low=safe,high=t;
                for(int j=0;j<10;j++)
                {
                    float middle=(low+high)*0.5f;
                    if(OverlapPose(x+(nextX-x)*middle,z+(nextZ-z)*middle,yaw+turn*middle,otherX,otherZ,fixedOther,fixedOwn,turn==0f,scaleA,scaleB))high=middle;
                    else low=middle;
                }
                return low;
            }
            safe=t;
        }
        return 1f;
    }
    public static bool Overlap(float ax,float az,float yawA,float bx,float bz,float yawB,float scaleA=1f,float scaleB=1f)
    {
        return OverlapPrepared(ax,az,new Axes(yawA),bx,bz,new Axes(yawB),scaleA,scaleB);
    }
    private static bool OverlapPose(float ax,float az,float yaw,float bx,float bz,Axes other,Axes own,bool fixedOwn,float scaleA,float scaleB)
    {return OverlapPrepared(ax,az,fixedOwn?own:new Axes(yaw),bx,bz,other,scaleA,scaleB);}
    private static bool OverlapPrepared(float ax,float az,Axes a,float bx,float bz,Axes b,float scaleA,float scaleB)
    {
        double dx=bx-ax,dz=bz-az;
        return Axis(dx,dz,a.RX,a.RZ,a.RX,a.RZ,a.FX,a.FZ,b.RX,b.RZ,b.FX,b.FZ,scaleA,scaleB)&&
            Axis(dx,dz,a.FX,a.FZ,a.RX,a.RZ,a.FX,a.FZ,b.RX,b.RZ,b.FX,b.FZ,scaleA,scaleB)&&
            Axis(dx,dz,b.RX,b.RZ,a.RX,a.RZ,a.FX,a.FZ,b.RX,b.RZ,b.FX,b.FZ,scaleA,scaleB)&&
            Axis(dx,dz,b.FX,b.FZ,a.RX,a.RZ,a.FX,a.FZ,b.RX,b.RZ,b.FX,b.FZ,scaleA,scaleB);
    }
    private static bool Axis(double dx,double dz,double x,double z,double arx,double arz,double afx,double afz,
        double brx,double brz,double bfx,double bfz,float scaleA,float scaleB)
    {
        double radius=HalfWidth*(scaleA*Math.Abs(x*arx+z*arz)+scaleB*Math.Abs(x*brx+z*brz))+
            HalfLength*(scaleA*Math.Abs(x*afx+z*afz)+scaleB*Math.Abs(x*bfx+z*bfz));
        return Math.Abs(dx*x+dz*z)<radius;
    }
}
