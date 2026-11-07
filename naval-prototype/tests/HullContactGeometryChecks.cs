using System;
class HullContactGeometryChecks{
 static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
 static void Main(){
  foreach(var ship in NavalShipDefinition.SteamChoices){
   float length=ship.HullLength;
   Check(NavalHullContactGeometry.SafeFraction(ship,ship,0,0,0,0,0,0,0,length+.2f,0)==1f,"No end contact beyond physical length");
   float separation=ship.HullHalfWidth(0)*ship.Scale*2f+.2f;
   Check(NavalHullContactGeometry.SafeFraction(ship,ship,0,0,0,0,0,0,separation,0,0)==1f,"No broadside contact across air gap");
   float stop=NavalHullContactGeometry.SafeFraction(ship,ship,-200,0,0,200,0,0,0,0,0);
   Check(stop>0&&stop<.5f,"Swept real hull contact");
   float x=-200+400*stop;
   Check(Math.Abs(x+separation-.2f)<.03f,"Stops at actual maximum beam");
   Check(NavalHullContactGeometry.SafeFraction(ship,ship,x,0,0,x-3,0,0,0,0,0)==1f,"Reverse away from actual contact");
  }
  Console.WriteLine("PASS: physical length/beam gaps, swept hull contact and reverse separation on all steam classes.");
 }
}
