using System;
using System.Collections.Generic;
class FurnishingClearanceChecks {
 static void Check(bool value,string why){if(!value)throw new Exception(why);}
 static bool Axis(float a,float d,float min,float max,ref float lo,ref float hi){
 if(Math.Abs(d)<.000001f)return a>min+.00001f&&a<max-.00001f;
 float x=(min-a)/d,y=(max-a)/d;if(x>y){float t=x;x=y;y=t;}lo=Math.Max(lo,x);hi=Math.Min(hi,y);return lo<hi-.00001f;}
 static bool Hits(NavalWalkPoint a,NavalWalkPoint b,NavalFurnishingBox box,float scale){
 float lo=0,hi=1,r=.45f/scale,h=2.6f/scale;
 return Axis(a.X,b.X-a.X,box.X-box.Width*.5f-r,box.X+box.Width*.5f+r,ref lo,ref hi)&&
 Axis(a.Y,b.Y-a.Y,box.Y-box.Height*.5f-h,box.Y+box.Height*.5f,ref lo,ref hi)&&
 Axis(a.Z,b.Z-a.Z,box.Z-box.Depth*.5f-r,box.Z+box.Depth*.5f+r,ref lo,ref hi);
 }
 static void Main(){try{Run();}catch(Exception e){Console.WriteLine(e.Message);Environment.ExitCode=1;}}
 static void Run(){int routes=0;foreach(var ship in NavalShipDefinition.Choices){
 var boxes=NavalFurnishingLayout.Build(ship);int cells=0;
 foreach(var box in boxes){
 Check(box.Width>0&&box.Height>0&&box.Depth>0&&box.Palette>=0&&box.Palette<6,"Valid destructible box");
 cells+=(int)Math.Ceiling(box.Width/.25f)*(int)Math.Ceiling(box.Height/.25f)*(int)Math.Ceiling(box.Depth/.25f);
 Check(Math.Abs(box.X)+box.Width*.5f<=ship.HullHalfWidth(box.Z)+.01f,"Furniture stays inside hull outline");
 Check(Math.Abs(box.X)-box.Width*.5f>2.5f,"Central walking and working area remains free");
 }
 Check(cells<700,"Bounded decorative voxel budget");
 Console.WriteLine(ship.Name+": "+boxes.Count+" furniture pieces, at most "+cells+" voxel cells");
 var posts=new List<NavalWalkPoint>();posts.Add(new NavalWalkPoint(0,ship.HelmDeck,NavalShipDefinition.HelmZ(ship)));
 for(int gun=0;gun<ship.GunCount;gun++)posts.Add(new NavalWalkPoint(NavalShipDefinition.GunSide(gun)*ship.GunPostX,ship.GunDeck,NavalShipDefinition.GunZ(ship,gun)));
 for(int level=0;level<ship.MaintenanceDeckCount;level++){
 float floor=ship.MaintenanceFloor(level);
 posts.Add(new NavalWalkPoint(0,floor,-4.8f));posts.Add(new NavalWalkPoint(0,floor,NavalPlankSupplyLayout.PickupZ(ship.Scale)));posts.Add(new NavalWalkPoint(0,floor,NavalCannonSupplyLayout.PickupZ(ship.Scale)));
 for(int sailor=0;sailor<ship.CrewCount;sailor++)posts.Add(new NavalWalkPoint(NavalShipDefinition.StandbyX(sailor),floor,NavalShipDefinition.StandbyZ(sailor)));
 foreach(float x in new float[]{-2.35f,2.35f})foreach(float z in new float[]{-4.8f,-1.4f,1.4f,4.8f})posts.Add(new NavalWalkPoint(x,NavalBallastLayout.Surface(x,z,floor,ship.Scale),z));
 }
 posts.Add(new NavalWalkPoint(NavalSailWorkLayout.PostX,ship.WeatherDeck,NavalSailWorkLayout.PostZ));
 for(int side=-1;side<=1;side+=2){posts.Add(new NavalWalkPoint(side*1.35f,ship.WeatherDeck,4.8f));posts.Add(new NavalWalkPoint(side*2.2f,ship.WeatherDeck,ship.LadderZ));}
 foreach(var start in posts)foreach(var goal in posts){var previous=start;
 foreach(var point in NavalSupplyRoutes.Plan(start,goal,ship)){
 foreach(var box in boxes)Check(!Hits(previous,point,box,ship.Scale),ship.Name+": "+box.Item+" obstructs full-size crew path");previous=point;
 }routes++;}
 }
 Console.WriteLine("PASS: "+routes+" full-body swept routes clear destructible furnishings, both galleon holds, supplies, stairs, pump, guns, sails and repair stations; bounded cell budgets.");
 }
}
