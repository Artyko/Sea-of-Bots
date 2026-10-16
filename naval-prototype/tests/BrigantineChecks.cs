using System;
class BrigantineChecks
{
    static void Check(bool ok,string name){if(!ok)throw new Exception(name);}
    static void Near(float a,float b){Check(Math.Abs(a-b)<.003f,"Volume mismatch: "+a+" / "+b);}
    static void Main()
    {
        var stationState=new NavalStationState();stationState.PressInteract(4,false,4);Check(stationState.Active==4,"Fourth gun can be operated by player");
        var old=NavalShipDefinition.Sloop;var brig=NavalShipDefinition.Brigantine;
        Check((.825f-BoatDimensions.HoldFloor)*brig.Scale/NavalWalkRoutes.StepCount<.25f,"Brig stairs stay below controller step height");
        Check(old.Scale==2f&&old.GunCount==2&&old.MastZ.Length==1&&old.Compartments,"Sloop size, guns and crew retained with shared compartments");
        Check(brig.Scale==3f&&brig.GunCount==4&&brig.CrewCount==4&&brig.MastZ.Length==2&&brig.Compartments,"Brigantine profile");
        Check(NavalShipDefinition.GunSide(0)==NavalShipDefinition.GunSide(2)&&NavalShipDefinition.GunZ(0)!=NavalShipDefinition.GunZ(2),"Two distinct guns per broadside");
        Check(Math.Abs(NavalShipDefinition.HelmZ(brig)-brig.MastZ[1])>1f,"Helm clear of aft mast");
        var grid=new NavalFloodGrid(12,24,BoatDimensions.HoldWidth,BoatDimensions.HoldLength,brig.Scale,4f);
        float[] floors=new float[grid.Water.Length];
        for(int i=0;i<grid.Water.Length;i++)
        {
            floors[i]=grid.X(i)*.2f;
            int region=NavalCompartmentMath.Region(grid.X(i),grid.Z(i));
            if(i%grid.Columns+1<grid.Columns&&region!=NavalCompartmentMath.Region(grid.X(i+1),grid.Z(i+1)))grid.RightSill[i]=float.PositiveInfinity;
            if(i/grid.Columns+1<grid.Rows&&region!=NavalCompartmentMath.Region(grid.X(i+grid.Columns),grid.Z(i+grid.Columns)))grid.ForwardSill[i]=float.PositiveInfinity;
        }
        int source=grid.Index(-2.2f,-2.625f);grid.Add(source,20f);
        for(int step=0;step<300;step++)grid.Spread(.05f,floors,1f);
        Near(grid.Total,20f);
        for(int i=0;i<grid.Water.Length;i++)if(NavalCompartmentMath.Region(grid.X(i),grid.Z(i))!=1)Near(grid.Water[i],0f);
        int doorway=grid.Index(-1.65f,-NavalCompartmentMath.DoorZ);grid.RightSill[doorway]=float.NegativeInfinity;
        for(int step=0;step<300;step++)grid.Spread(.05f,floors,1f);
        float corridor=0f;for(int i=0;i<grid.Water.Length;i++)if(NavalCompartmentMath.Region(grid.X(i),grid.Z(i))==0)corridor+=grid.Water[i];
        Check(corridor>0f,"Open door admits real flow");Near(grid.Total,20f);
        var pair=new NavalFloodGrid(2,1,2f,1f,1f,4f);pair.Add(0,.4f);pair.RightSill[0]=1f;
        pair.Spread(1f,new float[2],1f);Near(pair.Water[1],0f);
        pair.RightSill[0]=0f;pair.Spread(1f,new float[2],1f);Check(pair.Water[1]>0f,"Low breach passes water, high dry breach does not");
        var balance=new NavalFloodGrid(12,24,5.75f,11.75f,3f,4f);int from=balance.Index(-2.2f,-3f),to=balance.Index(2.2f,-3f);
        balance.Add(from,8f);float before=balance.Total;float moved=NavalCompartmentMath.Move(balance,from,to,1.8f);
        Near(moved,1.8f);Near(balance.Total,before);Check(Math.Abs(balance.Side(true)-balance.Side(false))<8f,"Cross pumping reduces heel mass difference");
        Check(NavalCompartmentMath.NeedsTransfer(20f,0f,12f)&&!NavalCompartmentMath.NeedsTransfer(20f,19f,12f),"Automatic balance does not overfill equal sides");
        var bucket=new NavalBucketLoad();bucket.Scoop(balance,-2.2f,-3f,1.8f,1.5f);Near(bucket.Amount,1.8f);Near(balance.Total+bucket.Amount,before);
        bucket.Return(balance,-2.2f,-3f);Near(balance.Total,before);
        foreach(float startX in new float[]{-2f,2f})foreach(float startZ in new float[]{-4f,4f})
        {
            NavalWalkPoint previous=new NavalWalkPoint(startX,BoatDimensions.HoldFloor,startZ);
            var route=NavalRepairRoutes.Plan(previous,new NavalWalkPoint(-startX,BoatDimensions.HoldFloor,-startZ),true);
            foreach(var point in route)
            {
                for(int n=1;n<=40;n++)
                {
                    float t=n/40f,x=previous.X+(point.X-previous.X)*t,z=previous.Z+(point.Z-previous.Z)*t;
                    float lastT=(n-1)/40f,px=previous.X+(point.X-previous.X)*lastT,pz=previous.Z+(point.Z-previous.Z)*lastT;
                    if(NavalCompartmentMath.Region(x,z)!=NavalCompartmentMath.Region(px,pz))
                        Check(Math.Abs(Math.Abs(z)-NavalCompartmentMath.DoorZ)<.65f,"Room crossings go through doors");
                }
                previous=point;
            }
        }
        Check(NavalShipContactMath.Overlap(0f,0f,0f,18f,0f,0f,1.5f,1.5f),"Large hull collision uses profile scale");
        Check(!NavalShipContactMath.Overlap(0f,0f,0f,18f,0f,0f),"Old hull size remains unchanged");
        Check(NavalShipContactMath.Overlap(0f,0f,0f,15f,0f,0f,1f,1.5f),"Mixed hull collisions supported");
        Console.WriteLine("PASS: both ship profiles, gun layout, sealed compartments, door and breach flow, conservation, balance transfer, 1.8m3 bucket, door routes, mixed-scale collision.");
    }
}
