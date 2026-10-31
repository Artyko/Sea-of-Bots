using System;
using System.Collections.Generic;
// Route around the two central stock boxes before local PhysX obstacle recovery.
public static class NavalSupplyRoutes
{
 public static List<NavalWalkPoint> Plan(NavalWalkPoint start,NavalWalkPoint goal,NavalShipDefinition ship)
 {
 var route=NavalShipRoutes.Plan(start,goal,ship);
  for(int i=0;i+1<route.Count;i++){
   var a=route[i];var b=route[i+1];
   // A directional stair lane ends slightly off-centre. Do not mistake the
   // short lane-to-corridor step at a landing for a full deck crossing: moving
   // it to the preferred cargo crossing deletes the stair exit entirely.
   bool stairLanding=NavalStairTraffic.InPassage(a,ship)||NavalStairTraffic.InPassage(b,ship);
   if(stairLanding||a.X*b.X>=0||System.Math.Abs(a.Y-b.Y)>.01f||System.Math.Abs(a.Z-b.Z)>.01f||!ship.MaintenanceFloorAt(a.Y))continue;
   float before=i==0?start.Z:route[i-1].Z,after=i+2<route.Count?route[i+2].Z:goal.Z;
   float z=NavalCargoLayout.Crossing(before,after,ship);a.Z=b.Z=z;route[i]=a;route[i+1]=b;
  }
  int crates=NavalCargoLayout.Boxes(ship,0)+NavalCargoLayout.Boxes(ship,1)+NavalCargoLayout.Boxes(ship,2);
  for(int i=0;i<crates;i++){var crate=NavalCargoLayout.Position(ship,i);
   route=AroundCargo(route,start,ship,crate);
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
  // Cargo detours are inserted after the base ship route. Reapply directional
  // stair lanes so a detour beside the aft stores cannot approach the upper
  // flight diagonally and push a full-size sailor onto the landing coaming.
  var repaired=RepairSegments(start,route,ship);
  if(ship.SteamPowered){repaired=NavalStairTraffic.Apply(start,repaired,ship);repaired=RepairSegments(start,repaired,ship);}
  // Stair traffic can legitimately move the top landing towards the centre.
  // Reapply the weather-deck exclusion last so a same-deck trip is never
  // pulled back through the open flight or the funnel corridor.
  return NavalShipRoutes.ClearSteamWeatherDeck(start,repaired,ship);
 }
 private static bool CargoClear(NavalWalkPoint a,NavalWalkPoint b,NavalShipDefinition ship){
  if(ship.SteamPowered&&!HullClear(a,b,ship))return false;
  for(int level=0;level<ship.MaintenanceDeckCount;level++){float floor=ship.MaintenanceFloor(level);
   if(System.Math.Abs(a.Y-floor)<.08f&&System.Math.Abs(b.Y-floor)<.08f&&Hits(a,b,-ship.StairWidth*.5f-.5f/ship.Scale,ship.StairWidth*.5f+.5f/ship.Scale,-3f-.5f/ship.Scale,.5f+.5f/ship.Scale))return false;
  }
  int count=NavalCargoLayout.Boxes(ship,0)+NavalCargoLayout.Boxes(ship,1)+NavalCargoLayout.Boxes(ship,2);float half=NavalCargoLayout.RouteHalfExtent(ship);
  for(int i=0;i<count;i++){var c=NavalCargoLayout.Position(ship,i);if(System.Math.Abs(a.Y-c.Y)>.08f||System.Math.Abs(b.Y-c.Y)>.08f)continue;
   if(Hits(a,b,c.X-half,c.X+half,c.Z-half,c.Z+half))return false;
  }
  if(ship.SteamPowered&&ship.MaintenanceFloorAt(a.Y)&&ship.MaintenanceFloorAt(b.Y)&&System.Math.Abs(a.Y-b.Y)<.08f){
   float mx=NavalMainMagazineLayout.RouteHalfWidth(ship);
   if(Hits(a,b,-mx,mx,NavalMainMagazineLayout.RouteAft(ship),NavalMainMagazineLayout.RouteForward(ship)))return false;
  }
  return true;
 }
 private static bool HullClear(NavalWalkPoint a,NavalWalkPoint b,NavalShipDefinition ship){
  float dx=b.X-a.X,dz=b.Z-a.Z,distance=(float)Math.Sqrt(dx*dx+dz*dz)*ship.Scale;
  int steps=Math.Max(1,(int)Math.Ceiling(distance/.55f));
  // Include both endpoints and sample more finely than a sailor radius. This
  // rejects straight chords which cut through the convex bow even when both
  // centres individually appear to be inside the outline.
  for(int i=0;i<=steps;i++){float t=(float)i/steps,x=a.X+dx*t,z=a.Z+dz*t;
   if(!NavalCrewDeckSafety.Inside(x,z,ship,.85f))return false;
  }
  return true;
 }
 private sealed class PassageGraph {
  public readonly List<NavalWalkPoint> Nodes=new List<NavalWalkPoint>();public bool[,] Edges;public float[,] Lengths;
  public readonly List<NavalWalkPoint> WorkNodes=new List<NavalWalkPoint>();public float[] Distance;public int[] Parent;public bool[] Done;
 }
 private static readonly Dictionary<NavalShipDefinition,Dictionary<float,PassageGraph>> graphs=new Dictionary<NavalShipDefinition,Dictionary<float,PassageGraph>>();
 public static int GraphBuildCount {get;private set;}
 public static void Warm(NavalShipDefinition ship){for(int level=0;level<ship.MaintenanceDeckCount;level++)Graph(ship,ship.MaintenanceFloor(level));}
 private static PassageGraph Graph(NavalShipDefinition ship,float floor){
  Dictionary<float,PassageGraph> floors;if(!graphs.TryGetValue(ship,out floors)){floors=new Dictionary<float,PassageGraph>();graphs.Add(ship,floors);}
  PassageGraph graph;if(floors.TryGetValue(floor,out graph))return graph;
  graph=new PassageGraph();
    int count=NavalCargoLayout.Boxes(ship,0)+NavalCargoLayout.Boxes(ship,1)+NavalCargoLayout.Boxes(ship,2);float half=NavalCargoLayout.RouteWaypointHalfExtent(ship);
    for(int i=0;i<count;i++){var c=NavalCargoLayout.Position(ship,i);if(System.Math.Abs(floor-c.Y)>.08f)continue;
     for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2){var node=new NavalWalkPoint(c.X+x*half,c.Y,c.Z+z*half);if(CargoClear(node,node,ship))graph.Nodes.Add(node);}
    }
    // Include flight corners as graph nodes: a crate detour may itself meet a staircase.
    for(int level=0;level<ship.MaintenanceDeckCount;level++){
     float landingFloor=ship.MaintenanceFloor(level);if(System.Math.Abs(floor-landingFloor)>.08f)continue;
     float x=ship.StairWidth*.5f+.55f/ship.Scale;
     foreach(float side in new float[]{-x,x})foreach(float z in new float[]{-3f-.55f/ship.Scale,.5f+.55f/ship.Scale}){
      var node=new NavalWalkPoint(side,landingFloor,z);if(CargoClear(node,node,ship))graph.Nodes.Add(node);
     }
    }
    if(ship.SteamPowered&&ship.MaintenanceFloorAt(floor)){
     float x=NavalMainMagazineLayout.RouteHalfWidth(ship),aft=NavalMainMagazineLayout.RouteAft(ship),forward=NavalMainMagazineLayout.RouteForward(ship);
     foreach(float side in new float[]{-x,x})foreach(float z in new float[]{aft,forward}){
      var node=new NavalWalkPoint(side,floor,z);if(CargoClear(node,node,ship))graph.Nodes.Add(node);
     }
    }

  int n=graph.Nodes.Count;graph.Distance=new float[n+2];graph.Parent=new int[n+2];graph.Done=new bool[n+2];graph.WorkNodes.Capacity=n+2;graph.Edges=new bool[n,n];graph.Lengths=new float[n,n];
  for(int i=0;i<n;i++)for(int j=i+1;j<n;j++){
   bool clear=CargoClear(graph.Nodes[i],graph.Nodes[j],ship);graph.Edges[i,j]=graph.Edges[j,i]=clear;
   if(clear){float dx=graph.Nodes[i].X-graph.Nodes[j].X,dz=graph.Nodes[i].Z-graph.Nodes[j].Z;graph.Lengths[i,j]=graph.Lengths[j,i]=(float)Math.Sqrt(dx*dx+dz*dz);}
  }
  // Only immutable layout is shared; start, goal, job ownership and physical checks remain live.
  if(floors.Count>=16)floors.Clear();floors.Add(floor,graph);GraphBuildCount++;return graph;
 }
 private static List<NavalWalkPoint> RepairSegments(NavalWalkPoint start,List<NavalWalkPoint> route,NavalShipDefinition ship){
  var result=new List<NavalWalkPoint>();var previous=start;
  foreach(var goal in route){
   // The passage graph is two-dimensional and is only valid on a deck.
   // Feeding it the final sloping stair segment can replace the bottom
   // landing with a floor-level route underneath the same flight. Native
   // collision then pushes the sailor back up the stairs forever.
   bool horizontal=Math.Abs(previous.Y-goal.Y)<.01f;
   if(horizontal&&!CargoClear(previous,goal,ship)){
    var graph=Graph(ship,previous.Y);var nodes=graph.WorkNodes;nodes.Clear();nodes.Add(previous);nodes.Add(goal);nodes.AddRange(graph.Nodes);
    float[] distance=graph.Distance;int[] parent=graph.Parent;bool[] done=graph.Done;Array.Clear(done,0,done.Length);
    for(int i=0;i<nodes.Count;i++){distance[i]=float.PositiveInfinity;parent[i]=-1;}distance[0]=0;
    for(int step=0;step<nodes.Count;step++){
     int best=-1;for(int i=0;i<nodes.Count;i++)if(!done[i]&&(best<0||distance[i]<distance[best]))best=i;
     if(best<0||float.IsPositiveInfinity(distance[best]))break;if(best==1)break;done[best]=true;
     for(int i=1;i<nodes.Count;i++){if(done[i])continue;
      bool staticEdge=best>=2&&i>=2;
      if(staticEdge?!graph.Edges[best-2,i-2]:!CargoClear(nodes[best],nodes[i],ship))continue;
      float dx=nodes[best].X-nodes[i].X,dz=nodes[best].Z-nodes[i].Z,next=distance[best]+(staticEdge?graph.Lengths[best-2,i-2]:(float)System.Math.Sqrt(dx*dx+dz*dz));
      if(next<distance[i]){distance[i]=next;parent[i]=best;}
     }
    }
    if(parent[1]>=0){var extra=new List<NavalWalkPoint>();for(int i=parent[1];i>0;i=parent[i])extra.Add(nodes[i]);extra.Reverse();result.AddRange(extra);}
   }
   result.Add(goal);previous=goal;
  }return result;
 }
 private static List<NavalWalkPoint> AroundCargo(List<NavalWalkPoint> route,NavalWalkPoint start,NavalShipDefinition ship,NavalWalkPoint crate)
 {
  List<NavalWalkPoint> result=null;var previous=start;float clear=NavalCargoLayout.RouteHalfExtent(ship),half=NavalCargoLayout.RouteWaypointHalfExtent(ship);
  float clearFront=crate.Z-clear,clearBack=crate.Z+clear,front=crate.Z-half,back=crate.Z+half;
  for(int i=0;i<route.Count;i++){
   var point=route[i];
   if(Math.Abs(previous.Y-crate.Y)<.15f&&Math.Abs(point.Y-crate.Y)<.15f&&
      Hits(new NavalWalkPoint(previous.X-crate.X,previous.Y,previous.Z),new NavalWalkPoint(point.X-crate.X,point.Y,point.Z),-clear,clear,clearFront,clearBack)){
    bool aInside=Math.Abs(previous.X-crate.X)<clear&&previous.Z>clearFront&&previous.Z<clearBack;
    bool bInside=Math.Abs(point.X-crate.X)<clear&&point.Z>clearFront&&point.Z<clearBack;
    if(!aInside&&!bInside){
     if(result==null){result=new List<NavalWalkPoint>(route.Count+2);for(int j=0;j<i;j++)result.Add(route[j]);}
     float side=crate.X+((previous.X+point.X-2*crate.X)<0?-half:half);
     float entry=previous.Z<crate.Z?front:back,exit=point.Z<crate.Z?front:back;
     result.Add(new NavalWalkPoint(side,crate.Y,entry));if(entry!=exit)result.Add(new NavalWalkPoint(side,crate.Y,exit));
    }
   }
   if(result!=null)result.Add(point);previous=point;
  }
  return result??route;
 }
 private static List<NavalWalkPoint> Around(List<NavalWalkPoint> route,NavalWalkPoint start,NavalShipDefinition ship,float floor,float z,float width,float depth,float centerX=0)
 {
  List<NavalWalkPoint> result=null;var previous=start;float half=(width*.5f+.5f)/ship.Scale,front=z-(depth*.5f+.5f)/ship.Scale,back=z+(depth*.5f+.5f)/ship.Scale;
  for(int i=0;i<route.Count;i++){
   var point=route[i];
   if(Math.Abs(previous.Y-floor)<.15f&&Math.Abs(point.Y-floor)<.15f&&Hits(new NavalWalkPoint(previous.X-centerX,previous.Y,previous.Z),new NavalWalkPoint(point.X-centerX,point.Y,point.Z),-half,half,front,back)){
    bool aInside=Math.Abs(previous.X-centerX)<half&&previous.Z>front&&previous.Z<back;
    bool bInside=Math.Abs(point.X-centerX)<half&&point.Z>front&&point.Z<back;
    if(!aInside&&!bInside){
     if(result==null){result=new List<NavalWalkPoint>(route.Count+2);for(int j=0;j<i;j++)result.Add(route[j]);}
     float side=centerX+((previous.X+point.X-2*centerX)<0?-half:half);
     float entry=previous.Z<z?front:back,exit=point.Z<z?front:back;
     result.Add(new NavalWalkPoint(side,floor,entry));if(entry!=exit)result.Add(new NavalWalkPoint(side,floor,exit));
    }
   }
   if(result!=null)result.Add(point);previous=point;
  }
  return result??route;
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
