using System;
using System.Collections.Generic;
public static class NavalRepairRoutes
{
    private static void Add(List<NavalWalkPoint> route,float x,float y,float z)
    {
        if(route.Count>0){NavalWalkPoint last=route[route.Count-1];if(Math.Abs(last.X-x)+Math.Abs(last.Y-y)+Math.Abs(last.Z-z)<0.01f)return;}
        route.Add(new NavalWalkPoint(x,y,z));
    }
    public static List<NavalWalkPoint> Plan(NavalWalkPoint start,NavalWalkPoint goal,bool compartments=false,float stairWidth=1.85f,float holdFloor=BoatDimensions.HoldFloor,float lowerCorridor=1.65f,float upperCorridor=1.4f)
    {
        if(!compartments)return OpenPlan(start,goal,stairWidth,holdFloor,lowerCorridor,upperCorridor);
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
        foreach(NavalWalkPoint p in OpenPlan(start,destination,stairWidth,holdFloor,lowerCorridor,upperCorridor))
        {float x=p.Y<0.3f?Math.Max(-.95f,Math.Min(.95f,p.X)):p.X;Add(result,x,p.Y,p.Z);}
        if(room)
        {
            float side=goal.X<0f?-1f:1f;Add(result,side*2f,holdFloor,destination.Z);
            Add(result,side*2f,holdFloor,goal.Z);Add(result,goal.X,goal.Y,goal.Z);
        }
        // Corridor points are clamped away from longitudinal bulkheads, but a
        // legitimate work post in the open centre lane must remain the exact
        // endpoint requested by the duty controller.
        else Add(result,goal.X,goal.Y,goal.Z);
        return result;
    }
    private static List<NavalWalkPoint> OpenPlan(NavalWalkPoint start,NavalWalkPoint goal,float stairWidth,float holdFloor,float lowerCorridor,float upperCorridor)
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
                if(Math.Abs(start.X)<1.1f&&start.Z>=-3f&&start.Z<NavalWalkRoutes.TopLandingZ)Add(route,0f,0.825f,NavalWalkRoutes.TopLandingZ);
                else Add(route,side*1.4f,0.825f,start.Z);
                Add(route,side*1.4f,0.825f,NavalWalkRoutes.TopLandingZ);
                foreach(NavalWalkPoint p in NavalWalkRoutes.Down(holdFloor))Add(route,p.X,p.Y,p.Z);
            }
            else
            {
                Add(route,side*lowerCorridor,holdFloor,start.Z);
                // Do not run the side lane all the way into the aft stair
                // landing.  Damage and the neighbouring store can turn the
                // old (side,-3.65) pin into a dead end.  Approach on intact
                // deck, then enter the centred landing diagonally.
                Add(route,side*lowerCorridor,holdFloor,NavalWalkRoutes.BottomApproachZ);
                Add(route,0f,holdFloor,NavalWalkRoutes.BottomLandingZ);
                foreach(NavalWalkPoint p in NavalWalkRoutes.UpFrom(holdFloor,NavalWalkRoutes.BottomLandingZ,holdFloor))Add(route,p.X,p.Y,p.Z);
            }
            start=route[route.Count-1];
        }
        float floor=upper?0.825f:holdFloor,corridor=upper?upperCorridor:lowerCorridor;
        float nextSide=goal.X<0f?-1f:1f;
        side=start.X< -0.1f?-1f:start.X>0.1f?1f:nextSide;
        if(upper&&Math.Abs(start.X)<1.1f&&start.Z>=-3f&&start.Z<NavalWalkRoutes.TopLandingZ)
        {Add(route,0f,floor,NavalWalkRoutes.TopLandingZ);start=new NavalWalkPoint(0f,floor,NavalWalkRoutes.TopLandingZ);}
        bool fromBottomLanding=upper&&Math.Abs(start.X)<.1f&&Math.Abs(start.Z-NavalWalkRoutes.BottomLandingZ)<.1f;
        if(fromBottomLanding){Add(route,0f,floor,NavalWalkRoutes.BottomApproachZ);start=new NavalWalkPoint(0f,floor,NavalWalkRoutes.BottomApproachZ);}
        Add(route,side*corridor,floor,start.Z);
        if(side!=nextSide)
        {
            float cross=upper?NavalWalkRoutes.TopLandingZ:.8f;
            if(!upper&&Math.Abs(start.Z-NavalWalkRoutes.BottomLandingZ)+Math.Abs(goal.Z-NavalWalkRoutes.BottomLandingZ)<Math.Abs(start.Z-0.8f)+Math.Abs(goal.Z-0.8f))cross=NavalWalkRoutes.BottomLandingZ;
            Add(route,side*corridor,floor,cross);Add(route,nextSide*corridor,floor,cross);
        }
        bool toBottomLanding=upper&&Math.Abs(goal.X)<.1f&&Math.Abs(goal.Z-NavalWalkRoutes.BottomLandingZ)<.1f;
        Add(route,nextSide*corridor,floor,toBottomLanding?NavalWalkRoutes.BottomApproachZ:goal.Z);Add(route,goal.X,goal.Y,goal.Z);
        return route;
    }
    public static float TravelCost(float x,float y,float z,float targetX,float targetY,float targetZ)
    {
        bool sameDeck=(y>0.3f)==(targetY>0.3f);
        float cost=Math.Abs(x-targetX)+Math.Abs(z-targetZ);
        if(!sameDeck)cost+=7f+Math.Abs(z-NavalWalkRoutes.BottomLandingZ);
        return cost;
    }
    public static float Priority(float leakPriority,float travelCost)
    {return leakPriority/(1f+Math.Max(0f,travelCost)*0.35f);}
}
