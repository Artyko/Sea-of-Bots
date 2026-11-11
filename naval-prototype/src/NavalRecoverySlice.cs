using System.Collections.Generic;
using System.Diagnostics;
public static class NavalRecoverySlice{
 private static int usedFrame=-1,grants;private static long frameStart;
 private static readonly List<int> queue=new List<int>();private static readonly Dictionary<int,int> seen=new Dictionary<int,int>();
 public static bool Take(int frame,int worker){
  if(!seen.ContainsKey(worker))queue.Add(worker);seen[worker]=frame;
  while(queue.Count>0&&frame-seen[queue[0]]>2){seen.Remove(queue[0]);queue.RemoveAt(0);}
  if(frame!=usedFrame){usedFrame=frame;grants=0;frameStart=Stopwatch.GetTimestamp();}
  if(grants>=4||queue.Count==0||queue[0]!=worker)return false;
  queue.RemoveAt(0);queue.Add(worker);grants++;return true;
 }
 public static bool Expired(long start){return (Stopwatch.GetTimestamp()-start)/(double)Stopwatch.Frequency>=.0005;}
}
