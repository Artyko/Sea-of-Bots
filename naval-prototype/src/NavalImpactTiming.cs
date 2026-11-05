using System;
using System.Diagnostics;
public struct NavalImpactTiming:IDisposable {
 private long start;private string label;
 public static NavalImpactTiming Measure(string name){return new NavalImpactTiming{start=Stopwatch.GetTimestamp(),label=name};}
 public void Dispose(){double ms=(Stopwatch.GetTimestamp()-start)*1000.0/Stopwatch.Frequency;
  if(ms>15)UnityEngine.Debug.Log("[NavalPrototype] Slow impact stage: "+label+" ms="+ms.ToString("F2"));}
}
