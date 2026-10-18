using System;
class FiringPositionChecks
{
    static void Check(bool ok,string label){if(!ok)throw new Exception(label);}
    static float Bearing(float x,float z){return (float)(Math.Atan2(x,z)*180/Math.PI);}
    static void Main()
    {
        Check(NavalFiringPositionMath.Threatens(-90,1,0,0),"Port fires to port");
        Check(!NavalFiringPositionMath.Threatens(90,1,0,0),"Broken starboard does not threaten");
        Check(NavalFiringPositionMath.Threatens(60,0,1,0)&&NavalFiringPositionMath.Threatens(120,0,1,0),"Actual 60 degree gun sector inclusive");
        Check(!NavalFiringPositionMath.Threatens(0,2,2,12)&&!NavalFiringPositionMath.Threatens(180,2,2,12),"Bow and stern remain blind");
        for(int port=0;port<=2;port++)for(int starboard=0;starboard<=2;starboard++)
        for(int angle=0;angle<360;angle+=5)
        {
            NavalFiringPositionMath p=new NavalFiringPositionMath();float x,z;
            double a=angle*Math.PI/180;
            p.Select((float)Math.Sin(a)*100,(float)Math.Cos(a)*100,80,port,starboard,out x,out z);
            Check(!NavalFiringPositionMath.Threatens(Bearing(x,z),port,starboard,12),"Selected station avoids live enemy arcs");
            Check(Math.Abs(Math.Sqrt(x*x+z*z)-80)<.001,"Station keeps firing radius");
        }
        NavalFiringPositionMath choice=new NavalFiringPositionMath();float px,pz;
        choice.Select(80,0,80,2,0,out px,out pz);Check(px>79&&Math.Abs(pz)<.001,"Use nearest disabled enemy side");
        choice.Reset();choice.Select(0,0,80,2,2,out px,out pz);
        choice.Select(0,-1,80,2,2,out px,out pz);Check(pz>0,"Small fluctuations do not flip route");
        choice.Select(0,-160,80,2,2,out px,out pz);Check(pz<0,"Large tactical change may switch station");
        foreach(float own in new float[]{2,3})foreach(float other in new float[]{2,3})
        {
            float r=NavalFiringPositionMath.Range(own,other);
            float margin=(float)Math.Sqrt(6.45*6.45+12.75*12.75)*(own+other)*.5f+8;
            Check(r>=margin+11.99f,"Keep scaled hull clearance");
            Check(r<NavalGunneryMath.LevelRange*.9f,"Station within cannon ballistic range with margin");
        }
        Console.WriteLine("PASS: 648 firing stations, live/broken enemy sectors, range, scaled hull clearance and route hysteresis.");
    }
}
