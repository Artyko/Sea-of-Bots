using System;

class LowerDeckSideRepairChecks
{
    static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
    static void Main()
    {
        NavalShipDefinition ship=NavalShipDefinition.Ironclad;int cases=0;
        Check(Math.Abs(NavalRepairWorkSites.Corridor(ship,0)-1.90f)<.001f,"Steam lower-hull search begins in the unobstructed outer service lane");
        foreach(int side in new int[]{-1,1})
        for(int row=0;row<16;row++)
        foreach(float z in new float[]{-4.8f,-3.7f,-1.4f,0f,1.4f,3.7f,4.8f})
        {
            float holeY=ship.HullY(-2.1f+row*.25f);
            if(holeY>=.575f)continue;
            float half=ship.HullHalfWidth(z),holeX=side*half;
            float targetX=NavalRepairAim.InnerSide(holeX,half,ship.Scale);
            float preferred=NavalRepairSite.Floor(holeY,ship.HoldFloor);
            bool reachable=false;NavalWalkPoint selected=new NavalWalkPoint();
            for(int deck=0;deck<ship.DeckCount&&!reachable;deck++)
            {
                float floor=ship.RepairFloor(preferred,deck);
                for(int corridor=0;corridor<NavalRepairWorkSites.CorridorCountFor(ship);corridor++)
                {
                    float x=side*NavalRepairWorkSites.Corridor(ship,corridor);
                    if(Math.Abs(x)>half-.85f/ship.Scale)continue;
                    float standY=ship.MaintenanceFloorAt(floor)?NavalBallastLayout.Surface(x,z,floor,ship):floor;
                    float aimY=ship.RepairAimY(holeY,standY);
                    float dx=(targetX-x)*ship.Scale,dy=(aimY-standY)*ship.Scale-NavalRepairAim.HandHeight;
                    if(!NavalRepairSite.WithinReach(dx,dy,0f))continue;
                    selected=new NavalWalkPoint(x,standY,z);reachable=true;break;
                }
            }
            Check(reachable,"Unreachable lower armour: side="+side+" z="+z+" y="+holeY);
            var start=new NavalWalkPoint(0,ship.HoldFloor,-4.8f);
            var route=NavalSupplyRoutes.Plan(start,selected,ship);Check(route.Count>0,"No route to reachable side repair point");
            var last=route[route.Count-1];Check(last.X==selected.X&&last.Y==selected.Y&&last.Z==selected.Z,"Side repair route loses exact endpoint");
            cases++;
        }
        Console.WriteLine("PASS: "+cases+" lower armour cells on both ironclad sides, from stern through both wing tanks to bow, have an in-reach routed repair point.");
    }
}
