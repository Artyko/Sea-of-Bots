using System;
class StationPassRegressionChecks {
 static void Check(bool ok,string reason){if(!ok)throw new Exception(reason);}
 static void Main(){try{Run();}catch(Exception e){Console.WriteLine(e);Environment.ExitCode=1;}}
 static void Run(){
 Check(NavalRouteArrival.Reached(.13f,0,0,0,0,0,true)&&!NavalStationArrival.CanAnchor(.13f*.13f*16),"Reproduce galleon final-waypoint/anchor gap");
 foreach(var ship in NavalShipDefinition.Choices){
 Check(!NavalStationArrival.CanAnchor(.5f*.5f),"Do not finish station route half a metre away");
 Check(NavalStationArrival.CanAnchor(.35f*.35f),"Can finish and anchor with same world tolerance");
 }
 var pass=new NavalFirePassMath();pass.Step(.05f,90,0,50,40,2,2,1,true,true);
 float heading=pass.Heading;
 for(int i=0;i<500;i++){
 float bearing=i*.01f;pass.Step(.05f,90+bearing,bearing,50,40,2,2,1,i%100==0,true);
 Check(pass.Phase==2,"Successful firing pass must not end unconditionally after sixteen seconds");
 Check(Math.Abs(NavalBroadsideMath.Delta(heading,pass.Heading))<=.601f,"Bounded live target tracking");heading=pass.Heading;
 }
 for(int i=0;i<300;i++)pass.Step(.05f,95,5,50,40,2,2,1,false,true);
 Check(pass.Phase==3,"No actual shots still triggers separation");
 Check(NavalCubeSmokeMath.RiseSpeed*NavalSmokePlumeMath.Lifetime>=43f,"Taller fire column without longer lifetime");
 Check(NavalSmokePlumeMath.ColumnInterval*NavalCubeSmokeMath.RiseSpeed<NavalSmokePlumeMath.ColumnSize,"Taller fire puffs remain connected");
 Console.WriteLine("PASS: galleon station mismatch reproduction, successful 25-second broadside, dry-pass escape, bounded steering and taller coherent smoke.");
 }
}
