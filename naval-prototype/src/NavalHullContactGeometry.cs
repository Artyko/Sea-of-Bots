using System;
using System.Collections.Generic;
using UnityEngine;
// Profile-specific convex waterline footprint in absolute world metres.
public static class NavalHullContactGeometry
{
 private static readonly Dictionary<NavalShipDefinition,Vector2[]> cache=new Dictionary<NavalShipDefinition,Vector2[]>();
 private static readonly Dictionary<NavalShipDefinition,Vector2[][]> buffers=new Dictionary<NavalShipDefinition,Vector2[][]>();
 private static Vector2[] Buffer(NavalShipDefinition ship,int index){Vector2[][] pair;if(!buffers.TryGetValue(ship,out pair)){int count=Outline(ship).Length;pair=new[]{new Vector2[count],new Vector2[count]};buffers[ship]=pair;}return pair[index];}
 private static float Cross(Vector2 a,Vector2 b,Vector2 c){return (b.x-a.x)*(c.y-a.y)-(b.y-a.y)*(c.x-a.x);}
 private static Vector2[] Outline(NavalShipDefinition ship){
  Vector2[] saved;if(cache.TryGetValue(ship,out saved))return saved;
  var points=new List<Vector2>();for(int i=0;i<=48;i++){float z=-6f+12f*i/48f,w=ship.HullHalfWidth(z);points.Add(new Vector2(-w*ship.Scale,z*ship.Scale));points.Add(new Vector2(w*ship.Scale,z*ship.Scale));}
  points.Sort((a,b)=>a.x==b.x?a.y.CompareTo(b.y):a.x.CompareTo(b.x));var hull=new List<Vector2>();
  foreach(var p in points){while(hull.Count>=2&&Cross(hull[hull.Count-2],hull[hull.Count-1],p)<=0)hull.RemoveAt(hull.Count-1);hull.Add(p);}
  int lower=hull.Count;for(int i=points.Count-2;i>=0;i--){var p=points[i];while(hull.Count>lower&&Cross(hull[hull.Count-2],hull[hull.Count-1],p)<=0)hull.RemoveAt(hull.Count-1);hull.Add(p);}hull.RemoveAt(hull.Count-1);
  saved=hull.ToArray();cache[ship]=saved;return saved;
 }
 private static void Pose(Vector2[] local,Vector2[] world,float x,float z,float yaw){double angle=yaw*Math.PI/180;float c=(float)Math.Cos(angle),s=(float)Math.Sin(angle);for(int i=0;i<local.Length;i++)world[i]=new Vector2(x+local[i].x*c+local[i].y*s,z+local[i].y*c-local[i].x*s);}
 private static bool Axes(Vector2[] a,Vector2[] b){for(int i=0;i<a.Length;i++){Vector2 d=a[(i+1)%a.Length]-a[i],n=new Vector2(-d.y,d.x);float loA=float.PositiveInfinity,hiA=float.NegativeInfinity,loB=loA,hiB=hiA;foreach(var p in a){float v=p.x*n.x+p.y*n.y;loA=Math.Min(loA,v);hiA=Math.Max(hiA,v);}foreach(var p in b){float v=p.x*n.x+p.y*n.y;loB=Math.Min(loB,v);hiB=Math.Max(hiB,v);}if(hiA<=loB||hiB<=loA)return false;}return true;}
 public static float SafeFraction(NavalShipDefinition a,NavalShipDefinition b,float x,float z,float yaw,float nx,float nz,float turn,float bx,float bz,float byaw){
  var la=Outline(a);var lb=Outline(b);var wa=Buffer(a,0);var wb=Buffer(b,1);Pose(lb,wb,bx,bz,byaw);
  int steps=Math.Max(1,(int)Math.Ceiling(Math.Max(Math.Sqrt((nx-x)*(nx-x)+(nz-z)*(nz-z))/.25,Math.Abs(turn)/.5)));float safe=0;
  for(int i=1;i<=steps;i++){float t=i/(float)steps;Pose(la,wa,x+(nx-x)*t,z+(nz-z)*t,yaw+turn*t);if(Axes(wa,wb)&&Axes(wb,wa)){float lo=safe,hi=t;for(int j=0;j<12;j++){float mid=(lo+hi)*.5f;Pose(la,wa,x+(nx-x)*mid,z+(nz-z)*mid,yaw+turn*mid);if(Axes(wa,wb)&&Axes(wb,wa))hi=mid;else lo=mid;}return lo;}safe=t;}return 1;
 }
 public static Vector3[] WorldOutline(NavalShipDefinition ship,Vector3 position,float yaw){var local=Outline(ship);var flat=new Vector2[local.Length];Pose(local,flat,position.x,position.z,yaw);var result=new Vector3[flat.Length];for(int i=0;i<flat.Length;i++)result[i]=new Vector3(flat[i].x,0,flat[i].y);return result;}
}
