using System;
using System.Collections.Generic;
// Route around the two central stock boxes before local PhysX obstacle recovery.
public static class ReferenceSupplyRoutes
{
 public static List<NavalWalkPoint> Plan(NavalWalkPoint start,NavalWalkPoint goal,NavalShipDefinition ship)
 {
  var route=NavalShipRoutes.Plan(start,goal,ship);
  for(int i=0;i+1<route.Count;i++){
   var a=route[i];var b=route[i+1];
   if(a.X*b.X>=0||System.Math.Abs(a.Y-b.Y)>.01f||System.Math.Abs(a.Z-b.Z)>.01f||!ship.MaintenanceFloorAt(a.Y))continue;
   float before=i==0?start.Z:route[i-1].Z,after=i+2<route.Count?route[i+2].Z:goal.Z;
   float z=NavalCargoLayout.Crossing(before,after,ship);a.Z=b.Z=z;route[i]=a;route[i+1]=b;
  }
  int crates=NavalCargoLayout.Boxes(ship,0)+NavalCargoLayout.Boxes(ship,1)+NavalCargoLayout.Boxes(ship,2);
  for(int i=0;i<crates;i++){var crate=NavalCargoLayout.Position(ship,i);
   route=Around(route,start,ship,crate.Y,crate.Z,1.3f,1.3f,crate.X);
  }
  // The underside of each flight is not a walk-through corridor.
  for(int level=0;level<ship.MaintenanceDeckCount;level++)
   route=Around(route,start,ship,ship.MaintenanceFloor(level),-1.25f,ship.StairWidth*ship.Scale,3.5f*ship.Scale);
  // A detour corner may overlap the stair exclusion; never make it a mandatory endpoint.
  for(int i=route.Count-2;i>=0;i--){
   var p=route[i];bool floor=false;for(int level=0;level<ship.MaintenanceDeckCount;level++)if(Math.Abs(p.Y-ship.MaintenanceFloor(level))<.001f)floor=true;
   bool stairLanding=p.Z>=.42f&&p.Z<=.85f&&Math.Abs(p.X)<ship.StairWidth*.5f;
   if(floor&&!stairLanding&&!CargoClear(p,p,ship))route.RemoveAt(i);
  }
  return RepairSegments(start,route,ship);
 }
 private static bool CargoClear(NavalWalkPoint a,NavalWalkPoint b,NavalShipDefinition ship){
  for(int level=0;level<ship.MaintenanceDeckCount;level++){float floor=ship.MaintenanceFloor(level);
   if(System.Math.Abs(a.Y-floor)<.08f&&System.Math.Abs(b.Y-floor)<.08f&&Hits(a,b,-ship.StairWidth*.5f-.5f/ship.Scale,ship.StairWidth*.5f+.5f/ship.Scale,-3f-.5f/ship.Scale,.5f+.5f/ship.Scale))return false;
  }
  int count=NavalCargoLayout.Boxes(ship,0)+NavalCargoLayout.Boxes(ship,1)+NavalCargoLayout.Boxes(ship,2);float half=1.125f/ship.Scale;
  for(int i=0;i<count;i++){var c=NavalCargoLayout.Position(ship,i);if(System.Math.Abs(a.Y-c.Y)>.08f||System.Math.Abs(b.Y-c.Y)>.08f)continue;
   if(Hits(a,b,c.X-half,c.X+half,c.Z-half,c.Z+half))return false;
  }return true;
 }
 private static List<NavalWalkPoint> RepairSegments(NavalWalkPoint start,List<NavalWalkPoint> route,NavalShipDefinition ship){
  var result=new List<NavalWalkPoint>();var previous=start;
  foreach(var goal in route){
   if(!CargoClear(previous,goal,ship)){
    var nodes=new List<NavalWalkPoint>();nodes.Add(previous);nodes.Add(goal);
    int count=NavalCargoLayout.Boxes(ship,0)+NavalCargoLayout.Boxes(ship,1)+NavalCargoLayout.Boxes(ship,2);float half=1.15f/ship.Scale;
    for(int i=0;i<count;i++){var c=NavalCargoLayout.Position(ship,i);if(System.Math.Abs(previous.Y-c.Y)>.08f)continue;
     for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2){var node=new NavalWalkPoint(c.X+x*half,c.Y,c.Z+z*half);if(CargoClear(node,node,ship))nodes.Add(node);}
    }
    // Include flight corners as graph nodes: a crate detour may itself meet a staircase.
    for(int level=0;level<ship.MaintenanceDeckCount;level++){
     float landingFloor=ship.MaintenanceFloor(level);if(System.Math.Abs(previous.Y-landingFloor)>.08f)continue;
     float x=ship.StairWidth*.5f+.55f/ship.Scale;
     foreach(float side in new float[]{-x,x})foreach(float z in new float[]{-3f-.55f/ship.Scale,.5f+.55f/ship.Scale}){
      var node=new NavalWalkPoint(side,landingFloor,z);if(CargoClear(node,node,ship))nodes.Add(node);
     }
    }


    float[] distance=new float[nodes.Count];int[] parent=new int[nodes.Count];bool[] done=new bool[nodes.Count];
    for(int i=0;i<nodes.Count;i++){distance[i]=float.PositiveInfinity;parent[i]=-1;}distance[0]=0;
    for(int step=0;step<nodes.Count;step++){
     int best=-1;for(int i=0;i<nodes.Count;i++)if(!done[i]&&(best<0||distance[i]<distance[best]))best=i;
     if(best<0||float.IsPositiveInfinity(distance[best]))break;if(best==1)break;done[best]=true;
     for(int i=1;i<nodes.Count;i++){if(done[i]||!CargoClear(nodes[best],nodes[i],ship))continue;
      float dx=nodes[best].X-nodes[i].X,dz=nodes[best].Z-nodes[i].Z,next=distance[best]+(float)System.Math.Sqrt(dx*dx+dz*dz);
      if(next<distance[i]){distance[i]=next;parent[i]=best;}
     }
    }
    if(parent[1]>=0){var extra=new List<NavalWalkPoint>();for(int i=parent[1];i>0;i=parent[i])extra.Add(nodes[i]);extra.Reverse();result.AddRange(extra);}
   }
   result.Add(goal);previous=goal;
  }return result;
 }
 private static List<NavalWalkPoint> Around(List<NavalWalkPoint> route,NavalWalkPoint start,NavalShipDefinition ship,float floor,float z,float width,float depth,float centerX=0)
 {
  var result=new List<NavalWalkPoint>();var previous=start;float half=(width*.5f+.5f)/ship.Scale,front=z-(depth*.5f+.5f)/ship.Scale,back=z+(depth*.5f+.5f)/ship.Scale;
  foreach(var point in route){
   if(Math.Abs(previous.Y-floor)<.15f&&Math.Abs(point.Y-floor)<.15f&&Hits(new NavalWalkPoint(previous.X-centerX,previous.Y,previous.Z),new NavalWalkPoint(point.X-centerX,point.Y,point.Z),-half,half,front,back)){
    // Endpoints inside the clearance box are handled by runtime body/obstacle checks.
    bool aInside=Math.Abs(previous.X-centerX)<half&&previous.Z>front&&previous.Z<back;
    bool bInside=Math.Abs(point.X-centerX)<half&&point.Z>front&&point.Z<back;
    if(!aInside&&!bInside){
     float side=centerX+((previous.X+point.X-2*centerX)<0?-half:half);
     float entry=previous.Z<z?front:back,exit=point.Z<z?front:back;
     result.Add(new NavalWalkPoint(side,floor,entry));
     if(entry!=exit)result.Add(new NavalWalkPoint(side,floor,exit));
    }
   }
   result.Add(point);previous=point;
  }
  return result;
 }
 public static bool Hits(NavalWalkPoint a,NavalWalkPoint b,float left,float right,float front,float back)
 {
  float lo=0,hi=1;
  return Axis(a.X,b.X-a.X,left,right,ref lo,ref hi)&&Axis(a.Z,b.Z-a.Z,front,back,ref lo,ref hi)&&lo<hi;
 }
 private static bool Axis(float start,float delta,float min,float max,ref float lo,ref float hi)
 {
  if(Math.Abs(delta)<.00001f)return start>min&&start<max;
  float a=(min-start)/delta,b=(max-start)/delta;if(a>b){float t=a;a=b;b=t;}
  lo=Math.Max(lo,a);hi=Math.Min(hi,b);return lo<hi;
 }
}
