using System;
class CargoAccountingChecks{
 static void Main(){foreach(var ship in NavalShipDefinition.SteamChoices){
  for(int kind=0;kind<3;kind++){int count=0;for(int rack=0;rack<NavalCargoLayout.Boxes(ship,kind);rack++)count+=NavalCargoLayout.Units(ship,kind,rack);
   int wanted=kind==2?ship.GunCount+(ship.CapitalShip?NavalCapitalLayout.Turrets:0):4*NavalCargoLayout.Factor(ship);
   if(count!=wanted)throw new Exception("Grouped cargo resource accounting");
  }
 }Console.WriteLine("PASS: grouped cargo units preserve ammunition, boards and secondary/main gun spares on all steam profiles.");}
}
