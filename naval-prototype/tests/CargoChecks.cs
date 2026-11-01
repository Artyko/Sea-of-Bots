using System;
class CargoChecks {
 static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
 static void Main(){try{Run();}catch(Exception e){Console.WriteLine(e);Environment.ExitCode=1;}}
 static void Run(){int routes=0;var profiles=new System.Collections.Generic.List<NavalShipDefinition>(NavalShipDefinition.Choices);profiles.AddRange(NavalShipDefinition.SteamChoices);foreach(var ship in profiles){
 int f=NavalCargoLayout.Factor(ship);Check(NavalCargoLayout.Boxes(ship,0)*20==80*f,"Hold ammunition totals");Check(NavalCargoLayout.Boxes(ship,1)*40==160*f,"Board totals");Check(NavalCargoLayout.Boxes(ship,2)==ship.GunCount,"One parts crate per gun");
 int total=NavalCargoLayout.Boxes(ship,0)+NavalCargoLayout.Boxes(ship,1)+NavalCargoLayout.Boxes(ship,2);
 if(ship.SteamPowered){var lastA=NavalCargoLayout.Position(ship,total-2);var lastB=NavalCargoLayout.Position(ship,total-1);Check(lastA.Y==ship.HoldFloor&&lastB.Y==ship.HoldFloor&&lastA.X*lastB.X<0,"Steam spare-gun kits share the lower deck without blocking the middle-deck bow repair pocket");}
 if(ship.SteamPowered){var escape=NavalSupplyRoutes.Plan(new NavalWalkPoint(1.43f,ship.HoldFloor,2.22f),new NavalWalkPoint(.64f,ship.HoldFloor,1.81f),ship);Check(escape.Count>1&&escape[0].X>1.5f,"Sailor displaced into a cargo clearance envelope first escapes its nearest face, first="+escape[0].X+","+escape[0].Y+","+escape[0].Z+" count="+escape.Count);
 var lidEscape=NavalSupplyRoutes.Plan(new NavalWalkPoint(-.76f,1.12f,2.00f),new NavalWalkPoint(-1.10f,ship.HoldFloor,4.05f),ship);Check(lidEscape.Count>1&&lidEscape[0].X>-.60f,"Sailor standing on the logged middle-deck rack steps beyond its physical lid before resuming the plank route: first="+lidEscape[0].X+","+lidEscape[0].Y+","+lidEscape[0].Z);}
 for(int n=0;n<total;n++){
 var p=NavalCargoLayout.Position(ship,n);Check(Math.Abs(p.X)+.675f/ship.Scale<ship.HullHalfWidth(p.Z)&&Math.Abs(p.Z)+.675f/ship.Scale<6,"Crates stay inside hull");
 var pickup=NavalCargoLayout.Pickup(ship,p);
 if(ship.SteamPowered)Check(NavalCrewDeckSafety.Inside(pickup.X,pickup.Z,ship,.8f),"Steam pickup retains full capsule clearance inside tapered hull");
 double pickupDistance=Math.Sqrt((pickup.X-p.X)*(pickup.X-p.X)+(pickup.Z-p.Z)*(pickup.Z-p.Z))*ship.Scale;
 Check(pickupDistance>.675f+.8f+.1f,"Actual 0.8m sailor radius clears crate at pickup");
 if(Math.Abs(p.Z)>NavalBallastLayout.InnerZ&&Math.Abs(p.Z)<5.2f)Check(Math.Abs(pickup.X)*ship.Scale+.8f<(NavalBallastLayout.X-NavalBallastLayout.Width*.5f)*ship.Scale,"Full-size pickup capsule fits before ballast wall");
 for(int gunIndex=0;gunIndex<ship.GunCount;gunIndex++)for(int direction=0;direction<2;direction++){
 var gunPost=new NavalWalkPoint(NavalShipDefinition.GunSide(gunIndex)*ship.GunPostX,ship.GunDeck,NavalShipDefinition.GunZ(ship,gunIndex));
 var start=direction==0?gunPost:pickup;var destination=direction==0?pickup:gunPost;
 var route=NavalSupplyRoutes.Plan(start,destination,ship);Check(route.Count>0,"Supply accessible by shared routes");
 var prev=start;foreach(var q in route){Check(Math.Abs(q.Y-prev.Y)*ship.Scale<.65f,"No deck teleport "+ship.Name+" crate="+n+" p="+p.X+","+p.Y+","+p.Z+" prev="+prev.X+","+prev.Y+","+prev.Z+" next="+q.X+","+q.Y+","+q.Z);
 if(ship.SteamPowered&&(q.X!=destination.X||q.Y!=destination.Y||q.Z!=destination.Z))Check(NavalCrewDeckSafety.Inside(q.X,q.Z,ship,.85f),"Steam supply detour clears tapered bow with full capsule");
 for(int j=0;j<total;j++){var c=NavalCargoLayout.Position(ship,j);if(q.Y<c.Y-.1f||q.Y>c.Y+1.8f/ship.Scale||prev.Y<c.Y-.1f||prev.Y>c.Y+1.8f/ship.Scale)continue;
 float half=NavalCargoLayout.RouteHalfExtent(ship);Check(!NavalSupplyRoutes.Hits(prev,q,c.X-half,c.X+half,c.Z-half,c.Z+half),"Cargo hit "+ship.Name+" n="+n+" obstacle="+j+" from="+prev.X+","+prev.Z+" to="+q.X+","+q.Z);}
 prev=q;}routes++;
 }
 }
 }
 var a=new NavalCargoStock(20);Check(a.Take(5)==5&&a.Count==15,"Fetch debits immediately");var gun=new NavalCargoStock(20);Check(gun.Take(20)==20&&gun.Count==0,"Twenty actual shots");Check(gun.Put(a.Take(5))==5&&a.Count==10,"Transfer conserves stock");a.Break();Check(a.Count==0&&a.Take(5)==0&&a.Put(5)==0,"Destroyed cargo cannot issue or receive");a.Reset();Check(a.Count==20&&!a.Destroyed,"Explicit restoration resets stock");
 Console.WriteLine("PASS: stock totals, finite transfer, destruction, reset; "+routes+" supply routes with full-body crate clearance.");
 }
}
