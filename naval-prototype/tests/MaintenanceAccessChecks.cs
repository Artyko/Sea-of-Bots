using System;
class MaintenanceAccessChecks
{
    static void Check(bool ok,string name){if(!ok)throw new Exception(name);}
    static void Main()
    {
        float floor=BoatDimensions.HoldFloor;
        foreach(float scale in new float[]{2f,3f})foreach(float x in new float[]{-2.5f,0f,2.5f})
        {
            float workX=Math.Max(-2.35f,Math.Min(2.35f,x));
            float horizontal=(float)Math.Sqrt((x-workX)*(x-workX)+.5f*.5f)*scale;
            float stand=NavalRepairAccessMath.StandY(.4f,scale,horizontal);
            float vertical=(.4f-stand-.8f)*scale;
            Check(horizontal*horizontal+vertical*vertical<=4.8f*4.8f,"Bow ceiling hole reachable without extending repair range");
            if(scale==3f)Check(stand>floor+.15f,"High brigantine repair uses raised support");
            else Check(stand==floor,"Sloop repair does not require unnecessary support");
            Check(stand+3f/scale<.575f,"Raised repair has room below ceiling");
        }
        Check(float.IsPositiveInfinity(NavalRepairAccessMath.StandY(.4f,3f,5f)),"No remote repair beyond horizontal reach");
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
        Console.WriteLine("PASS: bow ceiling repair at both scales, bounded reach, platform headroom, parallel crew duties, unavailable-repair fallback and clear pump approach.");
    }
}
