using System;
class MaintenanceAccessChecks
{
    static void Check(bool ok,string name){if(!ok)throw new Exception(name);}
    static void Main()
    {
        foreach(NavalShipDefinition ship in new NavalShipDefinition[]{NavalShipDefinition.Sloop,NavalShipDefinition.Brigantine})
        {
            float floor=ship.HoldFloor;
            foreach(float holeX in new float[]{-2.5f,0f,2.5f})
            {
                float workX=Math.Max(-2.35f,Math.Min(2.35f,holeX));
                float horizontal=(float)Math.Sqrt((holeX-workX)*(holeX-workX)+.5f*.5f)*ship.Scale;
                float vertical=(.55f-floor-.8f)*ship.Scale;
                Check(horizontal*horizontal+vertical*vertical<4.8f*4.8f,"Ceiling holes reachable from lower deck without a repair ladder");
            }
            Check(ship.HoldClearance>=3.5f,"Player camera and robot have comfortable headroom");
        }
        Check(NavalMaintenanceMath.FreeWorkersNeeded(true,false,true,false)==2,"Repair and pump each need a sailor");
        Check(NavalMaintenanceMath.FreeWorkersNeeded(true,true,true,false)==1,"Existing repairer leaves one pump vacancy");
        Check(NavalMaintenanceMath.FreeWorkersNeeded(true,true,true,true)==0,"Assigned jobs do not reserve extra sailors");
        Check(NavalMaintenanceMath.NeedPump(true,false,false,10f,100f),"Unreachable leak cannot block pump");
        Check(!NavalMaintenanceMath.NeedPump(true,false,true,10f,100f),"Accessible leak gets first repairer");
        Check(NavalMaintenanceMath.NeedPump(true,true,true,10f,100f),"Second sailor can pump during repairs");
        Check(NavalMaintenanceMath.NeedPump(true,false,true,150f,100f),"Dangerous flooding permits urgent pump");
        float pumpFront=NavalMaintenanceMath.PumpOffsetZ+.25f;
        Check(-pumpFront*3f>.38f,"Pump operator stands clear of pump body");
        for(int i=0;i<=50;i++)
        {
            float z=-3.65f+(-4.9f+3.65f)*i/50f;
            Check(z> -4.9f+pumpFront+.38f/3f,"Approach from stairs does not cross pump casing");
        }
        Console.WriteLine("PASS: ceiling repairs from lower deck on both profiles, camera headroom, parallel crew duties, unavailable-repair fallback and clear pump approach.");
    }
}
