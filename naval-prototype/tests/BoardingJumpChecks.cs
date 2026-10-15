using System;
class BoardingJumpChecks
{
    static void Check(bool ok,string name){if(!ok)throw new Exception(name);}
    static void Main()
    {
        float from=1.76f,to=1.76f,rail=4.5f;
        float height=NavalBoardingJumpMath.Height(from,to,rail,rail);
        Check(from+NavalBoardingJumpMath.Lift(0.18f,height)>rail+0.3f,"Outbound jump clears departure rail");
        Check(to+NavalBoardingJumpMath.Lift(0.82f,height)>rail+0.3f,"Outbound jump clears destination rail");
        Check(NavalBoardingJumpMath.Lift(0f,height)==0f&&Math.Abs(NavalBoardingJumpMath.Lift(1f,height))<0.00001f,"Jump starts and ends on deck");
        Check(NavalBoardingJumpMath.Reachable(7f,height),"Nearby ships reachable");
        Check(!NavalBoardingJumpMath.Reachable(8.1f,height),"No impossible distant jump");
        float outbound=NavalBoardingJumpMath.Height(1.76f,3f,4.5f,5.74f);
        float back=NavalBoardingJumpMath.Height(3f,1.76f,5.74f,4.5f);
        Check(Math.Abs(outbound-back)<0.00001f,"Reverse crossing uses symmetric clearance");
        Check(1.76f*0.82f+3f*0.18f+NavalBoardingJumpMath.Lift(0.18f,outbound)>4.8f,"Uneven decks clear source rail");
        Check(1.76f*0.18f+3f*0.82f+NavalBoardingJumpMath.Lift(0.82f,outbound)>6.04f,"Uneven decks clear target rail");
        Check(!NavalBoardingJumpMath.Reachable(4f,9f),"Excessive rail height rejected");
        Console.WriteLine("PASS: rails at both ends, reverse jump, uneven decks, endpoints and range.");
    }
}
