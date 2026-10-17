using System;
class BroadsideChecks
{
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 static void Main(){
 Check(NavalBroadsideMath.CombatSide(1,0,2)==2,"Prepare opposite guns when current side is destroyed");
 Check(NavalBroadsideMath.CombatSide(2,2,0)==1,"Symmetric for port guns");
 Check(NavalBroadsideMath.CombatSide(1,1,2)==2,"Turn to the side with more working guns");
 Check(NavalBroadsideMath.CombatSide(2,2,1)==1,"Prefer stronger port broadside symmetrically");
 Check(NavalBroadsideMath.CombatSide(1,2,2)==1&&NavalBroadsideMath.CombatSide(2,2,2)==2,"Equal broadsides preserve current side");
 Check(NavalBroadsideMath.OrbitSign(2,1,-1f)==1f&&NavalBroadsideMath.OrbitSign(1,2,1f)==-1f,"Stronger side controls course even when weaker side still has a gun");
 Check(NavalBroadsideMath.CombatSide(1,0,0)==1,"Both sides broken leaves boarding fallback intact");
 Check(NavalBroadsideMath.OrbitSign(1,1,-1f)==-1f,"Intact broadsides preserve existing tactics");
 Check(NavalBroadsideMath.OrbitSign(0,0,1f)==1f,"No guns preserves fallback direction");
 foreach(int wanted in new[]{1,2})foreach(float previous in new[]{-1f,1f})
 for(int angle=0;angle<360;angle+=15)foreach(float approach in new[]{-1f,.15f,1f}){
  double a=angle*Math.PI/180,rx=Math.Sin(a),rz=Math.Cos(a);
  float orbit=NavalBroadsideMath.OrbitSign(wanted==1?1:0,wanted==2?1:0,previous);
  double forwardX=rz*orbit+rx*approach,forwardZ=-rx*orbit+rz*approach;
  double targetRight=rx*forwardZ-rz*forwardX;
  Check((targetRight<0?1:2)==wanted,"Desired course exposes working broadside from every target bearing");
 }
 var brig=NavalShipDefinition.Brigantine;
 Check(brig.HoldClearance-3.6f>=.79f,"At least half a metre of additional camera room");
 float vertical=(.55f-brig.HoldFloor-.8f)*brig.Scale;
 Check(vertical*vertical+2.5f*2.5f<4.8f*4.8f,"High holes remain repairable from floor with horizontal margin");
 Console.WriteLine("Broadside tactics and raised ceiling checks passed");
 }
}
