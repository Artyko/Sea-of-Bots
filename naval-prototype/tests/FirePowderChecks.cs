using System;
class FirePowderChecks
{
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 static void Main()
 {
  var dry=new NavalFireState(false);Check(dry.Ignite(0),"Dry wood ignites");
  Check(!dry.Step(1,false,1)&&dry.Burning&&dry.Burned>.8f&&dry.Burned<1f&&dry.Intensity>.3f,"Fire grows gradually, not instant voxel removal");
  var developed=new NavalFireState(false);developed.Ignite(0,.9f);Check(developed.Cool(1,1.8f)&&developed.Burning,"One bucket knocks down but does not erase an established fire");
  Check(!developed.Cool(2,1.8f)&&!developed.Burning,"A second bucket extinguishes the cooled fire");
  dry.Douse(1);Check(!dry.Burning&&!dry.Ignite(18),"Water extinguishes and prevents immediate re-ignition");
  float charred=dry.Burned;Check(dry.Ignite(19)&&dry.Burned==charred,"Drying permits re-ignition without healing charred fuel");
  Check(dry.Step(25,false,44)&&!dry.Burning&&!dry.Ignite(100),"Burned-out wood produces exactly one destruction event");
  Check(!dry.Step(1,false,101),"No duplicate burn damage");
  var cloth=new NavalFireState(true);cloth.Ignite(0);Check(cloth.Step(10,false,10),"Cloth burns faster than timber");
  var submerged=new NavalFireState(false);submerged.Ignite(0);Check(!submerged.Step(.25f,true,.25f)&&!submerged.Burning,"Submergence extinguishes before burn damage");
  Check(NavalArmorMath.InternalFireChance(5.2f,2f)>NavalArmorMath.InternalFireChance(1.55f,.2f),"Heavy internal burst has greater fire risk");
  Check(NavalArmorMath.InternalFireChance(5.2f,2f)<.25f&&NavalArmorMath.InternalFireChance(1.55f,.2f)<.08f,"v251 internal ignition remains exceptional rather than ship-wide");
  Check(NavalFireSystem.FireLimit==12,"Per-ship fire simulation is bounded before collider rebuilds and crew routing dominate frame time");
  Check(NavalArmorMath.InternalFireLimit(5.2f,4f)==3&&NavalArmorMath.InternalFireLimit(1.55f,.2f)==1,"Internal burst fire count is caliber and residual-energy bounded");
  var directReady=new NavalPowderState();Check(!NavalExplosiveImpact.DirectHit(directReady,.5f)&&directReady.Lit,"Direct shell hit always lights surviving ready ammunition");
  var directInstant=new NavalPowderState();Check(NavalExplosiveImpact.DirectHit(directInstant,.05f)&&directInstant.Spent,"Direct ready-ammunition hit preserves immediate detonation chance");
  var directMagazine=new NavalMagazineState();Check(!NavalExplosiveImpact.DirectHit(directMagazine,.5f)&&directMagazine.Lit,"Penetrating direct hit always lights an enclosed magazine");
  var wetDirect=new NavalPowderState();wetDirect.Douse();Check(!NavalExplosiveImpact.DirectHit(wetDirect,0)&&!wetDirect.Lit,"Flooded ammunition remains inert on impact");
  int lit=0;for(int i=0;i<1000;i++){var test=new NavalPowderState();if(test.Ignite((i+.5f)/1000))lit++;}
  Check(lit==400,"Each direct-hit roll has a 40 percent ignition interval");
  int instant=0,fuses=0;for(int i=0;i<1000;i++){var t=new NavalPowderState();if(t.Impact((i+.5f)/1000))instant++;else if(t.Lit)fuses++;}
  Check(instant==100&&fuses==400,"Direct hits: 10% instant, 40% fuse, 50% neither");
  var spent=new NavalPowderState();Check(spent.Impact(0)&&!spent.Impact(0)&&!spent.Step(20,true,false),"Instant blast is emitted once");
  spent.Reset();spent.Douse();Check(!spent.Impact(0)&&!spent.Lit,"Wet powder resists direct hits");
  Check(NavalFireResponseMath.MayJoin(0,false)&&!NavalFireResponseMath.MayJoin(1,false)&&NavalFireResponseMath.MayJoin(1,true)&&!NavalFireResponseMath.MayJoin(2,true),"One firefighter normally; bounded critical assistance");
  var barrel=new NavalPowderState();Check(barrel.Ignite(0)&&barrel.Lit,"Successful hit starts a fuse");
  for(int i=0;i<47;i++)Check(!barrel.Step(.25f,false,false),"No explosion before 12 seconds");
  barrel.Douse();Check(!barrel.Lit&&!barrel.Step(10,true,false)&&!barrel.Ignite(0),"Last-moment bucket cancels fuse and wets powder");
  barrel.Reset();barrel.Ignite(0);Check(barrel.Step(12,false,false)&&barrel.Spent,"Unattended fuse explodes");
  Check(!barrel.Step(12,true,false)&&!barrel.Ignite(0),"Spent powder cannot explode twice or reignite");
  var adjacent=new NavalPowderState();for(int i=0;i<7;i++)adjacent.Step(.25f,true,false);
  Check(!adjacent.Lit,"Brief heat does not instantly explode powder");adjacent.Step(.25f,true,false);Check(adjacent.Lit&&adjacent.Fuse==12,"Sustained heat starts full warning fuse");
  adjacent.Step(.25f,false,true);Check(adjacent.Wet&&!adjacent.Lit,"Flooding cancels a live fuse");
  var first=new NavalPowderState();var second=new NavalPowderState();first.Ignite(0);Check(first.Step(12,false,false),"First barrel explodes");second.Ignite(0);Check(!second.Step(.25f,false,false),"Chain ignition also leaves warning time");
  var grid=new NavalFloodGrid(12,24,5.75f,11.75f,2,4);grid.Add(grid.Index(1,1),4);
  var bucket=new NavalBucketLoad();float before=grid.Total;bucket.Scoop(grid,1,1,NavalBucketLoad.DefaultCapacity,1);
  Check(Math.Abs(grid.Total+bucket.Amount-before)<.0001,"Taking firefighting water from the bilge conserves mass");
  float poured=bucket.Dump();grid.Add(grid.Index(-1,1),poured);Check(Math.Abs(grid.Total-before)<.0001,"Pouring into the enclosed hold returns water to the grid");
  Check(NavalFireResponseMath.YieldHelm(true,false,1),"Only free helmsman may save uncovered powder fuse");
  Check(!NavalFireResponseMath.YieldHelm(false,false,1)&&!NavalFireResponseMath.YieldHelm(true,true,1)&&!NavalFireResponseMath.YieldHelm(true,false,2),"Ordinary fire, existing firefighter and spare hands preserve helm");
  Check(NavalFireResponseMath.InterruptPump(true,false)&&!NavalFireResponseMath.InterruptPump(true,true),"Fuse can interrupt routine pumping but not critical flood pumping");
  Check(NavalFireResponseMath.NeedsResponder(true,false,true,true)&&!NavalFireResponseMath.NeedsResponder(true,true,false,true),"Urgent fire requests one responder without duplicate staffing");
  Check(!NavalFireResponseMath.ReservesMaintenance(false,true),"Engine watch no longer suppresses an ordinary-fire responder");
  Check(NavalFireResponseMath.ReservesMaintenance(true,false),"Actual repair work still reserves the maintenance slot");
  Console.WriteLine("PASS: staged fire growth and bucket cooling, finite fuel, AP internal ignition, wetting/drying, fuses, submerged powder, single detonation and bucket mass conservation.");
 }
}
