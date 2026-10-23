using System;
public sealed class NavalFirePassMath
{
 public int Phase {get;private set;} // 0 idle, 1 acquire, 2 firing pass, 3 separate
 public int Side {get;private set;}
 public float Heading {get;private set;}
 public float Throttle {get;private set;}
 private float age,noShot,lost,yaw,previous,anchor,outOfRange;
 public void Reset(){Phase=Side=0;age=noShot=lost=yaw=outOfRange=0;}
 private void Separate(float bearing){Phase=3;age=0;Heading=bearing+180+(Side==1?-15:15);}
 public bool Step(float dt,float ownHeading,float bearing,float distance,float clearance,int port,int starboard,int firingSide,bool shot,bool reachable)
 {
  dt=Math.Max(0,dt);
  if(port+starboard==0){Reset();return false;}
  if(Phase==0){
   if(!reachable||distance>clearance+32)return false;
   Side=NavalBroadsideMath.HelmSide(ownHeading,bearing,port,starboard,0,firingSide);
   Phase=1;age=noShot=lost=yaw=0;previous=ownHeading;anchor=NavalBroadsideMath.Heading(bearing,Side);
  }
  outOfRange=reachable?0:outOfRange+dt;
  // Waves may briefly hide range; sustained loss returns control to interception.
  if(outOfRange>=3f){Reset();return false;}
  age+=dt;noShot=shot?0:noShot+dt;yaw+=Math.Abs(NavalBroadsideMath.Delta(previous,ownHeading));previous=ownHeading;
  bool working=Side==1?port>0:starboard>0;
  if(!working&&Phase!=3){Side=port>0?1:2;Phase=1;age=noShot=lost=yaw=0;anchor=NavalBroadsideMath.Heading(bearing,Side);}
  if(Phase==1){
   Heading=anchor+Math.Max(-25,Math.Min(25,NavalBroadsideMath.Delta(anchor,NavalBroadsideMath.Heading(bearing,Side))));Throttle=.8f;
   if(firingSide==Side){Phase=2;age=noShot=lost=yaw=0;anchor=NavalBroadsideMath.Heading(bearing,Side);}
   else if(age>30||yaw>150&&noShot>8)Separate(bearing);
  }
  if(Phase==2){
   // Hold one battery long enough for aiming and two normal reload opportunities.
   // Track the moving target with bounded heading changes rather than a fixed old bearing.
   Heading=NavalHelmIntentMath.Heading(Heading,NavalBroadsideMath.Heading(bearing,Side),dt,12f);Throttle=.65f;
   lost=firingSide==Side?0:lost+dt;
   if(age>45||lost>6||noShot>12||yaw>85&&noShot>8)Separate(bearing);
  }
  if(Phase==3){Throttle=1;if(age>20||age>10&&distance>clearance+18){Reset();return false;}}
  return true;
 }
}
