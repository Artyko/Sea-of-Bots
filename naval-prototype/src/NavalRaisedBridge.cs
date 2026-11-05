using System;
using System.Collections.Generic;
public static class NavalRaisedBridge {
 public const float Rise=3.2f,Run=3.4f,StairX=2.2f,StairWidth=1.9f;
 public const int Steps=24;
 public static float Center(NavalShipDefinition s){return s.Destroyer?33f/s.Scale:s.CapitalShip?NavalCapitalLayout.BridgeZ:NavalConningTowerLayout.CenterZ;}
 public static float HalfWidth(NavalShipDefinition s){return s.Destroyer?3.5f/s.Scale:s.CapitalShip?.70f:NavalConningTowerLayout.HalfWidth;}
 public static float HalfDepth(NavalShipDefinition s){return s.Destroyer?3f/s.Scale:s.CapitalShip?.50f:NavalConningTowerLayout.HalfDepth;}
 public static float Base(NavalShipDefinition s){return s.Destroyer?NavalDestroyerLayout.Foredeck(s):s.WeatherDeck;}
 public static float Top(NavalShipDefinition s){return Base(s)+Rise/s.Scale;}
 public static NavalWalkPoint Entry(NavalShipDefinition s){if(s.Destroyer)return new NavalWalkPoint(0,Base(s),Center(s)+4f/s.Scale);return new NavalWalkPoint(0,Base(s),Center(s)+(s.CapitalShip?-1:1)*(HalfDepth(s)+(s.Destroyer?1.2f:.7f)/s.Scale));}
 public static bool Upper(NavalWalkPoint p,NavalShipDefinition s){return s.SteamPowered&&p.Y>=Top(s)-.10f/s.Scale&&Math.Abs(p.X)<HalfWidth(s)&&Math.Abs(p.Z-Center(s))<HalfDepth(s);}
 public static bool Flight(NavalWalkPoint p,NavalShipDefinition s){return s.SteamPowered&&Math.Abs(p.X-(s.Destroyer?2.1f:StairX)/s.Scale)<((s.Destroyer?2.4f:StairWidth)*.5f+.4f)/s.Scale&&Math.Abs(p.Z-Center(s))<(Run*.5f+.5f)/s.Scale&&p.Y>=Base(s)-.1f&&p.Y<=Top(s)+.1f;}
 public static List<NavalWalkPoint> Ascent(NavalShipDefinition s){var r=new List<NavalWalkPoint>();float z=Center(s),q=s.Scale;
  if(s.Destroyer){r.Add(Entry(s));r.Add(new NavalWalkPoint(2.1f/q,Base(s),z+4f/q));r.Add(new NavalWalkPoint(2.1f/q,Base(s),z+Run*.5f/q));for(int i=1;i<=Steps;i++)r.Add(new NavalWalkPoint(2.1f/q,Base(s)+Rise*i/Steps/q,z+(Run*.5f-Run*i/Steps)/q));r.Add(new NavalWalkPoint(2.1f/q,Top(s),z-2.2f/q));r.Add(new NavalWalkPoint(0,Top(s),z-2.2f/q));r.Add(new NavalWalkPoint(0,Top(s),z));return r;}

  r.Add(Entry(s));r.Add(new NavalWalkPoint(0,Base(s),z));r.Add(new NavalWalkPoint(0,Base(s),z+Run*.5f/q));r.Add(new NavalWalkPoint(StairX/q,Base(s),z+Run*.5f/q));
  for(int i=1;i<=Steps;i++)r.Add(new NavalWalkPoint(StairX/q,Base(s)+Rise*i/Steps/q,z+(Run*.5f-Run*i/Steps)/q));
  r.Add(new NavalWalkPoint(0,Top(s),z-Run*.5f/q));r.Add(new NavalWalkPoint(0,Top(s),z));return r;
 }
 public static bool TryRoute(NavalWalkPoint start,NavalWalkPoint goal,NavalShipDefinition s,out List<NavalWalkPoint> route){
  route=null;if(!s.SteamPowered)return false;bool from=Upper(start,s)||Flight(start,s),to=Upper(goal,s);if(!from&&!to)return false;
  route=new List<NavalWalkPoint>();var up=Ascent(s);var entry=Entry(s);
  if(from&&to&&!Flight(start,s)){route.Add(new NavalWalkPoint(0,Top(s),Center(s)-Run*.5f/s.Scale));route.Add(goal);return true;}
  if(from){int nearest=up.Count-1;float best=float.PositiveInfinity;for(int i=0;i<up.Count;i++){float dx=(start.X-up[i].X)*s.Scale,dy=(start.Y-up[i].Y)*s.Scale,dz=(start.Z-up[i].Z)*s.Scale,d=dx*dx+dy*dy+dz*dz;if(d<best){best=d;nearest=i;}}
   for(int i=nearest;i>=0;i--)route.Add(up[i]);
  }else route.AddRange(NavalSupplyRoutes.Plan(start,entry,s));
  if(to){route.AddRange(up);route.Add(goal);}else route.AddRange(NavalSupplyRoutes.Plan(entry,goal,s));return true;
 }
}
