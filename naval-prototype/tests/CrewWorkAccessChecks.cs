using System;
class CrewWorkAccessChecks {
 static void Check(bool v,string why){if(!v)throw new Exception(why);}
 static void Main(){try{foreach(var ship in NavalShipDefinition.Choices){
 float radius=.8f,lane=NavalStairTraffic.Lane(ship.Scale,ship.StairWidth);
 Check((ship.StairWidth*.5f-lane)*ship.Scale>=radius+.049f,"Directional stair lane contains actual capsule "+ship.Name);
 Check(NavalSailWorkLayout.CleatOffset-.3f>radius,"Sail cleat crossbar clears sailor");
 Check(NavalSailWorkLayout.CleatOffset<=NavalSailWorkLayout.Reach,"Moved sail cleat remains in work reach");
 Check(.2f*ship.Scale+.65f>radius+.1f,"Pump approach clears pump front face");
 for(int i=0;i<ship.CrewCount;i++){
 float x=Math.Abs(NavalShipDefinition.StandbyX(ship,i));
 Check((NavalBallastLayout.X-NavalBallastLayout.Width*.5f-x)*ship.Scale>radius,"Standby clear of ballast wall");
 if(ship.Scale<=2)Check(.95f*ship.Scale-.675f-x*ship.Scale>radius,"Sloop standby clear of central stocks");
 }
 }
 Console.WriteLine("PASS: full-size capsule clearances at standby, directional stairs, sail cleat and pump approach for all three ships; native audit checks actual equipment colliders after spawn.");
 }catch(Exception e){Console.WriteLine(e);Environment.ExitCode=1;}}
}
