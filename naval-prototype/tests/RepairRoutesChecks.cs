using System;
using System.Collections.Generic;
class RepairRoutesChecks
{
    static void Check(bool ok,string name){if(!ok)throw new Exception(name);}
    static float Length(NavalWalkPoint start,List<NavalWalkPoint> route)
    {
        float total=0f;foreach(NavalWalkPoint p in route)
        {float x=p.X-start.X,y=p.Y-start.Y,z=p.Z-start.Z;total+=(float)Math.Sqrt(x*x+y*y+z*z);start=p;}return total;
    }
    static void Main()
    {
        float floor=BoatDimensions.HoldFloor;
        NavalWalkPoint from=new NavalWalkPoint(-2.1f,floor,2f),to=new NavalWalkPoint(-2.1f,floor,3f);
        var same=NavalRepairRoutes.Plan(from,to);
        foreach(var p in same)Check(p.Y==floor,"Adjacent hold jobs never return to upper deck");
        Check(Length(from,same)<2f,"Adjacent jobs use a short local route");
        var cross=NavalRepairRoutes.Plan(new NavalWalkPoint(-2.1f,floor,3f),new NavalWalkPoint(2.1f,floor,4f));
        foreach(var p in cross)Check(p.Y==floor,"Cross-hold jobs stay in hold");
        for(int i=1;i<cross.Count;i++)
            if(cross[i-1].X*cross[i].X<0f)Check(cross[i].Z>=0.8f||cross[i].Z<=-3.65f,"Cross-hold route avoids underside of stairs");
        var down=NavalRepairRoutes.Plan(new NavalWalkPoint(0f,-0.7f,-1.4f),new NavalWalkPoint(-2.1f,floor,0f));
        bool landed=false;float previous=-0.7f;
        foreach(var p in down)
        {
            if(!landed){Check(p.X==0f,"Interrupted descent finishes before sideways turn");Check(p.Y<=previous+0.03f,"Continuing down does not go back up");previous=p.Y;}
            if(p.Y==floor)landed=true;
        }
        Check(landed,"Interrupted descent reaches lower landing");
        var up=NavalRepairRoutes.Plan(new NavalWalkPoint(0f,-0.7f,-1.4f),new NavalWalkPoint(2.1f,0.825f,-1f));
        foreach(var p in up)if(p.Y<0.78f)Check(p.X==0f,"Interrupted ascent stays centred");
        Check(NavalRepairRoutes.Priority(1.1f,2f)>NavalRepairRoutes.Priority(1.1f,14f),"Nearby equivalent leak selected first");
        Check(NavalRepairRoutes.TravelCost(1.6f,floor,2f,2f,0.825f,3f)>NavalRepairRoutes.TravelCost(1.6f,floor,2f,2f,floor,3f),"Deck changes cost more than local repair");
        Check(NavalCrewRoles.CanRepair(true,0,-1)&&NavalCrewRoles.CanRepair(true,1,-1),"Two available sailors can repair separate patches");
        Check(!NavalCrewRoles.CanRepair(true,0,0)&&NavalCrewRoles.CanRepair(true,1,0),"Active gunner preserved");
        Check(!NavalCrewRoles.CanRepair(false,1,-1),"Unavailable sailor cannot receive repair job");
        Console.WriteLine("PASS: local repair chains, safe deck changes, mid-stair rerouting, nearby priority and parallel dispatch.");
    }
}
