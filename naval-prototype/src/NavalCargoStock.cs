using System;
// Independent finite stocks. A transfer removes units before returning them.
public sealed class NavalCargoStock {
 public readonly int Capacity;public int Count {get;private set;}public bool Destroyed {get;private set;}
 public NavalCargoStock(int capacity){Capacity=Math.Max(0,capacity);Count=Capacity;}
 public int Take(int wanted){if(Destroyed)return 0;int n=Math.Min(Count,Math.Max(0,wanted));Count-=n;return n;}
 public int Put(int amount){if(Destroyed)return 0;int n=Math.Min(Capacity-Count,Math.Max(0,amount));Count+=n;return n;}
 public void Break(){Destroyed=true;Count=0;}public void Reset(){Destroyed=false;Count=Capacity;}
}
