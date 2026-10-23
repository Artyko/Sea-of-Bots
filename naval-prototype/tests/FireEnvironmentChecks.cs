using System;
class FireEnvironmentChecks
{
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 static void Main()
 {
  foreach(var ship in NavalShipDefinition.Choices)
  {
   Check(!NavalFireEnvironmentMath.HoldFire(ship.WeatherDeck-.06f/ship.Scale,ship.WeatherDeck,ship.Scale),"Deck plank centre does not produce phantom hold smoke");
   Check(NavalFireEnvironmentMath.HoldFire(ship.HoldFloor+1/ship.Scale,ship.WeatherDeck,ship.Scale),"Hold fire is classified separately from exposed deck");
  }
  foreach(var ship in NavalShipDefinition.Choices){
   Check(NavalFireEnvironmentMath.SeaContact(ship.WeatherDeck,ship.WeatherDeck,ship.Scale,2,2),"A wave touching deck fire extinguishes it");
   Check(!NavalFireEnvironmentMath.SeaContact(ship.WeatherDeck,ship.WeatherDeck,ship.Scale,1,2),"Dry deck fire is not extinguished by a lower sea");
   Check(!NavalFireEnvironmentMath.SeaContact(ship.HoldFloor,ship.WeatherDeck,ship.Scale,3,0),"External sea cannot extinguish dry hold through intact hull");
  }
  Check(NavalFireEnvironmentMath.Washed(1,-.3f,.2f,0,0,0,2.5f,-.8f,0),"Incoming water reaches fire along stream");
  Check(!NavalFireEnvironmentMath.Washed(1,-.3f,2,0,0,0,2.5f,-.8f,0),"Distant fire remains burning");
  Check(!NavalFireEnvironmentMath.Washed(-2,0,0,0,0,0,2.5f,-.8f,0),"Water does not extinguish behind intake");
  var fire=new NavalFireState(false);fire.Ignite(0);fire.Douse(1);
  Check(!fire.Burning&&!fire.Ignite(2),"Wash extinguishes and prevents immediate re-ignition");
  Console.WriteLine("PASS: deck/hold smoke classification on all ships, localized stream wetting and wet fuel protection.");
 }
}
