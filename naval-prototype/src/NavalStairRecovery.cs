using System;
using System.Collections.Generic;
// A displaced sailor must leave underneath the flight before following treads above him.
public static class NavalStairRecovery
{
 public static bool Needed(NavalWalkPoint feet,NavalWalkPoint target,NavalShipDefinition ship,out float floor)
 {
  floor=ship.HoldFloor;
  // Recover under the flight for every duty, including floor-level cargo routes.
  for(int level=ship.DeckCount-2;level>=0;level--){
   float offset=level==0?0:ship.UpperDeckOffset;
   float surface=NavalWalkRoutes.SurfaceY(feet.Z,ship.HoldFloor)+offset;
   if(feet.Z> -3.05f&&feet.Z<.55f&&Math.Abs(feet.X)<ship.StairWidth*.5f+.45f/ship.Scale&&
      feet.Y>=ship.HoldFloor+offset-.15f&&(surface-feet.Y)*ship.Scale>.8f){floor=ship.HoldFloor+offset;return true;}
  }
  return false;
 }
 public static List<NavalWalkPoint> Exit(NavalWalkPoint feet,NavalWalkPoint goal,NavalShipDefinition ship,float floor,int side)
 {
  float x=side*(ship.StairWidth*.5f+.65f/ship.Scale);
  var exit=new NavalWalkPoint(x,feet.Y,feet.Z);
  var landing=new NavalWalkPoint(x,floor,feet.Z);
  var result=new List<NavalWalkPoint>();result.Add(exit);result.Add(landing);
  result.AddRange(NavalSupplyRoutes.Plan(landing,goal,ship));return result;
 }
}
