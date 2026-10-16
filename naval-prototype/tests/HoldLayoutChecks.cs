using System;
class HoldLayoutChecks
{
 static void Check(bool ok,string name){if(!ok)throw new Exception(name);}
 static void Near(float a,float b){Check(Math.Abs(a-b)<.0001f,"Geometry mismatch");}
 static void Main(){
 foreach(var ship in new[]{NavalShipDefinition.Sloop,NavalShipDefinition.Brigantine}){
  float floor=ship.HoldFloor;
  Near(ship.HullY(BoatDimensions.HoldFloor),floor);Near(ship.HullY(.575f),.575f);
  Near(ship.HullY(.825f),.825f);
  Check(ship.HullY(-2.35f)<floor,"Bottom stays below walkable floor");
  float partition=NavalCompartmentMath.PartitionHeight(ship.Scale);
  Near(partition*ship.Scale,2.2f);
  Check(partition<.575f-floor,"Partition does not reach ceiling");
  Near(NavalCompartmentMath.CoamingHeight(ship.Scale)*ship.Scale,.45f);
  Check(NavalCompartmentMath.CoamingHeight(ship.Scale)*ship.Scale/4f<.15f,"Coaming has small walkable risers");
  var down=NavalWalkRoutes.Down(floor);Near(down[down.Count-1].Y,floor);
  foreach(var p in down)Check(p.Y>=floor&&p.Y<=.825f,"Stair route stays between actual decks");
  var route=NavalRepairRoutes.Plan(new NavalWalkPoint(-2f,floor,-4f),new NavalWalkPoint(2f,floor,4f),true,ship.StairWidth,floor);
  foreach(var p in route)Near(p.Y,floor);
  var escape=NavalRepairRoutes.Plan(new NavalWalkPoint(-2f,floor,-4f),new NavalWalkPoint(1.65f,.825f,1.1f),true,ship.StairWidth,floor);
  foreach(var p in escape)Check(p.Y>=floor,"Escape never targets the previous lower deck");
  Check(NavalHoldEscapeRules.InHold(2f,floor,3f,floor),"Flooded hold detected at actual floor");
  Check(!NavalHoldEscapeRules.InHold(2f,floor-.5f,3f,floor),"Below keel is open sea");
  float reach=(.55f-floor-.8f)*ship.Scale;
  Check(reach*reach+2f*2f<4.8f*4.8f,"Ceiling repair reachable with horizontal working distance");
 }
 Near(NavalShipDefinition.Brigantine.HoldClearance,NavalShipDefinition.Sloop.HoldClearance);
 Near(NavalShipDefinition.Brigantine.HoldClearance,5.6f);
 Near(NavalShipDefinition.Sloop.HoldFloor,BoatDimensions.HoldFloor);
 var grid=new NavalFloodGrid(2,1,2f,1f,1f,4f);grid.RightSill[0]=2.2f;grid.Add(0,2f);
 grid.Spread(.2f,new float[2],1f);Near(grid.Water[1],0f);
 grid.Add(0,.6f);grid.Spread(.2f,new float[2],1f);Check(grid.Water[1]>0f,"Low partition overtops above robot height");Near(grid.Total,2.6f);
 Console.WriteLine("Hold layout checks passed: floor, shell, stairs, routes, escape, ceiling reach, raised coamings and low-partition overflow.");
 }
}
