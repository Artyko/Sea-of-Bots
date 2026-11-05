using System;
class RaisedBridgeChecks {
 static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
 static void Main(){int count=0;foreach(var ship in new[]{NavalShipDefinition.Ironclad,NavalShipDefinition.Dreadnought}){
  Check(Math.Abs((ship.HelmDeck-ship.WeatherDeck)*ship.Scale-3.2f)<.001f,"Command level rises 3.2m");
  Check(NavalRaisedBridge.StairWidth>1.6f+.2f,"Full capsule and skin fit internal stair");
  Check(NavalRaisedBridge.Rise-.16f>2.5f+.2f,"Vestibule headroom");
  Check(NavalRaisedBridge.HalfDepth(ship)*ship.Scale>NavalRaisedBridge.Run*.5f+.85f,"Opening extends past the treads for capsule head clearance");
  var posts=new[]{new NavalWalkPoint(1.55f,ship.MaintenanceFloor(ship.MaintenanceDeckCount-1),-4.55f),new NavalWalkPoint(0,ship.HoldFloor,-4.9f),new NavalWalkPoint(.92f,ship.WeatherDeck,0),new NavalWalkPoint(0,ship.HelmDeck,NavalShipDefinition.HelmZ(ship))};
  foreach(var a in posts)foreach(var b in posts){var route=NavalSupplyRoutes.Plan(a,b,ship);Check(route.Count>0,"Route exists");var prior=a;
   foreach(var p in route){if(Math.Abs(prior.Y-p.Y)*ship.Scale>.6f)throw new Exception("Vertical jump in internal access");prior=p;}
   Check(Math.Abs(prior.Y-b.Y)+Math.Abs(prior.X-b.X)+Math.Abs(prior.Z-b.Z)<.001f,"Exact goal");count++;
  }
 }
 Console.WriteLine("PASS: raised bridge headroom, internal stair clearance and "+count+" bidirectional helm/hold/gun routes.");}
}
