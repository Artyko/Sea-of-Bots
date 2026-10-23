using System;
using System.Diagnostics;
class FloodPerfChecks {
 static void Main(){foreach(var ship in NavalShipDefinition.Choices){
 var g=new NavalFloodGrid(12,24,6,12,ship.Scale,5,ship.DeckCount>2);var floors=new float[g.Water.Length];
 for(int i=0;i<floors.Length;i++)floors[i]=g.X(i)*.17f+g.Z(i)*.09f;
 for(int i=0;i<100;i++)g.Spread(.016f,floors,.94f);
 var clock=Stopwatch.StartNew();for(int i=0;i<50000;i++)g.Spread(.016f,floors,.94f);clock.Stop();
 Console.WriteLine(ship.Name+" dry_ms="+clock.Elapsed.TotalMilliseconds.ToString("F2",System.Globalization.CultureInfo.InvariantCulture));
 for(int i=0;i<g.Water.Length;i++)g.Add(i,(i%7)*.05f);g.RightSill[20]=float.PositiveInfinity;g.ForwardSill[21]=2;
 clock.Restart();for(int i=0;i<4000;i++){if(i%50==0)g.Add(g.Index(-1,2),.1f);g.Spread(.016f,floors,.94f);}clock.Stop();
 double checksum=0;for(int i=0;i<g.Water.Length;i++)checksum+=g.Water[i]*(i+1);
 Console.WriteLine(ship.Name+" wet_ms="+clock.Elapsed.TotalMilliseconds.ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+" checksum="+checksum.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+" total="+g.Total.ToString("R",System.Globalization.CultureInfo.InvariantCulture));
 }}
}
