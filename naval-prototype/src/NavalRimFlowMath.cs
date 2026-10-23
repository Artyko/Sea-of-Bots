using System;
public static class NavalRimFlowMath
{
 public static float Overlap(float a,float width,float b,float otherWidth)
 {return Math.Max(0,Math.Min(a+width*.5f,b+otherWidth*.5f)-Math.Max(a-width*.5f,b-otherWidth*.5f));}
 public static float UncoveredLadder(float z,NavalShipDefinition ship)
 {
  float low=Math.Max(z-.5f,ship.LadderZ-.65f),high=Math.Min(z+.5f,ship.LadderZ+.65f);
  if(high<=low)return 0;
  float covered=0;for(int gun=0;gun<ship.GunCount;gun+=2){float center=NavalShipDefinition.GunZ(ship,gun);covered+=Math.Max(0,Math.Min(high,center+1.1f)-Math.Max(low,center-1.1f));}
  return Math.Max(0,high-low-covered);
 }
}
