using System;
public static class NavalShipContactMath
{
    public const float HalfWidth=6.45f,HalfLength=12.75f;
    public static float SafeFraction(float x,float z,float yaw,float nextX,float nextZ,float turn,float otherX,float otherZ,float otherYaw)
    {
        double dx=nextX-x,dz=nextZ-z;
        int steps=Math.Max(1,(int)Math.Ceiling(Math.Max(Math.Sqrt(dx*dx+dz*dz)/0.25,Math.Abs(turn)/0.5)));
        float safe=0f;
        for(int i=1;i<=steps;i++)
        {
            float t=(float)i/steps;
            if(Overlap(x+(nextX-x)*t,z+(nextZ-z)*t,yaw+turn*t,otherX,otherZ,otherYaw))
            {
                float low=safe,high=t;
                for(int j=0;j<10;j++)
                {
                    float middle=(low+high)*0.5f;
                    if(Overlap(x+(nextX-x)*middle,z+(nextZ-z)*middle,yaw+turn*middle,otherX,otherZ,otherYaw))high=middle;
                    else low=middle;
                }
                return low;
            }
            safe=t;
        }
        return 1f;
    }
    public static bool Overlap(float ax,float az,float yawA,float bx,float bz,float yawB)
    {
        double a=yawA*Math.PI/180.0,b=yawB*Math.PI/180.0;
        double arx=Math.Cos(a),arz=-Math.Sin(a),afx=Math.Sin(a),afz=Math.Cos(a);
        double brx=Math.Cos(b),brz=-Math.Sin(b),bfx=Math.Sin(b),bfz=Math.Cos(b);
        double dx=bx-ax,dz=bz-az;
        return Axis(dx,dz,arx,arz,arx,arz,afx,afz,brx,brz,bfx,bfz)&&
            Axis(dx,dz,afx,afz,arx,arz,afx,afz,brx,brz,bfx,bfz)&&
            Axis(dx,dz,brx,brz,arx,arz,afx,afz,brx,brz,bfx,bfz)&&
            Axis(dx,dz,bfx,bfz,arx,arz,afx,afz,brx,brz,bfx,bfz);
    }
    private static bool Axis(double dx,double dz,double x,double z,double arx,double arz,double afx,double afz,
        double brx,double brz,double bfx,double bfz)
    {
        double radius=HalfWidth*(Math.Abs(x*arx+z*arz)+Math.Abs(x*brx+z*brz))+
            HalfLength*(Math.Abs(x*afx+z*afz)+Math.Abs(x*bfx+z*bfz));
        return Math.Abs(dx*x+dz*z)<radius;
    }
}
