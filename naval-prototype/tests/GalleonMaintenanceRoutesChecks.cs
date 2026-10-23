using System;
class GalleonMaintenanceRoutesChecks {
 static void Check(bool value,string why){if(!value)throw new Exception(why);}
 static void Main(){var ship=NavalShipDefinition.Galleon;int routes=0;
 for(int deck=0;deck<ship.MaintenanceDeckCount;deck++){
 float floor=ship.MaintenanceFloor(deck);
 var starts=new NavalWalkPoint[]{new NavalWalkPoint(0,floor,NavalPlankSupplyLayout.PickupZ(ship.Scale)),new NavalWalkPoint(0,floor,NavalCannonSupplyLayout.PickupZ(ship.Scale)),new NavalWalkPoint(0,floor,-4.8f),new NavalWalkPoint(2.35f,floor+NavalBallastLayout.Height/ship.Scale,4.9f)};
 foreach(var start in starts)for(int gun=0;gun<6;gun++){
 var goal=new NavalWalkPoint(NavalShipDefinition.GunSide(gun)*ship.GunPostX,ship.GunDeck,NavalShipDefinition.GunZ(ship,gun));
 var path=NavalSupplyRoutes.Plan(start,goal,ship);var last=path[path.Count-1];Check(last.Y==goal.Y,"Stock/tank-to-gun route reaches upper deck");
 var back=NavalSupplyRoutes.Plan(goal,start,ship);last=back[back.Count-1];Check(last.Y==start.Y,"Return reaches correct maintenance floor, including middle tank top");routes+=2;
 }
 }
 var onTank=new NavalWalkPoint(2.35f,.825f+NavalBallastLayout.Height/ship.Scale,4.9f);
 var middle=NavalShipRoutes.Plan(onTank,new NavalWalkPoint(0,.825f,-4.8f),ship);
 foreach(var point in middle)Check(point.Y<ship.WeatherDeck-.3f,"Middle ballast tank is not misclassified as weather deck");
 var old=NavalShipDefinition.Brigantine;Check(old.SinkingVolume==BoatDimensions.SinkVolume*old.VolumeScale,"Brigantine sinking threshold unchanged");
 Console.WriteLine("PASS: "+routes+" routes linking both maintenance decks, all six guns, upper access and middle tank descent.");
 }
}
