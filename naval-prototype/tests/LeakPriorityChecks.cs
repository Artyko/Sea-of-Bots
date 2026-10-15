using System;
class LeakPriorityChecks
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static void Main()
    {
        float now=100f;
        float recent=NavalLeakPriority.Score(0f,99f,0.1f,now);
        Check(recent>0f,"A short wave between scans remains repairable");
        Check(NavalLeakPriority.Score(0f,60f,0.1f,now)>0f,"Wave trough does not erase a known leak");
        Check(NavalLeakPriority.Score(0.01f,float.NegativeInfinity,0f,now)>recent,"Active leak has priority");
        Check(NavalLeakPriority.Score(0f,float.NegativeInfinity,0f,now)==0f,"Never wet decorative damage is not a leak job");
        Check(NavalLeakPriority.Score(0f,0f,0.1f,now)==0f,"Long dry hole drops from leak priority");
        Check(NavalRepairSite.AimY(0.65f,BoatDimensions.HoldFloor)==0.65f,"Lower-deck aim stays below ceiling");
        Check(NavalRepairSite.AimY(0.65f,0.825f)>0.825f,"Upper-deck aim reaches exposed seam edge");
        Check(NavalRepairSite.WithinReach((3f-2.1f)*2f,(0.4f-BoatDimensions.HoldFloor-0.8f)*2f,1.4f),"Near-wall work position reaches around a ceiling beam");
        Console.WriteLine("PASS: remembered intermittent leaks, priority, expiry and deck-aware reach.");
    }
}
