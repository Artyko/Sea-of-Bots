using System;
class RetreatChecks
{
 static void Check(bool value,string name){if(!value)throw new Exception(name);}
 static void Main(){
 var state=new NavalRetreatState();
 state.Step(.1f,false,true,true);Check(!state.Active,"No repair need does not trigger retreat");
 state.Step(.1f,true,false,true);Check(!state.Active,"No flooding: no repair retreat");
 state.Step(.1f,true,true,false);Check(!state.Active,"No opponent: no retreat");
 state.Step(.1f,true,true,true);Check(state.Active,"Repair need and danger trigger retreat");
 for(int i=0;i<200;i++)state.Step(.1f,false,true,true);
 Check(state.Active,"Continue until flooding is handled even if a gun is remanned");
 state.Step(.1f,false,false,true);Check(!state.Active,"Immediately resume combat when retreat danger clears");
 state.Step(.1f,true,true,true);Check(state.Active,"A new emergency can start another retreat");
 state.Step(.1f,true,true,false);Check(!state.Active,"Lost threat or manual helm immediately clears autopilot retreat");
 var emergency=new NavalCombatEmergency();
 emergency.Step(.5f,10f);Check(!emergency.Critical,"Ordinary flooding does not call the last gunner");
 Check(!NavalCombatEmergency.ReleaseGunner(1,emergency.Critical),"Keep final gunner on duty");
 Check(NavalCombatEmergency.ReleaseGunner(2,false),"Extra gunner may help repairs normally");
 emergency.Step(.75f,5f);Check(emergency.Critical,"Severe loss of buoyancy releases final gunner");
 Check(NavalCombatEmergency.ReleaseGunner(1,emergency.Critical),"Critical emergency can release last gunner");
 emergency.Step(.7f,5f);Check(emergency.Critical,"No rapid role toggles near entry threshold");
 emergency.Step(.6f,14f);Check(!emergency.Critical,"Safe level triggers immediate recall with some water remaining");
 emergency.Step(.35f,20f);Check(emergency.Critical,"Dangerous heel with substantial flooding triggers emergency");
 emergency.Step(.3f,16f);Check(emergency.Critical,"Do not recall while heel remains unsafe");
 emergency.Step(.3f,12f);Check(!emergency.Critical,"Recall once heel recovers");
 emergency.Step(.1f,21f);Check(!emergency.Critical,"A wave-induced roll with little water does not strip the guns");
 Check(!NavalCombatEmergency.ReleaseGunner(0,true),"No phantom gunner to release");
 var early=new NavalCombatEmergency();
 early.Step(.349f,5f);Check(!early.RepairRetreat&&!early.Critical,"Minor flood stays in combat without buckets");
 early.Step(.35f,5f);Check(early.RepairRetreat&&!early.Critical,"Repair retreat starts before critical bucket emergency");
 var helm=new NavalRetreatState();helm.Step(.1f,true,early.RepairRetreat,true);Check(helm.Active,"Danger retreat can begin with guns still crewed");
 early.Step(.3f,5f);Check(early.RepairRetreat,"Retreat has separate hysteresis");
 early.Step(.25f,12f);Check(!early.RepairRetreat,"Resume combat after earlier safe threshold");
 helm.Step(.1f,true,early.RepairRetreat,true);Check(!helm.Active,"Repair retreat stops at safe threshold");
 early.Step(.3f,16f);Check(early.RepairRetreat&&!early.Critical,"Moderate flood and heel trigger earlier escape, not buckets");
 early.Step(.3f,13f);Check(early.RepairRetreat,"Do not toggle escape at a wave crest");
 early.Step(.25f,12f);Check(!early.RepairRetreat,"Heel recovery ends escape");
 early.Step(.1f,22f);Check(!early.RepairRetreat&&!early.Critical,"A dry wave roll triggers neither retreat nor buckets");
 early.Step(.75f,5f);Check(early.RepairRetreat&&early.Critical,"Severe flood activates both stages");
 early.Step(.6f,5f);Check(early.RepairRetreat&&!early.Critical,"Stop new bucket trips before retreat finishes");
 var sloop=NavalShipDefinition.Sloop;
 Check(sloop.Compartments&&sloop.GunCount==2&&sloop.CrewCount==4,"Sloop gets systems with expanded crew and original guns");
 Check(NavalCompartmentMath.WallX-sloop.StairWidth*.5f>.8f,"Sloop stair leaves side corridors clear");
 Check(sloop.StairWidth*sloop.Scale>2f,"Stair remains wide enough for robots and player");
 Console.WriteLine("Retreat and sloop layout checks passed");
 }
}
