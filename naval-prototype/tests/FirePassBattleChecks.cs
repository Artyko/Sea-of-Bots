using System;
class FirePassBattleChecks
{
 class Ship{public double X,Z;public float Heading,Command,Rudder,Speed=3,Scale;public int Port,Starboard,Shots,Separations;public float Reload,Aim,LastYaw;public bool Fired;public NavalFirePassMath Plan=new NavalFirePassMath();public Ship(float scale,int port,int starboard,double x,double z,float heading){Scale=scale;Port=port;Starboard=starboard;X=x;Z=z;Heading=Command=heading;}}
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 static float Bearing(Ship a,Ship b){return (float)(Math.Atan2(b.X-a.X,b.Z-a.Z)*180/Math.PI);}
 static float Distance(Ship a,Ship b){return (float)Math.Sqrt(Math.Pow(a.X-b.X,2)+Math.Pow(a.Z-b.Z,2));}
 static int Sector(Ship a,Ship b){float d=Distance(a,b);if(!NavalEngagementRange.BallisticReach(d,0))return 0;float relative=NavalBroadsideMath.Delta(a.Heading,Bearing(a,b));return a.Port>0&&Math.Abs(NavalBroadsideMath.Delta(-90,relative))<28?1:a.Starboard>0&&Math.Abs(NavalBroadsideMath.Delta(90,relative))<28?2:0;}
 static void Command(Ship a,Ship b){float bearing=Bearing(a,b),distance=Distance(a,b),clearance=NavalPursuitMath.StandOff(a.Scale,b.Scale);int side=Sector(a,b);float throttle=.8f;
 bool active=a.Plan.Step(.05f,a.Heading,bearing,distance,clearance,a.Port,a.Starboard,side,a.Fired,NavalEngagementRange.BallisticReach(distance,0));a.Fired=false;
 float wanted;if(active){wanted=a.Plan.Heading;throttle=a.Plan.Throttle;}else {int gun=NavalBroadsideMath.HelmSide(a.Heading,bearing,a.Port,a.Starboard,0,side);wanted=distance>clearance+30?NavalEngagementRange.ApproachCourse(bearing,gun,distance,a.Speed,0,0):NavalBroadsideMath.Heading(bearing,gun);}
 if(a.Plan.Phase==3&&a.LastYaw!=3)a.Separations++;a.LastYaw=a.Plan.Phase;
 a.Command=NavalHelmIntentMath.Heading(a.Command,wanted,.05f,16);
 a.Rudder=NavalRigMath.StepRudder(a.Rudder,Math.Max(-1,Math.Min(1,NavalBroadsideMath.Delta(a.Heading,a.Command)/25)),.05f);
 a.Speed=NavalRigMath.StepSpeed(a.Speed,throttle,1,.05f,4.2f);
 a.Heading+=NavalRigMath.RudderRate(a.Speed,a.Rudder)*.05f;
 if(side>0){a.Aim+=.05f;if(a.Reload<=0&&a.Aim>=2.5f){a.Shots++;a.Fired=true;a.Reload=NavalGunneryMath.ReloadSeconds;}}else a.Aim=0;
 a.Reload-=.05f;
 }
 static void Move(Ship a,Ship b){float nx=(float)(a.X+Math.Sin(a.Heading*Math.PI/180)*a.Speed*.05),nz=(float)(a.Z+Math.Cos(a.Heading*Math.PI/180)*a.Speed*.05);float safe=NavalShipContactMath.SafeFraction((float)a.X,(float)a.Z,a.Heading,nx,nz,0,(float)b.X,(float)b.Z,b.Heading,a.Scale/2,b.Scale/2);a.X+=(nx-a.X)*safe;a.Z+=(nz-a.Z)*safe;}
 static void Main(){int scenarios=0,total=0;foreach(float scale in new float[]{2,3,4})foreach(int type in new int[]{0,1,2}){
 float gap=NavalPursuitMath.StandOff(scale,scale)+6;var a=new Ship(scale,type==1?0:2,type==2?0:2,0,0,0);var b=new Ship(scale,2,2,type==0?gap:8,type==0?0:-gap,type==0?180:0);
 float dryClose=0,maxDryClose=0;
 for(int i=0;i<4800;i++){Command(a,b);Command(b,a);Move(a,b);Move(b,a);dryClose=a.Fired||b.Fired||Distance(a,b)>gap+10?0:dryClose+.05f;maxDryClose=Math.Max(maxDryClose,dryClose);}
 Check(maxDryClose<45,"No prolonged close-range orbit without shots");
 Console.WriteLine("battle scale="+scale+" layout="+type+" shots="+a.Shots+"/"+b.Shots+" separations="+a.Separations+"/"+b.Separations);
 Check(a.Shots>=2&&b.Shots>=2,"Both ships must get repeated firing opportunities with surviving batteries");
 Check(a.Separations+b.Separations>0||a.Shots>=20&&b.Shots>=20,"Close fight must separate or sustain successful firing from both ships");total+=a.Shots+b.Shots;scenarios++;
 }
 var policy=new NavalFirePassMath();policy.Step(.05f,90,0,50,40,2,2,1,false,true);
 policy.Step(.05f,90,0,50,40,1,2,1,true,true);Check(policy.Side==1,"In-progress firing pass is retained after partial battery damage");
 policy.Reset();policy.Step(.05f,90,0,50,40,1,2,1,false,true);Check(policy.Side==2,"New pass selects stronger battery");
 policy.Step(.05f,90,0,50,40,0,2,0,false,true);Check(policy.Side==2,"Destroyed battery forces surviving side");
 for(int i=0;i<300;i++)policy.Step(.05f,90,0,50,40,0,2,2,false,true);Check(policy.Phase==3,"Geometric sector without real shots cannot hold a deadlock forever");
 Console.WriteLine("PASS: "+scenarios+" two-ship CPU battles, "+total+" shots, 5s reload, 2.5s aim, real rudder/inertia, hull footprints and broken batteries. Not a Unity/PhysX playtest.");
 }
}
