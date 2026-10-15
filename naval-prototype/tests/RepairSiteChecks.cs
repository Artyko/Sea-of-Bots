using System;
public static class RepairSiteChecks
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static void Main()
    {
        Check(NavalRepairSite.Upper(0.65f),"Deck seam must be repaired from above");
        Check(!NavalRepairSite.Upper(0.4f),"Opening under deck must be repaired from below");
        Check(NavalRepairSite.Floor(0.65f)==0.825f,"Seam floor");
        Check(NavalRepairSite.AimY(0.65f)>0.825f,"Reach exposed seam edge instead of aiming through floor");
        Check(NavalRepairSite.Floor(0.4f)==BoatDimensions.HoldFloor,"Below deck floor");
        float x=NavalRepairSite.WorkX(3f,0.4f),dy=(0.4f-BoatDimensions.HoldFloor-0.8f)*2f;
        Check(NavalRepairSite.WithinReach((3f-x)*2f,dy,0f),"Top of hold hole must be reachable");
        Check(!NavalRepairSite.WithinReach(5f,0f,0f),"Remote repair excluded");
        Check(Math.Abs(NavalRepairSite.WorkX(-3f,0.65f))<2f,"Work position clear of wall frames");
        NavalRepairProgress progress=new NavalRepairProgress();
        progress.StepAccepted(20f,false);Check(progress.Elapsed==0f,"Travel cannot repair");
        progress.StepAccepted(1f,true);progress.StepAccepted(3f,true);
        Check(progress.Elapsed==4f&&progress.DrySeconds==0f,"Accepted job finishes across dry wave troughs");
        Console.WriteLine("PASS: deck seam approach, lower-deck reach, exposed edge and persistent repair.");
    }
}
