using System;
class RouteFailureMemoryChecks{
 static void Main(){var m=new NavalRouteFailureMemory();m.Mark(0,0,0,1,0,0,2,5);
  if(!m.Blocked(.1f,0,0,1,0,0,2,6))throw new Exception("Repeated blocked segment");
  if(m.Blocked(.1f,0,0,1,0,0,3,6)||m.Blocked(.1f,0,0,1,0,0,2,16)||m.Blocked(.1f,0,0,1,0,1,2,6)||m.Blocked(2,0,0,1,0,0,2,6))throw new Exception("New geometry, time, approach or start must permit retry");
  Console.WriteLine("PASS: repeated segment suppressed, alternate approach and changed geometry/start/time remain available.");
 }
}
