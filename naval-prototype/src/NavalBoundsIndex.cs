using System;
using System.Collections.Generic;
// Conservative immutable-world broad phase. Every occupied bucket of a bound is indexed.
public sealed class NavalBoundsIndex
{
 private struct Key:IEquatable<Key>{public int X,Y,Z;public bool Equals(Key other){return X==other.X&&Y==other.Y&&Z==other.Z;}public override bool Equals(object value){return value is Key&&Equals((Key)value);}public override int GetHashCode(){unchecked{return X*73856093^Y*19349663^Z*83492791;}}}
 private readonly Dictionary<Key,List<int>> buckets=new Dictionary<Key,List<int>>();
 private readonly HashSet<int> seen=new HashSet<int>();
 private static int Bin(float value){return (int)Math.Floor(value/32f);}
 public void Add(int id,float minX,float minY,float minZ,float maxX,float maxY,float maxZ)
 {
  for(int x=Bin(minX);x<=Bin(maxX);x++)for(int y=Bin(minY);y<=Bin(maxY);y++)for(int z=Bin(minZ);z<=Bin(maxZ);z++){
   var key=new Key{X=x,Y=y,Z=z};List<int> bucket;if(!buckets.TryGetValue(key,out bucket)){bucket=new List<int>();buckets.Add(key,bucket);}bucket.Add(id);
  }
 }
 public void Query(float minX,float minY,float minZ,float maxX,float maxY,float maxZ,List<int> output)
 {
  output.Clear();seen.Clear();int x0=Bin(minX),x1=Bin(maxX),y0=Bin(minY),y1=Bin(maxY),z0=Bin(minZ),z1=Bin(maxZ);
  long volume=((long)x1-x0+1)*((long)y1-y0+1)*((long)z1-z0+1);
  if(volume>buckets.Count*2L){
   foreach(var entry in buckets)if(entry.Key.X>=x0&&entry.Key.X<=x1&&entry.Key.Y>=y0&&entry.Key.Y<=y1&&entry.Key.Z>=z0&&entry.Key.Z<=z1)Append(entry.Value,output);
  }else for(int x=x0;x<=x1;x++)for(int y=y0;y<=y1;y++)for(int z=z0;z<=z1;z++){
   List<int> bucket;if(buckets.TryGetValue(new Key{X=x,Y=y,Z=z},out bucket))Append(bucket,output);
  }
 }
 private void Append(List<int> bucket,List<int> output){foreach(int id in bucket)if(seen.Add(id))output.Add(id);}
}
