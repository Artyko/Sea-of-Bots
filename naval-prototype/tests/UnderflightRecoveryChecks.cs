using System;
class UnderflightRecoveryChecks {
 static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
 static void Main(){try{Run();}catch(Exception e){Console.WriteLine(e);Environment.ExitCode=1;}}
 static void Run(){
 var brig=NavalShipDefinition.Brigantine;float floor;
 Check(NavalStairRecovery.Needed(new NavalWalkPoint(.34f,-1.32f,-1.36f),new NavalWalkPoint(.47f,-1.29f,-1.78f),brig,out floor),"v217 floor-level plank route under staircase must recover");
 Check(NavalStairRecovery.Needed(new NavalWalkPoint(.36f,-1.31f,-1.29f),new NavalWalkPoint(.47f,-1.29f,-1.78f),brig,out floor),"v217 returning route under staircase must recover");
 Check(NavalStairRecovery.Needed(new NavalWalkPoint(.06f,-.82f,-1.34f),new NavalWalkPoint(.14f,-.79f,-2.96f),NavalShipDefinition.Galleon,out floor),"Logged plank carrier under flight must exit before approaching lower treads");
 Check(NavalStairRecovery.Needed(new NavalWalkPoint(.22f,-.71f,-.33f),new NavalWalkPoint(.18f,.34f,-.34f),brig,out floor),"Exact logged stall must trigger recovery");
 foreach(var ship in NavalShipDefinition.Choices)for(int flight=0;flight<ship.DeckCount-1;flight++){
 float offset=flight==0?0:ship.UpperDeckOffset;
 float z=-.33f,y=NavalWalkRoutes.SurfaceY(z,ship.HoldFloor)+offset;
 var feet=new NavalWalkPoint(.1f,y-1.2f/ship.Scale,z);var target=new NavalWalkPoint(.1f,y,z);
 Check(NavalStairRecovery.Needed(feet,target,ship,out floor),ship.Name+" must recover both flights");
 Check(Math.Abs(floor-(ship.HoldFloor+offset))<.001f,"Use this flight's floor");
 var goal=new NavalWalkPoint(1.4f,ship.WeatherDeck,1.6f);
 foreach(int side in new int[]{-1,1}){
 var route=NavalStairRecovery.Exit(feet,goal,ship,floor,side);
 Check(Math.Abs(route[0].X)>ship.StairWidth*.5f+.6f/ship.Scale,"Clear capsule beyond staircase edge");
 Check(route[0].Y==feet.Y&&route[0].Z==feet.Z,"Leave sideways without climbing through treads");
 Check(route[1].Y==floor,"Descend to actual floor before new route");
 var last=route[route.Count-1];Check(last.X==goal.X&&last.Y==goal.Y&&last.Z==goal.Z,"Keep original job destination");
 }
 Check(!NavalStairRecovery.Needed(target,target,ship,out floor),"Ordinary stair traffic unchanged");
 feet.Y=y-.2f/ship.Scale;Check(!NavalStairRecovery.Needed(feet,target,ship,out floor),"Normal contact skin tolerance unchanged");
 feet.X=ship.StairWidth;feet.Y=y-1.2f/ship.Scale;Check(!NavalStairRecovery.Needed(feet,target,ship,out floor),"No false recovery outside flight");
 }
 Console.WriteLine("PASS: logged upward stall, both galleon flights, sideways exits, unchanged destinations and normal stair traffic.");
 }
}
