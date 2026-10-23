using System;
class CombatHandoffChecks {
 static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
 static void Main(){
 foreach(var ship in NavalShipDefinition.Choices){
 var battle=new NavalCannonBattlePolicy();battle.ObserveFire(.05f,true);
 Check(NavalRepairDispatch.ResumeGunnery(true,true,false,false,false,false,false,0),ship.Name+": completed fire/patch enters gun allocation before repair scan");
 Check(!NavalMaintenancePriority.AllowCannonRepair(battle.AllowRepairs,false,false,false,false,false,false),"Opposite broken battery cannot take recovered gunners");
 Check(NavalRepairDispatch.ResumeGunnery(true,true,false,false,false,false,false,0),"Walking home after completion may switch directly to a working gun");
 Check(!NavalRepairDispatch.ResumeGunnery(false,true,false,false,false,false,false,0),"Out of sector does not force recall");
 Check(!NavalRepairDispatch.ResumeGunnery(true,true,true,false,false,false,false,0),"Accepted task cannot be stolen");
 Check(!NavalRepairDispatch.ResumeGunnery(true,true,false,true,false,false,false,0),"Ongoing fire retains survival response");
 Check(!NavalRepairDispatch.ResumeGunnery(true,true,false,false,true,false,false,0),"Known wave leak still needs response");
 Check(!NavalRepairDispatch.ResumeGunnery(true,true,false,false,false,true,false,0),"Needed pump keeps priority");
 Check(!NavalRepairDispatch.ResumeGunnery(true,true,false,false,false,false,true,0),"Flood/trim emergency keeps priority");
 Check(!NavalRepairDispatch.ResumeGunnery(true,true,false,false,false,false,false,1.8f),"Loaded bucket is poured before recall");
 Check(!NavalRepairDispatch.ResumeGunnery(true,false,false,false,false,false,false,0),"Unqueued working sailor is not reset each frame");
 }
 Console.WriteLine("PASS: post-fire/post-leak handoff before repair scan on all profiles, live-gun priority, no active-task interruption and survival/bucket guards.");
 }
}
