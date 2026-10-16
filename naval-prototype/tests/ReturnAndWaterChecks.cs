using System;
class ReturnAndWaterChecks
{
    static void Check(bool value,string label){if(!value)throw new Exception(label);}
    static void Near(float actual,float expected){Check(Math.Abs(actual-expected)<0.0001f,"Water level "+actual+" expected "+expected);}
    static void Main()
    {
        Near(NavalInteriorWaterMath.WorldLevel(10f,-2f,-1.8f,.5f),10.4f);
        Near(NavalInteriorWaterMath.WorldLevel(10f,-1f,-1.8f,.5f),8.4f);
        Near(NavalInteriorWaterMath.WorldLevel(3f,-1f,-.5f,1f),3.5f);
        Check(float.IsNegativeInfinity(NavalInteriorWaterMath.WorldLevel(0f,0f,0f,-.5f)),"Capsized local surface does not lift swimmers");
        Check(NavalRouteArrival.Reached(0f,.7f,-4.75f,0f,.825f,-4.8f,true),"Controller floor offset permits return completion");
        Check(!NavalRouteArrival.Reached(0f,-2.225f,-4.8f,0f,.825f,-4.8f,true),"Wrong deck cannot complete return");
        Check(!NavalRouteArrival.Reached(1f,.825f,-4.8f,0f,.825f,-4.8f,true),"Distant robot cannot finish return");
        var route=NavalRepairRoutes.Plan(new NavalWalkPoint(-1.65f,BoatDimensions.HoldFloor,2f),new NavalWalkPoint(0f,.825f,-4.8f));
        bool climbed=false,exited=false;NavalWalkPoint previous=new NavalWalkPoint(-1.65f,BoatDimensions.HoldFloor,2f);
        foreach(var p in route)
        {
            if(p.Y>.8f)climbed=true;
            if(exited&&p.Y>.8f&&previous.Y>.8f)
            {
                for(int i=0;i<=20;i++)
                {
                    float t=i/20f,x=previous.X+(p.X-previous.X)*t,z=previous.Z+(p.Z-previous.Z)*t;
                    Check(!(Math.Abs(x)<.95f&&z> -3f&&z<.5f),"Return path must not cross stair opening");
                }
            }
            if(climbed&&p.Z>=.8f&&p.Y>.8f)exited=true;
            previous=p;
        }
        Check(climbed,"Hold return climbs stairs");
        Console.WriteLine("PASS: return arrival, safe helm return and tilted local flood surface.");
    }
}
