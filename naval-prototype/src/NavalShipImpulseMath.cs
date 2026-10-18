using System;
// Horizontal, frictionless contact impulse. Normal points from A toward B.
public static class NavalShipImpulseMath
{
    public static float Impulse(float closing,float massA,float massB)
    {
        if(closing<=.02f)return 0f;
        return 1.12f*closing/(1f/Math.Max(.1f,massA)+1f/Math.Max(.1f,massB));
    }
    public static void Normal(float ax,float az,float yawA,float bx,float bz,float yawB,float scaleA,float scaleB,out float nx,out float nz)
    {
        double a=yawA*Math.PI/180,b=yawB*Math.PI/180;
        double arx=Math.Cos(a),arz=-Math.Sin(a),afx=Math.Sin(a),afz=Math.Cos(a);
        double brx=Math.Cos(b),brz=-Math.Sin(b),bfx=Math.Sin(b),bfz=Math.Cos(b);
        double dx=bx-ax,dz=bz-az,best=double.MaxValue;nx=1;nz=0;
        for(int i=0;i<4;i++)
        {
            double x=i==0?arx:i==1?afx:i==2?brx:bfx,z=i==0?arz:i==1?afz:i==2?brz:bfz;
            double radius=NavalShipContactMath.HalfWidth*(scaleA*Math.Abs(x*arx+z*arz)+scaleB*Math.Abs(x*brx+z*brz))+
                NavalShipContactMath.HalfLength*(scaleA*Math.Abs(x*afx+z*afz)+scaleB*Math.Abs(x*bfx+z*bfz));
            double projection=dx*x+dz*z,overlap=radius-Math.Abs(projection);
            if(overlap<best){best=overlap;float sign=projection<0?-1f:1f;nx=(float)x*sign;nz=(float)z*sign;}
        }
    }
}
public sealed class NavalShipMomentum
{
    private float pendingX,pendingZ;
    public float DriftX {get;private set;}
    public float DriftZ {get;private set;}
    public void Add(float x,float z){pendingX+=x;pendingZ+=z;}
    public void Stop(){pendingX=pendingZ=DriftX=DriftZ=0f;}
    public void Step(float forwardX,float forwardZ,float dt,ref float speed,float forwardResponse=1f)
    {
        float length=(float)Math.Sqrt(forwardX*forwardX+forwardZ*forwardZ);
        if(length<.001f){forwardX=0;forwardZ=1;}else {forwardX/=length;forwardZ/=length;}
        float longitudinal=pendingX*forwardX+pendingZ*forwardZ;
        speed+=longitudinal/Math.Max(.1f,forwardResponse);
        DriftX+=pendingX-forwardX*longitudinal;DriftZ+=pendingZ-forwardZ*longitudinal;
        pendingX=pendingZ=0;
        float drag=(float)Math.Exp(-.45f*Math.Max(0,dt));DriftX*=drag;DriftZ*=drag;
    }
}
