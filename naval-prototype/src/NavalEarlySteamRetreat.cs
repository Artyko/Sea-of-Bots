using System;
public sealed class NavalEarlySteamRetreat {
 private float previous,filteredRate,quiet;private bool initialized;
 public bool Active {get;private set;}
 public bool Step(float dt,float depth,float nearbyDepth,bool inflow,float pressure,float integrity){
  dt=Math.Max(.001f,dt);if(!initialized){previous=depth;initialized=true;}
  float rate=Math.Max(-.1f,Math.Min(.1f,(depth-previous)/dt));previous=depth;
  filteredRate+=(rate-filteredRate)*Math.Min(1,dt/2f);
  bool danger=depth>=.30f||inflow&&nearbyDepth>=.55f||depth>.08f&&filteredRate>.003f&&(.65f-depth)/filteredRate<90f||inflow&&depth>.1f&&(pressure<.45f||integrity<.7f);
  if(danger){Active=true;quiet=0;}
  else if(Active){if(depth<.12f&&nearbyDepth<.3f&&!inflow&&pressure>.5f)quiet+=dt;else quiet=0;if(quiet>=15f){Active=false;quiet=0;}}
  return Active;
 }
 public void Reset(){Active=initialized=false;quiet=previous=filteredRate=0;}
}
