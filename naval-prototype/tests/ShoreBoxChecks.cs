using System;
class ShoreBoxChecks
{
 static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
 static NavalShoreBox Box(float x,float y,float z,float hx,float hy,float hz,float yaw=0,float roll=0){double a=yaw*Math.PI/180,b=roll*Math.PI/180;return new NavalShoreBox{Center=new NavalBoxVector(x,y,z),Half=new NavalBoxVector(hx,hy,hz),Right=new NavalBoxVector((float)(Math.Cos(a)*Math.Cos(b)),(float)Math.Sin(b),(float)(-Math.Sin(a)*Math.Cos(b))),Up=new NavalBoxVector((float)(-Math.Cos(a)*Math.Sin(b)),(float)Math.Cos(b),(float)(Math.Sin(a)*Math.Sin(b))),Forward=new NavalBoxVector((float)Math.Sin(a),0,(float)Math.Cos(a))};}
 static void Main(){
 var land=Box(0,0,0,30,30,30);
 Check(NavalShoreBoxMath.Penetration(Box(0,0,0,6,4,12),land)>0,"Fully contained ship must collide even without mesh front faces");
 Check(NavalShoreBoxMath.Penetration(Box(50,0,0,6,4,12),land)==0,"Open sea remains clear");
 Check(NavalShoreBoxMath.Penetration(Box(0,40,0,6,4,12),land)==0,"Height separation permits passing above submerged terrain");
 foreach(float scale in new float[]{2,3,4})foreach(float roll in new float[]{0,25,90}){
 var rock=Box(0,0,0,3,8,3);var from=Box(45,0,0,3.2f*scale,4,6.25f*scale,0,roll);var to=Box(0,0,0,3.2f*scale,4,6.25f*scale,70,roll);
 Check(NavalShoreBoxMath.Penetration(from,rock)==0,"Approach starts clear");Check(NavalShoreBoxMath.Penetration(to,rock)>0,"Yaw and roll cannot hide island overlap");
 }
 var initial=Box(37,0,0,6,4,12);var turned=Box(37,0,0,6,4,12,90);
 Check(NavalShoreBoxMath.Penetration(initial,land)==0&&NavalShoreBoxMath.Penetration(turned,land)>0,"Rotation alone can enter shore and must be blocked");
 Console.WriteLine("PASS: solid island containment, all ship sizes, yaw, heel, rotation-only contact and submerged terrain height.");
 }
}
