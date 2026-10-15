using System;
class CrewRolesChecks
{
    static void Check(bool value,string name){if(!value)throw new Exception(name);}
    static void Main()
    {
        Check(NavalCrewRoles.RepairCandidate(true,true,0)==1,"Second gunner repairs while first fights");
        Check(NavalCrewRoles.RepairCandidate(true,true,1)==0,"First gunner repairs while second fights");
        Check(NavalCrewRoles.RepairCandidate(true,false,0)==-1,"Sole fighting gunner stays at cannon");
        Check(NavalCrewRoles.RepairCandidate(false,true,0)==1,"Free surviving sailor can repair");
        Check(NavalCrewRoles.HelmCandidate(true,true,0,false)==1,"Free sailor takes helm");
        Check(NavalCrewRoles.HelmCandidate(true,true,1,false)==0,"Helm role follows combat assignment");
        Check(NavalCrewRoles.HelmCandidate(true,true,0,true)==-1,"Player has priority at helm");
        Check(NavalCrewRoles.HelmCandidate(false,false,-1,false)==-1,"Dead or busy crew cannot steer");
        Check(!NavalCrewRoles.HelmUseful(0f,0f),"No drive and no motion releases helmsman");
        Check(NavalCrewRoles.HelmUseful(0f,0.5f),"Coasting ship still benefits from steering");
        Check(NavalCrewRoles.HelmUseful(0.2f,0f),"Partly damaged sail still needs a helmsman");
        Check(NavalCrewRoles.HelmUseful(1f,0f),"Repaired sail enables helm again");
        Check(NavalCrewRoles.HelmUseful(0f,-0.5f),"Reverse coasting still benefits from steering");
        Console.WriteLine("PASS: repair avoids combat gunner, flexible helm assignment and player priority.");
    }
}
