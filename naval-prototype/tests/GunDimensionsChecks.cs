using System;
class GunDimensionsChecks {
 static void Check(bool v,string why){if(!v)throw new Exception(why);}
 static void Main(){int[] mm={102,152,203,305};float last=0;foreach(int c in mm){Check(NavalGunDimensions.Bore(c)<NavalGunDimensions.TubeDiameter(c),"Bore smaller than outside diameter");Check(NavalGunDimensions.TubeDiameter(c)>last,"Larger calibre has thicker tube");last=NavalGunDimensions.TubeDiameter(c);}
 var s=NavalShipDefinition.Novik;Check(Math.Abs(s.Scale*s.GunFixtureScale-1)<.0001,"Destroyer mount independent of hull scale");foreach(var ship in NavalShipDefinition.SteamChoices){int c=ship.Destroyer?102:NavalGunDimensions.CasemateCalibre;Check(Math.Abs(ship.GunMuzzleZ*ship.Scale*ship.GunFixtureScale-NavalGunDimensions.Muzzle(c))<.001,"Muzzle follows real tube tip");}Check(NavalGunDimensions.TubeDiameter(305)>NavalGunDimensions.TubeDiameter(102)*2f,"Capital barrel approximately three destroyer diameters");Console.WriteLine("PASS: unified metre dimensions, calibre ordering, hull-scale independence and muzzle alignment on all steam profiles.");}
}
