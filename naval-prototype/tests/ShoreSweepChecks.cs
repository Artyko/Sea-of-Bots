using System;
class ShoreSweepChecks
{
 static void Check(bool value){if(!value)throw new Exception("Unsafe shore sweep spacing");}
 static void Main(){
 Check(NavalShoreSweepMath.Steps(0,0,20)==1);
 foreach(float radius in new float[]{14,21,28})foreach(float distance in new float[]{0,.03f,1,8})foreach(float turn in new float[]{0,.5f,10,90}){
 int steps=NavalShoreSweepMath.Steps(distance,turn,radius);
 Check((distance+turn*Math.PI/180*radius)/steps<=.100001);
 }
 Check(NavalShoreSweepMath.Steps(0,90,28)>400);
 Console.WriteLine("PASS: shore sweep corner spacing for translation, rotation and all ship sizes.");
 }
}
