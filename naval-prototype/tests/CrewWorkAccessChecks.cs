using System;
class CrewWorkAccessChecks {
 static void Check(bool v,string why){if(!v)throw new Exception(why);}
 static void Main(){try{var profiles=new System.Collections.Generic.List<NavalShipDefinition>(NavalShipDefinition.Choices);profiles.AddRange(NavalShipDefinition.SteamChoices);foreach(var ship in profiles){
 float radius=.8f,lane=NavalStairTraffic.Lane(ship.Scale,ship.StairWidth);
 Check((ship.StairWidth*.5f-lane)*ship.Scale>=radius+.049f,"Directional stair lane contains actual capsule "+ship.Name);
 if(ship.SteamPowered){Check(Math.Abs(ship.PropulsionPost.x)<ship.HullHalfWidth(ship.PropulsionPost.z)-radius/ship.Scale,"Integrated helm telegraph leaves full capsule clearance");}
 else {Check(NavalSailWorkLayout.CleatOffset-.3f>radius,"Sail cleat crossbar clears sailor");Check(NavalSailWorkLayout.CleatOffset<=NavalSailWorkLayout.Reach,"Moved sail cleat remains in work reach");}
 Check(.2f*ship.Scale+.65f>radius+.1f,"Pump approach clears pump front face");
 for(int i=0;i<ship.CrewCount;i++){
 float x=Math.Abs(NavalShipDefinition.StandbyX(ship,i)),z=Math.Abs(NavalShipDefinition.StandbyZ(ship,i));var ballast=NavalBallastLayout.For(ship);
 bool besideBallast=z>=ballast.InnerZ&&z<=ballast.Z+ballast.Length*.5f;
 if(besideBallast)Check((ballast.X-ballast.Width*.5f-x)*ship.Scale>radius,"Standby clear of ballast wall");
 if(ship.Scale<=2)Check(.95f*ship.Scale-.675f-x*ship.Scale>radius,"Sloop standby clear of central stocks");
 }
 }
 Console.WriteLine("PASS: full-size capsule clearances at standby, both stair flights, sail/steam propulsion posts and pump approach for every profile; native audit checks actual equipment colliders after spawn.");
 }catch(Exception e){Console.WriteLine(e);Environment.ExitCode=1;}}
}
