using System;
// Bounded per-sailor memory shared by all routed duties. Coordinates are world metres.
public sealed class NavalRouteFailureMemory
{
 private struct Failure{public float SX,SY,SZ,X,Y,Z,Until;public int Revision;}
 private readonly Failure[] entries=new Failure[8];private int cursor;
 public bool Blocked(float sx,float sy,float sz,float x,float y,float z,int revision,float now){
  foreach(var f in entries)if(f.Until>now&&f.Revision==revision&&Distance(sx,sy,sz,f.SX,f.SY,f.SZ)<1f&&Distance(x,y,z,f.X,f.Y,f.Z)<.35f*.35f)return true;return false;
 }
 private static float Distance(float x,float y,float z,float a,float b,float c){return (x-a)*(x-a)+(y-b)*(y-b)+(z-c)*(z-c);}
 public void Mark(float sx,float sy,float sz,float x,float y,float z,int revision,float now){entries[cursor]=new Failure{SX=sx,SY=sy,SZ=sz,X=x,Y=y,Z=z,Revision=revision,Until=now+10f};cursor=(cursor+1)%entries.Length;}
}
