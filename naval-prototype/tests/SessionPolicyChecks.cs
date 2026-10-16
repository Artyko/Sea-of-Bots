using System;
class SessionPolicyChecks
{
 static void Check(bool value,string name){if(!value)throw new Exception(name);}
 static void Main(){
 Check(NavalSessionPolicy.Runs(false,true,true,true,false,true),"Live player starts naval simulation");
 Check(!NavalSessionPolicy.Runs(false,true,false,true,false,true),"Dead player without spectator transition cannot drive live controls");
 Check(NavalSessionPolicy.Runs(true,true,false,false,false,true),"Battle continues after player death with disabled input");
 Check(NavalSessionPolicy.Runs(true,false,false,false,false,true),"Battle survives destroyed player object");
 Check(!NavalSessionPolicy.Runs(true,false,false,false,true,true),"Native pause remains respected in spectator mode");
 Check(!NavalSessionPolicy.Runs(true,false,false,false,false,false),"Spectator does not keep simulation running across scene change");
 Check(!NavalSessionPolicy.Runs(false,false,false,false,false,true),"No world simulation without player or spectator");
 Check(!NavalSessionPolicy.Runs(false,true,true,false,false,true),"Live disabled input still pauses ordinary naval controls");
 Console.WriteLine("Session policy checks passed: live play, death, destroyed body, pause and scene transition");
 }
}
