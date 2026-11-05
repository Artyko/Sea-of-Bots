using System;
class PairedRackChecks {
 static void Main(){foreach(var ship in new[]{NavalShipDefinition.Sloop,NavalShipDefinition.Brigantine,NavalShipDefinition.Galleon,NavalShipDefinition.Ironclad,NavalShipDefinition.Dreadnought})for(int kind=0;kind<3;kind++){
 int sum=0;for(int i=0;i<NavalCargoLayout.Boxes(ship,kind);i++){int units=NavalCargoLayout.Units(ship,kind,i);if(units<1||units>2)throw new Exception("Invalid paired capacity");sum+=units;}
 if(sum!=NavalCargoLayout.OriginalBoxes(ship,kind))throw new Exception("Resources lost in pairing");}
 Console.WriteLine("PASS: paired racks retain all ammunition, boards and parts on all five ship types.");}
}
