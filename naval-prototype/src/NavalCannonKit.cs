// A fetched kit survives loss of the stock crate; an unfetched kit does not exist.
public sealed class NavalCannonKit
{
 public bool Held {get;private set;}
 public bool Take(bool stockAvailable){if(Held||!stockAvailable)return false;Held=true;return true;}
 public bool Deliver(){if(!Held)return false;Held=false;return true;}
 public void Drop(){Held=false;}
}
