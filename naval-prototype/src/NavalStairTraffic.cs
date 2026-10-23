using System;
using System.Collections.Generic;
// Keep right in the direction of travel; lanes fit the smallest full-size crew stair.
public static class NavalStairTraffic
{
 public static float Lane(float scale,float width){return Math.Min(width*.26f,.55f/scale);}
 public static bool InPassage(NavalWalkPoint point,NavalShipDefinition ship)
 {
  if(AtFlight(point,ship,0))return true;
  return ship.DeckCount>2&&AtFlight(point,ship,ship.UpperDeckOffset);
 }
 private static bool AtFlight(NavalWalkPoint p,NavalShipDefinition ship,float offset)
 {
  if(Math.Abs(p.X)>ship.StairWidth*.5f+.45f/ship.Scale||p.Z< -3.85f||p.Z>1.05f)return false;
  float expected=p.Z< -3?ship.HoldFloor:p.Z>.5f?.825f:NavalWalkRoutes.SurfaceY(p.Z,ship.HoldFloor);
  float rise=(p.Y-offset-expected)*ship.Scale;return rise>-.35f&&rise<.7f;
 }
 private static bool Tread(NavalWalkPoint p,NavalShipDefinition ship,float offset)
 {
  return Math.Abs(p.X)<ship.StairWidth*.5f+.05f&&p.Z> -3.05f&&p.Z<.55f&&
   Math.Abs(p.Y-offset-NavalWalkRoutes.SurfaceY(p.Z,ship.HoldFloor))*ship.Scale<.4f;
 }
 public static List<NavalWalkPoint> Apply(NavalWalkPoint start,List<NavalWalkPoint> route,NavalShipDefinition ship)
 {
  if(route.Count==0)return route;NavalWalkPoint destination=route[route.Count-1];
  // Only apply a lane to flights actually traversed, never to a route underneath the stairs.
  for(int flight=0;flight<ship.DeckCount-1;flight++){
   float offset=flight==0?0:ship.UpperDeckOffset;int direction=0;var previous=start;
   foreach(var point in route){
    if((Tread(point,ship,offset)||Tread(previous,ship,offset))&&Math.Abs(point.Y-previous.Y)>.005f){direction=point.Y>previous.Y?1:-1;break;}
    previous=point;
   }
   if(direction==0)continue;
   float lane=Lane(ship.Scale,ship.StairWidth)*direction;
   for(int i=0;i<route.Count;i++){
    var point=route[i];if(!AtFlight(point,ship,offset)||Math.Abs(point.X)>ship.StairWidth*.5f+.05f)continue;
    point.X=lane;route[i]=point;
   }
  }
  // A destination at the staircase is still the caller's requested final position.
  route[route.Count-1]=destination;
  return route;
 }
 public static bool FriendlyPass(bool sameTeam,bool aOnPassage,bool bOnPassage,float separation)
 {return sameTeam&&(aOnPassage||bOnPassage)&&separation<2.5f;}
}
