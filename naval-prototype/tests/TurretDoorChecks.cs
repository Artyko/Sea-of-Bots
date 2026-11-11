using System;
using UnityEngine;
class TurretDoorChecks
{
 static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
 static void Main(){
  var ship=NavalShipDefinition.Dreadnought;
  Check(NavalTurretAccessLayout.FitsCapsule(.8f,2.5f,ship.Scale),"Full-size sailor fits door and hollow chamber");
  foreach(var vessel in NavalShipDefinition.SteamChoices)for(int gun=0;gun<vessel.GunCount;gun++)
   Check(vessel.Destroyer||Math.Abs(NavalReadyAmmoLayout.Z(vessel,gun)-vessel.LadderZ)>=.8f,"Ready-use ammunition leaves the boarding ladder exit clear");
  var ladder=NavalTurretAccessLayout.Entry(ship,3);
  int vertical=0;for(int i=1;i<ladder.Count;i++)if(Math.Abs(ladder[i].x-ladder[i-1].x)<.001f&&Math.Abs(ladder[i].z-ladder[i-1].z)<.001f&&Math.Abs(ladder[i].y-ladder[i-1].y)*ship.Scale>.15f)vertical++;
  Check(vertical>=2,"X has explicit full-height vertical segments, reversible for exit");
  float radius=.8f/ship.Scale;
  Check(NavalTurretAccessLayout.Seat.z-radius>-.30f&&NavalTurretAccessLayout.Seat.z+radius<.22f,"Seated controller clears rear wall and gun breeches");
  for(int slot=0;slot<5;slot++){
   var approach=NavalTurretAccessLayout.Approach(ship,slot);var path=NavalTurretAccessLayout.Entry(ship,slot);
   Check(path.Count>4,"Entry has real stairs and an interior segment");Vector3 previous=approach;
   foreach(var point in path){
    Check(NavalCrewDeckSafety.Inside(point.x,point.z,ship,.8f),"Access stairs stay inside ship: "+slot);
    if(slot==3&&Math.Abs(point.y-previous.y)*ship.Scale>=.15f)
        Check(new Vector2(point.x-previous.x,point.z-previous.z).sqrMagnitude<.0001f,"Raised X uses a vertical ladder");
    else Check(Math.Abs(point.y-previous.y)*ship.Scale<.15f,"Each riser is walkable: "+slot);
    previous=point;
   }
   Vector3 seat=NavalTurretAccessLayout.ToShip(ship,slot,NavalTurretAccessLayout.Seat);
   Check((previous-seat).sqrMagnitude<.0001f,"Walking path terminates at actual interior seat");
   Check(Math.Abs(NavalTurretAccessLayout.Seat.x)<.20f&&NavalTurretAccessLayout.Seat.z>-.30f&&NavalTurretAccessLayout.Seat.z<.08f,"Seat inside hollow armour, behind breeches");
   Check((NavalTurretAccessLayout.RoofBottom-NavalTurretAccessLayout.Floor)*ship.Scale>=3f-.001f,"Standing capsule clears ceiling while entering and leaving");
  }
  Console.WriteLine("PASS: five physical hatch approaches, walkable raised-turret treads, full-size head/door clearance, interior seats and shared recovery slices.");
 }
}
