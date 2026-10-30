using System;
using System.Collections.Generic;
// A displaced sailor must leave underneath the flight before following treads above him.
public static class NavalStairRecovery
{
 public static float SideLane(NavalShipDefinition ship,float bodyRadius=.8f)
 {return ship.StairWidth*.5f+(Math.Max(.35f,bodyRadius)+1.6f)/ship.Scale;}
 public static bool Needed(NavalWalkPoint feet,NavalWalkPoint target,NavalShipDefinition ship,out float floor)
 {
  floor=ship.HoldFloor;
  if(NavalStairTraffic.InPassage(feet,ship))return false;
  // Recover under the flight for every duty, including floor-level cargo routes.
  for(int level=ship.DeckCount-2;level>=0;level--){
   float offset=level==0?0:ship.UpperDeckOffset;
   float stairFloor=level==0?ship.HoldFloor:ship.UpperStairFloor;
   float surface=NavalWalkRoutes.SurfaceY(feet.Z,stairFloor)+offset;
   if(feet.Z> -3.05f&&feet.Z<.55f&&Math.Abs(feet.X)<ship.StairWidth*.5f+.45f/ship.Scale&&
      feet.Y>=stairFloor+offset-.15f&&(surface-feet.Y)*ship.Scale>.8f){floor=stairFloor+offset;return true;}
  }
  return false;
 }
 public static bool Arrived(float dx,float dy,float dz,float scale)
 {return (dx*dx+dz*dz)*scale*scale<.12f*.12f&&Math.Abs(dy)*scale<.35f;}
 public static List<NavalWalkPoint> Exit(NavalWalkPoint feet,NavalWalkPoint goal,NavalShipDefinition ship,float floor,int side,float bodyRadius=.45f)
 {
  // The old .30 m margin ended exactly on the broad native capsule cast at
  // x ~= .83 on the ironclad. The next tick then rebuilt the same escape.
  float x=side*SideLane(ship,bodyRadius);
  var exit=new NavalWalkPoint(x,feet.Y,feet.Z);
  var landing=new NavalWalkPoint(x,floor,feet.Z);
  var result=new List<NavalWalkPoint>();result.Add(exit);result.Add(landing);
  // Do not let the freshly planned route cut straight back under the same
  // flight. Stay in the side aisle until beyond either end of its footprint.
  var clear=new NavalWalkPoint(x,floor,goal.Z>=feet.Z?.80f:-3.25f);result.Add(clear);
  result.AddRange(NavalSupplyRoutes.Plan(clear,goal,ship));return result;
 }
}
