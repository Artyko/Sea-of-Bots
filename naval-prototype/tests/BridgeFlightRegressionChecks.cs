using System;
class BridgeFlightRegressionChecks{
 static void Main(){var s=NavalShipDefinition.Dreadnought;
  if(NavalRaisedBridge.Flight(new NavalWalkPoint(.15f,s.WeatherDeck+.01f,2.88f),s))throw new Exception("Ground beside wall is not a stair");
  if(NavalRaisedBridge.ReachedFlight(0,-.48f/s.Scale,0,s.Scale))throw new Exception("Do not step sideways half a metre below landing");
  if(!NavalRaisedBridge.ReachedFlight(0,.54f/s.Scale,0,s.Scale))throw new Exception("Supported capsule above a tread must advance");
  if(!NavalLedgeRecoveryMath.Arrived(0,.40f/s.Scale,0,s.Scale)||NavalLedgeRecoveryMath.Arrived(0,-.4f/s.Scale,0,s.Scale))throw new Exception("Support-height tolerance must be asymmetric");
  var captain=new NavalWalkPoint(.45f,1.99f,3.29f);
  if(!NavalRaisedBridge.Flight(captain,s))throw new Exception("Logged captain pose must remain on bridge stair");
  System.Collections.Generic.List<NavalWalkPoint> up;
  if(!NavalRaisedBridge.TryRoute(captain,new NavalWalkPoint(0,s.HelmDeck,NavalShipDefinition.HelmZ(s)),s,out up))throw new Exception("Captain helm route");
  foreach(var point in up)if(point.Y<captain.Y-.65f/s.Scale)throw new Exception("Captain must continue upward rather than restart ascent");
  foreach(var p in NavalRaisedBridge.Ascent(s))if(p.X>.3f&&p.Y>s.WeatherDeck+.01f&&p.Y<NavalRaisedBridge.Top(s)-.01f&&!NavalRaisedBridge.Flight(p,s))throw new Exception("Actual tread rejected");
  if(NavalGunStationLayout.RearDistance<NavalGunStationLayout.ShieldHalfWidth+.8f+.5f)throw new Exception("Full-size gunner clearance");
  Console.WriteLine("PASS: logged wall pose rejected, real treads accepted, landing height and full-size gunner clearance.");
 }
}
