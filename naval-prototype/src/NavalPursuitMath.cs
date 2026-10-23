using System;
public sealed class NavalPursuitMath
{
 private float release;
 public bool Active {get;private set;}
 public void Reset(){Active=false;release=0;}
 public bool Step(float dt,bool retreating,bool escaping,bool armed)
 {
  if(!armed){Reset();return false;}
  if(retreating||escaping){Active=true;release=6;}
  else {release=Math.Max(0,release-Math.Max(0,dt));if(release<=0)Active=false;}
  return Active;
 }
 public static float StandOff(float ownScale,float enemyScale){return 6.25f*(ownScale+enemyScale)+3f;}
 public static float FireBlend(float error){return Math.Max(0,Math.Min(.92f,1-error/22f));}
 public static int Quarter(int port,int starboard,int previous)
 {if(port<starboard)return -1;if(starboard<port)return 1;return previous==0?1:previous;}
}
