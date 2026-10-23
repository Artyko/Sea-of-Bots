using System;
public static class NavalFireEnvironmentMath
{
    public static bool HoldFire(float localY,float deck,float scale){return localY<deck-.25f/scale;}
    public static bool SeaContact(float localY,float deck,float scale,float sea,float worldY)
    {return !HoldFire(localY,deck,scale)&&sea>=worldY-.03f;}
    public static bool Washed(float px,float py,float pz,float ax,float ay,float az,float bx,float by,float bz)
    {
        float x=bx-ax,y=by-ay,z=bz-az,length=x*x+y*y+z*z;
        if(length<.00001f)return false;
        float t=Math.Max(0,Math.Min(1,((px-ax)*x+(py-ay)*y+(pz-az)*z)/length));
        float dx=px-ax-x*t,dy=py-ay-y*t,dz=pz-az-z*t;
        return dx*dx+dy*dy+dz*dz<.64f;
    }
}
