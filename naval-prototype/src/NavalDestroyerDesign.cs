using System;
using System.Collections.Generic;
// World-metre design data. Independent length/beam avoid inheriting the old uniform ship scale.
public static class NavalDestroyerDesign {
 public const float Length=102.4f,Beam=14f,MainDeck=1.4f,Forecastle=2.6f,WalkRadius=.8f;
 public static readonly float LowerDeck=MainDeck-.2f-NavalShipDefinition.Ironclad.HoldClearance;
 public const int StairSteps=32;
 public const float ForecastleEdge=29.5f,RampStart=25.5f;
 public struct Box {public string Name;public int Kind;public float X,Y,Z,W,H,D;public Box(string name,int kind,float x,float y,float z,float w,float h,float d){Name=name;Kind=kind;X=x;Y=y;Z=z;W=w;H=h;D=d;}}
 // Kind 0 boiler, 1 turbine, 2 funnel, 3 torpedo mount marker, 4 gun marker, 5 bridge.
 private static readonly List<Box> equipment=BuildEquipment();
 public static List<Box> Equipment(){return equipment;}
 private static List<Box> BuildEquipment(){var list=new List<Box>();
  for(int i=0;i<6;i++)list.Add(new Box("Oil boiler "+(i+1),0,0,LowerDeck+1.1f,26-i*8,3.2f,2.2f,3));
  for(int i=0;i<3;i++)list.Add(new Box("Turbine "+(i+1),1,(i-1)*1.05f,LowerDeck+.8f,-25, .9f,1.6f,8));
  foreach(float z in new[]{22f,12f,2f,-8f})list.Add(new Box("Funnel",2,0,MainDeck+3.3f,z,1.4f,6.6f,2));
  foreach(float z in new[]{18f,6f,-3f,-16f})list.Add(new Box("Twin 450 mm mount",3,0,MainDeck+.6f,z,2.6f,1.2f,2));
  foreach(float z in new[]{41f,-33f,-43f})list.Add(new Box("102 mm gun",4,0,(z>25?Forecastle:MainDeck)+.65f,z,1.8f,1.3f,1.8f));
  list.Add(new Box("Bridge footprint",5,0,Forecastle+1.7f,33,7f,3.4f,4.6f));return list;
 }
 public static float HalfWidth(float z){float a=Math.Abs(z);if(z>35)return Beam*.5f*Math.Max(.22f,(Length*.5f-z)/(Length*.5f-35));if(a>43)return Beam*.5f*Math.Max(.35f,(Length*.5f-a)/(Length*.5f-43));return Beam*.5f;}
 public static bool LowerClear(float ax,float az,float bx,float bz){
  var a=new NavalWalkPoint(ax,LowerDeck,az);var b=new NavalWalkPoint(bx,LowerDeck,bz);
  foreach(var box in Equipment())if(box.Kind<=1&&NavalSupplyRoutes.Hits(a,b,box.X-box.W*.5f-WalkRadius,box.X+box.W*.5f+WalkRadius,box.Z-box.D*.5f-WalkRadius,box.Z+box.D*.5f+WalkRadius))return false;
  return true;
 }
 public static List<NavalWalkPoint> ServicePosts(){var posts=new List<NavalWalkPoint>();foreach(var b in Equipment())if(b.Kind<=1)for(int side=-1;side<=1;side+=2)posts.Add(new NavalWalkPoint(side*2.65f,LowerDeck,b.Z));return posts;}
 public static List<NavalWalkPoint> LowerRoute(NavalWalkPoint a,NavalWalkPoint b){
  var result=new List<NavalWalkPoint>();float side=a.X<0?-1:1,other=b.X<0?-1:1;
  result.Add(new NavalWalkPoint(side*3,LowerDeck,a.Z));
  if(side!=other){float z=Math.Abs(a.Z-32)+Math.Abs(b.Z-32)<Math.Abs(a.Z+19)+Math.Abs(b.Z+19)?32:-19;result.Add(new NavalWalkPoint(side*3,LowerDeck,z));result.Add(new NavalWalkPoint(other*3,LowerDeck,z));}
  result.Add(new NavalWalkPoint(other*3,LowerDeck,b.Z));result.Add(b);return result;
 }
}
