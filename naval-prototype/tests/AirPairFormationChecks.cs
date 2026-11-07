using System;
class AirPairFormationChecks{
 static void Main(){for(int i=0;i<6;i+=2){var leader=NavalAirRaidMath.PairOffset(i);var wing=NavalAirRaidMath.PairOffset(i+1);
  if(wing.z-leader.z<80||Math.Abs(wing.x-leader.x)<25)throw new Exception("Wingman must remain aft and offset");
  if(i>=2&&leader.z-NavalAirRaidMath.PairOffset(i-2).z<250)throw new Exception("Pairs need longitudinal separation");
  for(int w=0;w<2;w++){var aim=NavalAirRaidMath.BombingOffset(i/2,w);if(Math.Abs(aim.x)>3||Math.Abs(aim.z)>8)throw new Exception("Bomb aim must remain near ship centre");}
 }Console.WriteLine("PASS: paired echelon spacing, ordered attack groups and on-hull bombing aim offsets.");}
}
