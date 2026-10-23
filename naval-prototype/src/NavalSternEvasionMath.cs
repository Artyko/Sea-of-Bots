using System;
public sealed class NavalSternEvasionMath
{
 private uint random;
 private float quiet,contact,phaseAge,halfPeriod;
 private int direction;
 private float amplitude;
 private int combatStage,combatSide;
 private bool wasRetreat;
 public int CounterSide {get;private set;}
 public bool Active {get;private set;}
 public bool BreakingAway {get;private set;}
 public float Offset {get;private set;}
 public NavalSternEvasionMath(int seed){random=(uint)seed^0x9e3779b9u;}
 private float Next(){random=unchecked(random*1664525u+1013904223u);return ((random>>8)%10000)/10000f;}
 public void Reset(){Active=BreakingAway=false;quiet=contact=phaseAge=Offset=0;combatStage=CounterSide=combatSide=0;}
 public bool Step(float dt,bool sternThreat,bool closeContact,bool closeQuarters,float lateral,bool retreat,int port,int starboard,bool canFire=false)
 {
  dt=Math.Max(0,dt);
  if(!Active){if(!sternThreat)return false;Active=true;quiet=contact=phaseAge=0;direction=lateral<-.5f?1:lateral>.5f?-1:starboard>port?1:port>starboard?-1:Next()<.5f?-1:1;halfPeriod=4.5f+Next()*2;amplitude=22+Next()*8;}
  if(wasRetreat!=retreat){combatStage=combatSide=0;phaseAge=0;BreakingAway=false;}
  wasRetreat=retreat;
  quiet=sternThreat||closeContact?0:quiet+dt;if(quiet>6){Reset();return false;}
  if(!retreat)
  {
   if(combatSide==0)combatSide=starboard>port?2:port>starboard?1:direction>0?2:1;
   phaseAge=canFire&&combatStage<2?0:phaseAge+dt;
   if(combatStage<2&&phaseAge>=12){phaseAge=0;combatStage++;if(combatStage==1)combatSide=combatSide==1?2:1;}
   if(combatStage==2&&phaseAge>=12){phaseAge=0;combatStage=0;combatSide=starboard>port?2:port>starboard?1:combatSide;}
   if(combatStage<2){
    if(combatSide==1&&port==0&&starboard>0)combatSide=2;
    if(combatSide==2&&starboard==0&&port>0)combatSide=1;
    CounterSide=combatSide;BreakingAway=false;
    float target=combatSide==2?88:-88,counterStep=22*dt;
    Offset=Offset<target?Math.Min(target,Offset+counterStep):Math.Max(target,Offset-counterStep);
   }else{
    CounterSide=0;BreakingAway=true;
    float target=direction*(phaseAge<4?48:12),escapeStep=16*dt;
    Offset=Offset<target?Math.Min(target,Offset+escapeStep):Math.Max(target,Offset-escapeStep);
   }
   return true;
  }
  CounterSide=0;
  contact=closeContact||closeQuarters?contact+dt:Math.Max(0,contact-dt*2);
  if(!BreakingAway&&contact>3){BreakingAway=true;phaseAge=0;}
  phaseAge+=dt;
  float wanted;
  if(BreakingAway){
   // A finite turn followed by a long straight leg, never a continuous orbit.
   float angle=phaseAge<4?48:12;
   wanted=direction*angle;
   if(phaseAge>12){BreakingAway=false;phaseAge=contact=0;direction=-direction;halfPeriod=5+Next()*2;}
  }else{
   float a=retreat?amplitude*.65f:amplitude;
   wanted=direction*a*(float)Math.Sin(phaseAge*Math.PI/halfPeriod);
  }
  float step=12*dt;Offset=Offset<wanted?Math.Min(wanted,Offset+step):Math.Max(wanted,Offset-step);
  return true;
 }
}
