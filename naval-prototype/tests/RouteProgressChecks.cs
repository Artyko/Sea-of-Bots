using System;
class RouteProgressChecks
{
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 static void Main(){
  float blocked=0;for(int i=0;i<100;i++)blocked=NavalRouteProgress.Step(blocked,2f,2f,.02f,false,false);
  Check(Math.Abs(blocked-2f)<.0001f,"Stationary robot accumulates obstruction time");
  Check(NavalRouteProgress.Step(blocked,2f,1.99f,.02f,false,false)==0f,"Walking progress resets obstruction");
  Check(NavalRouteProgress.Step(blocked,2f,2f,.02f,false,true)==0f,"Removing a shield is an intentional wait");
  Check(NavalRouteProgress.Step(blocked,0f,0f,.02f,true,false)==0f,"Waypoint arrival resets obstruction");
  Check(NavalRouteProgress.Step(1f,2f,2.1f,.02f,false,false)>1f,"Moving away is not forward progress");
  Check(NavalRouteProgress.Step(1f,2f,2f,-1f,false,false)==1f,"Negative timestep cannot reduce obstruction time");
  Console.WriteLine("Shared route progress checks passed");
 }
}
