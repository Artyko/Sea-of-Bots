using System;
class FloodDragChecks{
 static void Main(){if(NavalFloodDragMath.SpeedRetention(1000,0,1)!=1)throw new Exception("Dry speed");float partial=NavalFloodDragMath.SpeedRetention(1000,500,1);if(partial<.85f||partial>=1)throw new Exception("Partial flooding must retain substantial speed when propulsion is intact");if(Math.Abs(partial-NavalFloodDragMath.SpeedRetention(2000,1000,1))>.0001)throw new Exception("Consistent displacement units");if(NavalFloodDragMath.SpeedRetention(1000,500,.5f)>=partial)throw new Exception("Detached displacement accounted");Console.WriteLine("PASS: dry speed, partial flood drag, absolute displacement consistency and detached hull loss.");}
}
