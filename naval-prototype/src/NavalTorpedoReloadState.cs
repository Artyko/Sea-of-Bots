using System;
// Finite gameplay reserve; loading pauses when the operator leaves and loses the round if damaged.
public sealed class NavalTorpedoReloadState {
 public const float Seconds=35f;public const int InitialReserve=8;
 public int Reserve {get;private set;}
 private readonly int[] tubes={-1,-1,-1,-1};private readonly float[] elapsed=new float[4];
 public NavalTorpedoReloadState(){Reset();}
 public int Tube(int mount){return tubes[mount];}public float Progress(int mount){return tubes[mount]<0?0:elapsed[mount]/Seconds;}
 public bool Begin(int mount,int tube){if(Reserve<=0||tubes[mount]>=0)return false;Reserve--;tubes[mount]=tube;elapsed[mount]=0;return true;}
 public bool Step(int mount,float dt,bool operatorPresent,bool intact){if(tubes[mount]<0)return false;if(!intact){Cancel(mount);return false;}if(!operatorPresent)return false;elapsed[mount]+=Math.Max(0,dt);if(elapsed[mount]<Seconds)return false;tubes[mount]=-1;elapsed[mount]=0;return true;}
 public void Cancel(int mount){tubes[mount]=-1;elapsed[mount]=0;}
 public void Reset(){Reserve=InitialReserve;for(int i=0;i<4;i++)Cancel(i);}
}
