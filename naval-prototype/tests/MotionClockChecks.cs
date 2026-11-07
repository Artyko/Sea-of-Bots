using System;
class MotionClockChecks{
 static void Main(){
  foreach(int fps in new[]{10,20,25,40,60,120}){float distance=0;for(int i=0;i<fps;i++)distance+=12f*NavalMotionClock.Elapsed(1f/fps);if(Math.Abs(distance-12f)>.001f)throw new Exception("Frame-rate dependent translation");}
  float mixed=0;foreach(float dt in new[]{.025f,.1f,.04f,.035f})mixed+=12f*NavalMotionClock.Elapsed(dt);
  if(Math.Abs(mixed-2.4f)>.001f||NavalMotionClock.Elapsed(2f)>.2f||NavalMotionClock.Elapsed(-1f)!=0f)throw new Exception("Mixed frames or pause guard");
  Console.WriteLine("PASS: equal displacement at 10–120 FPS, mixed frames and bounded long-stall recovery.");
 }
}
