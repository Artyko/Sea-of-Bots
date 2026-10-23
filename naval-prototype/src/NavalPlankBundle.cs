// One board pays for a completed patch, never for travel or an interrupted job.
public sealed class NavalPlankBundle
{
 public const int Capacity=4;
 public int Count {get;private set;}
 public bool Refill(bool available){if(!available||Count>0)return false;Count=Capacity;return true;}
 public bool Consume(){if(Count==0)return false;Count--;return true;}
 public void Drop(){Count=0;}
}
public static class NavalPlankSupplyLayout
{
 public const float Z=2.4f,Width=1.4f,Height=1.05f,Depth=1.4f;
 public static float PickupZ(float scale){return Z-(Depth*.5f+.85f)/scale;}
}
