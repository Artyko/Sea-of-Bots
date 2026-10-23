using System;
using System.Collections.Generic;
class GalleonChecks
{
 static void Check(bool ok,string name){if(!ok)throw new Exception(name);}
 static void Near(float a,float b,string name){Check(Math.Abs(a-b)<.0001f,name);}
 static void Route(NavalWalkPoint start,NavalWalkPoint goal,NavalShipDefinition ship)
 {
  var route=NavalShipRoutes.Plan(start,goal,ship);Check(route.Count>0,"Non-empty route");var previous=start;
  foreach(var p in route)
  {
   Check(!float.IsNaN(p.Y)&&Math.Abs(p.X)<=2.5f&&Math.Abs(p.Z)<=5.9f,"Route stays on hull");
   Check(Math.Abs(p.Y-previous.Y)*ship.Scale<.5f,"No teleport between decks");previous=p;
  }
  Near(previous.X,goal.X,"Route reaches target X");Near(previous.Y,goal.Y,"Route reaches target deck");Near(previous.Z,goal.Z,"Route reaches target Z");
 }
 static void Main()
 {
  var ship=NavalShipDefinition.Galleon;
  Check(ship.DeckCount==3&&ship.MastZ.Length==3&&ship.CrewCount==6&&ship.GunCount==6,"Three decks, three masts, six sailors, six guns");
  Check(ship.Scale>NavalShipDefinition.Brigantine.Scale,"Galleon is larger than brigantine");
  Near(ship.HoldClearance,NavalShipDefinition.Brigantine.HoldClearance,"Lower hold uses brigantine world headroom");
  Near((ship.WeatherDeck-.25f-.825f)*ship.Scale,ship.HoldClearance,"Middle deck has the same clear ceiling height");
  Check(ship.SailSize(0)>ship.SailSize(1)&&ship.SailSize(1)>ship.SailSize(2),"Three distinct descending sail sizes");
  Near(ship.GunFixtureScale*ship.Scale,2f,"Cannons retain standard world size on the large hull");
  Check(ship.GunPostX<ship.GunMountX&&ship.GunMountX<BoatDimensions.HalfWidth,"Crew stands behind hull-mounted guns");
  Near(ship.GunDeck,ship.WeatherDeck,"All guns on the upper weather deck");
  Near(ship.HelmDeck,ship.WeatherDeck,"Helm on weather deck");
  for(int i=0;i<6;i++)
  {
   Check(ship.IsOpening(NavalShipDefinition.GunSide(i)*3f,ship.GunMountY+.35f*ship.GunFixtureScale,NavalShipDefinition.GunZ(ship,i),.1f,.1f,.1f),"All six barrel muzzles have actual hull openings");
   Check(!ship.IsOpening(NavalShipDefinition.GunSide(i)*3f,ship.WeatherDeck-.11f,NavalShipDefinition.GunZ(ship,i),.1f,.1f,.1f),"Gunports do not cut weather-deck rail");
   Check(Math.Abs(NavalShipDefinition.GunZ(ship,i)-ship.LadderZ)>1.45f,"Boarding ladder clear of all guns");
   for(int j=0;j<i;j++)if(NavalShipDefinition.GunSide(j)==NavalShipDefinition.GunSide(i))Check(Math.Abs(NavalShipDefinition.GunZ(ship,i)-NavalShipDefinition.GunZ(ship,j))>3,"Three separate guns on each side");
  }
  foreach(float x in new[]{-1.65f,1.65f})foreach(float z in new[]{-4.5f,1.2f,4.8f})
  {
   var low=new NavalWalkPoint(x,ship.HoldFloor,z);var middle=new NavalWalkPoint(x,.825f,z);var upper=new NavalWalkPoint(x,ship.WeatherDeck,z);
   Route(low,upper,ship);Route(upper,low,ship);Route(middle,upper,ship);Route(upper,middle,ship);
   Route(new NavalWalkPoint(0,ship.HelmDeck,-5.45f),new NavalWalkPoint(0,ship.HoldFloor,-4.9f),ship);
  }
  Route(new NavalWalkPoint(0,.825f+ship.UpperDeckOffset*.5f,-1.25f),new NavalWalkPoint(1.65f,ship.HoldFloor,3),ship);
  Route(new NavalWalkPoint(0,.825f+ship.UpperDeckOffset*.5f,-1.25f),new NavalWalkPoint(1.65f,ship.WeatherDeck,3),ship);
  var random=new Random(113);
  foreach(var old in new[]{NavalShipDefinition.Sloop,NavalShipDefinition.Brigantine})for(int i=0;i<1500;i++)
  {
   var a=new NavalWalkPoint((float)(random.NextDouble()*4-2),i%2==0?old.HoldFloor:.825f,(float)(random.NextDouble()*10-5));
   var b=new NavalWalkPoint((float)(random.NextDouble()*4-2),i%3==0?old.HoldFloor:.825f,(float)(random.NextDouble()*10-5));
   var before=NavalStairTraffic.Apply(a,NavalBallastRoutes.Plan(a,b,old.Partitions,old.StairWidth,old.HoldFloor,old.Scale),old);var after=NavalShipRoutes.Plan(a,b,old);
   Check(before.Count==after.Count,"Old profiles keep route length");for(int k=0;k<before.Count;k++)Check(before[k].X==after[k].X&&before[k].Y==after[k].Y&&before[k].Z==after[k].Z,"Old profiles retain deck geometry with shared stair lanes");
  }
  Check(NavalHoldEscapeRules.InHold(1,.825f,2,ship.HoldFloor,ship.WeatherDeck),"Enclosed middle deck is part of evacuation interior");
  Check(!NavalHoldEscapeRules.InHold(1,ship.WeatherDeck,2,ship.HoldFloor,ship.WeatherDeck),"Weather deck is not enclosed hold");
  Check(NavalBoardingRules.EnemyOnDeck(true,false,false,1,ship.WeatherDeck+.8f,2,ship.WeatherDeck),"Upper-deck intruder can be detected");
  var grid=new NavalFloodGrid(12,24,5.75f,11.75f,ship.Scale,ship.RimY-ship.HoldFloor,true);grid.Add(grid.Index(-2,3),20);float water=grid.Total;
  var tanks=new[]{new NavalBallastTank(NavalBallastLayout.Capacity(ship.Scale),-2.45f,-3),new NavalBallastTank(NavalBallastLayout.Capacity(ship.Scale),-2.45f,3),new NavalBallastTank(NavalBallastLayout.Capacity(ship.Scale),2.45f,-3),new NavalBallastTank(NavalBallastLayout.Capacity(ship.Scale),2.45f,3)};
  float moved=NavalBallastTransfer.Move(grid,tanks,true,5);Near(moved,5,"Shared ballast pump works on galleon");Near(grid.Total+tanks[2].Total+tanks[3].Total,water,"Galleon ballast conserves mass");
  Check(NavalRigImpactMath.TouchesHull(0,ship.WeatherDeck*ship.Scale,0,0,0,0,ship.Scale*.5f,(ship.RimY+.3f)*ship.Scale,(ship.HoldFloor-.25f)*ship.Scale),"Rig contact includes raised weather deck");
  var stability=ship.CreateStability();Check(stability.Torque(new NavalMassMoments(),5,0)<0,"Shared stability restores dry galleon");
  Console.WriteLine("PASS: large galleon profile, matching headroom on all decks, three varied sails, 6 crew/6 distinct guns, safe ladder, upper/lower/partial-stair routes, 3000 old-route parity cases, upper evacuation/boarding and shared ballast/stability.");
 }
}
