using System;
using System.Collections.Generic;
public static class NavalRepairRoutes
{
    private static void Add(List<NavalWalkPoint> route,float x,float y,float z)
    {
        if(route.Count>0){NavalWalkPoint last=route[route.Count-1];if(Math.Abs(last.X-x)+Math.Abs(last.Y-y)+Math.Abs(last.Z-z)<0.01f)return;}
        route.Add(new NavalWalkPoint(x,y,z));
    }
    public static List<NavalWalkPoint> Plan(NavalWalkPoint start,NavalWalkPoint goal,bool compartments=false,float stairWidth=1.85f,float holdFloor=BoatDimensions.HoldFloor)
    {
        if(!compartments)return OpenPlan(start,goal,stairWidth,holdFloor);
        List<NavalWalkPoint> result=new List<NavalWalkPoint>();
        if(start.Y<.3f&&goal.Y<.3f&&NavalCompartmentMath.Region(start.X,start.Z)!=0&&NavalCompartmentMath.Region(start.X,start.Z)==NavalCompartmentMath.Region(goal.X,goal.Z))
        {
            float x=start.X<0f?-2f:2f;Add(result,x,holdFloor,start.Z);Add(result,x,holdFloor,goal.Z);Add(result,goal.X,goal.Y,goal.Z);return result;
        }
        if(start.Y<0.3f&&NavalCompartmentMath.Region(start.X,start.Z)!=0)
        {
            float side=start.X<0f?-1f:1f,z=start.Z<0f?-NavalCompartmentMath.DoorZ:NavalCompartmentMath.DoorZ;
            Add(result,side*2f,holdFloor,start.Z);Add(result,side*2f,holdFloor,z);
            Add(result,side*.95f,holdFloor,z);start=result[result.Count-1];
        }
        NavalWalkPoint destination=goal;
        bool room=goal.Y<0.3f&&NavalCompartmentMath.Region(goal.X,goal.Z)!=0;
        if(room)destination=new NavalWalkPoint(goal.X<0f?-.95f:.95f,holdFloor,goal.Z<0f?-NavalCompartmentMath.DoorZ:NavalCompartmentMath.DoorZ);
        foreach(NavalWalkPoint p in OpenPlan(start,destination,stairWidth,holdFloor))
        {float x=p.Y<0.3f?Math.Max(-.95f,Math.Min(.95f,p.X)):p.X;Add(result,x,p.Y,p.Z);}
        if(room)
        {
            float side=goal.X<0f?-1f:1f;Add(result,side*2f,holdFloor,destination.Z);
            Add(result,side*2f,holdFloor,goal.Z);Add(result,goal.X,goal.Y,goal.Z);
        }
        return result;
    }
    private static List<NavalWalkPoint> OpenPlan(NavalWalkPoint start,NavalWalkPoint goal,float stairWidth,float holdFloor)
    {
        List<NavalWalkPoint> route=new List<NavalWalkPoint>();
        bool upper=goal.Y>0.3f;
        if(NavalWalkRoutes.InStairwell(start.X,start.Y,start.Z,stairWidth*.5f+.175f,holdFloor))
        {
            Add(route,0f,start.Y,start.Z);
            if(upper)
                foreach(NavalWalkPoint p in NavalWalkRoutes.UpFrom(start.Y,start.Z,holdFloor))Add(route,p.X,p.Y,p.Z);
            else
                foreach(NavalWalkPoint p in NavalWalkRoutes.Down(holdFloor))
                    if(p.Y<=start.Y+0.03f&&p.Z<=start.Z+0.1f)Add(route,p.X,p.Y,p.Z);
            start=route[route.Count-1];
        }
        bool fromUpper=start.Y>0.3f;
        float side=start.X< -0.1f?-1f:start.X>0.1f?1f:goal.X<0f?-1f:1f;
        if(fromUpper!=upper)
        {
            if(fromUpper)
            {
                if(Math.Abs(start.X)<1.1f&&start.Z>=-3f&&start.Z<0.8f)Add(route,0f,0.825f,0.8f);
                else Add(route,side*1.4f,0.825f,start.Z);
                Add(route,side*1.4f,0.825f,0.8f);
                foreach(NavalWalkPoint p in NavalWalkRoutes.Down(holdFloor))Add(route,p.X,p.Y,p.Z);
            }
            else
            {
                Add(route,side*1.65f,holdFloor,start.Z);
                Add(route,side*1.65f,holdFloor,-3.65f);
                Add(route,0f,holdFloor,-3.65f);
                foreach(NavalWalkPoint p in NavalWalkRoutes.UpFrom(holdFloor,-3.65f,holdFloor))Add(route,p.X,p.Y,p.Z);
            }
            start=route[route.Count-1];
        }
        float floor=upper?0.825f:holdFloor,corridor=upper?1.4f:1.65f;
        float nextSide=goal.X<0f?-1f:1f;
        side=start.X< -0.1f?-1f:start.X>0.1f?1f:nextSide;
        if(upper&&Math.Abs(start.X)<1.1f&&start.Z>=-3f&&start.Z<0.8f)
        {Add(route,0f,floor,0.8f);start=new NavalWalkPoint(0f,floor,0.8f);}
        Add(route,side*corridor,floor,start.Z);
        if(side!=nextSide)
        {
            float cross=0.8f;
            if(!upper&&Math.Abs(start.Z+3.65f)+Math.Abs(goal.Z+3.65f)<Math.Abs(start.Z-0.8f)+Math.Abs(goal.Z-0.8f))cross=-3.65f;
            Add(route,side*corridor,floor,cross);Add(route,nextSide*corridor,floor,cross);
        }
        Add(route,nextSide*corridor,floor,goal.Z);Add(route,goal.X,goal.Y,goal.Z);
        return route;
    }
    public static float TravelCost(float x,float y,float z,float targetX,float targetY,float targetZ)
    {
        bool sameDeck=(y>0.3f)==(targetY>0.3f);
        float cost=Math.Abs(x-targetX)+Math.Abs(z-targetZ);
        if(!sameDeck)cost+=7f+Math.Abs(z+3.65f);
        return cost;
    }
    public static float Priority(float leakPriority,float travelCost)
    {return leakPriority/(1f+Math.Max(0f,travelCost)*0.35f);}
}
