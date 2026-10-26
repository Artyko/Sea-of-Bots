using System;
class LaptopPerformanceChecks
{
 static void Check(bool value,string why){if(!value)throw new Exception(why);}
 static void Main()
 {
  var standard=new NavalSailPoseClock();int standardWrites=0;
  for(int frame=0;frame<100;frame++)if(standard.Due(frame/10f,1,false,frame,1))standardWrites++;
  var laptop=new NavalSailPoseClock();int laptopWrites=0;
  for(int frame=0;frame<100;frame++)if(laptop.Due(frame/10f,1,false,frame,2))laptopWrites++;
  Check(standardWrites==100,"Standard low-FPS path keeps every available pose frame");
  Check(laptopWrites==50,"Laptop path uploads the cloth pose every other rendered frame");
  Check(laptop.Due(10f,.5f,false,100,2),"A sail-control change bypasses the cadence limit");
  Console.WriteLine("PASS: standard/laptop pose uploads 100/50; control changes remain immediate.");
 }
}
