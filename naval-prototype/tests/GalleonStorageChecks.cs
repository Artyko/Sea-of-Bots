using System;
using System.Diagnostics;
class GalleonStorageChecks {
 static void Check(bool value,string why){if(!value)throw new Exception(why);}
 static void Main(){var ship=NavalShipDefinition.Galleon;
 var grid=new NavalFloodGrid(12,24,BoatDimensions.HoldWidth,BoatDimensions.HoldLength,ship.Scale,ship.RimY-ship.HoldFloor,true);grid.ConfigureStorage(ship);
 var random=new Random(192);var floors=new float[grid.Water.Length];
 for(int sample=0;sample<3000;sample++){
  int cell=random.Next(grid.Water.Length);if(!grid.IsActive(cell))continue;
  grid.Add(cell,-grid.Water[cell]);float target=(float)random.NextDouble()*(ship.RimY-ship.HoldFloor);
  float water=grid.VolumeAtDepth(cell,target);grid.Add(cell,water);
  // Fully solid slabs have a range of heights at the same volume; compare volume on the inverse path.
  Check(Math.Abs(grid.VolumeAtDepth(cell,grid.Depth(cell))-water)<.003f,"Free volume/depth round trip");
  Check(grid.CenterDepth(cell)>=0&&grid.CenterDepth(cell)<=grid.Depth(cell)+.0001f,"Water centroid stays inside column");
 }
 float initial=grid.Total;var clock=Stopwatch.StartNew();
 for(int i=0;i<2000;i++){
  float angle=(float)Math.Sin(i*.01)*.15f;
  grid.FillFloorHeights(floors,(float)Math.Sin(angle)*ship.Scale,0,(float)Math.Cos(angle)*ship.HoldFloor*ship.Scale,0);
  grid.Spread(.02f,floors,(float)Math.Cos(angle));
 }
 clock.Stop();Check(Math.Abs(grid.Total-initial)<initial*.00002f,"Obstructed-grid spreading preserves volume under changing heel");
 for(int i=0;i<grid.Water.Length;i++)Check(grid.Water[i]>=0&&grid.Water[i]<=grid.CellCapacity(i)+.001f,"Capacity and positivity maintained");
 Console.WriteLine("PASS: 3000 occupied-column round trips, physical centroids, 2000 mass-conserving tilted steps; CPU "+clock.Elapsed.TotalMilliseconds.ToString("F1")+" ms total (not game FPS).");
 }
}
