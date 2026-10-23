using System;
class SternEvasionChecks
{
 static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
 static void Main(){
 var p=new NavalSternEvasionMath(42);Check(!p.Step(.05f,false,false,false,0,true,2,2),"Do not evade without a stern threat");
 bool left=false,right=false,breakout=false,straight=false;float before=0;
 for(int i=0;i<1200;i++){
 Check(p.Step(.05f,true,false,false,0,true,2,2),"Maintain escape while stern remains threatened");
 Check(Math.Abs(p.Offset-before)<=.60001f,"No abrupt steering reversal");before=p.Offset;
 Check(Math.Abs(p.Offset)<=30.01f,"Normal zigzag bounded relative to fixed entry heading");left|=p.Offset< -10;right|=p.Offset>10;
 }
 Check(left&&right,"Serpentine includes both tacks");
 for(int i=0;i<340;i++){
 p.Step(.05f,true,true,true,-3,true,2,2);breakout|=p.BreakingAway;
 straight|=p.BreakingAway&&Math.Abs(p.Offset)<13;
 Check(Math.Abs(p.Offset)<=48.01f,"Breakout heading cannot grow into an orbit");
 }
 Check(breakout&&straight,"Persistent clinch causes finite turn then long straight escape leg");
 for(int i=0;i<130;i++)p.Step(.05f,false,false,false,0,true,2,2);Check(!p.Active,"Return to normal after threat clears");
 var combat=new NavalSternEvasionMath(42);combat.Step(.05f,true,false,false,0,false,1,2);Check(combat.CounterSide==2,"First turn uses stronger working battery");
 for(int i=0;i<250;i++)combat.Step(.05f,true,true,true,0,false,1,2);
 Check(combat.CounterSide==1,"Failed stern counter changes to opposite side after a committed attempt");
 for(int i=0;i<250;i++)combat.Step(.05f,true,true,true,0,false,1,2);
 Check(combat.CounterSide==0&&combat.BreakingAway,"Two unsuccessful turns trigger separation leg");
 var firing=new NavalSternEvasionMath(7);firing.Step(.05f,true,false,false,0,false,1,2);
 for(int i=0;i<600;i++)firing.Step(.05f,true,false,false,0,false,1,2,true);
 Check(firing.CounterSide==2&&!firing.BreakingAway,"Working firing solution is held instead of needless side switch");
 firing.Step(.05f,true,false,false,0,false,1,0);Check(firing.CounterSide==1,"Destroyed battery is skipped immediately");
 firing.Step(.05f,true,false,false,0,true,1,0);Check(firing.CounterSide==0,"Repair retreat returns to evasive motion");
 Console.WriteLine("PASS: stern trigger, both tacks, gradual steering, finite clinch escape with straight leg, repair retreat and release.");
 }
}
