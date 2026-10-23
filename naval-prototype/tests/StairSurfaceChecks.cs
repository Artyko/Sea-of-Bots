using System;
class StairSurfaceChecks {
 static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
 static void Main(){foreach(var ship in NavalShipDefinition.Choices){
 Check(!NavalWalkRoutes.InStairwell(0,-.28f,.41f,ship.StairWidth*.5f+.175f,ship.HoldFloor),"Logged robot below top treads is not on the stairs");
 for(int i=6;i<42;i+=3){float z=-3+(i+.5f)*3.5f/48;float y=NavalWalkRoutes.SurfaceY(z,ship.HoldFloor);
 Check(NavalWalkRoutes.InStairwell(0,y,z,ship.StairWidth*.5f+.175f,ship.HoldFloor),"Real staircase surface remains recognized");}
 var start=new NavalWalkPoint(0,-.28f,.41f);var goal=new NavalWalkPoint(1.35f,ship.WeatherDeck,3.3f);
 var route=NavalShipRoutes.Plan(start,goal,ship);Check(route.Count>0,"Route out from underneath exists");
 Check(route[0].Y<=start.Y&&Math.Abs(route[0].X)>1,"Leave underneath before attempting ascent through solid treads");
 Check(!NavalWalkRoutes.Reached(0,-1f/ship.Scale,0,ship.Scale),"Cannot advance step with feet a metre below it");
 Check(NavalWalkRoutes.Reached(.05f/ship.Scale,.1f/ship.Scale,.05f/ship.Scale,ship.Scale),"Controller skin and real stair motion remain tolerated");}
 Console.WriteLine("PASS: logged underneath-stairs regression, real tread classification, exit-first routes and world-space waypoint tolerance on all ships.");}
}
