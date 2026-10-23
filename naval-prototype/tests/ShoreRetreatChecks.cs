using System;
class ShoreRetreatChecks {
 static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
 static NavalShoreBox Box(float x,float z,float yaw,float hx,float hz){float a=yaw*(float)Math.PI/180;
 return new NavalShoreBox{Center=new NavalBoxVector(x,0,z),Half=new NavalBoxVector(hx,2,hz),Right=new NavalBoxVector((float)Math.Cos(a),0,-(float)Math.Sin(a)),Up=new NavalBoxVector(0,1,0),Forward=new NavalBoxVector((float)Math.Sin(a),0,(float)Math.Cos(a))};}
 static bool Allowed(NavalShoreBox old,NavalShoreBox next,NavalShoreBox rock){var p=new NavalShoreEscape();p.Observe(NavalShoreBoxMath.Penetration(old,rock),NavalShoreBoxMath.Penetration(next,rock));return p.Allowed;}
 static void Main(){var rock=Box(0,6,0,2,2);var ship=Box(0,0,0,3,6);
 Check(!Allowed(ship,Box(0,-.2f,10,3,6),rock),"Bow rotation deepens shore contact despite reverse translation");
 Check(Allowed(ship,Box(0,-.2f,0,3,6),rock),"Separately checked straight retreat reduces shore penetration");
 Check(!Allowed(ship,Box(0,.2f,0,3,6),rock),"Fallback must not admit forward motion into island");
 for(int i=1;i<=20;i++){var next=Box(0,-i*.2f,0,3,6);Check(Allowed(ship,next,rock),"Every straight retreat sample stays non-worsening");}
 foreach(float scale in new float[]{2,3,4})Check(NavalHelmRecovery.StraightReverseDistance(scale)>=6.5f&&NavalHelmRecovery.StraightReverseDistance(scale)<NavalHelmRecovery.ReverseDistance(scale),"Straight reverse distance scales with hull and precedes turning");
 Console.WriteLine("PASS: blocked yaw cannot veto safe retreat; forward penetration rejected; all reverse samples safe; scaled straight-first recovery.");}
}
