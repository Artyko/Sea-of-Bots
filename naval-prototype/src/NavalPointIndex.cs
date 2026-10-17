using System;
using System.Collections.Generic;

// Conservative broad phase for fixed local voxel centres. Exact damage is tested afterwards.
public sealed class NavalPointIndex
{
    private struct Key:IEquatable<Key>
    {
        public int X,Y,Z;
        public bool Equals(Key b){return X==b.X&&Y==b.Y&&Z==b.Z;}
        public override bool Equals(object value){return value is Key&&Equals((Key)value);}
        public override int GetHashCode(){unchecked{return X*73856093^Y*19349663^Z*83492791;}}
    }
    private readonly Dictionary<Key,List<int>> buckets=new Dictionary<Key,List<int>>();
    private static int Bin(float value){return (int)Math.Floor(value);}
    public void Clear(){buckets.Clear();}
    public void Add(int id,float x,float y,float z)
    {
        Key key=new Key{X=Bin(x),Y=Bin(y),Z=Bin(z)};List<int> bucket;
        if(!buckets.TryGetValue(key,out bucket)){bucket=new List<int>();buckets.Add(key,bucket);}
        bucket.Add(id);
    }
    public void Query(float x,float y,float z,float radius,List<int> result)
    {
        result.Clear();int x0=Bin(x-radius),x1=Bin(x+radius),y0=Bin(y-radius),y1=Bin(y+radius),z0=Bin(z-radius),z1=Bin(z+radius);
        // Large debug blasts visit existing buckets rather than an enormous empty volume.
        long volume=((long)x1-x0+1)*((long)y1-y0+1)*((long)z1-z0+1);
        if(volume>buckets.Count*2L)
        {
            foreach(KeyValuePair<Key,List<int>> entry in buckets)
                if(entry.Key.X>=x0&&entry.Key.X<=x1&&entry.Key.Y>=y0&&entry.Key.Y<=y1&&entry.Key.Z>=z0&&entry.Key.Z<=z1)result.AddRange(entry.Value);
            return;
        }
        for(int a=x0;a<=x1;a++)for(int b=y0;b<=y1;b++)for(int c=z0;c<=z1;c++)
        {List<int> bucket;if(buckets.TryGetValue(new Key{X=a,Y=b,Z=c},out bucket))result.AddRange(bucket);}
    }
}
