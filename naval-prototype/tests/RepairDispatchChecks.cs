using System;
class RepairDispatchChecks
{
    static void Check(bool value,string name){if(!value)throw new Exception(name);}
    static void Main()
    {
        Check(!NavalRepairDispatch.CanRecall(true,true,true,0),"active accepted patch is not interrupted even if another repairer exists");
        Check(!NavalRepairDispatch.CanRecall(true,false,false,0),"wave trough cannot cancel accepted patch");
        Check(!NavalRepairDispatch.CanRecall(false,true,false,0),"sole repairer cannot leave known leaks for a gun");
        Check(NavalRepairDispatch.CanRecall(false,true,true,0),"last emergency gunner can return after patch when another repairer remains");
        Check(NavalRepairDispatch.CanRecall(false,false,false,0),"dry stabilized ship releases emergency gunner");
        Check(!NavalRepairDispatch.CanRecall(false,false,true,1.8f),"loaded bucket still must be poured");
        Check(NavalRepairDispatch.Pending(true,true,true,.2f,float.NegativeInfinity,20),"new unclaimed jet is pending without wet history");
        Check(NavalRepairDispatch.Pending(true,true,true,0,18,20),"brief wave trough retains pending duty");
        Check(!NavalRepairDispatch.Pending(true,true,true,0,1,20),"long dry inaccessible hole does not hold a waiting sailor forever");
        Check(!NavalRepairDispatch.Pending(false,true,true,.2f,20,20),"repaired cell releases wait immediately");
        Check(!NavalRepairDispatch.Pending(true,false,true,.2f,20,20),"hole claimed by another sailor does not demand a duplicate waiting worker");
        Check(!NavalRepairDispatch.Pending(true,true,false,.2f,20,20),"irreparable equipment never holds a repairer");
        Check(NavalRepairDispatch.RetryDelay(true)==1f&&NavalRepairDispatch.RetryDelay(false)==3f,"live blocked jet receives short retry");
        foreach(float floor in new float[]{BoatDimensions.HoldFloor,NavalShipDefinition.Brigantine.HoldFloor})
        {
            Check(NavalRepairDispatch.PreferCurrentDeck(floor,floor,false),"hold floor stays first choice");
            Check(NavalRepairDispatch.PreferCurrentDeck(.825f,floor,false),"upper deck stays first choice");
            Check(!NavalRepairDispatch.PreferCurrentDeck(floor,floor,true),"stair position is not treated as a safe work floor");
            Check(!NavalRepairDispatch.PreferCurrentDeck((floor+.825f)*.5f,floor,false),"midair height is not a work floor");
        }
        NavalRepairProgress patch=new NavalRepairProgress();
        for(int step=0;step<80;step++)
        {
            bool crest=step%20<3;
            patch.StepAccepted(.05f,true);
            Check(!NavalRepairDispatch.CanRecall(patch.Elapsed<4f,crest,true,0)||patch.Elapsed>=4f,"accepted patch survives recall requests across waves");
        }
        Check(patch.Elapsed>=3.99f,"patch accumulates four seconds through troughs");
        float remembered=NavalLeakPriority.Score(0,9,.2f,10);
        Check(NavalMaintenanceMath.FreeWorkersNeeded(remembered>0,false,false,false)==1,"remembered wave leak still reserves a repair hand");
        Check(!NavalCombatEmergency.ReleaseGunner(1,false),"last regular gunner remains protected outside critical emergency");
        Console.WriteLine("PASS: accepted patch continuity, recall handoff, nearby pending jets, quick retries, deck selection, wave staffing and last-gunner protection.");
    }
}
