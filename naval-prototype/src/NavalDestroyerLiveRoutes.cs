using System.Collections.Generic;
using UnityEngine;
// Damage-aware route selection is per hull, never cached by ship class or shared with its opponent.
public static class NavalDestroyerLiveRoutes {
 private static List<NavalWalkPoint> Candidate(NavalHull hull,NavalWalkPoint a,NavalWalkPoint b,float z,float side){var s=hull.Definition;var result=new List<NavalWalkPoint>();var entry=NavalRaisedBridge.Entry(s);bool from=NavalRaisedBridge.Upper(a,s)||NavalRaisedBridge.Flight(a,s),to=NavalRaisedBridge.Upper(b,s);var up=NavalRaisedBridge.Ascent(s);
  if(from){int nearest=0;float best=float.PositiveInfinity;for(int i=0;i<up.Count;i++){float dx=up[i].X-a.X,dy=up[i].Y-a.Y,dz=up[i].Z-a.Z,dist=dx*dx+dy*dy+dz*dz;if(dist<best){best=dist;nearest=i;}}for(int i=nearest;i>=0;i--)result.Add(up[i]);a=entry;}
  result.AddRange(NavalDestroyerLayout.Route(a,to?entry:b,s,z,side));if(to){result.AddRange(up);result.Add(b);}return result;
 }
 private static bool ClearFlights(NavalHull hull,List<NavalWalkPoint> route){foreach(var point in route)if(NavalDestroyerLayout.Stair(point,hull.Definition)&&!NavalPhysicsQueries.HasLocalSupport(hull,new Vector3(point.X,point.Y,point.Z)))return false;return true;}
 public static List<NavalWalkPoint> Alternate(NavalHull hull,NavalWalkPoint a,NavalWalkPoint b,int attempt){float side=(a.X<0?-1:1)*(attempt%2==1?-1:1),z=(a.Z<0?-20.5f:20.5f)*(attempt>=2?-1:1);var route=Candidate(hull,a,b,z,side);return ClearFlights(hull,route)?route:Plan(hull,a,b);}
 public static List<NavalWalkPoint> Plan(NavalHull hull,NavalWalkPoint a,NavalWalkPoint b){var normal=NavalSupplyRoutes.Plan(a,b,hull.Definition);if(ClearFlights(hull,normal))return normal;float preferred=a.X<0?-1:1;
  foreach(float side in new[]{preferred,-preferred})foreach(float z in new[]{20.5f,-20.5f}){var alternative=Candidate(hull,a,b,z,side);if(ClearFlights(hull,alternative)){Debug.Log("[NavalPrototype] Destroyer route bypass: ship="+hull.Root.name+" stair="+z+" side="+side);return alternative;}}
  return normal; // Existing recovery/repair dispatch owns a completely severed deck connection.
 }
}
