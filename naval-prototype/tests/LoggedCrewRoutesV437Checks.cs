using System;
class LoggedCrewRoutesV437Checks{
 static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
 static void Main(){var s=NavalShipDefinition.Novik;float q=s.Scale;
  foreach(int sign in new[]{-1,1}){
   var start=new NavalWalkPoint(sign*5.6f/q,s.WeatherDeck,29.6f/q);
   var goal=new NavalWalkPoint(sign*5.6f/q,NavalDestroyerLayout.Foredeck(s),32f/q);
   var path=NavalDestroyerLayout.Route(start,goal,s);int steps=0;
   foreach(var p in path)if(NavalDestroyerLayout.ForedeckStair(p,s)&&p.Y>s.WeatherDeck+.1f/q)steps++;
   Check(steps>=12,"Below forecastle lip must approach real stairs, not assume already on raised deck");
   Check(path[0].Y==s.WeatherDeck,"Initial lane must retain actual lower deck height");
  }
  Check(NavalRouteArrival.ReachedWorld(0,.43f/q,0,q,false),"Logged deck skin height must not block a reached transit point");
  Check(!NavalRouteArrival.ReachedWorld(0,1.2f/q,0,q,false),"Forecastle height difference must not be accepted as arrival");
  Check(!NavalRouteArrival.ReachedWorld(.5f/q,0,0,q,false),"Corner horizontal precision remains strict");
  Check(NavalStairRecovery.Arrived(.25f/q,.12f/q,0,q),"Underflight recovery accepts physically reached clear landing");
  Check(!NavalStairRecovery.Arrived(1f/q,0,0,q),"No remote recovery completion");
  Console.WriteLine("PASS: logged forecastle lip routes, floor skin tolerance and underflight exit precision.");
 }
}
