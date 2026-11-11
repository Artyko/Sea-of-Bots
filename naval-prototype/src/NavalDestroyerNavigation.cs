using System;
public static class NavalDestroyerNavigation {
 public static bool Clear(NavalWalkPoint a,NavalWalkPoint b,NavalShipDefinition s){
  if(!NavalCrewDeckSafety.Inside(a.X,a.Z,s,.8f)||!NavalCrewDeckSafety.Inside(b.X,b.Z,s,.8f))return false;
  if(Math.Abs(a.Y-b.Y)>.08f)return true;
  float q=s.Scale,margin=.85f/q;
  foreach(var box in NavalDestroyerDesign.Equipment()){
   bool lower=a.Y<s.WeatherDeck-.1f;if(lower!=(box.Kind<=1))continue;
   if(box.Kind==4||box.Kind==5)continue; // actual gun geometry is described by Gun(), not the old mockup marker
   float x=box.X/q,z=box.Z/q,w=box.W*.5f/q+margin,d=box.D*.5f/q+margin;
   if(NavalSupplyRoutes.Hits(a,b,x-w,x+w,z-d,z+d))return false;
  }
  if(Math.Abs(a.Y-NavalDestroyerLayout.Foredeck(s))<.05f)foreach(int side in new[]{-1,1})if(NavalSupplyRoutes.Hits(a,b,(side*3.45f-.925f)/q,(side*3.45f+.925f)/q,(33-3.15f)/q,(33+3.15f)/q))return false;

  if(a.Y<s.WeatherDeck-.1f)foreach(int side in new[]{-1,1})if(NavalSupplyRoutes.Hits(a,b,(side*1.2f-1.85f)/q,(side*1.2f+1.85f)/q,-20.25f/q,-16.75f/q))return false;
  if(a.Y>=s.WeatherDeck-.1f)for(int gun=0;gun<s.GunCount;gun++){var p=s.GunPosition(gun);float h=1f/q+margin;if(NavalSupplyRoutes.Hits(a,b,p.x-h,p.x+h,p.z-h,p.z+h))return false;}
  if(a.Y<s.WeatherDeck-.1f)for(int gun=0;gun<s.GunCount;gun++){
   float rx=NavalReadyAmmoLayout.X(s,gun),rz=NavalReadyAmmoLayout.Z(s,gun);
   if(NavalSupplyRoutes.Hits(a,b,rx-(.45f+.8f)/q,rx+(.45f+.8f)/q,rz-(.35f+.8f)/q,rz+(.35f+.8f)/q))return false;
  }
  int count=NavalCargoLayout.Boxes(s,0)+NavalCargoLayout.Boxes(s,1)+NavalCargoLayout.Boxes(s,2);
  if(a.Y<s.WeatherDeck-.1f)for(int i=0;i<count;i++){var p=NavalCargoLayout.Position(s,i);float h=1.5f/q;if(NavalSupplyRoutes.Hits(a,b,p.X-h,p.X+h,p.Z-h,p.Z+h))return false;}
  return true;
 }
}
