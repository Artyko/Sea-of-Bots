using System;
class MaintenanceEscalationChecks
{
    static void Check(bool ok,string name){if(!ok)throw new Exception(name);}
    static void Main()
    {
        NavalMaintenanceEscalation state=new NavalMaintenanceEscalation();
        for(int i=0;i<100;i++)state.Step(.05f,.01f+i*.00001f,5,false);
        Check(!state.Parallel,"minor slow leak and small heel keep one worker");
        Check(NavalMaintenanceMath.FreeWorkersNeeded(true,false,true,false)==1,"one hand handles leak then pump");
        Check(NavalMaintenanceMath.FreeWorkersNeeded(true,true,true,false)==0,"accepted repair does not call a second pump hand");
        Check(!NavalMaintenanceMath.NeedPump(true,true,true),"no parallel pump while another sailor patches in calm water");
        Check(NavalMaintenanceMath.NeedPump(false,false,false),"sole worker can pump after patching");
        Check(NavalMaintenanceMath.NeedPump(true,false,false),"inaccessible leak still permits sole worker to pump");
        state.Step(.05f,.25f,5,false);Check(state.Parallel,"substantial water calls assistance");
        Check(NavalMaintenanceMath.FreeWorkersNeeded(true,false,true,false,false,false,false,false,state.Parallel)==2,"danger staffs patch and pump simultaneously");
        state.Step(.05f,.2f,9,false);Check(state.Parallel,"do not release help immediately at threshold");
        for(int i=0;i<240;i++)state.Step(.05f,.15f,7,false);
        Check(!state.Parallel,"stable low water and heel release extra hands");
        NavalMaintenanceEscalation heel=new NavalMaintenanceEscalation();heel.Step(.05f,.1f,12,false);Check(heel.Parallel,"meaningful water and heel call help");
        NavalMaintenanceEscalation wave=new NavalMaintenanceEscalation();wave.Step(.05f,.01f,25,false);Check(!wave.Parallel,"nearly dry wave roll does not strip guns");
        NavalMaintenanceEscalation growth=new NavalMaintenanceEscalation();growth.Step(.05f,.05f,2,false);
        for(int i=1;i<=80;i++)growth.Step(.05f,.05f+i*.001f,2,false);
        Check(growth.Parallel,"rapid actual water growth escalates before volume/heel limits");
        NavalMaintenanceEscalation emergency=new NavalMaintenanceEscalation();emergency.Step(.05f,.01f,0,true);Check(emergency.Parallel,"critical policy always permits extra hands");
        Check(!NavalCombatEmergency.ReleaseGunner(1,false),"moderate escalation still protects final gunner");
        Check(NavalMaintenanceMath.FreeWorkersNeeded(true,false,false,false,false,false,true,false)==1,"minor pump damage and leak handled sequentially");
        Check(NavalMaintenanceMath.FreeWorkersNeeded(true,false,true,false,true,false)>=2,"critical state retains emergency simultaneous staffing");
        Console.WriteLine("PASS: sequential patch/pump, no duplicate staffing, volume/heel/growth escalation, hysteresis, inaccessible repair fallback and last-gunner protection.");
    }
}
