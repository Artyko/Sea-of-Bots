using System;
using System.Collections.Generic;
public struct NavalFurnishingBox
{
 public string Item;public int Palette;
 public float X,Y,Z,Width,Height,Depth;
}
// Fixed-world-size, shallow wall fittings preserve the repair and walking corridors.
public static class NavalFurnishingLayout
{
 private static void Box(List<NavalFurnishingBox> boxes,string item,int palette,float x,float y,float z,float width,float height,float depth,float scale)
 {boxes.Add(new NavalFurnishingBox{Item=item,Palette=palette,X=x,Y=y,Z=z,Width=width/scale,Height=height/scale,Depth=depth/scale});}
 private static void Wall(List<NavalFurnishingBox> boxes,string item,int palette,int side,float floor,float z,float y,float depth,float height,float length,float scale)
 {Box(boxes,item,palette,side*(2.875f-depth*.5f/scale),floor+y/scale,z,depth,height,length,scale);}
 public static List<NavalFurnishingBox> Build(NavalShipDefinition ship)
 {
  var boxes=new List<NavalFurnishingBox>();float s=ship.Scale;
  for(int level=0;level<ship.MaintenanceDeckCount;level++){
   float floor=ship.MaintenanceFloor(level);
   for(int side=-1;side<=1;side+=2){
    // Small provision casks are lashed on the outer edge of the existing ballast chests.
    foreach(float z in new float[]{-3.5f,3.5f}){
     float x=side*(2.875f-.25f/s),baseY=NavalBallastLayout.Surface(x,z,floor,s);
     for(int row=0;row<3;row++){float width=row==1?.5f:.42f;Box(boxes,"Provision cask",0,x,baseY+(.15f+row*.28f)/s,z,width,.28f,width,s);}
     for(int ring=0;ring<2;ring++)Box(boxes,"Cask hoop",2,x,baseY+(.20f+ring*.45f)/s,z,.52f,.045f,.52f,s);
    }
    float crateX=side*(2.875f-.225f/s),crateZ=-4.7f,crateBase=NavalBallastLayout.Surface(crateX,crateZ,floor,s);
    Box(boxes,"Lashed cargo crate",0,crateX,crateBase+.3f/s,crateZ,.45f,.6f,.8f,s);
    for(int end=-1;end<=1;end+=2){
     Box(boxes,"Cargo crate corner",1,crateX,crateBase+.3f/s,crateZ+end*.35f/s,.47f,.64f,.09f,s);
     Box(boxes,"Cargo crate rope",3,crateX,crateBase+(.30f+end*.20f)/s,crateZ,.48f,.045f,.83f,s);
    }
    // Rolled rope hung flat against the frames.
    for(int segment=0;segment<8;segment++){
     double angle=segment*Math.PI/4;float y=2.25f+(float)Math.Sin(angle)*.32f,z=-1.4f+(float)Math.Cos(angle)*.32f/s;
     Wall(boxes,"Hanging rope coil",3,side,floor,z,y,.13f,.20f,.22f,s);
    }
    // Canvas berth folds: three shallow panels form a sagging hammock along the wall.
    for(int panel=-1;panel<=1;panel++)Wall(boxes,"Canvas hammock",4,side,floor,panel*.55f/s,1.82f+Math.Abs(panel)*.16f,.32f,.08f,.6f,s);
    for(int end=-1;end<=1;end+=2){
     Wall(boxes,"Hammock rope",3,side,floor,end*.85f/s,2.15f,.16f,.45f,.06f,s);
     Wall(boxes,"Hammock wall bracket",1,side,floor,end*.85f/s,2.4f,.22f,.13f,.18f,s);
    }
    // Shallow galley shelf with wooden bowls. Above heads, with no real-time light cost.
    Wall(boxes,"Galley shelf",1,side,floor,2.1f,3.1f,.25f,.12f,1.2f,s);
    for(int bowl=-1;bowl<=1;bowl++)Wall(boxes,"Wooden bowl",0,side,floor,2.1f+bowl*.32f/s,3.23f,.20f,.16f,.24f,s);
    Wall(boxes,"Navigation chart backing",0,side,floor,.3f,3.3f,.1f,.8f,1.1f,s);
    Wall(boxes,"Navigation chart parchment",4,side,floor,.3f,3.3f,.12f,.65f,.95f,s);
    foreach(float z in new float[]{-4.7f,-1.4f,1.4f,4.7f})Lantern(boxes,side,floor,z,3.25f,s);
   }
  }
  // Foredeck ropes and stern lanterns sit against the bulwark, clear of guns and ladders.
  for(int side=-1;side<=1;side+=2){
   for(int segment=0;segment<8;segment++){
    double angle=segment*Math.PI/4;
    Wall(boxes,"Foredeck rope",3,side,ship.WeatherDeck,5.05f+(float)Math.Cos(angle)*.28f/s,.65f+(float)Math.Sin(angle)*.28f,.12f,.18f,.20f,s);
   }
   Lantern(boxes,side,ship.WeatherDeck,-5.1f,1.05f,s);
  }
  return boxes;
 }
 private static void Lantern(List<NavalFurnishingBox> boxes,int side,float floor,float z,float y,float scale)
 {
  Wall(boxes,"Lantern back",2,side,floor,z,y,.10f,.55f,.32f,scale);
  Wall(boxes,"Warm lantern pane",5,side,floor,z,y,.16f,.36f,.24f,scale);
  for(int sign=-1;sign<=1;sign+=2){
   Wall(boxes,"Lantern cap",2,side,floor,z,y+sign*.25f,.20f,.07f,.37f,scale);
   Wall(boxes,"Lantern frame",2,side,floor,z+sign*.15f/scale,y,.19f,.50f,.04f,scale);
  }
 }
}
