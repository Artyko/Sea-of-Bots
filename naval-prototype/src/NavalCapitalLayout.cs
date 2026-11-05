using System;
using System.Collections.Generic;
using UnityEngine;

// Data for a five-turret all-big-gun ship. Coordinates are gameplay-scaled,
// not a metre-for-metre replica of the historical ship.
public static class NavalCapitalLayout
{
    public const int Turrets=5,CalibreMm=305;
    public const float BridgeZ=3.30f,Power=7.8f,Speed=125f,Range=850f,Reload=30f,Radius=.72f;
    public static float SecondaryZ(int gun){float[] z={-3.1f,-1.4f,0f,3f};return z[Math.Min(3,gun/2)];}
    public static Vector3 Mount(int slot){Vector3[] p={new Vector3(0,0,4.4f),new Vector3(-1.4f,0,1.7f),new Vector3(1.4f,0,1.7f),new Vector3(0,0,-4.25f),new Vector3(0,0,-5.5f)};return p[Math.Max(0,Math.Min(4,slot))];}
    public static float TurretRise(int slot){return slot==3?.55f:0f;}
    public static float MinimumPitch(int slot){return slot==3?-6f:-12f;}
    public static Vector3 PlayerStart(NavalShipDefinition ship){return new Vector3(.90f,ship.WeatherDeck,-.65f);}
    public static Vector3 ServicePoint(NavalShipDefinition ship,int slot){
        Vector3[] posts={new Vector3(.90f,0,4f),new Vector3(-.40f,0,1.7f),new Vector3(.40f,0,1.7f),new Vector3(.90f,0,-3.70f),new Vector3(.90f,0,-5f)};
        Vector3 p=posts[Math.Max(0,Math.Min(4,slot))];p.y=ship.WeatherDeck;return p;
    }
    public static float BaseYaw(int slot){return slot==1?-90f:slot==2?90f:slot>=3?180f:0f;}
    public static bool Bears(int slot,float yaw){float delta=NavalBroadsideMath.Delta(BaseYaw(slot),yaw);return Math.Abs(delta)<(slot==1||slot==2?100f:135f);}
    public static string Name(int slot){string[] names={"A","P","Q","X","Y"};return names[slot];}
    private struct Block {public float L,R,A,F;public Block(float l,float r,float a,float f){L=l;R=r;A=a;F=f;}}
    private static readonly Dictionary<NavalShipDefinition,List<Block>> blockCache=new Dictionary<NavalShipDefinition,List<Block>>();
    private static List<Block> Blocks(NavalShipDefinition ship){
        List<Block> cached;if(blockCache.TryGetValue(ship,out cached))return cached;
        float m=.90f/ship.Scale;var b=new List<Block>();
        for(int i=0;i<Turrets;i++){Vector3 p=Mount(i);float w=Radius+m;b.Add(new Block(p.x-w,p.x+w,p.z-w,p.z+w));}
        // Raised X access staircase is a physical obstruction for other duties.
        Vector3 ramp=Mount(3);b.Add(new Block(ramp.x-1.40f-m,ramp.x-.43f+m,ramp.z-.20f-m,ramp.z+.20f+m));
        b.Add(new Block(-ship.StairWidth*.5f-m,ship.StairWidth*.5f+m,-3.15f,.65f));
        b.Add(new Block(-.70f-m,.70f+m,BridgeZ-.50f-m,BridgeZ+.50f+m));
        b.Add(new Block(-1.14f-m,-.76f+m,.20f-m,.60f+m));
        b.Add(new Block(-1.14f-m,-.76f+m,-2.2f-m,-1.8f+m));
        foreach(float z in new[]{3.42f,-3.40f})b.Add(new Block(.65f-m,1.05f+m,z-.06f-m,z+.06f+m));
        blockCache[ship]=b;return b;
    }
    private static bool Clear(NavalWalkPoint a,NavalWalkPoint b,NavalShipDefinition ship,List<Block> blocks){
        foreach(var box in blocks)if(NavalSupplyRoutes.Hits(a,b,box.L,box.R,box.A,box.F))return false;
        for(int i=0;i<=12;i++){float t=i/12f;if(!NavalCrewDeckSafety.Inside(a.X+(b.X-a.X)*t,a.Z+(b.Z-a.Z)*t,ship,1.4f))return false;}
        return true;
    }
    public static bool WeatherClear(NavalWalkPoint a,NavalWalkPoint b,NavalShipDefinition ship){return Clear(a,b,ship,Blocks(ship));}
    public static bool MagazineBlocks(NavalWalkPoint a,NavalWalkPoint b,NavalShipDefinition ship){
        if(!ship.CapitalShip||!ship.MaintenanceFloorAt(a.Y)||Math.Abs(a.Y-b.Y)>.08f)return false;
        float hx=(NavalMainMagazineLayout.Width*.5f+1.15f)/ship.Scale,hz=(NavalMainMagazineLayout.Depth*.5f+1.15f)/ship.Scale;
        for(int i=0;i<Turrets;i++){Vector3 p=Mount(i);if(NavalSupplyRoutes.Hits(a,b,p.x-hx,p.x+hx,p.z-hz,p.z+hz))return true;}return false;
    }
    private static readonly Dictionary<NavalShipDefinition,List<NavalWalkPoint>> corners=new Dictionary<NavalShipDefinition,List<NavalWalkPoint>>();
    private static readonly Dictionary<NavalShipDefinition,bool[,]> connections=new Dictionary<NavalShipDefinition,bool[,]>();
    private static List<NavalWalkPoint> Corners(NavalShipDefinition ship,List<Block> blocks){
        List<NavalWalkPoint> points;if(corners.TryGetValue(ship,out points))return points;
        points=new List<NavalWalkPoint>();foreach(var b in blocks)foreach(float x in new[]{b.L-.025f,b.R+.025f})foreach(float z in new[]{b.A-.025f,b.F+.025f}){var p=new NavalWalkPoint(x,ship.WeatherDeck,z);if(Clear(p,p,ship,blocks))points.Add(p);}
        corners.Add(ship,points);var links=new bool[points.Count,points.Count];for(int i=0;i<points.Count;i++)for(int j=i+1;j<points.Count;j++)links[i,j]=links[j,i]=Clear(points[i],points[j],ship,blocks);connections.Add(ship,links);return points;
    }
    private static void Segment(List<NavalWalkPoint> result,NavalWalkPoint from,NavalWalkPoint goal,NavalShipDefinition ship,List<Block> blocks){
        if(Clear(from,goal,ship,blocks)){result.Add(goal);return;}
        var nodes=new List<NavalWalkPoint>();nodes.Add(from);nodes.Add(goal);nodes.AddRange(Corners(ship,blocks));var links=connections[ship];
        int n=nodes.Count;float[] distance=new float[n];int[] parent=new int[n];bool[] done=new bool[n];for(int i=0;i<n;i++){distance[i]=float.PositiveInfinity;parent[i]=-1;}distance[0]=0;
        for(int pass=0;pass<n;pass++){int best=-1;for(int i=0;i<n;i++)if(!done[i]&&(best<0||distance[i]<distance[best]))best=i;
            if(best<0||float.IsInfinity(distance[best])||best==1)break;done[best]=true;
            for(int i=1;i<n;i++)if(!done[i]&&(best>=2&&i>=2?links[best-2,i-2]:Clear(nodes[best],nodes[i],ship,blocks))){float dx=nodes[best].X-nodes[i].X,dz=nodes[best].Z-nodes[i].Z,cost=distance[best]+(float)Math.Sqrt(dx*dx+dz*dz);if(cost<distance[i]){distance[i]=cost;parent[i]=best;}}
        }
        if(parent[1]<0){result.Add(goal);return;}var reverse=new List<NavalWalkPoint>();for(int i=1;i!=0;i=parent[i])reverse.Add(nodes[i]);reverse.Reverse();result.AddRange(reverse);
    }
    public static List<NavalWalkPoint> Route(NavalWalkPoint start,List<NavalWalkPoint> planned,NavalShipDefinition ship){
        if(!ship.CapitalShip||planned.Count==0)return planned;var result=new List<NavalWalkPoint>();var previous=start;var blocks=Blocks(ship);
        for(int i=0;i<planned.Count;i++){var p=planned[i];bool weather=previous.Y>=ship.WeatherDeck-.005f&&p.Y>=ship.WeatherDeck-.005f&&Math.Abs(previous.Y-p.Y)<.08f;
            bool inBridge=Math.Abs(p.X)<.3f&&Math.Abs(p.Z-BridgeZ)<.35f;
            bool fromBridge=Math.Abs(previous.X)<.3f&&Math.Abs(previous.Z-BridgeZ)<.35f;
            if(weather&&fromBridge){var exit=new NavalWalkPoint(0,ship.WeatherDeck,BridgeZ-.90f);result.Add(exit);previous=exit;}
            if(weather&&inBridge){var entry=new NavalWalkPoint(0,ship.WeatherDeck,BridgeZ-.90f);Segment(result,previous,entry,ship,blocks);result.Add(p);}
            else if(weather){if(i<planned.Count-1&&!Clear(p,p,ship,blocks))continue;Segment(result,previous,p,ship,blocks);}
            else result.Add(p);previous=p;
        }
        return result;
    }
}
