using System;
namespace UnityEngine {
 public struct Vector3 {public float x,y,z;public Vector3(float a,float b,float c){x=a;y=b;z=c;}public static Vector3 operator +(Vector3 a,Vector3 b){return new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);}public static Vector3 operator -(Vector3 a,Vector3 b){return new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);}public float Length(){return (float)Math.Sqrt(x*x+y*y+z*z);}}
 public class Transform {
  public float Scale,Roll;public Vector3 Translation;
  public Vector3 TransformPoint(Vector3 p){float c=(float)Math.Cos(Roll),s=(float)Math.Sin(Roll);return Translation+new Vector3((p.x*c-p.y*s)*Scale,(p.x*s+p.y*c)*Scale,p.z*Scale);}
  public Vector3 InverseTransformPoint(Vector3 p){p=p-Translation;float c=(float)Math.Cos(Roll),s=(float)Math.Sin(Roll);return new Vector3((p.x*c+p.y*s)/Scale,(-p.x*s+p.y*c)/Scale,p.z/Scale);}
 }
}
class RiderCarryChecks {
 static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
 static void Main(){try{int cases=0;foreach(float scale in new float[]{2,3,4})foreach(float deck in new float[]{-.8f,.825f,2.55f})foreach(float height in new float[]{.3f,1.2f,2.3f}){
 var ship=new UnityEngine.Transform{Scale=scale};var foot=new UnityEngine.Vector3(1.4f,deck,2);var offset=new UnityEngine.Vector3(.1f,height,-.1f);var root=ship.TransformPoint(foot)+offset;
 for(int frame=0;frame<300;frame++){
  var local=ship.InverseTransformPoint(root-offset);ship.Roll=(float)Math.Sin(frame*.06f)*.35f;ship.Translation=new UnityEngine.Vector3(frame*.1f,(float)Math.Sin(frame*.1f)*.3f,frame*.07f);
  root=NavalRiderCarry.Position(ship,local,offset);
  Check((ship.InverseTransformPoint(root-offset)-foot).Length()<.0008f,"Foot anchor drift across moving/rolling deck");cases++;
 }
 }
 var tilt=new UnityEngine.Transform{Scale=4,Roll=.3f};var tall=new UnityEngine.Vector3(0,2,0);var wrong=tilt.TransformPoint(new UnityEngine.Vector3(0,.5f,0))-tall;Check(wrong.Length()>.5f,"Old root rotation reproduces large foot displacement");
 Console.WriteLine("PASS: "+cases+" moving/rolling foot carry steps at all ship scales and deck/root heights; old root-based carry reproduces lateral drift. Transform math mocked, not native gameplay.");
 }catch(Exception e){Console.WriteLine(e);Environment.ExitCode=1;}}
}
