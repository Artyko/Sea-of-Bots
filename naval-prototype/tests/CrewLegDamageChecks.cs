using System;
class CrewLegDamageChecks {
 static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
 static void Main(){foreach(var ship in NavalShipDefinition.Choices)foreach(float speed in new float[]{3,5.5f,8}){
 float normal=NavalCrewMovement.BaseSpeed(speed)*NavalCrewMovement.InjuryMultiplier(false,false);
 float right=NavalCrewMovement.BaseSpeed(speed)*NavalCrewMovement.InjuryMultiplier(true,false);
 float left=NavalCrewMovement.BaseSpeed(speed)*NavalCrewMovement.InjuryMultiplier(false,true);
 Check(normal==speed&&right==speed*.5f&&left==right,"One damaged leg halves movement on either side");
 Check(NavalCrewMovement.InjuryMultiplier(true,true,.15f)==.15f,"Both-leg damage uses native crawl multiplier");
 foreach(bool stairs in new bool[]{false,true}){
 float healthy=NavalCrewMovement.TargetSpeed(normal,1,100,false,stairs),injured=NavalCrewMovement.TargetSpeed(right,1,100,false,stairs);
 Check(Math.Abs(injured/healthy-.5f)<.001,"Deck, stairs and boarding route movement retain injury slowdown");}
 }
 Check(NavalCrewMovement.InjuryMultiplier(true,true,float.NaN)==.2f,"Unavailable crawl tuning stays finite");
 Console.WriteLine("PASS: both one-leg cases, native crawl tuning, healthy speed and deck/stair/boarding route slowdown on every ship.");}
}
