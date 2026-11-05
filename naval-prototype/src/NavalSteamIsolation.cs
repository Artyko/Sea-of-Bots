using System;
// Feed isolation and finite trapped steam; closing a valve cannot repair its pipe.
public sealed class NavalSteamIsolation {
 private readonly bool[] closed=new bool[3],automatic=new bool[3];
 private readonly float[] residual=new float[3];
 public bool Closed(int i){return closed[i];}
 public float Steam(int i){return residual[i];}
 public float Supply(int i,float integrity){return closed[i]?0:integrity;}
 public void Toggle(int i){closed[i]=!closed[i];automatic[i]=false;}
 public void Step(float dt,float pressure,float left,float right,float auxiliary,bool engineer){

  for(int i=0;i<3;i++){
   float integrity=i==0?left:i==1?right:auxiliary;
   if(engineer){bool redundant=i==2||(i==0?right:left)>.75f&&!closed[1-i];
    if(!closed[i]&&integrity<.75f&&redundant){closed[i]=true;automatic[i]=true;}
    else if(automatic[i]&&integrity>.98f){closed[i]=false;automatic[i]=false;}
   }
   residual[i]=closed[i]?Math.Max(0,residual[i]-Math.Max(0,dt)*.25f):pressure;
  }
 }
 public float Leak(float left,float right,float auxiliary){return ((closed[0]?0:1-left)+(closed[1]?0:1-right)+(closed[2]?0:1-auxiliary))/3f;}
}
