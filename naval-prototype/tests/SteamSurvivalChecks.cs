using System;
class SteamSurvivalChecks {
 static void Check(bool value,string reason){if(!value)throw new Exception(reason);}
 static void Main(){
  var fast=new NavalSteamState();var economical=new NavalSteamState();
  for(int i=0;i<100;i++){fast.StepFlooded(.1f,1,true,1,1,1,true);economical.StepFlooded(.1f,.5f,true,1,1,1,true);}
  Check(fast.Pressure>0,"Flooded firebox retains finite steam for retreat");
  Check(economical.Pressure>fast.Pressure,"Economical orders conserve pressure");
  for(int i=0;i<1000;i++)fast.StepFlooded(.1f,1,true,1,1,1,true);
  Check(fast.Pressure==0&&fast.Power(1)==0,"No magical propulsion after reserve exhaustion");
  var healthy=new NavalSteamState();var flooded=new NavalSteamState();
  for(int i=0;i<1200;i++){healthy.StepFlooded(.1f,1,true,1,1,0,true);flooded.StepFlooded(.1f,1,true,1,1,1,true);}
  Check(healthy.Power(1)>.9f&&flooded.Power(1)==0,"Independent boiler states preserve one working source");
  Check(NavalBoilerFloodMath.Flooding(Math.Max(0,.5f-.5f))==0,"Raised firebox remains dry above bilge water");
  var grid=new NavalFloodGrid(2,1,2,1,1,3);grid.Add(0,.6f);grid.RightSill[0]=1.1f;
  float[] floor={0,0};grid.Spread(.1f,floor,1);Check(grid.Water[1]==0,"Intact sill prevents low bilge water entering protected zone");
  grid.RightSill[0]=float.NegativeInfinity;grid.Spread(.1f,floor,1);
  Check(grid.Water[1]>0&&Math.Abs(grid.Total-.6f)<.0001f,"Breached protection admits water without creating or deleting mass");
  Console.WriteLine("PASS: finite steam reserve, economical escape, complete depletion and independent boiler loss.");
 }
}
