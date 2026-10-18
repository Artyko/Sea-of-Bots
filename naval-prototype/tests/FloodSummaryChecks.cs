using System;
using System.Diagnostics;
class FloodSummaryChecks
{
 static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
 static void Same(NavalFloodGrid a,ReferenceFloodGridV111 b)
 {
  Check(a.Total==b.Total&&a.Side(true)==b.Side(true)&&a.Side(false)==b.Side(false)&&a.LongitudinalMoment==b.LongitudinalMoment,"Exact summary parity after every writer");
  for(int i=0;i<a.Water.Length;i++)Check(a.Water[i]==b.Water[i],"Every water cell remains exactly unchanged");
 }
 static double Consume;
 static void Main()
 {
  var random=new Random(112);
  foreach(int columns in new[]{5,12})foreach(float scale in new[]{2f,3f})
  {
   var a=new NavalFloodGrid(columns,24,5.75f,11.75f,scale,3);var b=new ReferenceFloodGridV111(columns,24,5.75f,11.75f,scale,3);var floors=new float[a.Water.Length];
   Same(a,b);
   for(int step=0;step<3000;step++)
   {
    int index=random.Next(a.Water.Length);
    if(step%9==0)
    {
     for(int i=0;i<floors.Length;i++){floors[i]=(float)(random.NextDouble()*.4)+a.X(i)*.2f;float sill=i%17==0?float.PositiveInfinity:i%11==0?.2f:float.NegativeInfinity;a.RightSill[i]=b.RightSill[i]=sill;}
     float dt=step%3==0?.05f:.016f;a.Spread(dt,floors,.7f);b.Spread(dt,floors,.7f);
    }
    else if(step%7==0){float amount=(float)random.NextDouble()*2;a.Pump(amount);b.Pump(amount);}
    else {float amount=(float)(random.NextDouble()*8-3);Check(a.Add(index,amount)==b.Add(index,amount),"Bounded insertion/removal unchanged");}
    Same(a,b);Check(a.Total==b.Total,"Repeated read uses the same current result");
   }
  }
  Check(typeof(NavalFloodCells).GetProperty("Item").GetSetMethod()==null,"External writes cannot bypass invalidation");
  var current=new NavalFloodGrid(12,24,5.75f,11.75f,3,3);var old=new ReferenceFloodGridV111(12,24,5.75f,11.75f,3,3);
  for(int i=0;i<288;i++){current.Add(i,.2f);old.Add(i,.2f);}Same(current,old);
  var watch=Stopwatch.StartNew();for(int i=0;i<100000;i++){Consume+=old.Total+old.Side(true)+old.Side(false)+old.LongitudinalMoment;}watch.Stop();double oldMs=watch.Elapsed.TotalMilliseconds;
  watch.Restart();for(int i=0;i<100000;i++){Consume+=current.Total+current.Side(true)+current.Side(false)+current.LongitudinalMoment;}watch.Stop();
  Console.WriteLine("PASS: 12000 mixed mutations with exact cell/summary parity, odd/even columns, scales, sills, pumping, spreading and read-only writes. 100000 unchanged summary sets: "+oldMs.ToString("F1")+" -> "+watch.Elapsed.TotalMilliseconds.ToString("F1")+" ms (not Unity FPS).");
 }
}
