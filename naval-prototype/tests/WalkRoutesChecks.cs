using System;
public static class WalkRoutesChecks
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static void Main()
    {
        var down=NavalWalkRoutes.Down();
        for(int i=1;i<down.Count;i++)
        {Check(down[i].X==0f,"Sideways turn on descent");Check(down[i].Y<=down[i-1].Y,"Descent rises");Check(down[i].Z<down[i-1].Z,"Descent reverses");}
        Check(down[down.Count-1].Y==BoatDimensions.HoldFloor,"Must reach lower deck");
        Check(down[down.Count-1].Z<-3.5f,"Must clear stair before turning");
        Check(NavalWalkRoutes.InStairwell(0f,-0.6f,-1.2f),"Interrupted descent detection");
        Check(!NavalWalkRoutes.InStairwell(1.65f,BoatDimensions.HoldFloor,0f),"Lower deck is not stairwell");
        float y=-0.6f,z=-1.2f;
        foreach(var point in NavalWalkRoutes.UpFrom(y,z))
        {Check(point.X==0f,"Interrupted job turns sideways");Check(point.Y>=y,"Return descends");Check(point.Z>=z,"Return reverses");y=point.Y;z=point.Z;}
        Check(y==0.825f&&z==0.8f,"Return must clear upper landing");
        Console.WriteLine("Walk route checks passed.");
    }
}
