using System;
// Shared by player swimming and all scripted sailor movement paths.
public static class NavalSwimMovement
{
 public static bool InWater(float depth,float bodyHeight,bool swimming)
 {return depth>Math.Max(1f,bodyHeight)*(swimming?.42f:.50f);}
 public static float SpeedLimit(float walkingSpeed)
 {return Math.Min(2.8f,Math.Max(.1f,walkingSpeed*.65f));}
 public static float Speed(float walkingSpeed,bool swimming)
 {return swimming?Math.Min(walkingSpeed,SpeedLimit(walkingSpeed)):walkingSpeed;}
}
