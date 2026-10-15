using System;
public static class ShipContactChecks
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static void Main()
    {
        Check(!NavalShipContactMath.Overlap(0,0,0,30,0,0),"Separated broadside");
        Check(NavalShipContactMath.Overlap(0,0,0,12,0,0),"Broadside contact");
        Check(NavalShipContactMath.Overlap(0,0,0,0,25,0),"Bow to stern contact");
        Check(!NavalShipContactMath.Overlap(0,0,0,0,27,0),"Separated bow to stern");
        Check(NavalShipContactMath.Overlap(0,0,0,17,0,90),"Crossing hulls");
        Check(!NavalShipContactMath.Overlap(0,0,0,14,14,0),"Corners separated");
        float fraction=NavalShipContactMath.SafeFraction(-50,0,0,50,0,0,0,0,0);
        Check(fraction>0f&&fraction<0.5f,"High speed crossing must stop before other hull");
        float stop=-50+100*fraction;
        Check(!NavalShipContactMath.Overlap(stop,0,0,0,0,0),"Sweep endpoint outside hull");
        Check(NavalShipContactMath.Overlap(stop+0.01f,0,0,0,0,0),"Sweep stops at contact");
        Check(NavalShipContactMath.SafeFraction(stop,0,0,stop-5,0,0,0,0,0)==1f,"Can reverse away from contact");
        Check(NavalShipContactMath.SafeFraction(0,0,0,0,0,90,18,0,0)<1f,"Turning bow must not enter hull");
        for(int a=0;a<360;a+=13)for(int b=0;b<360;b+=17)
        {
            bool contact=NavalShipContactMath.Overlap(0,0,a,14,15,b);
            Check(contact==NavalShipContactMath.Overlap(14,15,b,0,0,a),"Contact symmetry");
            Check(contact==NavalShipContactMath.Overlap(100,-40,a,114,-25,b),"Translation invariance");
        }
        Console.WriteLine("PASS: side, bow, crossing, corners and symmetric rotated hull contact.");
    }
}
