using System;
class SwimPathChecks
{
    static void Check(bool ok,string name){if(!ok)throw new Exception(name);}
    static void Main()
    {
        NavalSwimPoint next;
        float direct=NavalSwimPath.Route(new NavalSwimPoint(5f,0f),new NavalSwimPoint(3.7f,3.8f),out next);
        Check(Math.Abs(next.X-3.7f)<0.001f&&Math.Abs(next.Z-3.8f)<0.001f,"Direct ladder on same side");
        NavalSwimPoint current=new NavalSwimPoint(-5f,0f),goal=new NavalSwimPoint(3.7f,3.8f);
        float detour=NavalSwimPath.Route(current,goal,out next);
        Check(detour>direct&&Math.Abs(next.Z)>=6.5f,"Opposite ladder requires going around hull");
        for(int i=0;i<8&&NavalSwimPath.Distance(current,goal)>0.01f;i++)
        {
            NavalSwimPath.Route(current,goal,out next);
            Check(NavalSwimPath.Clear(current,next),"Every route segment stays outside hull");current=next;
        }
        Check(NavalSwimPath.Distance(current,goal)<0.01f,"Route reaches ladder");
        float close=NavalSwimPath.Route(new NavalSwimPoint(3.3f,3.8f),goal,out next);
        Check(close<float.MaxValue&&next.X>3.45f,"Swimmer near hull first clears safety margin");
        var narrowLadder=new NavalSwimPoint(1.6f,3.8f);
        current=new NavalSwimPoint(-5f,0f);
        for(int i=0;i<12&&NavalSwimPath.Distance(current,narrowLadder)>.01f;i++)
        {
            float length=NavalSwimPath.RouteToLadder(current,narrowLadder,2.4f,.1f,out next);
            Check(length<float.MaxValue,"Tapered hull ladder has a finite approach");
            current=next;
        }
        Check(NavalSwimPath.Distance(current,narrowLadder)<.01f,"Swimmer reaches a ladder inside the detour rectangle");
        Check(!NavalSwimPath.Clear(new NavalSwimPoint(-5f,0f),new NavalSwimPoint(5f,0f)),"No swimming straight through ship");
        Console.WriteLine("PASS: nearest-side approach, hull detour, route completion and safety margin escape.");
    }
}
