using System;
class CannonDepressionChecks {
 static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
 static void Main(){int cases=0;foreach(float distance in new float[]{20,30,45,60})foreach(float below in new float[]{5,8,10}){
 float pitch;Check(NavalBattleMath.FiringAngle(distance,-below,32,out pitch),"Lower ship has ballistic solution");
 if(pitch>=-25){Check(NavalGunneryMath.WithinTraverse(0,pitch),"Lower target within new depression arc");cases++;}
 double time=NavalGunneryMath.FlightTime(distance,pitch);double actual=32*Math.Sin(pitch*Math.PI/180)*time-4.905*time*time;
 Check(Math.Abs(actual+below)<.001,"Downward arc reaches target height");}
 Check(NavalGunneryMath.WithinTraverse(30,-25)&&!NavalGunneryMath.WithinTraverse(31,-25)&&!NavalGunneryMath.WithinTraverse(0,-26),"Yaw and depression remain bounded");
 foreach(var ship in NavalShipDefinition.Choices){float lip=ship.GunPortSill;Check(lip>ship.GunDeck,"Port retains a lip above deck");Check(lip<ship.GunMountY+.35f*ship.GunFixtureScale,"Port lip remains below bore centre");}
 Console.WriteLine("PASS: "+cases+" low-target firing cases, ballistic height, bounded traverse and lowered port lips on all ships.");}
}
