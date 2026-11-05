public static class NavalGunSupportGeometry {
 public const float Bottom=-.55f;
 public static float Height(float axis){return axis-Bottom;}
 public static float Centre(float axis){return (axis+Bottom)*.5f;}
}
