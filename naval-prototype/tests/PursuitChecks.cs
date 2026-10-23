using System;
class PursuitChecks
{
 static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
 static void Main(){var p=new NavalPursuitMath();
 Check(p.Step(.05f,true,false,true),"Repair retreat triggers pursuit immediately");
 for(int i=0;i<1000;i++)Check(p.Step(.05f,true,false,true),"Pursuit persists while target retreats; no firing-range switch");
 Check(p.Step(.05f,false,true,true),"Escaping target also triggers pursuit");
 for(int i=0;i<50;i++)Check(p.Step(.05f,false,false,true),"Short maneuver does not cancel chase");
 for(int i=0;i<150;i++)p.Step(.05f,false,false,true);Check(!p.Active,"Return to battle when target stops escaping");
 Check(!p.Step(.05f,true,true,false),"Unarmed ships retain final ram behavior");
 Check(NavalPursuitMath.FireBlend(100)==0&&NavalPursuitMath.FireBlend(0)>.9f,"No distant broadside turn; close pass can bring guns to bear");
 Check(NavalPursuitMath.Quarter(0,2,1)==-1&&NavalPursuitMath.Quarter(2,0,-1)==1,"Choose quarter with fewer enemy guns");
 Check(NavalPursuitMath.Quarter(2,2,-1)==-1,"Equal batteries do not cause side switching");
 foreach(float scale in new float[]{2,3,4})Check(NavalPursuitMath.StandOff(scale,scale)>12.5f*scale,"Stern anchor respects hull length");
 Console.WriteLine("PASS: persistent chase, close firing blend, weaker rear quarter, contact spacing and transition to normal combat.");}
}
