using System;
class MastDurabilityChecks
{
 static void Check(bool ok,string name){if(!ok)throw new Exception(name);}
 static void Main(){
 float first=NavalMastDurability.Accumulate(0f,1f);
 Check(!NavalMastDurability.Broken(first),"First cannonball cannot remove an intact mast voxel");
 Check(NavalMastDurability.Broken(NavalMastDurability.Accumulate(first,1f)),"Second hit at same voxel breaks timber");
 Check(!NavalMastDurability.Broken(NavalMastDurability.Accumulate(0f,100f)),"A single shot never contributes two hits");
 float left=NavalMastDurability.Accumulate(0f,1f),right=NavalMastDurability.Accumulate(0f,1f);
 Check(!NavalMastDurability.Broken(left)&&!NavalMastDurability.Broken(right),"Separated hits do not pool damage across the mast");
 float wet=0f;for(int i=0;i<3;i++)wet=NavalMastDurability.Accumulate(wet,.5f);
 Check(!NavalMastDurability.Broken(wet),"Underwater impacts retain reduced power");
 Check(NavalMastDurability.Broken(NavalMastDurability.Accumulate(wet,.5f)),"Repeated reduced impacts eventually break timber");
 Check(NavalMastDurability.Accumulate(1f,-1f)==1f,"Negative impact cannot heal timber");
 Console.WriteLine("Mast durability checks passed");
 }
}
