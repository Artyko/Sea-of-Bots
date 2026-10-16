using System;
using System.Diagnostics;
class WaveFieldChecks
{
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 static void Main(){
 const int count=101;var field=new NavalWaveField(count);float maximum=0f;
 foreach(float start in new[]{-150f,4090f,-8500f})foreach(float time in new[]{0f,12.5f,1000f})foreach(float amplitude in new[]{0f,.2f,.65f}){
  field.Prepare(start,start+17f,3f,amplitude,time);
  for(int z=0;z<count;z++)for(int x=0;x<count;x++){
   float expected=NavalWaveField.Sample(start+x*3f,start+17f+z*3f,amplitude,time);
   maximum=Math.Max(maximum,Math.Abs(expected-field.At(x,z)));
  }
 }
 Check(maximum<.00001f,"Rendered and physics wave heights agree");
 double sink=0;var watch=new Stopwatch();const int frames=120;
 // Warm both paths before timing. This measures sampling only, not mesh clipping or game FPS.
 for(int f=0;f<5;f++){field.Prepare(-150f,-150f,3f,.65f,f);for(int z=0;z<count;z++)for(int x=0;x<count;x++)sink+=field.At(x,z);}
 watch.Start();for(int f=0;f<frames;f++)for(int z=0;z<count;z++)for(int x=0;x<count;x++)sink+=NavalWaveField.Sample(-150f+x*3f,-150f+z*3f,.65f,f*.02f);
 watch.Stop();double analytic=watch.Elapsed.TotalMilliseconds;
 watch.Restart();for(int f=0;f<frames;f++){field.Prepare(-150f,-150f,3f,.65f,f*.02f);for(int z=0;z<count;z++)for(int x=0;x<count;x++)sink+=field.At(x,z);}
 watch.Stop();double cached=watch.Elapsed.TotalMilliseconds;
 Console.WriteLine("PASS: wave/physics agreement, maximum error="+maximum.ToString("G4"));
 Console.WriteLine("Sampling 120 grids: analytic="+analytic.ToString("F1")+"ms; cached="+cached.ToString("F1")+"ms; ratio="+(analytic/cached).ToString("F2")+"x; checksum="+sink.ToString("F2"));
 }
}
