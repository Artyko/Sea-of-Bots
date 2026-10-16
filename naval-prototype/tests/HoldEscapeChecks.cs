using System;
class HoldEscapeChecks
{
    static void Check(bool value,string name){if(!value)throw new Exception(name);}
    static void Main()
    {
        float floor=BoatDimensions.HoldFloor;
        Check(NavalHoldEscapeRules.InHold(1.65f,floor,2f),"Flooded lower hold uses internal exit");
        Check(NavalHoldEscapeRules.InHold(0f,-.7f,-1.4f),"Sailor on stairs is still inside");
        Check(!NavalHoldEscapeRules.InHold(3.7f,-1f,3.8f),"Outside swimmer uses sea ladder");
        Check(!NavalHoldEscapeRules.InHold(1f,.825f,2f),"Upper deck exits internal mode");
        Check(!NavalHoldEscapeRules.InHold(1f,floor-.7f,2f),"Fall below keel returns to sea rescue");
        foreach(float x in new float[]{-1.65f,1.65f})
        foreach(float z in new float[]{-4.5f,2f,4.5f})
        {
            var route=NavalRepairRoutes.Plan(new NavalWalkPoint(x,floor,z),new NavalWalkPoint(1.65f,.825f,1.1f));
            bool climbed=false;float previous=floor;
            foreach(var p in route)
            {
                Check(Math.Abs(p.X)<BoatDimensions.HalfWidth&&Math.Abs(p.Z)<BoatDimensions.HalfLength,"Escape route stays inside hull");
                if(p.Y>previous+.03f){Check(Math.Abs(p.X)<.01f,"Ascent happens in staircase");climbed=true;}
                previous=p.Y;
            }
            Check(climbed,"Every lower hold route ascends internal staircase");
            var last=route[route.Count-1];Check(last.Y>.8f&&last.Z>.8f,"Escape finishes on upper landing");
        }
        Console.WriteLine("PASS: flooded hold vs open sea classification, internal escape routes from both sides and keel fall.");
    }
}
