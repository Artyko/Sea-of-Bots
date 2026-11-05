// Physical room boundaries must remain outside the complete descending stair volume.
public static class NavalDestroyerAccess {
 public static readonly int[] BulkheadRows={4,11,14,18};
 public const float PumpX=-3.6f,PumpZ=-32f;
 public static float BulkheadZ(int row){return -51.2f+(row+1)*102.4f/24f;}
 public static bool OutsideStairs(float z){return System.Math.Abs(z-20.5f)>4.6f&&System.Math.Abs(z+20.5f)>4.6f;}
}
