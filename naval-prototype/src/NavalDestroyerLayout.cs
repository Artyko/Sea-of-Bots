using System;
using System.Collections.Generic;
using UnityEngine;
// All coordinates enter through the vessel's independent metre layout, then convert once.
public static class NavalDestroyerLayout {
 public static Vector3 Point(NavalShipDefinition s,float x,float y,float z){return new Vector3(x/s.Scale,s.WeatherDeck+(y-NavalDestroyerDesign.MainDeck)/s.Scale,z/s.Scale);}
 private static readonly float[] gunZ={41,-33,-43};
 public static Vector3 Gun(NavalShipDefinition s,int i){return Point(s,0,(i==0?2.6f:1.4f)+.55f,gunZ[i]);}
 public static float Foredeck(NavalShipDefinition s){return s.WeatherDeck+1.2f/s.Scale;}
 public static bool ForedeckStair(NavalWalkPoint p,NavalShipDefinition s){
  if(!s.Destroyer)return false;float x=p.X*s.Scale,z=p.Z*s.Scale;
  if(Math.Abs(Math.Abs(x)-3.1f)>.65f||z<25.2f||z>29.8f)return false;
  float progress=Math.Max(0f,Math.Min(1f,(z-25.5f)/4f));
  float dy=(p.Y-s.WeatherDeck)*s.Scale-progress*1.2f;
  return dy>=-.3f&&dy<=.7f;
 }
 public static bool ReachedRamp(float dx,float dy,float dz,float scale){return Math.Abs(dx)*scale<.82f&&Math.Abs(dz)*scale<.45f&&dy*scale>=-.25f&&dy*scale<.7f;}
 public static bool Stair(NavalWalkPoint p,NavalShipDefinition s){float x=p.X*s.Scale,z=p.Z*s.Scale;return Math.Abs(Math.Abs(x)-3.6f)<1.2f&&(Math.Abs(z-20.5f)<4.8f||Math.Abs(z+20.5f)<4.8f)&&p.Y>=s.HoldFloor-.05f&&p.Y<=s.WeatherDeck+.05f;}
 public static List<NavalWalkPoint> Route(NavalWalkPoint a,NavalWalkPoint b,NavalShipDefinition s,float stairZ=float.NaN,float laneSide=0,bool gunApproach=true){
  // Resume along the actual treads when a task changes midway through a flight.
  // Starting a fresh route at hold-floor height pulled a sailor diagonally into
  // the side of the stair and left him waiting forever above that waypoint.
  if(Stair(a,s)&&NavalStairTraffic.InPassage(a,s)){
   float q0=s.Scale,centre=a.Z<0?-20.5f:20.5f,side0=a.X<0?-1:1;
   float t0=Math.Max(0,Math.Min(1,(a.Z*q0-centre+4)/8));bool up=b.Y>=s.WeatherDeck-.1f;
   var resumed=new List<NavalWalkPoint>();int first=up?(int)Math.Ceiling(t0*32):(int)Math.Floor(t0*32);
   for(int step=first;up?step<=32:step>=0;step+=up?1:-1){float t=step/32f;resumed.Add(new NavalWalkPoint(side0*3.6f/q0,s.HoldFloor+(s.WeatherDeck-s.HoldFloor)*t,(centre-4+8*t)/q0));}
   float floor0=up?s.WeatherDeck:s.HoldFloor;
   var landing=new NavalWalkPoint(side0*5.6f/q0,floor0,(centre+(up?5f:-5f))/q0);
   resumed.Add(new NavalWalkPoint(side0*3.6f/q0,floor0,landing.Z));resumed.Add(landing);
   resumed.AddRange(Route(landing,b,s,stairZ,laneSide,gunApproach));return resumed;
  }
  if(gunApproach&&a.Y>=s.WeatherDeck-.05f)for(int gun=0;gun<s.GunCount;gun++){
   var mount=s.GunPosition(gun);float dx=(a.X-mount.x)*s.Scale,dz=(a.Z-mount.z)*s.Scale,distance=(float)Math.Sqrt(dx*dx+dz*dz);
   if(distance>NavalGunStationLayout.RearDistance-.8f&&distance<NavalGunStationLayout.RearDistance+.8f&&Math.Abs(a.Y-(mount.y-.55f/s.Scale))*s.Scale<.8f){
    var departure=new NavalWalkPoint(a.X+dx/distance*NavalGunStationLayout.ApproachDistance/s.Scale,a.Y,a.Z+dz/distance*NavalGunStationLayout.ApproachDistance/s.Scale);
    var departureRoute=new List<NavalWalkPoint>();departureRoute.Add(departure);departureRoute.AddRange(Route(departure,b,s,stairZ,laneSide,false));return departureRoute;
   }
  }
  if(gunApproach&&b.Y>=s.WeatherDeck-.05f){for(int gun=0;gun<s.GunCount;gun++){var mount=s.GunPosition(gun);float dx=(b.X-mount.x)*s.Scale,dz=(b.Z-mount.z)*s.Scale,distance=(float)Math.Sqrt(dx*dx+dz*dz);if(Math.Abs(distance-NavalGunStationLayout.RearDistance)<.12f&&Math.Abs(b.Y-(mount.y-.55f/s.Scale))<.05f){var approach=new NavalWalkPoint(b.X+dx/distance*NavalGunStationLayout.ApproachDistance/s.Scale,b.Y,b.Z+dz/distance*NavalGunStationLayout.ApproachDistance/s.Scale);var entry=Route(a,approach,s,stairZ,laneSide,false);entry.Add(b);return entry;}}}
  float scale=s.Scale;bool fromRaised=a.Z>29.5f/scale&&a.Y>s.WeatherDeck+.1f,toRaised=b.Z>29.5f/scale&&b.Y>s.WeatherDeck+.1f;
  if(fromRaised&&b.Y<s.WeatherDeck-.1f||toRaised&&a.Y<s.WeatherDeck-.1f){float sideGate=(fromRaised?a.X:b.X)<0?-1:1;var gate=new NavalWalkPoint(sideGate*5.2f/scale,s.WeatherDeck,25.5f/scale);var split=Route(a,gate,s,stairZ,laneSide);split.AddRange(Route(gate,b,s,stairZ,laneSide));return split;}
  var r=new List<NavalWalkPoint>();float q=s.Scale;bool belowA=a.Y<s.WeatherDeck-.1f,belowB=b.Y<s.WeatherDeck-.1f;
  float side=Math.Abs(a.X)>.05f?Math.Sign(a.X):Math.Abs(b.X)>.05f?Math.Sign(b.X):1;
  if(laneSide!=0)side=laneSide;float lane=side*5.6f/q;
  if(belowA!=belowB){float z=(Math.Abs(a.Z-20.5f/q)+Math.Abs(b.Z-20.5f/q)<Math.Abs(a.Z+20.5f/q)+Math.Abs(b.Z+20.5f/q)?20.5f:-20.5f)/q;
   if(!float.IsNaN(stairZ))z=stairZ/q;float x=side*3.6f/q;
   if(belowA){r.Add(new NavalWalkPoint(lane,s.HoldFloor,a.Z));r.Add(new NavalWalkPoint(lane,s.HoldFloor,z-4.8f/q));r.Add(new NavalWalkPoint(x,s.HoldFloor,z-4.8f/q));}
   else {r.Add(new NavalWalkPoint(lane,s.WeatherDeck,a.Z));r.Add(new NavalWalkPoint(lane,s.WeatherDeck,z+4f/q));r.Add(new NavalWalkPoint(x,s.WeatherDeck,z+4f/q));}
   for(int i=0;i<=32;i++){float t=belowA?i/32f:1-i/32f;r.Add(new NavalWalkPoint(x,s.HoldFloor+(s.WeatherDeck-s.HoldFloor)*t,z+(-4+8*t)/q));}
   r.Add(new NavalWalkPoint(lane,belowB?s.HoldFloor:s.WeatherDeck,z+(belowB?-4f:4f)/q));
   r.Add(new NavalWalkPoint(lane,belowB?s.HoldFloor:s.WeatherDeck,z+(belowB?-5f:5f)/q));
  }else r.Add(new NavalWalkPoint(lane,belowB?s.HoldFloor:a.Z>29.5f/q&&a.Y>=Foredeck(s)-.35f/q?Foredeck(s):s.WeatherDeck,a.Z));
  float floor=belowB?s.HoldFloor:s.WeatherDeck;
  bool forecastle=!belowB&&b.Z>29.5f/q;
  bool fromForecastle=!belowA&&a.Z>29.5f/q&&a.Y>=Foredeck(s)-.35f/q;
  if(!belowB&&(forecastle||fromForecastle)){
   if(fromForecastle&&!forecastle){r.Clear();r.Add(new NavalWalkPoint(side*5.6f/q,Foredeck(s),a.Z));r.Add(new NavalWalkPoint(side*5.6f/q,Foredeck(s),29.5f/q));r.Add(new NavalWalkPoint(side*3.1f/q,Foredeck(s),29.5f/q));for(int i=15;i>=0;i--)r.Add(new NavalWalkPoint(side*3.1f/q,s.WeatherDeck+1.2f*(i+1)/16/q,(25.5f+(i+.5f)*.25f)/q));}
   else if(forecastle&&!fromForecastle){r.Add(new NavalWalkPoint(lane,floor,25.5f/q));r.Add(new NavalWalkPoint(side*3.1f/q,floor,25.5f/q));for(int i=0;i<16;i++)r.Add(new NavalWalkPoint(side*3.1f/q,s.WeatherDeck+1.2f*(i+1)/16/q,(25.5f+(i+.5f)*.25f)/q));r.Add(new NavalWalkPoint(side*5.6f/q,Foredeck(s),29.5f/q));}
   if(forecastle)floor=Foredeck(s);
  }
  // Cross the lower centreline in the gap between fireboxes; never through a boiler.
  if(b.X*side<0){float crossing=belowB?-2.133333f/q:-29f/q;r.Add(new NavalWalkPoint(lane,floor,crossing));lane=-lane;r.Add(new NavalWalkPoint(lane,floor,crossing));}
  float limit=s.HullHalfWidth(b.Z)-1f/q;lane=Math.Max(-limit,Math.Min(limit,lane));
  r.Add(new NavalWalkPoint(lane,floor,b.Z));r.Add(b);for(int i=0;i<r.Count;i++){var p=r[i];float safe=Math.Min(s.HullHalfWidth(p.Z-1f/q),s.HullHalfWidth(p.Z+1f/q))-1f/q;p.X=Math.Max(-safe,Math.Min(safe,p.X));r[i]=p;}
  var result=new List<NavalWalkPoint>();var previous=a;foreach(var p in r){if(Math.Abs(previous.Y-p.Y)<.001f){
   if(previous.Z>35f/q&&p.Z<35f/q)result.Add(new NavalWalkPoint(Math.Sign(previous.X)*5.6f/q,p.Y,34f/q));
   if(previous.Z<-43f/q&&p.Z>-43f/q)result.Add(new NavalWalkPoint(Math.Sign(previous.X)*5.6f/q,p.Y,-42f/q));
   if(previous.Z<35f/q&&p.Z>35f/q)result.Add(new NavalWalkPoint(Math.Sign(p.X)*5.6f/q,p.Y,34f/q));
   if(previous.Z>-43f/q&&p.Z<-43f/q)result.Add(new NavalWalkPoint(Math.Sign(p.X)*5.6f/q,p.Y,-42f/q));
  }result.Add(p);previous=p;}return result;
 }
 public static void Box(NavalHull h,Material m,Vector3 p,Vector3 worldSize,bool shell,bool solid,List<NavalHullVoxel> group){
  Vector3 size=worldSize/h.WorldScale;int nx=Math.Max(1,(int)Math.Ceiling(worldSize.x/1f)),ny=Math.Max(1,(int)Math.Ceiling(worldSize.y/1f)),nz=Math.Max(1,(int)Math.Ceiling(worldSize.z/1f));
  for(int x=0;x<nx;x++)for(int y=0;y<ny;y++)for(int z=0;z<nz;z++){
   Vector3 unit=new Vector3(size.x/nx,size.y/ny,size.z/nz),at=p+new Vector3((x+.5f)/nx*size.x-size.x*.5f,(y+.5f)/ny*size.y-size.y*.5f,(z+.5f)/nz*size.z-size.z*.5f);
   var c=h.AddVoxel(h.Root,m,at,unit,shell,solid);c.Metallic=true;c.Flammable=false;if(group!=null)group.Add(c);
   if(shell)c.OpeningArea=unit.y<unit.x&&unit.y<unit.z?unit.x*unit.z:unit.y*Math.Max(unit.x,unit.z);
  }
 }
 public static void BuildHull(NavalHull h,Material steel,Material deck){
  var s=h.Definition;float q=s.Scale;
  for(int z=0;z<103;z++){float z0=-51.2f+z*102.4f/103,z1=-51.2f+(z+1)*102.4f/103,zc=(z0+z1)*.5f,dz=z1-z0;
   float w=NavalDestroyerDesign.HalfWidth(zc),slope=Math.Abs(NavalDestroyerDesign.HalfWidth(z1)-NavalDestroyerDesign.HalfWidth(z0));
   float top=zc>=29.5f?Foredeck(s):s.WeatherDeck;
   for(int side=-1;side<=1;side+=2)Box(h,steel,new Vector3(side*w/q,(s.HoldFloor-.2f/q+top+.8f/q)*.5f,zc/q),new Vector3(.20f+slope,(top-s.HoldFloor)*q+1f,dz),true,true,null);
   // One watertight structural floor, directly accessible from the hold.
   // A separate sealed slab below this deck made bottom leaks impossible to reach.
   Box(h,steel,new Vector3(0,s.HoldFloor-.1f/q,zc/q),new Vector3(w*2,.2f,dz),true,true,null);
   // Protective lower casing encloses the existing bilge tanks and pipe runs.
   // The accessible upper floor remains the watertight boundary of the occupied hold.
   Box(h,steel,new Vector3(0,s.HoldFloor-1.5f/q,zc/q),new Vector3(w*2,.2f,dz),false,true,null);
   for(int side=-1;side<=1;side+=2)Box(h,steel,new Vector3(side*w/q,s.HoldFloor-.85f/q,zc/q),new Vector3(.20f+slope,1.5f,dz),false,true,null);
   bool hatch=Math.Abs(Math.Abs(zc)-20.5f)<4.6f;
   if(hatch){Box(h,deck,new Vector3(0,top-.1f/q,zc/q),new Vector3(4.4f,.2f,dz),false,true,null);for(int side=-1;side<=1;side+=2)Box(h,deck,new Vector3(side*(w+4.6f)*.5f/q,top-.1f/q,zc/q),new Vector3(Math.Max(.1f,w-4.6f),.2f,dz),false,true,null);}
   else Box(h,deck,new Vector3(0,top-.1f/q,zc/q),new Vector3(w*2,.2f,dz),false,true,null);
  }
  for(int end=-1;end<=1;end+=2)Box(h,steel,new Vector3(0,s.HoldFloor-.85f/q,end*6),new Vector3(s.HullHalfWidth(end*6)*q*2,1.5f,.2f),false,true,null);
  for(int end=-1;end<=1;end+=2){float top=end>0?Foredeck(s):s.WeatherDeck;Box(h,steel,new Vector3(0,(s.HoldFloor-.2f/q+top+.8f/q)*.5f,end*6),new Vector3(s.HullHalfWidth(end*6)*q*2,(top-s.HoldFloor)*q+1f,.2f),true,true,null);}
 }
 public static void BuildDecks(NavalHull h,Material steel,Material trim){var s=h.Definition;float q=s.Scale;
  for(int side=-1;side<=1;side+=2){foreach(float z in new[]{20.5f,-20.5f}){var flight=new List<NavalHullVoxel>();for(int i=0;i<32;i++){float top=s.HoldFloor+(s.WeatherDeck-s.HoldFloor)*(i+1)/32;Box(h,steel,new Vector3(side*3.6f/q,top-.075f/q,(z-4+(i+.5f)*8/32)/q),new Vector3(1.8f,.15f,8f/32),false,true,flight);}new NavalRepairableMechanism(h,flight,false,false,"Внутренний трап");}
   for(int i=0;i<16;i++){float rise=1.2f*(i+1)/16;Box(h,steel,new Vector3(side*3.1f/q,s.WeatherDeck+rise*.5f/q,(25.5f+(i+.5f)*.25f)/q),new Vector3(1.8f,rise,.25f),false,true,null);}
  }
  // Compact raised bridge: central door and an internal stair, two side bypasses.
  float centre=33f/q,baseY=Foredeck(s),topY=s.HelmDeck;
  Box(h,steel,new Vector3(-1.4f/q,topY-.1f/q,centre),new Vector3(4.2f,.2f,4.6f),false,true,null);
  for(int side=-1;side<=1;side+=2)Box(h,steel,new Vector3(side*3.45f/q,(baseY+topY)*.5f,centre),new Vector3(.15f,(topY-baseY)*q,4.6f),false,true,null);
  for(int i=0;i<24;i++){float t=(i+1)/24f;Box(h,steel,new Vector3(2.1f/q,baseY+(topY-baseY)*t-.075f/q,centre+(1.7f-(i+.5f)*3.4f/24)/q),new Vector3(2.4f,.15f,3.4f/24),false,true,null);}
  // Enclosed pilothouse: solid lower armour, window embrasures, cap and protected rear landing.
  int bridgeFirst=h.Voxels.Count;
  Box(h,steel,new Vector3(0,topY-.1f/q,centre-2.4f/q),new Vector3(7f,.2f,1.4f),false,true,null);
  for(int side=-1;side<=1;side+=2){
   Box(h,steel,new Vector3(side*3.45f/q,topY+.8f/q,centre-.25f/q),new Vector3(.22f,1.6f,6.1f),false,true,null);
   Box(h,trim,new Vector3(side*3.45f/q,topY+3.25f/q,centre-.25f/q),new Vector3(.25f,.5f,6.1f),false,true,null);
   foreach(float z in new[]{-3.2f,-.2f,2.7f})Box(h,steel,new Vector3(side*3.45f/q,topY+2.3f/q,centre+z/q),new Vector3(.24f,1.4f,.22f),false,true,null);
   float wallZ=side<0?-3.3f:2.8f;
   Box(h,steel,new Vector3(0,topY+.8f/q,centre+wallZ/q),new Vector3(7f,1.6f,.22f),false,true,null);
   Box(h,trim,new Vector3(0,topY+3.25f/q,centre+wallZ/q),new Vector3(7f,.5f,.25f),false,true,null);
   foreach(float x in new[]{-2.3f,0f,2.3f})Box(h,steel,new Vector3(x/q,topY+2.3f/q,centre+wallZ/q),new Vector3(.2f,1.4f,.22f),false,true,null);
  }
  Box(h,steel,new Vector3(0,topY+3.6f/q,centre-.25f/q),new Vector3(7.3f,.3f,6.5f),false,true,null);
  // Front service opening includes the stair toe; no sideways entry into high treads.
  Box(h,steel,new Vector3(-2.4f/q,(baseY+topY)*.5f,centre+2.3f/q),new Vector3(2.2f,(topY-baseY)*q,.2f),false,true,null);
  Box(h,steel,new Vector3(3.35f/q,(baseY+topY)*.5f,centre+2.3f/q),new Vector3(.3f,(topY-baseY)*q,.2f),false,true,null);
  for(int i=bridgeFirst;i<h.Voxels.Count;i++)h.Voxels[i].Armored=true;
 }
}
