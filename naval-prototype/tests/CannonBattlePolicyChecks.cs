using System;
class CannonBattlePolicyChecks {
 static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
 static void Main(){foreach(var ship in NavalShipDefinition.Choices){
 var policy=new NavalCannonBattlePolicy();Check(policy.AllowRepairs,"Initial out-of-range cannon work is permitted");
 policy.ObserveFire(.05f,true);Check(!policy.AllowRepairs,"Working broadside in range immediately prioritizes shooting");
 for(int i=0;i<59;i++)policy.ObserveFire(.1f,false);
 Check(!policy.AllowRepairs,"Momentary lost sector does not restart fetching");
 policy.ObserveFire(.2f,false);Check(policy.AllowRepairs,"Sustained lack of target permits cannon repair again");
 for(int i=0;i<50;i++)policy.ObserveCrew(.1f,true,1,1,1,0);
 Check(!policy.Retreat,"One manned cannon preserves combat even during other gun repairs or reloads");
 for(int i=0;i<30;i++)policy.ObserveCrew(.1f,true,ship.GunCount,0,0,1);
 Check(!policy.Retreat,"Idle replacement is allocated before deciding to retreat");
 for(int i=0;i<30;i++)policy.ObserveCrew(.1f,true,ship.GunCount,1,0,0);
 Check(!policy.Retreat,"Replacement walking to a working gun does not trigger a new retreat");
 for(int i=0;i<21;i++)policy.ObserveCrew(.1f,true,ship.GunCount,0,0,0);
 Check(policy.Retreat,"No firing hands while crew refurbishes guns triggers withdrawal");
 for(int i=0;i<30;i++)policy.ObserveCrew(.1f,true,1,1,0,0);
 Check(policy.Retreat,"Existing withdrawal stays until replacement actually arrives");
 policy.ObserveCrew(.1f,true,1,1,1,0);Check(policy.Retreat,"One transient ready frame cannot flip course");
 for(int i=0;i<20;i++)policy.ObserveCrew(.1f,true,1,1,1,0);
 Check(!policy.Retreat,"Stable firing crew resumes battle");
 for(int i=0;i<21;i++)policy.ObserveCrew(.1f,true,1,0,0,0);
 policy.ObserveCrew(.1f,false,1,0,0,0);Check(!policy.Retreat,"No live threat clears withdrawal");
 policy.Reset();Check(policy.AllowRepairs&&!policy.Retreat,"New task or final-attack reset clears previous staffing state");
 }
 Console.WriteLine("PASS: shooting priority, six-second quiet gate, replacement/repair staffing, no reload retreat, delayed withdrawal and stable return on all profiles.");}
}
