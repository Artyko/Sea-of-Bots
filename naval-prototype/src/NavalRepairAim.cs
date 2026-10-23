using System;
// Inner shell face avoids aiming through the intact edge of the deck slab.
public static class NavalRepairAim
{
 public const float HandHeight=1.6f;
 public static float InnerSide(float x,float halfWidth,float scale)
 {return Math.Sign(x)*(halfWidth-.125f-.06f/scale);}
 public static float InnerEnd(float z,float scale)
 {return Math.Sign(z)*(BoatDimensions.HalfLength-.125f-.06f/scale);}
 public static bool AtSide(float x,float halfWidth){return Math.Abs(x)>halfWidth-.2f;}
 public static bool AtEnd(float z){return Math.Abs(z)>BoatDimensions.HalfLength-.2f;}
}
