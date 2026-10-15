using System;
class CannonPortChecks
{
    static void Main()
    {
        // Sweep the barrel's full horizontal/elevation range across the outer
        // face of both bulwarks. Include the 0.42 m collar, not just its axis.
        for(int side=-1;side<=1;side+=2)
        for(int yaw=-30;yaw<=30;yaw+=5)
        for(int pitch=-5;pitch<=45;pitch+=5)
        {
            double a=yaw*Math.PI/180.0, e=pitch*Math.PI/180.0;
            double distance=3.16-2.25;
            double z=-1+distance*Math.Tan(a);
            double y=1.35+distance/Math.Cos(a)*Math.Tan(e);
            double halfZ=0.21/Math.Cos(a), halfY=0.21/Math.Cos(e);
            if(Math.Abs(z+1)+halfZ>=1.1 || y-halfY<=BoatDimensions.CannonPortSill)
                throw new Exception("Barrel obstructed at yaw="+yaw+", pitch="+pitch);
            if(!BoatDimensions.IntersectsCannonPort(side*3.16f,(float)y,(float)z,0f,0f,0f))
                throw new Exception("Barrel crosses solid hull");
        }
        Console.WriteLine("PASS: both gunports clear the barrel/collar through +/-30 yaw and -5..45 elevation.");
    }
}
