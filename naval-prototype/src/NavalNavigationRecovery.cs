using System;
// Tracks actual displacement across route rebuilds: rebuilding cannot reset a stall.
public sealed class NavalNavigationRecovery {
 private bool ready;private float x,y,z,elapsed;private int phase;
 public int Step(float dt,float px,float py,float pz){
  if(!ready||(px-x)*(px-x)+(py-y)*(py-y)+(pz-z)*(pz-z)>.6f*.6f){ready=true;x=px;y=py;z=pz;elapsed=0;phase=0;return 0;}
  elapsed+=Math.Max(0,dt);
  int next=elapsed>=12?3:elapsed>=6?2:elapsed>=2?1:0;
  if(next<=phase)return 0;phase=next;return next;
 }
 public void Reset(){ready=false;elapsed=0;phase=0;}
}
