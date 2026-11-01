using System;
class RouteCornerArrivalChecks {
 static void Main(){foreach(float scale in new float[]{2,3,4}){
 if(NavalRouteArrival.ReachedWorld(.25f,0,0,scale,false))throw new Exception("Must not skip corner before capsule clears obstacle");
 if(!NavalRouteArrival.ReachedWorld(.1f/scale,.1f/scale,0,scale,false))throw new Exception("Must accept physically reached corner");
 if(NavalRouteArrival.ReachedWorld(0,1f/scale,0,scale,false))throw new Exception("Wrong deck must not count as arrival");
 }Console.WriteLine("PASS: route corner arrival uses world distance on all ship scales and rejects wrong-deck shortcuts.");}
}
