using System;
class CargoChecks {
 static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
 static void Main(){try{Run();}catch(Exception e){Console.WriteLine(e);Environment.ExitCode=1;}}
 static void Run(){int routes=0;foreach(var ship in NavalShipDefinition.Choices){
 int f=NavalCargoLayout.Factor(ship);Check(NavalCargoLayout.Boxes(ship,0)*20==80*f,"Hold ammunition totals");Check(NavalCargoLayout.Boxes(ship,1)*40==160*f,"Board totals");Check(NavalCargoLayout.Boxes(ship,2)==ship.GunCount,"One parts crate per gun");
 int total=NavalCargoLayout.Boxes(ship,0)+NavalCargoLayout.Boxes(ship,1)+NavalCargoLayout.Boxes(ship,2);
 for(int n=0;n<total;n++){
 var p=NavalCargoLayout.Position(ship,n);Check(Math.Abs(p.X)+.675f/ship.Scale<ship.HullHalfWidth(p.Z)&&Math.Abs(p.Z)+.675f/ship.Scale<6,"Crates stay inside hull");
 var pickup=NavalCargoLayout.Pickup(ship,p);
 Check(Math.Abs(pickup.X-p.X)*ship.Scale>.675f+.8f+.1f,"Actual 0.8m sailor radius clears crate at pickup");
 if(Math.Abs(p.Z)>NavalBallastLayout.InnerZ&&Math.Abs(p.Z)<5.2f)Check(Math.Abs(pickup.X)*ship.Scale+.8f<(NavalBallastLayout.X-NavalBallastLayout.Width*.5f)*ship.Scale,"Full-size pickup capsule fits before ballast wall");
 for(int gunIndex=0;gunIndex<ship.GunCount;gunIndex++)for(int direction=0;direction<2;direction++){
 var gunPost=new NavalWalkPoint(NavalShipDefinition.GunSide(gunIndex)*ship.GunPostX,ship.GunDeck,NavalShipDefinition.GunZ(ship,gunIndex));
 var start=direction==0?gunPost:pickup;var destination=direction==0?pickup:gunPost;
 var route=NavalSupplyRoutes.Plan(start,destination,ship);Check(route.Count>0,"Supply accessible by shared routes");
 var prev=start;foreach(var q in route){Check(Math.Abs(q.Y-prev.Y)*ship.Scale<.65f,"No deck teleport "+ship.Name+" crate="+n+" p="+p.X+","+p.Y+","+p.Z+" prev="+prev.X+","+prev.Y+","+prev.Z+" next="+q.X+","+q.Y+","+q.Z);
 for(int j=0;j<total;j++){var c=NavalCargoLayout.Position(ship,j);if(q.Y<c.Y-.1f||q.Y>c.Y+1.8f/ship.Scale||prev.Y<c.Y-.1f||prev.Y>c.Y+1.8f/ship.Scale)continue;
 float half=1.125f/ship.Scale;Check(!NavalSupplyRoutes.Hits(prev,q,c.X-half,c.X+half,c.Z-half,c.Z+half),"Cargo hit "+ship.Name+" n="+n+" obstacle="+j+" from="+prev.X+","+prev.Z+" to="+q.X+","+q.Z);}
 prev=q;}routes++;
 }
 }
 }
 var a=new NavalCargoStock(20);Check(a.Take(5)==5&&a.Count==15,"Fetch debits immediately");var gun=new NavalCargoStock(20);Check(gun.Take(20)==20&&gun.Count==0,"Twenty actual shots");Check(gun.Put(a.Take(5))==5&&a.Count==10,"Transfer conserves stock");a.Break();Check(a.Count==0&&a.Take(5)==0&&a.Put(5)==0,"Destroyed cargo cannot issue or receive");a.Reset();Check(a.Count==20&&!a.Destroyed,"Explicit restoration resets stock");
 Console.WriteLine("PASS: stock totals, finite transfer, destruction, reset; "+routes+" supply routes with full-body crate clearance.");
 }
}
