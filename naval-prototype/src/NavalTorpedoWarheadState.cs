using System;
// Torpedo explosives use their own sensitivity, separate from gun propellant magazines.
public sealed class NavalTorpedoWarheadState:INavalExplosiveStore {
 public const float InstantChance=.06f,IgnitionChance=.20f,FuseSeconds=10f;
 public bool Spent {get;private set;}public bool Wet {get;private set;}public float Fuse {get;private set;}private float heat;
 public bool Lit {get{return !Spent&&!Wet&&Fuse>0;}}
 public bool Ignite(float roll){if(Spent||Wet||Lit||roll>=IgnitionChance)return false;Fuse=FuseSeconds;return true;}
 public bool Impact(float roll){if(Spent||Wet)return false;if(roll<InstantChance){Spent=true;Fuse=0;return true;}if(roll<InstantChance+IgnitionChance)Ignite(0);return false;}
 public bool Step(float dt,bool nearFire,bool underwater){if(underwater)Douse();if(Spent||Wet)return false;if(!Lit){heat=Math.Max(0,heat+(nearFire?dt:-dt));if(heat>5)Ignite(0);return false;}Fuse=Math.Max(0,Fuse-Math.Max(0,dt));if(Fuse>0)return false;Spent=true;return true;}
 public void Douse(){Wet=true;Fuse=heat=0;}public void Reset(){Spent=Wet=false;Fuse=heat=0;}
}
