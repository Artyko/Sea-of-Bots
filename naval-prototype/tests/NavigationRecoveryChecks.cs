using System;
class NavigationRecoveryChecks {
 static void Main(){var r=new NavalNavigationRecovery();r.Step(0,0,0,0);
 if(r.Step(2,0,0,0)!=1||r.Step(4,0,0,0)!=2||r.Step(6,0,0,0)!=3)throw new Exception("Recovery escalation");
 if(r.Step(1,1,0,0)!=0)throw new Exception("Real progress resets escalation");
 r.Reset();if(r.Step(0,1,0,0)!=0)throw new Exception("Reset");
 Console.WriteLine("PASS: persistent stall escalation and reset on real displacement.");}
}
