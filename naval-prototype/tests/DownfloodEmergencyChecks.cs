using System;
class DownfloodEmergencyChecks
{
 static void Check(bool ok,string name){if(!ok)throw new Exception(name);}
 static void Main()
 {
  Check(NavalDownfloodMath.OverflowRate(0,.1f,-2,4)==0,"Dry intact rail does not create flooding");
  Check(NavalDownfloodMath.OverflowRate(0,-.2f,.1f,4)==0,"Interior level above sea prevents inward overtopping");
  Check(NavalDownfloodMath.OverflowRate(0,-.2f,-1,4)>.02f,"Submerged intact rail admits water without any hull hole");
  Check(NavalDownfloodMath.OverflowRate(0,-.4f,-1,4)>NavalDownfloodMath.OverflowRate(0,-.2f,-1,4),"More deeply submerged rail admits more water");
  var emergency=new NavalDownfloodEmergency();
  emergency.Step(.05f,false,1.2f,14);Check(emergency.Active,"Intact low-side opening flooding demands immediate trim");
  Check(NavalBallastTrim.AllowsTransfer(false,1.2f,emergency.Active),"Opening inflow no longer blocks corrective transfer");
  Check(!NavalBallastTrim.AllowsTransfer(true,1.2f,true),"New active hull leak retains patch priority");
  Check(!NavalBallastTrim.AllowsTransfer(false,1.2f,false),"Ordinary non-emergency inflow still uses overboard mode");
  Check(NavalCombatEmergency.ReleaseGunner(1,emergency.Active),"Urgent downflooding can recall the final gunner");
  Check(!NavalCrewPriorities.ReserveHelm(1,emergency.Active,true),"Last surviving sailor may prioritize the pump over steering");
  emergency.Step(1,false,0,10);Check(emergency.Active,"Wave trough does not instantly cancel emergency assignment");
  emergency.Step(.05f,false,0,2.5f);Check(!emergency.Active,"Levelled ship releases emergency");
  emergency.Step(.05f,true,2,18);Check(!emergency.Active,"Actual leaking hull returns to ordinary repair priorities");
  emergency.Step(.05f,false,1.2f,14);emergency.Step(8.1f,false,0,10);Check(!emergency.Active,"No renewed downflooding ends urgency after reserve interval");
  emergency.Step(.05f,false,2,1);Check(!emergency.Active,"Wave wash without a persistent list does not create a trim emergency");
  var ongoing=new NavalDownfloodEmergency();
  for(int i=0;i<400;i++)
  {
   // The pump operator needs time to arrive; accepted grid volume may be zero when locally full.
   ongoing.Step(.05f,false,NavalDownfloodMath.OverflowRate(0,-.2f,-1,4),22);
   Check(ongoing.Active&&NavalBallastTrim.AllowsTransfer(false,2,ongoing.Active),"Persistent intact-rail flooding maintains urgent pump access beyond route time");
  }
  var trim=new NavalBallastTrim();for(int i=0;i<190;i++){trim.Update(.1f,16);trim.Observe(.1f,.16f,true);}
  Check(trim.Cooldown>0,"Initial ineffective attempt enters cooldown");
  trim.Update(.1f,16,true);Check(trim.Cooldown==0&&trim.WantsTransfer,"Fresh downflooding overrides stale cooldown immediately");
  for(int i=0;i<190;i++){trim.Update(.1f,16,true);trim.Observe(.1f,.16f,true);}
  Check(trim.Cooldown>0,"Persistent emergency does not disable genuine ineffectiveness fallback");
  trim.Update(.1f,16,true);Check(trim.Cooldown>0,"Same emergency cannot reset cooldown every frame");
  Console.WriteLine("PASS: immediate intact-opening emergency, wave reserve, repaired-hull transfer permission, patch priority, final-gunner/last-survivor release, recovery and bounded ineffective trials.");
 }
}
