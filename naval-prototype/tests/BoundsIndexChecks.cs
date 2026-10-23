using System;
using System.Collections.Generic;
class BoundsIndexChecks {
 static void Check(bool value,string why){if(!value)throw new Exception(why);}
 static void Main(){var index=new NavalBoundsIndex();var random=new Random(195);float[,] bounds=new float[716,6];
 for(int id=0;id<716;id++){
 float x=(float)random.NextDouble()*1600-800,z=(float)random.NextDouble()*1600-800,y=(float)random.NextDouble()*70-30;
 float sx=(float)random.NextDouble()*50+1,sy=(float)random.NextDouble()*15+1,sz=(float)random.NextDouble()*50+1;
 if(id==715){x=160;sx=400;z=0;sz=40;y=0;sy=20;}
 bounds[id,0]=x-sx*.5f;bounds[id,1]=y-sy*.5f;bounds[id,2]=z-sz*.5f;bounds[id,3]=x+sx*.5f;bounds[id,4]=y+sy*.5f;bounds[id,5]=z+sz*.5f;
 index.Add(id,bounds[id,0],bounds[id,1],bounds[id,2],bounds[id,3],bounds[id,4],bounds[id,5]);
 }
 var result=new List<int>();var found=new HashSet<int>();long candidates=0;
 for(int query=0;query<1500;query++){
 float x=(float)random.NextDouble()*1600-800,z=(float)random.NextDouble()*1600-800,y=(float)random.NextDouble()*70-30;
 float radius=query%20==0?500:35;
 index.Query(x-radius,y-20,z-radius,x+radius,y+20,z+radius,result);found.Clear();
 foreach(int id in result)Check(found.Add(id),"A terrain block cannot be returned twice");
 for(int id=0;id<716;id++){
 bool touches=bounds[id,0]<=x+radius&&bounds[id,3]>=x-radius&&bounds[id,1]<=y+20&&bounds[id,4]>=y-20&&bounds[id,2]<=z+radius&&bounds[id,5]>=z-radius;
 if(touches)Check(found.Contains(id),"Conservative broad phase must not miss any intersecting terrain block");
 }candidates+=result.Count;
 }
 index.Query(-1,-1,-1,1,1,1,result);Check(result.Contains(715),"Large island whose centre is far away is still indexed at its near edge");
 Check(candidates<1500L*716/4,"Island candidate work is materially reduced");
 Console.WriteLine("PASS: 1500 terrain queries against brute-force bounds, giant overlapping block, duplicate-free results; "+(100.0*candidates/(1500*716)).ToString("F1")+"% candidates versus full terrain scans. Not game FPS.");
 }
}
