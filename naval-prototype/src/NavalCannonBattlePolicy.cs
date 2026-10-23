using System;
// Firing opportunities beat gun refurbishment; survival duties remain independent.
public sealed class NavalCannonBattlePolicy
{
 public const float QuietSeconds=6f;
 private float quiet=QuietSeconds,uncovered,ready;
 public bool AllowRepairs {get{return quiet>=QuietSeconds;}}
 public bool Retreat {get;private set;}
 public void Reset(){quiet=QuietSeconds;uncovered=ready=0;Retreat=false;}
 public void ObserveFire(float dt,bool opportunity)
 {quiet=opportunity?0:Math.Min(QuietSeconds,quiet+Math.Max(0,dt));}
 public void ObserveCrew(float dt,bool threat,int repairing,int plannedGuns,int readyGuns,int idleHands)
 {
  dt=Math.Max(0,dt);
  if(!threat||repairing<=0){Retreat=false;uncovered=ready=0;return;}
  if(readyGuns>0){uncovered=0;ready+=dt;if(ready>=2)Retreat=false;return;}
  ready=0;
  // A replacement already walking to a working gun is not a reason to flee.
  if(plannedGuns>0||idleHands>0){uncovered=0;return;}
  uncovered+=dt;if(uncovered>=2)Retreat=true;
 }
}
