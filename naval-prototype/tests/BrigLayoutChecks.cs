using System;
class BrigLayoutChecks
{
    static void Check(bool value,string label){if(!value)throw new Exception(label);}
    static void Near(float a,float b){Check(Math.Abs(a-b)<.0001f,"Dimension mismatch "+a+" / "+b);}
    static void Main()
    {
        NavalShipDefinition old=NavalShipDefinition.Sloop,brig=NavalShipDefinition.Brigantine;
        foreach(float y in new float[]{1.025f,1.4f,1.775f,2.25f})
            Near((brig.AboveDeckY(y)-.825f)*brig.Scale,(old.AboveDeckY(y)-.825f)*old.Scale);
        Near(brig.FixtureScale*brig.Scale,old.Scale);
        Near(brig.LadderZ,0f);
        for(int gun=0;gun<brig.GunCount;gun++)Check(Math.Abs(brig.LadderZ-NavalShipDefinition.GunZ(gun))>=1f,"Sea ladder no longer directly opposite cannon");
        float passage=(NavalCompartmentMath.WallX-.07f-brig.StairWidth*.5f)*brig.Scale;
        Check(passage>2.4f,"Both lower side passages have camera/body clearance");
        Check(!NavalWalkRoutes.InStairwell(.95f,-.5f,-1f,brig.StairWidth*.5f+.175f),"Side passage is not mistaken for staircase");
        foreach(float side in new float[]{-1f,1f})foreach(float end in new float[]{-1f,1f})
        {
            var start=new NavalWalkPoint(0f,BoatDimensions.HoldFloor,-3.65f);
            var route=NavalRepairRoutes.Plan(start,new NavalWalkPoint(side*2f,BoatDimensions.HoldFloor,end*4.8f),true,brig.StairWidth);
            NavalWalkPoint previous=start;
            foreach(var point in route)
            {
                if(Math.Abs(point.X)<NavalCompartmentMath.WallX&&point.Z> -3f&&point.Z<.5f)
                    Check(Math.Abs(point.X)>brig.StairWidth*.5f+.2f,"Route to distant doors clears stair underside");
                previous=point;
            }
        }
        Console.WriteLine("PASS: old rail/wheel physical height, centered ladder, wide stair passages and all four door approaches.");
    }
}
