using System;
using System.Collections.Generic;
class StairCapsuleTravelChecks {
 static void Check(bool value,string why){if(!value)throw new Exception(why);}
 static bool Traverse(NavalShipDefinition ship,bool upward,bool fixedArrival){
 var points=upward?NavalWalkRoutes.UpFrom(ship.HoldFloor,-3.65f,ship.HoldFloor):NavalWalkRoutes.Down(ship.HoldFloor);
 float z=upward?-3.65f:.8f;int next=0;
 for(int frame=0;frame<4000&&next<points.Count;frame++){
  var target=points[next];float step=.06f/ship.Scale;z+=Math.Max(-step,Math.Min(step,target.Z-z));
  // Rounded lower capsule contacts the next higher tread, not the ideal centre sample.
  float support=z+.45f/ship.Scale;
  float ground=support< -3?ship.HoldFloor:NavalWalkRoutes.SurfaceY(support,ship.HoldFloor);
  float feet=ground+.08f/ship.Scale;
  bool reached=fixedArrival?NavalWalkRoutes.Reached(0,feet-target.Y,z-target.Z,ship.Scale):Math.Abs(feet-target.Y)*ship.Scale<.32f&&Math.Abs(z-target.Z)*ship.Scale<.2f;
  if(reached)next++;
 }
 return next==points.Count;
 }
 static void Main(){try{Run();}catch(Exception e){Console.WriteLine(e.Message);Environment.ExitCode=1;}}
 static void Run(){foreach(var ship in NavalShipDefinition.Choices){
 Check(Traverse(ship,false,true),ship.Name+": descending capsule follows every tread through contact-height lag");
 Check(Traverse(ship,true,true),ship.Name+": ascending capsule follows every tread through contact-height lag");
 Check(!Traverse(ship,false,false),ship.Name+": old symmetric arrival reproduces stalled descent");
 }
 Console.WriteLine("PASS: rounded-capsule step-contact simulation reproduces old descent lock and completes ascent/descent on every ship with corrected arrival. No native PhysX test.");
 }
}
